using System.Collections.Generic;
using Godot;

public class MusicData
{
    public enum PlayerRole
    {
        Guitar,
        Drums,
        Bass,
    }

    public struct Player
    {
        public string Id;
        public string Name;
        public PlayerRole Role;

        public override readonly string ToString()
        {
            return $"{Name} ({Role})";
        }
    }

    public struct Note
    {
        public Refs.NoteType Type;
        public short Measure;
        public short Beat;
        public short Sixteenth;

        public override readonly string ToString()
        {
            return $"{Type} @ {Measure}:{Beat}:{Sixteenth}";
        }
    }

    public string Id { get; set; }
    public string Title { get; set; }
    public int BPM { get; set; }
    public string Length { get; set; } // in format X 'm', Y 's'
    public AudioStream MusicStream { get; set; }
    public Texture2D CoverImage { get; set; }
    public List<Player> Players { get; set; } = new();
    public Dictionary<PlayerRole, List<Note>> Notes { get; set; } = new();

    // pretty print
    public override string ToString()
    {
        var playersStr = string.Join(", ", Players);
        var notesStr = string.Empty;
        foreach (var role in Notes.Keys)
        {
            notesStr += $"{role}: [{string.Join(", ", Notes[role])}]\n";
        }
        var musicStr = MusicStream != null ? MusicStream.GetPath() : "null";
        var coverStr = CoverImage != null ? CoverImage.GetPath() : "null";
        return $"MusicData(Id={Id}, Title={Title}, BPM={BPM}, Length={Length}, Players=[{playersStr}], Notes={{\n{notesStr}}}, Music={musicStr}, Cover={coverStr})";
    }
}
