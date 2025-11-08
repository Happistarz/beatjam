using System;
using System.Collections.Generic;
using System.IO;
using Godot;

public partial class TracksLoader : Node
{
    public static TracksLoader Instance;

    public override void _Ready()
    {
        if (Instance == null)
        {
            Instance = this;
            GD.Print("TracksLoader initialized.");
        }
        else
        {
            QueueFree();
        }
    }

    public List<MusicData> LoadAllTracks()
    {
        var dir = DirAccess.Open(Refs.Instance.TracksDirectory);
        if (dir == null)
        {
            GD.PrintErr("Failed to open tracks directory: " + Refs.Instance.TracksDirectory);
            return new List<MusicData>();
        }

        string[] files = dir.GetFiles();
        if (files.Length == 0)
        {
            GD.Print("No tracks found in directory: " + Refs.Instance.TracksDirectory);
            return new List<MusicData>();
        }

        var beats = new List<MusicData>();
        foreach (var file in files)
        {
            if (file.EndsWith(".beat"))
            {
                GD.Print("Loading track: " + Path.Combine(Refs.Instance.TracksDirectory, file));
                var track = LoadTrack(Path.Combine(Refs.Instance.TracksDirectory, file));
                if (track != null)
                {
                    beats.Add(track);
                }
            }
        }

        return beats;
    }

    public static MusicData LoadTrack(string path)
    {
        var track = new MusicData();

        var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr("Failed to open track file: " + path);
            return null;
        }

        string line;
        while (!file.EofReached())
        {
            line = file.GetLine();

            if (line == null)
                break;

            // skip comments
            if (line.StartsWith(";") || string.IsNullOrWhiteSpace(line))
                continue;

            // parse Players section
            if (line.StartsWith("Players"))
            {
                var numPlayers = int.Parse(line.Split(' ')[1]);
                track.Players = ParsePlayers(file, numPlayers);
                continue;
            }

            // parse Beats section
            if (line.StartsWith("Beats"))
            {
                var numBeats = int.Parse(line.Split(' ')[1]);
                track.Notes = ParseBeats(file, track.Players, numBeats);
                continue;
            }

            // parse key=value
            var parts = line.Split('=');
            if (parts.Length != 2)
            {
                GD.PrintErr("Invalid line in track file: " + line);
                continue;
            }

            var key = parts[0].Trim();
            var value = parts[1].Trim();
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
            {
                GD.PrintErr("Invalid key or value in track file: " + line);
                continue;
            }

            var property = track.GetType().GetProperty(key);
            if (property != null && property.CanWrite)
            {
                try
                {
                    object convertedValue;
                    if (property.PropertyType.IsEnum)
                    {
                        convertedValue = Enum.Parse(property.PropertyType, value, ignoreCase: true);
                    }
                    else
                    {
                        convertedValue = Convert.ChangeType(value, property.PropertyType);
                    }
                    property.SetValue(track, convertedValue);
                    continue;
                }
                catch (Exception ex)
                {
                    GD.PrintErr(
                        "Failed to set property " + key + " with value " + value + ": " + ex.Message
                    );
                    continue;
                }
            }
        }

        if (string.IsNullOrEmpty(track.Id))
        {
            GD.PrintErr("Track ID is not set, cannot load associated music and cover files.");
            return null;
        }

        // load music resource
        var musicPath = Path.Combine(Refs.Instance.AudioDirectory, track.Id + ".wav");
        if (Godot.FileAccess.FileExists(musicPath))
        {
            track.MusicStream = GD.Load<AudioStreamWav>(musicPath);
            if (track.MusicStream == null)
                GD.PrintErr("Failed to load music stream: " + musicPath);
        }

        // load cover image
        var coverPath = Path.Combine(Refs.Instance.CoverDirectory, track.Id + ".png");
        if (Godot.FileAccess.FileExists(coverPath))
        {
            track.CoverImage = GD.Load<Texture2D>(coverPath);
            if (track.CoverImage == null)
                GD.PrintErr("Failed to load cover image: " + coverPath);
        }

