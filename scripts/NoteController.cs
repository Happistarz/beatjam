using Godot;

public partial class NoteController : TextureRect
{
    [Export] public Timer DeleteTimer;
    [Export] public Refs.NoteType NoteType;

    public MusicData.PlayerRole PlayerRole;

    // Size = lane width * ratio
    [Export] public float SizeRatioFromLane = 0.25f;

    // Texture shown when the note is pushed (hit)
    [Export] public Texture2D HitTexture;

    // How long to keep the pushed state visible before returning to pool
    [Export] public float HitFeedbackSeconds = 0.08f;

    private Texture2D _defaultTexture;

    private float _speed = 200f;
    private float _missThresholdCenterGlobalY = 1000f;

    private ObjectPool<NoteController> _pool;

    public bool HasPassed { get; private set; } = false;

    // When false, gameplay code should ignore this note (cannot be hit again)
    public bool IsTouchable => _state == NoteState.Active;

    private enum NoteState
    {
        Inactive, // In pool / not in use
        Active,   // Moving and can be hit
        Pushed,   // Hit feedback; cannot be hit; waiting to be returned
    }

    private NoteState _state = NoteState.Inactive;

    private bool _pushedCountdownActive = false;
    private double _pushedRemaining = 0.0;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;

        // Keep circle aspect
        StretchMode = StretchModeEnum.KeepAspectCentered;
        ExpandMode = ExpandModeEnum.IgnoreSize;

        _defaultTexture = Texture;

        if (DeleteTimer != null)
            DeleteTimer.Stop();

        Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;

        _state = NoteState.Inactive;
    }

    public override void _Process(double delta)
    {
        if (_state == NoteState.Pushed)
        {
            // Stay visible for a short feedback duration, then return to pool
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

        if (_state != NoteState.Active)
            return;

        Position += new Vector2(0f, _speed * (float)delta);

        if (!HasPassed && GetCenterGlobalY() > _missThresholdCenterGlobalY)
        {
            HasPassed = true;
            ReturnToPool(force: true);
        }
    }

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

        RestoreDefaultTexture();

        _pushedCountdownActive = false;
        _pushedRemaining = 0.0;

        Visible = true;
        ProcessMode = ProcessModeEnum.Inherit;

        _state = NoteState.Active;

        ApplySizeFromLane();

        // Center the note at spawn position
        Position = new Vector2(
            localSpawnPosition.X - Size.X * 0.5f,
            localSpawnPosition.Y - Size.Y * 0.5f
        );

        StartDeleteTimer();
    }

    public float GetCenterGlobalY()
    {
        return GlobalPosition.Y + Size.Y * 0.5f;
    }

    // Call this when the player successfully hits the note.
    // After calling this, the note becomes untouchable and will return to pool after HitFeedbackSeconds.
    public void MarkPushed()
    {
        if (_state != NoteState.Active)
            return;

        // Prevent any further hit detection / interactions
        _state = NoteState.Pushed;

        // Stop miss timer: we're handling removal ourselves now
        if (DeleteTimer != null)
            DeleteTimer.Stop();

        HasPassed = true;

        // Swap texture for feedback
        if (HitTexture != null)
            Texture = HitTexture;

        // If duration is zero, return immediately
        if (HitFeedbackSeconds <= 0f)
        {
            ReturnToPool(force: true);
            return;
        }

        _pushedCountdownActive = true;
        _pushedRemaining = HitFeedbackSeconds;
    }

    // Use force=true for miss / pushed completion. Use force=false if you want to avoid
    // prematurely returning while pushed.
    public void ReturnToPool(bool force = false)
    {
        // While pushed, ignore non-forced returns (prevents immediate disappearance on hit)
        if (_state == NoteState.Pushed && !force)
            return;

        if (DeleteTimer != null)
            DeleteTimer.Stop();

        Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;

        RestoreDefaultTexture();

        _pushedCountdownActive = false;
        _pushedRemaining = 0.0;

        _state = NoteState.Inactive;

        if (_pool == null)
        {
            QueueFree();
            return;
        }

        _pool.Return(this);
    }

    public void _on_delete_timer_timeout()
    {
        // Miss path: force return
        HasPassed = true;
        ReturnToPool(force: true);
    }

    public void Reset()
    {
        if (DeleteTimer != null)
            DeleteTimer.Stop();

        HasPassed = false;
        _pool = null;

        _pushedCountdownActive = false;
        _pushedRemaining = 0.0;

        Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;

        RestoreDefaultTexture();

        _state = NoteState.Inactive;
    }

    private void ApplySizeFromLane()
    {
        // Prefer the lane control as size reference.
        // NoteContainer (parent) is usually the lane or a lane-relative container.
        Control sizeRef = GetParent() as Control;

        if (sizeRef == null)
            return;

        float laneWidth = sizeRef.Size.X;
        if (laneWidth <= 1f)
            return;

        float s = laneWidth * SizeRatioFromLane;

        CustomMinimumSize = new Vector2(s, s);
        Size = new Vector2(s, s);
    }

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

    private void RestoreDefaultTexture()
    {
        if (_defaultTexture == null)
            _defaultTexture = Texture;

        if (_defaultTexture != null && _state != NoteState.Pushed)
            Texture = _defaultTexture;
    }
}
