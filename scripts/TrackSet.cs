using System.Collections.Generic;
using Godot;

public partial class TrackSet : Control
{
    [Export] public Control TracksNoteContainer;
    [Export] public Control TracksHitZoneContainer;
    [Export] public BeatController BeatController;

    public List<TrackController> TrackControllers { get; private set; } = new();

    public override void _Ready()
    {
        // Validate exported references early
        if (TracksNoteContainer == null)
        {
            GD.PushError("TrackSet: TracksNoteContainer is not assigned.");
            return;
        }

        if (TracksHitZoneContainer == null)
        {
            GD.PushError("TrackSet: TracksHitZoneContainer is not assigned.");
            return;
        }

        if (BeatController == null)
        {
            GD.PushError("TrackSet: BeatController is not assigned.");
            return;
        }

        TrackControllers = new List<TrackController>(Refs.Instance.MaxPlayers);

        InitTrack(MusicData.PlayerRole.Drums);
        InitTrack(MusicData.PlayerRole.Guitar);
        InitTrack(MusicData.PlayerRole.Bass);

        BeatController.CalculateLeadOffset();
    }

    private void InitTrack(MusicData.PlayerRole role)
    {
        if (Refs.Instance == null)
        {
            GD.PushError("TrackSet: Refs.Instance is null.");
            return;
        }

        if (Refs.Instance.TrackScene == null)
        {
            GD.PushError("TrackSet: Refs.Instance.TrackScene is null.");
            return;
        }

        Node trackScene = Refs.Instance.TrackScene.Instantiate();
        if (trackScene == null)
        {
            GD.PushError("TrackSet: Failed to instantiate TrackScene.");
            return;
        }

        // Try to locate TrackController in the instantiated scene
        TrackController trackController = null;

        // Most robust: find by type in the subtree
        foreach (Node child in trackScene.GetChildren())
        {
            if (child is TrackController tc)
            {
                trackController = tc;
                break;
            }
        }

        // Fallback: root itself could be the controller
        if (trackController == null && trackScene is TrackController rootTc)
            trackController = rootTc;

        if (trackController == null)
        {
            GD.PushError("TrackSet: No TrackController found in TrackScene instance.");
            trackScene.QueueFree();
            return;
        }

        trackController.Role = role;

        // Create a per-track note container (Node2D) under the (now Control) note layer
        var trackNoteContainer = new Node2D
        {
            Name = "NoteContainer_" + role.ToString()
        };

        TracksNoteContainer.AddChild(trackNoteContainer);

        // Ensure it starts at origin of the note layer
        trackNoteContainer.Position = Vector2.Zero;

        trackController.NoteContainer = trackNoteContainer;

        // Notes are now UI: use the shared Control container
        if (trackController.HitZones != null)
        {
            foreach (var hitZone in trackController.HitZones)
            {
                if (hitZone == null)
                    continue;

                hitZone.NoteContainer = TracksNoteContainer;
                hitZone.Initialize();
            }
        }

        // Add the track UI under the hit zone container
        TracksHitZoneContainer.AddChild(trackScene);

        // If the instantiated root is a Control, make it cooperate with containers
        if (trackScene is Control trackRootControl)
        {
            trackRootControl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            trackRootControl.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        }

        TrackControllers.Add(trackController);
    }

    public void SpawnNotesForTiming(MusicData.Note note)
    {
        if (TrackControllers == null || TrackControllers.Count == 0)
            return;

        var targetRole = GetPlayerRoleFromNote(note);

        foreach (TrackController trackController in TrackControllers)
        {
            if (trackController == null)
                continue;

            if (trackController.Role != targetRole)
                continue;

            trackController.SpawnNoteAtTiming(note);
            break;
        }
    }

    private static MusicData.PlayerRole GetPlayerRoleFromNote(MusicData.Note note)
    {
        // Defensive defaults
        if (GameManager.Instance == null || GameManager.Instance.CurrentTrack == null)
            return MusicData.PlayerRole.Guitar;

        if (GameManager.Instance.CurrentTrack.Notes == null)
            return MusicData.PlayerRole.Guitar;

        foreach (var playerNotes in GameManager.Instance.CurrentTrack.Notes)
        {
            if (playerNotes.Value != null && playerNotes.Value.Contains(note))
                return playerNotes.Key;
        }

        return MusicData.PlayerRole.Guitar;
    }
}
