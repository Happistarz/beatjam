# MIDI Parser Tool

Ce dossier contient les outils pour convertir des fichiers MIDI en format `.beat` pour BeatJam.

## Structure

- **tool/** : Contient le script Python de conversion (`main.py` et `midi_utils.py`).
- **projects/** : Placez vos projets ici. Chaque chanson dans son propre dossier.
  - `chillcosy/` : Exemple de projet.
  - `meowmeow/` : Exemple de projet.

## Comment utiliser

1. **Créer un dossier de projet**
   Créez un nouveau dossier dans `projects/` (ex: `projects/MaChanson/`).
   Placez-y votre fichier MIDI (ex: `song.mid`).

2. **Créer la configuration**
   Créez un fichier `config.json` dans votre dossier de projet.
   Inspirez-vous des exemples existants.

   **Format du config.json :**
   ```json
   {
       "input_file": "song.mid",
       "output_file": "./output.txt",
       "players": [
           {
               "index": 0,
               "name": "DrumGod",
               "role": "Drums",
               "timeline": [
                   {
                       "track_name_contains": "KICK",
                       "start_measure": 1,
                       "end_measure": 100,
                       "mapping": {
                           "low": "C5",   // Note FL Studio (C5 = Note 60)
                           "high": "C6"
                       }
                   }
               ]
           }
       ]
   }
   ```
   *Note : Les octaves correspondent à celles affichées dans FL Studio (C5 est le Do central).*

3. **Lancer la conversion**
   Ouvrez un terminal dans `scripts/midi parser/`.
   Lancez la commande :
   ```bash
   python tool/main.py "projects/MaChanson/config.json"
   ```

4. **Résultat**
   Le fichier de sortie (ex: `output.txt`) sera généré dans le dossier de votre projet.
   Copiez son contenu dans votre fichier `.beat` final.

## Options

- `"scan_only": true` : Dans le config.json, permet d'afficher les pistes du MIDI sans générer de fichier, utile pour trouver les noms des pistes.
