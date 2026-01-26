using Godot;

public partial class NoteController : TextureRect
{
    // Emitted when the note is missed (passes the hit zone without being hit)
    // Arguments are kept as int for Godot signal compatibility
    [Signal]
    public delegate void MissedEventHandler(
        int noteType,
        int playerRole,
        Vector2 centerGlobal
    );

    // Timer used to auto-remove the note when it goes past the miss threshold
    [Export] public Timer DeleteTimer;

    // Lane type (High / Medium / Low)
    [Export] public Refs.NoteType NoteType;

    // Which player / instrument this note belongs to
    public MusicData.PlayerRole PlayerRole;

    // Visual sizing relative to the lane width
    [Export] public float SizeRatioFromLane = 0.25f;

    // Texture shown when the note is successfully hit
    [Export] public Texture2D HitTexture;

    // How long the hit texture stays visible before returning to pool
    [Export] public float HitFeedbackSeconds = 0.08f;

    // Default texture captured at runtime
    private Texture2D _defaultTexture;

    // Falling speed (pixels per second)
    private float _speed;

    // Global Y threshold (center of note) after which the note is considered missed
    private float _missThresholdCenterGlobalY;

    // Pool reference for reuse
    private ObjectPool<NoteController> _pool;

    // True once the note is either hit or missed
    public bool HasPassed { get; private set; } = false;

    // Whether the note can still be interacted with
    public bool IsTouchable => _state == NoteState.Active;

    // Internal lifecycle state
    private enum NoteState
    {
        Inactive, // In pool / not visible
        Active,   // Falling and can be hit
        Pushed,   // Hit feedback state (untouchable)
    }

    private NoteState _state = NoteState.Inactive;

    // Hit feedback countdown
    private bool _pushedCountdownActive = false;
    private double _pushedRemaining = 0.0;

    public override void _Ready()
    {
        // Notes should never block input
        MouseFilter = MouseFilterEnum.Ignore;

        // Preserve sprite aspect ratio
        StretchMode = StretchModeEnum.KeepAspectCentered;
        ExpandMode = ExpandModeEnum.IgnoreSize;

        // Store initial texture
        _defaultTexture = Texture;

        if (DeleteTimer != null)
            DeleteTimer.Stop();

        Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;
    }

    public override void _Process(double delta)
    {
        // --- HIT FEEDBACK STATE ---
        // During this phase the note is frozen and waiting to be removed
        if (_state == NoteState.Pushed)
        {
            if (_pushedCountdownActive)
            {
                _pushedRemaining -= delta;
                if (_pushedRemaining <= 0.0)
                {
                    _pushedCountdownActive = false;
                    ReturnToPool(force: true);
                }
            }
            return;
        }

        // Only active notes move
        if (_state != NoteState.Active)
            return;

        // Move the note downward
        Position += new Vector2(0f, _speed * (float)delta);

        // Check miss condition
        if (!HasPassed && GetCenterGlobalY() > _missThresholdCenterGlobalY)
        {
            EmitMiss();
            ReturnToPool(force: true);
        }
    }

    // Called by the spawner when reusing a note from the pool
    public void Initialize(
        Refs.NoteType type,
        MusicData.PlayerRole role,
        Vector2 localSpawnPosition,
        float speed,
        ObjectPool<NoteController> pool,
        float missThresholdCenterGlobalY
    )
    {
        NoteType = type;
        PlayerRole = role;
        _speed = speed;
        _pool = pool;
        _missThresholdCenterGlobalY = missThresholdCenterGlobalY;

        HasPassed = false;
        _state = NoteState.Active;

        RestoreDefaultTexture();

        _pushedCountdownActive = false;
        _pushedRemaining = 0.0;

        Visible = true;
        ProcessMode = ProcessModeEnum.Inherit;

        ApplySizeFromLane();

        // Center note on spawn position
        Position = new Vector2(
            localSpawnPosition.X - Size.X * 0.5f,
            localSpawnPosition.Y - Size.Y * 0.5f
        );

        StartDeleteTimer();
    }

    // Called when the player successfully hits the note
    public void MarkPushed()
    {
        if (_state != NoteState.Active)
            return;

        // Lock the note
        _state = NoteState.Pushed;
        HasPassed = true;

        // Cancel miss timer
        if (DeleteTimer != null)
            DeleteTimer.Stop();

        // Swap to hit feedback texture
        if (HitTexture != null)
            Texture = HitTexture;

        // Immediate removal if no feedback time
        if (HitFeedbackSeconds <= 0f)
        {
            ReturnToPool(force: true);
            return;
        }

        _pushedCountdownActive = true;
        _pushedRemaining = HitFeedbackSeconds;
    }

    // Emits the Missed signal (only once)
    private void EmitMiss()
    {
        if (_state != NoteState.Active)
            return;

        HasPassed = true;

        EmitSignal(
            SignalName.Missed,
            (int)NoteType,
            (int)PlayerRole,
            GlobalPosition + Size * 0.5f
        );
    }

    // Returns the note to the pool
    public void ReturnToPool(bool force = false)
    {
        // Prevent premature removal during hit feedback
        if (_state == NoteState.Pushed && !force)
            return;

        if (DeleteTimer != null)
            DeleteTimer.Stop();

        Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;

        RestoreDefaultTexture();

        _state = NoteState.Inactive;

        if (_pool == null)
        {
            QueueFree();
            return;
        }

        _pool.Return(this);
    }

    // Safety: timer-based miss
    public void _on_delete_timer_timeout()
    {
        EmitMiss();
        ReturnToPool(force: true);
    }

    // Resize the note based on its lane width
    private void ApplySizeFromLane()
    {
        if (GetParent() is not Control sizeRef)
            return;

        float laneWidth = sizeRef.Size.X;
        if (laneWidth <= 1f)
            return;

        float s = laneWidth * SizeRatioFromLane;
        CustomMinimumSize = new Vector2(s, s);
        Size = new Vector2(s, s);
    }

    // Starts the miss timer based on travel distance
    private void StartDeleteTimer()
    {
        if (DeleteTimer == null)
            return;

        DeleteTimer.Stop();

        float remaining = _missThresholdCenterGlobalY - GetCenterGlobalY();
        float seconds = remaining / Mathf.Max(1f, _speed);

        DeleteTimer.WaitTime = Mathf.Max(0.01f, seconds);
        DeleteTimer.Start();
    }

    // Returns the global Y position of the note center
    public float GetCenterGlobalY()
    {
        return GlobalPosition.Y + Size.Y * 0.5f;
    }

    // Restore the default sprite when reusing from pool
    private void RestoreDefaultTexture()
    {
        if (_defaultTexture != null)
            Texture = _defaultTexture;
    }
}