        file.Close();
        return track;
    }

    private static List<MusicData.Player> ParsePlayers(Godot.FileAccess file, int numPlayers = -1)
    {
        var players = new List<MusicData.Player>();

        string playerLine;
        int playersRead = 0;

        while (playersRead < numPlayers && !file.EofReached())
        {
            var currentPos = file.GetPosition();

            playerLine = file.GetLine();

            if (playerLine == null)
                break;

            if (playerLine.StartsWith(";") || string.IsNullOrWhiteSpace(playerLine))
                continue;

            // stop if we reach a non player line
            if (!playerLine.StartsWith("-"))
            {
                file.Seek(currentPos);
                break;
            }

            // parse player line
            var playerParts = playerLine[1..].Split(':');
            if (playerParts.Length != 3)
            {
                GD.PrintErr("Invalid player line: " + playerLine);
                continue;
            }

            var playerId = playerParts[0];
            var playerName = playerParts[1];
            var instrument = playerParts[2];
            if (!Enum.TryParse<MusicData.PlayerRole>(instrument, out var role))
            {
                GD.PrintErr("Unknown instrument in player line: " + playerLine);
                continue;
            }

            players.Add(
                new MusicData.Player
                {
                    Id = playerId,
                    Name = playerName,
                    Role = role,
                }
            );

            playersRead++;
        }

        return players;
    }

    private static Dictionary<MusicData.PlayerRole, List<MusicData.Note>> ParseBeats(
        Godot.FileAccess file,
        List<MusicData.Player> players,
        int numBeats = -1
    )
    {
        var beats = new Dictionary<MusicData.PlayerRole, List<MusicData.Note>>();

        string beatLine;
        int beatsRead = 0;

        while (beatsRead < numBeats && !file.EofReached())
        {
            var currentPos = file.GetPosition();

            beatLine = file.GetLine();

            if (beatLine == null)
                break;

            if (beatLine.StartsWith(";") || string.IsNullOrWhiteSpace(beatLine))
                continue;

            // stop if we reach a non beat line
            if (!beatLine.StartsWith("-"))
            {
                file.Seek(currentPos);
                break;
            }

            // parse beat line
            var instrumentParts = beatLine[1..].Split(' ');
            if (instrumentParts.Length < 4)
            {
                GD.PrintErr("Invalid beat line: " + beatLine);
                continue;
            }

            var measure = int.Parse(instrumentParts[0]);
            var beat = int.Parse(instrumentParts[1]);
            var sixteenth = int.Parse(instrumentParts[2]);
            var playersNotes = instrumentParts[3].Split(',');

            foreach (var playerNotes in playersNotes)
            {
                var pnParts = playerNotes.Split(':');
                if (pnParts.Length != 2)
                {
                    GD.PrintErr("Invalid player notes in beat line: " + beatLine);
                    continue;
                }

                var playerIndex = int.Parse(pnParts[0]);
                var notesStr = pnParts[1];

                if (playerIndex < 0 || playerIndex >= players.Count)
                {
                    GD.PrintErr("Player index out of range in beat line: " + beatLine);
                    continue;
                }

                var playerRole = players[playerIndex].Role;

                if (!beats.ContainsKey(playerRole))
                {
                    beats[playerRole] = new List<MusicData.Note>();
                }

                var notes = ParseNotes(notesStr, (short)measure, (short)beat, (short)sixteenth);
                beats[playerRole].AddRange(notes);
            }

            beatsRead++;
        }

        return beats;
    }

    private static List<MusicData.Note> ParseNotes(
        string notesStr,
        short measure,
        short beat,
        short sixteenth
    )
    {
        var notes = new List<MusicData.Note>();

        foreach (var noteChar in notesStr)
        {
            switch (noteChar)
            {
                case 'H':
                    notes.Add(
                        new MusicData.Note
                        {
                            Type = Refs.NoteType.High,
                            Measure = measure,
                            Beat = beat,
                            Sixteenth = sixteenth,
                        }
                    );
                    break;
                case 'M':
                    notes.Add(
                        new MusicData.Note
                        {
                            Type = Refs.NoteType.Medium,
                            Measure = measure,
                            Beat = beat,
                            Sixteenth = sixteenth,
                        }
                    );
                    break;
                case 'L':
                    notes.Add(
                        new MusicData.Note
                        {
                            Type = Refs.NoteType.Low,
                            Measure = measure,
                            Beat = beat,
                            Sixteenth = sixteenth,
                        }
                    );
                    break;
                default:
                    GD.PrintErr("Unknown note type: " + noteChar);
                    break;
            }
        }

        return notes;
    }
}
