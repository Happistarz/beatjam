using System.Linq;
using Godot;

public partial class TrackController : Control
{
    [Export] public Control NoteSpawnPoint;

    [Export]
    public MusicData.PlayerRole Role;

    [Export]
    public HitZoneController[] HitZones;

    [Export]
    public Node2D NoteContainer;

    public void SpawnNoteAtTiming(MusicData.Note note)
    {
        var hitZone = HitZones.FirstOrDefault(hz => hz.NoteType == note.Type);

        if (hitZone == null)
        {
            GD.PrintErr($"No HitZone found for NoteType {note.Type} in Role {Role}");
            return;
        }

        hitZone.SpawnNote(Refs.Instance.NoteSpeed);
    }
}
