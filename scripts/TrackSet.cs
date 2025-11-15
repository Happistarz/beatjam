using System.Collections.Generic;
using Godot;

public partial class TrackSet : Node2D
{
    [Export]
    public Node2D TracksNoteContainer { get; set; }

    [Export]
    public Control TracksHitZoneContainer { get; set; }

    [Export]
    public BeatController BeatController { get; set; }

    public List<TrackController> TrackControllers { get; private set; }

    public override void _Ready()
    {
        TrackControllers = new List<TrackController>(Refs.Instance.MaxPlayers);

        InitTrack(MusicData.PlayerRole.Drums);
        InitTrack(MusicData.PlayerRole.Guitar);
        InitTrack(MusicData.PlayerRole.Bass);

        BeatController.CalculateLeadOffset();
    }

    private void InitTrack(MusicData.PlayerRole role)
    {
        var track = Refs.Instance.TrackScene.Instantiate<TrackController>();
        track.Role = role;

        var trackNoteContainer = new Node2D { Name = $"NoteContainer_{role}" };
        TracksNoteContainer.AddChild(trackNoteContainer);
        track.NoteContainer = trackNoteContainer;

        foreach (HitZoneController hitZone in track.HitZones)
        {
            hitZone.NoteContainer = trackNoteContainer;
            hitZone.Track = track;

            hitZone.Initialize();
        }

        TracksHitZoneContainer.AddChild(track);
        TrackControllers.Add(track);
    }

    public void SpawnNotesForTiming(MusicData.Note note)
    {
        foreach (TrackController trackController in TrackControllers)
        {
            if (trackController != null && trackController.Role == GetPlayerRoleFromNote(note))
            {
                trackController.SpawnNoteAtTiming(note);
                break;
            }
        }
    }

    private static MusicData.PlayerRole GetPlayerRoleFromNote(MusicData.Note note)
    {
        foreach (var playerNotes in GameManager.Instance.CurrentTrack.Notes)
        {
            if (playerNotes.Value.Contains(note))
            {
                return playerNotes.Key;
            }
        }
        return MusicData.PlayerRole.Guitar;
    }
}
