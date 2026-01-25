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

        if (Refs.Instance == null)
        {
            GD.PushError("TrackSet: Refs.Instance is null.");
            return;
        }

        TrackControllers = new List<TrackController>(Refs.Instance.MaxPlayers);

        InitTrack(MusicData.PlayerRole.Drums);
        InitTrack(MusicData.PlayerRole.Guitar);
        InitTrack(MusicData.PlayerRole.Keyboard);

        BeatController.CalculateLeadOffset();
    }

    private void InitTrack(MusicData.PlayerRole role)
    {
        if (Refs.Instance == null || Refs.Instance.TrackScene == null)
        {
            GD.PushError("TrackSet: Refs.Instance or TrackScene is null.");
            return;
        }

        var trackSceneRoot = Refs.Instance.TrackScene.Instantiate();
        if (trackSceneRoot == null)
        {
            GD.PushError("TrackSet: Failed to instantiate TrackScene.");
            return;
        }

        // Find PlayerTrack (prefer root; fallback to a named child)
        var playerTrack = trackSceneRoot as PlayerTrack;
        if (playerTrack == null)
            playerTrack = trackSceneRoot.FindChild("PlayerTrack", true, false) as PlayerTrack;

        if (playerTrack == null)
        {
            GD.PushError("TrackSet: No PlayerTrack found in TrackScene instance.");
            trackSceneRoot.QueueFree();
            return;
        }

        // IMPORTANT: Set Role before adding to the scene tree so PlayerTrack._Ready() uses the correct value
        playerTrack.Role = role;

        // Add to the UI container first, so any initialization that relies on being in the tree works correctly
        TracksHitZoneContainer.AddChild(trackSceneRoot);

        // Ensure layout cooperation for UI
        if (trackSceneRoot is Control trackRootControl)
        {
            trackRootControl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            trackRootControl.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        }

        // Get TrackController from PlayerTrack
        var trackController = playerTrack.TrackUI;
        if (trackController == null)
        {
            GD.PushError("TrackSet: PlayerTrack.TrackUI is null.");
            trackSceneRoot.QueueFree();
            return;
        }

        // Create a per-track note container (Node2D) under the shared note layer
        var trackNoteContainer = new Node2D
        {
            Name = "NoteContainer_" + role
        };

        TracksNoteContainer.AddChild(trackNoteContainer);
        trackNoteContainer.Position = Vector2.Zero;

        trackController.NoteContainer = trackNoteContainer;

        // Initialize hit zones after the track has been added to the tree
        if (trackController.HitZones != null)
        {
            foreach (var hitZone in trackController.HitZones)
            {
                if (hitZone == null)
                    continue;

                hitZone.NoteContainer = TracksNoteContainer;
                hitZone.Initialize(role);
            }
        }

        TrackControllers.Add(trackController);
    }

    public void SpawnNotesForTiming(MusicData.Note note)
    {
        if (TrackControllers == null || TrackControllers.Count == 0)
            return;

        var targetRole = GetPlayerRoleFromNote(note);

        foreach (var trackController in TrackControllers)
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
