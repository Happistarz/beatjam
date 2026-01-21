using Godot;

public partial class NoteController : TextureRect
{
    [Export] public Timer DeleteTimer;

    [Export] public Refs.NoteType NoteType;

    public MusicData.PlayerRole PlayerRole;

    [Export] public float ThresholdY = 775f;

    private float _speed = 200f;
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

        // ThresholdY is in canvas coordinates; GlobalPosition.Y is also canvas coordinates for Control
        if (GlobalPosition.Y > ThresholdY && !hasPassed)
        {
            hasPassed = true;
            ReturnToPool();
        }
    }

    public void Initialize(
        Refs.NoteType type,
        MusicData.PlayerRole role,
        Vector2 localPositionInContainer,
        float speed,
        ObjectPool<NoteController> pool
    )
    {
        NoteType = type;
        PlayerRole = role;
        _speed = speed;
        _pool = pool;

        hasPassed = false;
        Visible = true;
        ProcessMode = ProcessModeEnum.Inherit;

        ApplySize();

        // Center the note on the spawn point
        Position = localPositionInContainer - Size * 0.5f;

        StartDeleteTimer();
    }

    private void ApplySize()
    {
        if (Refs.Instance == null)
            return;

        float s = Refs.Instance.MinimumNoteSize;
        CustomMinimumSize = new Vector2(s, s);
        Size = new Vector2(s, s);
    }

    private void StartDeleteTimer()
    {
        if (DeleteTimer == null)
            return;

        DeleteTimer.Stop();

        float remaining = ThresholdY - GlobalPosition.Y;
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
