using System;
using System.Linq;
using Godot;

public partial class HitZoneController : TextureRect
{
    [Export] public Control NoteContainer;
    [Export] public Control SpawnPoint;

    [Export] public TrackController Track;
    [Export] public Refs.NoteType NoteType;

    [Export] public float MissOutFactor = 0.5f;

    [Export] public float ScaleOnHit = 1.15f;
    private const float ScaleReturnSpeed = 20f;

    [Signal] public delegate void NoteHitEventHandler(int noteType, int accuracy);

    private ObjectPool<NoteController> _notePool;
    private string inputAction;

    public override void _Ready()
    {
        InitializePool();
        CustomMinimumSize = new Vector2(Refs.Instance.MinimumNoteSize, Refs.Instance.MinimumNoteSize);
    }

    public void Initialize(MusicData.PlayerRole? roleOverride = null)
    {
        if (Track == null)
        {
            GD.PushError($"HitZoneController {Name}: Track is null during Initialize!");
            return;
        }

        var effectiveRole = roleOverride ?? Track.Role;
        inputAction = Refs.GetInputAction(effectiveRole, NoteType);
    }

    private void InitializePool()
    {
        if (_notePool != null)
            return;

        _notePool = new ObjectPool<NoteController>();
        AddChild(_notePool);
        _notePool.Initialize(Refs.Instance.NoteScene, initialSize: 10, maxSize: 50);
    }

    public override void _Process(double delta)
    {
        Scale = new Vector2(
            Mathf.Lerp(Scale.X, 1f, ScaleReturnSpeed * (float)delta),
            Mathf.Lerp(Scale.Y, 1f, ScaleReturnSpeed * (float)delta)
        );

        if (Input.IsActionJustPressed(inputAction))
        {
            Scale = new Vector2(ScaleOnHit, ScaleOnHit);

            if (BeatController.Instance != null && BeatController.Instance.InMetronomeMode)
            {
                // Only allow input during metronome if we are very close to the start (within hit window)
                // This allows hitting the first note which is at time 0.0
                float timeToStart = BeatController.Instance.GetCurrentMusicTime();
                if (timeToStart < -Refs.Instance.NoteHitWindow)
                    return;
            }

            if (NoteContainer == null || NoteContainer.GetChildren().Count < 1)
                return;

            var note = GetClosestNote();
            if (note == null)
                return;

            var hitLineY = GetHitLineGlobalY();
            var distance = Math.Abs(note.GetCenterGlobalY() - hitLineY);
            var accuracy = Refs.Instance.GetNoteAccuracy(distance);

            if (Track != null)
            {
                var playerRole = Track.Role;
                ScoreController.Instance.AddPlayerScore(playerRole, Refs.GetScoreForAccuracy(accuracy));
            }

            SpawnAccuracyFeedbackOnNote(note, accuracy);

            var pt = GetPlayerTrack();
            pt?.OnJudgement(accuracy);

            EmitSignal(SignalName.NoteHit, (int)NoteType, (int)accuracy);

            note.MarkPushed();
            HandleStackedNotes(note);
        }
    }

    private void HandleStackedNotes(NoteController primaryNote)
    {
        float stackThreshold = 20f;
        var primaryY = primaryNote.GetCenterGlobalY();

        var stackedNotes = NoteContainer
            .GetChildren()
            .OfType<NoteController>()
            .Where(n =>
                n != primaryNote &&
                n.NoteType == NoteType &&
                n.PlayerRole == Track.Role &&
                !n.HasPassed &&
                n.IsTouchable &&
                Math.Abs(n.GetCenterGlobalY() - primaryY) <= stackThreshold
            );

        foreach (var stacked in stackedNotes)
        {
            stacked.MarkPushed();
        }
    }

    private NoteController GetClosestNote()
    {
        var hitLineY = GetHitLineGlobalY();

        return NoteContainer
            .GetChildren()
            .OfType<NoteController>()
            .Where(n =>
                n.NoteType == NoteType &&
                n.PlayerRole == Track.Role &&
                !n.HasPassed &&
                n.IsTouchable
            )
            .OrderByDescending(n => n.GetCenterGlobalY())
            .FirstOrDefault();
    }

