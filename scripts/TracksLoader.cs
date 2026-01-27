using System;
using System.Collections.Generic;
using Godot;

public partial class TracksLoader : Node
{
    public static TracksLoader Instance;

    private static readonly string[] AudioExts = { ".wav", ".ogg", ".mp3" };

    public override void _Ready()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            QueueFree();
        }
    }

    public List<MusicData> LoadAllTracks()
    {
        var tracksDir = NormalizeResDir(Refs.Instance.TracksDirectory);
        var result = new List<MusicData>();

        var dir = DirAccess.Open(tracksDir);
        if (dir == null)
        {
            GD.PrintErr("Failed to open tracks directory: " + tracksDir);
            return result;
        }

        var files = dir.GetFiles();
        foreach (var fileName in files)
        {
            if (!fileName.EndsWith(".beat", StringComparison.OrdinalIgnoreCase))
                continue;

            var fullPath = CombineResPath(tracksDir, fileName);
            var track = LoadTrack(fullPath);
            if (track != null)
                result.Add(track);
        }

        return result;
    }

    public static MusicData LoadTrack(string path)
    {
        if (!FileAccess.FileExists(path))
        {
            GD.PrintErr("Beat file does not exist: " + path);
            return null;
        }

        var track = new MusicData();
        var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr("Failed to open beat file: " + path);
            return null;
        }

        try
        {
            while (!file.EofReached())
            {
                var line = file.GetLine();
                if (line == null)
                    break;

                line = line.Trim();
                if (line.Length == 0 || line.StartsWith(";"))
                    continue;

                if (line.StartsWith("Players", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && int.TryParse(parts[1], out var numPlayers))
                        track.Players = ParsePlayers(file, numPlayers);
                    else
                        GD.PrintErr("Invalid Players line: " + line);

                    continue;
                }

                if (line.StartsWith("Beats", StringComparison.OrdinalIgnoreCase))
                {
                    track.Notes = ParseBeats(file, track.Players);
                    continue;
                }

                var kv = line.Split('=', 2, StringSplitOptions.TrimEntries);
                if (kv.Length != 2)
                {
                    GD.PrintErr("Invalid header line: " + line);
                    continue;
                }

                var property = track.GetType().GetProperty(kv[0]);
                if (property == null || !property.CanWrite)
                {
                    GD.PrintErr("Unknown property: " + kv[0]);
                    continue;
                }

                try
                {
                    object value =
                        property.PropertyType.IsEnum
                            ? Enum.Parse(property.PropertyType, kv[1], true)
                            : Convert.ChangeType(kv[1], property.PropertyType);

                    property.SetValue(track, value);
                }
                catch (Exception ex)
                {
                    GD.PrintErr("Failed to set property " + kv[0] + ": " + ex.Message);
                }
            }
        }
        finally
        {
            file.Close();
        }

        if (string.IsNullOrEmpty(track.Id))
        {
            GD.PrintErr("Track Id is missing");
            return null;
        }

        var audioDir = NormalizeResDir(Refs.Instance.AudioDirectory);
        var coverDir = NormalizeResDir(Refs.Instance.CoverDirectory);

        track.MusicStream = TryLoadAudio(audioDir, track.Id);
        track.CoverImage = TryLoadTexture(coverDir, track.Id + ".png");

        return track;
    }

    private static List<MusicData.Player> ParsePlayers(FileAccess file, int numPlayers)
    {
        var players = new List<MusicData.Player>();
        int read = 0;

        while (read < numPlayers && !file.EofReached())
        {
            var pos = file.GetPosition();
            var line = file.GetLine();
            if (line == null)
                break;

            line = line.Trim();
            if (line.Length == 0 || line.StartsWith(";"))
                continue;

            if (!line.StartsWith("-"))
            {
                file.Seek(pos);
                break;
            }

            var parts = line[1..].Split(':');
            if (parts.Length != 3)
            {
                GD.PrintErr("Invalid player line: " + line);
                continue;
            }

            if (!Enum.TryParse(parts[2].Trim(), true, out MusicData.PlayerRole role))
            {
                GD.PrintErr("Invalid player role: " + line);
                continue;
            }

            players.Add(new MusicData.Player
            {
                Id = parts[0].Trim(),
                Name = parts[1].Trim(),
                Role = role
            });

            read++;
        }

        return players;
    }

    private static Dictionary<MusicData.PlayerRole, List<MusicData.Note>> ParseBeats(
        FileAccess file,
        List<MusicData.Player> players
    )
    {
        var beats = new Dictionary<MusicData.PlayerRole, List<MusicData.Note>>();

        while (!file.EofReached())
        {
            var pos = file.GetPosition();
            var line = file.GetLine();
            if (line == null)
                break;

            line = line.Trim();
            if (line.Length == 0 || line.StartsWith(";"))
                continue;

            if (!line.StartsWith("-"))
            {
                file.Seek(pos);
                break;
            }

            var parts = line[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4)
            {
                GD.PrintErr("Invalid beat line: " + line);
                continue;
            }

            if (!int.TryParse(parts[0], out var measure) ||
                !int.TryParse(parts[1], out var beat) ||
                !int.TryParse(parts[2], out var sixteenth))
            {
                GD.PrintErr("Invalid timing in beat line: " + line);
                continue;
            }

            var playersNotes = parts[3].Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pn in playersNotes)
            {
                var pnParts = pn.Split(':', 2);
                if (pnParts.Length != 2)
                {
                    GD.PrintErr("Invalid player notes: " + line);
                    continue;
                }

                if (!int.TryParse(pnParts[0], out var playerIndex) ||
                    playerIndex < 0 || playerIndex >= players.Count)
                {
                    GD.PrintErr("Invalid player index: " + line);
                    continue;
                }

                var role = players[playerIndex].Role;
                if (!beats.ContainsKey(role))
                    beats[role] = new List<MusicData.Note>();

                beats[role].AddRange(
                    ParseNotes(pnParts[1], role, (short)measure, (short)beat, (short)sixteenth)
                );
            }
        }

        return beats;
    }

    private static List<MusicData.Note> ParseNotes(
        string notesStr,
        MusicData.PlayerRole role,
        short measure,
        short beat,
        short sixteenth
    )
    {
        var notes = new List<MusicData.Note>();

        foreach (var c in notesStr)
        {
            switch (c)
            {
                case 'H':
                    notes.Add(new MusicData.Note { Type = Refs.NoteType.High, Measure = measure, Beat = beat, Sixteenth = sixteenth, PlayerRole = role });
                    break;
                case 'M':
                    notes.Add(new MusicData.Note { Type = Refs.NoteType.Medium, Measure = measure, Beat = beat, Sixteenth = sixteenth, PlayerRole = role });
                    break;
                case 'L':
                    notes.Add(new MusicData.Note { Type = Refs.NoteType.Low, Measure = measure, Beat = beat, Sixteenth = sixteenth, PlayerRole = role });
                    break;
                default:
                    GD.PrintErr("Unknown note char: " + c);
                    break;
            }
        }

        return notes;
    }

    private static AudioStream TryLoadAudio(string audioDir, string id)
    {
        foreach (var ext in AudioExts)
        {
            var p = CombineResPath(audioDir, id + ext);
            if (ResourceLoader.Exists(p))
                return GD.Load<AudioStream>(p);
        }

        GD.PrintErr("No audio resource found for track id=" + id);
        return null;
    }

    private static Texture2D TryLoadTexture(string dir, string fileName)
    {
        var p = CombineResPath(dir, fileName);
        if (!ResourceLoader.Exists(p))
            return null;

        return GD.Load<Texture2D>(p);
    }

    private static string NormalizeResDir(string dir)
    {
        dir = (dir ?? string.Empty).Trim();
        while (dir.EndsWith("/"))
            dir = dir[..^1];
        return dir;
    }

    private static string CombineResPath(string dir, string file)
    {
        dir = NormalizeResDir(dir);
        file = (file ?? string.Empty).TrimStart('/');
        return $"{dir}/{file}";
    }
}
