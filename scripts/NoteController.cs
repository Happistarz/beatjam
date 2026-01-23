using Godot;

public partial class NoteController : TextureRect
{
    [Export] public Timer DeleteTimer;
    [Export] public Refs.NoteType NoteType;

    public MusicData.PlayerRole PlayerRole;

    // Size = lane width * ratio
    [Export] public float SizeRatioFromLane = 0.25f;

    private float _speed = 200f;
    private float _missThresholdCenterGlobalY = 1000f;

    public bool hasPassed = false;
    private ObjectPool<NoteController> _pool;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;

        // Keep circle aspect
        StretchMode = StretchModeEnum.KeepAspectCentered;
        ExpandMode = ExpandModeEnum.IgnoreSize;

        if (DeleteTimer != null)
            DeleteTimer.Stop();

        Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;
    }

    public override void _Process(double delta)
    {
        Position += new Vector2(0f, _speed * (float)delta);

        if (!hasPassed && GetCenterGlobalY() > _missThresholdCenterGlobalY)
        {
            hasPassed = true;
            ReturnToPool();
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

        hasPassed = false;
        Visible = true;
        ProcessMode = ProcessModeEnum.Inherit;

        ApplySizeFromLane();

        // Center + optional X offset
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

    public void ReturnToPool()
    {
        if (DeleteTimer != null)
            DeleteTimer.Stop();

        Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;

        if (_pool == null)
        {
            QueueFree();
            return;
        }

        _pool.Return(this);
    }

    public void _on_delete_timer_timeout()
    {
        hasPassed = true;
        ReturnToPool();
    }

    public void Reset()
    {
        if (DeleteTimer != null)
            DeleteTimer.Stop();

        hasPassed = false;
        _pool = null;
        Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;
    }
}
