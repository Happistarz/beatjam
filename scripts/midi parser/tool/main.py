# flake8: noqa
"""Convertisseur MIDI vers BeatJam (.beat)."""
import json
import os
import sys
from collections import defaultdict
from mido import MidiFile

# Import local utils
try:
    from midi_utils import note_name_to_number
except ImportError:
    # Fallback si lancé depuis un autre dossier sans package
    sys.path.append(os.path.dirname(os.path.abspath(__file__)))
    from midi_utils import note_name_to_number

DEFAULT_CONFIG_FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "config.json")

def load_config(path):
    if not os.path.exists(path):
        print(f"Erreur: Fichier de config {path} introuvable.")
        sys.exit(1)
    with open(path, 'r', encoding='utf-8') as f:
        return json.load(f)

def get_midi_note_type(note_number, low_threshold_str, high_threshold_str):
    """Détermine si une note est L, M ou H selon les seuils."""
    low = note_name_to_number(low_threshold_str)
    high = note_name_to_number(high_threshold_str)
    
    if note_number < low:
        return 'L'
    elif note_number > high:
        return 'H'
    else:
        return 'M'

def scan_tracks(mid):
    """Affiche la liste des pistes du fichier MIDI pour aider à la config."""
    print("\n--- ANALYSE DU FICHIER MIDI ---")
    print(f"Resolution: {mid.ticks_per_beat} PPQ")

    # Detection BPM (Tempo)
    detected_bpm = 120 # Default
    for track in mid.tracks:
        for msg in track:
            if msg.type == 'set_tempo':
                from mido import tempo2bpm
                detected_bpm = tempo2bpm(msg.tempo)
                print(f"BPM détecté: {detected_bpm:.2f}")
                break
        if detected_bpm != 120: break
    
    for i, track in enumerate(mid.tracks):
        track_name = "<Sans Nom>"
        note_count = 0
        min_note = 128
        max_note = -1
        
        for msg in track:
            if msg.type == 'track_name':
                track_name = msg.name
            if msg.type == 'note_on' and msg.velocity > 0:
                note_count += 1
                min_note = min(min_note, msg.note)
                max_note = max(max_note, msg.note)
        
        if note_count > 0:
            print(f"[{i}] '{track_name}': {note_count} notes (Range: {min_note}-{max_note})")
        else:
            print(f"[{i}] '{track_name}': (Vide ou Meta)")
    print("-------------------------------\n")

def process_track(track, track_name, beat_data, mapping_rules, mid_ticks_per_beat):
    """Traite une piste MIDI unique et remplit beat_data."""
    # Trouver TOUTES les règles qui correspondent à ce nom de piste
    applicable_rules = []
    for rule in mapping_rules:
        keyword = rule.get("track_name_contains", "").lower()
        if keyword and keyword in track_name.lower():
            applicable_rules.append(rule)
            
    if not applicable_rules:
        return

    print(f"  -> Traitement '{track_name}': {len(applicable_rules)} règles applicables trouvées.")

    current_ticks = 0
    ticks_per_sixteenth = mid_ticks_per_beat / 4.0
    ticks_per_measure = mid_ticks_per_beat * 4.0

    for msg in track:
        current_ticks += msg.time
        
        if msg.type == 'note_on' and msg.velocity > 0:
            current_measure = current_ticks / ticks_per_measure
            
            # Trouver TOUTES les règles actives pour cette mesure (plus de break)
            active_rules = []
            for rule in applicable_rules:
                start_m = rule.get("start_measure", 0)
                end_m = rule.get("end_measure", 99999)
                
                # Note: 'measures' in config are usually 1-based for users
                # But math might be 0-based. Let's assume 1-based in config, 0-based in match?
                # Usually config says "start_measure: 1". current_measure starts at 0.
                if start_m <= (current_measure + 1) < end_m:
                    active_rules.append(rule)
            
            if not active_rules:
                continue
                
            # Pour chaque règle active, on génère une note
            for rule in active_rules:
                p_idx = rule.get("player_index", 0)
                min_vel = rule.get("min_velocity", 0)
                
                if msg.velocity < min_vel:
                    continue
                    
                mapping = rule.get("mapping", {})
                if not mapping: continue
                
                note_type = get_midi_note_type(msg.note, mapping["low"], mapping["high"])
                
                # Calcul du slot temporel (16th note)
                # On arrondit au 16eme le plus proche
                total_sixteenths = round(current_ticks / ticks_per_sixteenth)
                
                beat_data[total_sixteenths][p_idx].add(note_type) 

