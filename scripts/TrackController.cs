using System.Linq;
using Godot;

public partial class TrackController : Control
{
    public MusicData.PlayerRole Role { get; set; }

    [Export] public HitZoneController[] HitZones;
    [Export] public Node2D NoteContainer;

    public override void _Ready()
    {
        ApplyLaneColors();
    }

    public void ApplyLaneColors()
    {
        if (HitZones == null || HitZones.Length == 0)
            return;

        for (int i = 0; i < HitZones.Length; i++)
        {
            var hz = HitZones[i];
            if (hz == null)
                continue;

            // Ensure HitZone has correct Track reference
            hz.Track = this;

            // Initialize also applies lane color (because it knows role + notetype)
            hz.Initialize(Role);
        }
    }

    public void SpawnNoteAtTiming(MusicData.Note note)
    {
        var hitZone = HitZones?.FirstOrDefault(hz => hz != null && hz.NoteType == note.Type);

        if (hitZone == null)
        {
            GD.PrintErr($"No HitZone found for NoteType {note.Type} in Role {Role}");
            return;
        }

        hitZone.SpawnNote(Refs.Instance.NoteSpeed);
    }
}
