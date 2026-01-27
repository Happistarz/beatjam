# flake8: noqa
"""Utilitaires pour le parsing MIDI."""
def note_name_to_number(name):
    """
    Convertit un nom de note (ex: 'C4', 'F#3', 'Gb5', 'B-1') en numéro MIDI.
    C4 = 60.
    """
    if isinstance(name, int):
        return name
        
    name = name.strip().upper()
    
    # Mapping des notes
    notes_offsets = {
        'C': 0, 'C#': 1, 'DB': 1,
        'D': 2, 'D#': 3, 'EB': 3,
        'E': 4,
        'F': 5, 'F#': 6, 'GB': 6,
        'G': 7, 'G#': 8, 'AB': 8,
        'A': 9, 'A#': 10, 'BB': 10,
        'B': 11
    }
    
    # Séparation note et octave
    # On cherche où commence le chiffre (ou le signe moins)
    import re
    match = re.search(r"(-?\d+)", name)
    if not match:
        raise ValueError(f"Format de note invalide: {name}")
        
    octave_str = match.group(1)
    note_part = name[:match.start()]
    
    if note_part not in notes_offsets:
        raise ValueError(f"Note inconnue: {note_part}")
        
    octave = int(octave_str)
    
    # FL Studio: C5 = 60 -> 5 * 12 = 60
    # Modified to match FL Studio octaves (Standard + 1)
    midi_num = octave * 12 + notes_offsets[note_part]
    return midi_num

def number_to_note_name(number):
    """Convertit un numéro MIDI en nom (Standard FL)."""
    notes = ['C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B']
    # FL Studio: 60 / 12 = 5. C5.
    octave = (number // 12)
    note = notes[number % 12]
    return f"{note}{octave}"