def generate_output_content(beat_data, players_config):
    lines = []
    # Headers
    # Players X
    lines.append(f"Players {len(players_config)}")
    for p in players_config:
        # Format: -id:Name:Role
        # Ex: -player1:Guitar Boss:Guitar
        # Mais le format actuel attendu par TracksLoader semble être -id:Name:Role
        # Dans track1.beat on a: -player2:Player 2:Guitar
        # On va générer des IDs basés sur l'index ex: player0, player1...
        pid = f"player{p['index'] + 1}" 
        name = p.get('name', f"Player {p['index'] + 1}")
        role = p.get('role', 'Guitar')
        lines.append(f"-{pid}:{name}:{role}")

    lines.append(f"Beats {len(beat_data)}")
    lines.append("; measure beat sixteenth player_index:notes,...")

    sorted_times = sorted(beat_data.keys())
    
    for t in sorted_times:
        measure = t // 16
        remainder = t % 16
        beat = remainder // 4
        sixteenth = remainder % 4
        
        parts = []
        players_in_step = sorted(beat_data[t].keys())
        
        for p_idx in players_in_step:
            # Combiner les notes (ex: HL si on a High et Low en même temps)
            # Trie l'ordre des lettres pour être cohérent (H, L, M -> HLM)
            # On peut définir un ordre de priorité si on veut H > M > L
            raw_types = beat_data[t][p_idx]
            
            # Petit ordre custom pour le rendu string: H, L, M
            sorted_types = ""
            if 'H' in raw_types: sorted_types += "H"
            if 'L' in raw_types: sorted_types += "L"
            if 'M' in raw_types: sorted_types += "M"
            
            if sorted_types:
                parts.append(f"{p_idx}:{sorted_types}")
        
        if parts:
            line_str = f"-{measure} {beat} {sixteenth} " + ",".join(parts)
            lines.append(line_str)
            
    return lines

def main():
    if len(sys.argv) > 1:
        config_path = sys.argv[1]
    else:
        print("Usage: python main.py <path_to_config.json>")
        print(f"Using default config: {DEFAULT_CONFIG_FILE}")
        config_path = DEFAULT_CONFIG_FILE

    config = load_config(config_path)
    config_dir = os.path.dirname(os.path.abspath(config_path))
    
    midi_path = config["input_file"]
    if not os.path.exists(midi_path):
        # Try relative to config file
        midi_path_rel = os.path.join(config_dir, midi_path)
        if os.path.exists(midi_path_rel):
            midi_path = midi_path_rel
        else:
            print(f"ERREUR: Fichier MIDI introuvable: {midi_path}")
            print(f"Recherché aussi dans: {midi_path_rel}")
            print("Veuillez éditer config.json avec le bon chemin.")
            return

    mid = MidiFile(midi_path)
    
    # Mode scan: on affiche juste les pistes et on quitte
    if config.get("scan_only", False):
        scan_tracks(mid)
        print("Mode scan terminé. Mettez 'scan_only': false dans la config pour générer.")
        return
        
    # Mode génération
    print(f"Traitement de {midi_path}...")
    scan_tracks(mid) # On affiche quand même pour info
    
    # beat_data[time][player] = set(types)
    beat_data = defaultdict(lambda: defaultdict(set))
    
    # Extraction des règles depuis la config
    all_rules = []
    # 1. Ancien format (rétro-compatibilité)
    if "mappings" in config and isinstance(config["mappings"], list):
        all_rules.extend(config["mappings"])
    
    # 2. Nouveau format: Players -> Timeline
    if "players" in config:
        for p in config["players"]:
            p_idx = p["index"]
            # Récupération de l'option min_velocity (par défaut 0/False)
            min_vel = p.get("min_velocity", 0)
            if min_vel is False: min_vel = 0

            if "timeline" in p:
                for rule in p["timeline"]:
                    # On injecte l'index du joueur dans la règle pour process_track
                    r = rule.copy()
                    r["player_index"] = p_idx
                    # Si la règle n'a pas son propre min_velocity, on prend celui du player
                    if "min_velocity" not in r:
                        r["min_velocity"] = min_vel
                    all_rules.append(r)

    if not all_rules:
         print("Aucune règle trouvée (ni 'mappings', ni 'players.timeline').")
         return
    
    for track in mid.tracks:
        # Récup nom
        t_name = ""
        for msg in track:
            if msg.type == 'track_name':
                t_name = msg.name
                break
        
        process_track(
            track, 
            t_name, 
            beat_data, 
            all_rules, 
            mid.ticks_per_beat
        )

    if not beat_data:
        print("Aucune donnée générée. Vérifiez vos mappings dans config.json.")
        return

    lines = generate_output_content(beat_data, config["players"])
    
    out_path = config.get("output_file", "output_beats.txt")
    # Si chemin relatif, on le met à côté du fichier de config
    if not os.path.isabs(out_path):
        out_path = os.path.join(config_dir, out_path)
        
    with open(out_path, 'w', encoding='utf-8') as f:
        f.write("\n".join(lines))
        
    print(f"\nSUCCÈS ! {len(lines)} lignes écrites dans: {out_path}")
    print("N'oubliez pas de copier le contenu dans votre fichier .beat final.")

if __name__ == "__main__":
    main()
