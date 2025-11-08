using System.Linq;
using Godot;

public partial class TrackController : Node2D
{
    [Export]
    public BeatController BeatController;

    [Export]
    public MusicData.PlayerRole Role;

    [Export]
    public HitZoneController[] HitZones;

    public override void _Ready()
    {
        BeatController.trackControllers.Add(this);
    }

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