    public void SpawnNote(float speed = -1f)
    {
        if (NoteContainer == null)
        {
            GD.PushError("HitZoneController: NoteContainer not assigned.");
            return;
        }

        if (SpawnPoint == null)
        {
            GD.PushError("HitZoneController: SpawnPoint not assigned.");
            return;
        }

        if (speed < 0f)
            speed = Refs.Instance.NoteSpeed;

        var note = _notePool.Get();

        // Important with pooling: prevent duplicate handlers
        note.Missed -= OnNoteMissed;
        note.Missed += OnNoteMissed;

        NoteContainer.AddChild(note);

        Vector2 spawnGlobal = SpawnPoint.GlobalPosition + SpawnPoint.Size * 0.5f;
        Vector2 spawnLocal = spawnGlobal - NoteContainer.GlobalPosition;

        float hitzoneBottomY = GlobalPosition.Y + Size.Y;
        float missThresholdCenterY = hitzoneBottomY + (Size.Y * MissOutFactor);

        note.Initialize(NoteType, Track.Role, spawnLocal, speed, _notePool, missThresholdCenterY);
    }

    private void OnNoteMissed(int noteType, int playerRole, Vector2 centerGlobal)
    {
        if (Track == null)
            return;

        var nt = (Refs.NoteType)noteType;
        var pr = (MusicData.PlayerRole)playerRole;

        // Only react for this lane + this track
        if (nt != NoteType || pr != Track.Role)
            return;

        // Visual feedback at the miss position
        SpawnAccuracyFeedbackAtGlobal(centerGlobal, Refs.Accuracy.Miss);

        ScoreController.Instance.AddPlayerScore(pr, 0);

        // Trigger character reaction
        var pt = GetPlayerTrack();
        pt?.OnJudgement(Refs.Accuracy.Miss);
    }

    private void SpawnAccuracyFeedbackOnNote(NoteController note, Refs.Accuracy accuracy)
    {
        if (Refs.Instance == null)
            return;

        var scene = Refs.Instance.GetFeedbackScene(accuracy);
        if (scene == null)
            return;

        if (NoteContainer == null)
            return;

        var feedback = scene.Instantiate<Control>();
        NoteContainer.AddChild(feedback);

        feedback.ZAsRelative = false;
        feedback.ZIndex = 1000;

        Vector2 size = feedback.Size;
        if (size.X <= 0.1f || size.Y <= 0.1f)
            size = feedback.CustomMinimumSize;

        var noteCenterGlobal = note.GlobalPosition + note.Size * 0.5f;
        var noteCenterLocal = noteCenterGlobal - NoteContainer.GlobalPosition;

        Vector2 offset = Vector2.Zero;
        if (feedback is AccuracyFeedback accuracyFeedback)
            offset = accuracyFeedback.PositionOffset;

        feedback.Position = noteCenterLocal - size * 0.5f + offset;
    }

    private void SpawnAccuracyFeedbackAtGlobal(Vector2 globalPos, Refs.Accuracy accuracy)
    {
        if (Refs.Instance == null)
            return;

        var scene = Refs.Instance.GetFeedbackScene(accuracy);
        if (scene == null)
            return;

        if (NoteContainer == null)
            return;

        var feedback = scene.Instantiate<Control>();
        NoteContainer.AddChild(feedback);

        feedback.ZAsRelative = false;
        feedback.ZIndex = 1000;

        Vector2 size = feedback.Size;
        if (size.X <= 0.1f || size.Y <= 0.1f)
            size = feedback.CustomMinimumSize;

        Vector2 offset = Vector2.Zero;
        if (feedback is AccuracyFeedback accuracyFeedback)
            offset = accuracyFeedback.PositionOffset;

        var local = globalPos - NoteContainer.GlobalPosition;
        feedback.Position = local - size * 0.5f + offset;
    }

    private PlayerTrack GetPlayerTrack()
    {
        Node current = this;
        while (current != null)
        {
            if (current is PlayerTrack pt)
                return pt;
            current = current.GetParent();
        }
        return null;
    }

    private float GetHitLineGlobalY()
    {
        return GlobalPosition.Y + Size.Y * 0.5f;
    }

    public override void _ExitTree()
    {
        _notePool?.ReturnAll();
    }
}
