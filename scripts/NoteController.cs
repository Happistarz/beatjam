using Godot;

public partial class NoteController : Sprite2D
{
    [Export] public Timer DeleteTimer;
    [Export] public Refs.NoteType NoteType;
    [Export] public float ThresholdY = 775f;

    [Export] public float Speed = 200f;

    public bool hasPassed = false;

    private ObjectPool<NoteController> _pool;

    public override void _Ready()
    {
        // Defensive: avoid null deref if not assigned in editor
        if (DeleteTimer != null)
            DeleteTimer.Stop();

        ApplyVisualScale();
        ProcessMode = ProcessModeEnum.Disabled;
        Visible = false;
    }

    public override void _Process(double delta)
    {
        GlobalPosition += new Vector2(0f, Speed * (float)delta);

        if (GlobalPosition.Y > ThresholdY && !hasPassed)
        {
            hasPassed = true;
            ReturnToPool();
        }
    }

    public void Initialize(
        Refs.NoteType type,
        Vector2 globalPosition,
        float speed,
        ObjectPool<NoteController> pool
    )
    {
        NoteType = type;
        GlobalPosition = globalPosition;
        Speed = speed;
        _pool = pool;

        hasPassed = false;
        Visible = true;
        ProcessMode = ProcessModeEnum.Inherit;

        ApplyVisualScale();
        StartDeleteTimer();
    }

    private void ApplyVisualScale()
    {
        if (Texture == null || Refs.Instance == null)
            return;

        // Target size in pixels on screen
        float targetPx = Refs.Instance.MinimumNoteSize;

        Vector2 texSize = Texture.GetSize();
        float denom = Mathf.Max(1f, Mathf.Max(texSize.X, texSize.Y));
        float s = targetPx / denom;

        Scale = new Vector2(s, s);
    }

    private void StartDeleteTimer()
    {
        if (DeleteTimer == null)
            return;

        DeleteTimer.Stop();

        // Compute remaining travel time from current position to threshold
        float remaining = ThresholdY - GlobalPosition.Y;
        float seconds = remaining / Mathf.Max(1f, Speed);

        DeleteTimer.WaitTime = Mathf.Max(0.01f, seconds);
        DeleteTimer.Start();
    }

    public void ReturnToPool()
    {
        if (DeleteTimer != null)
            DeleteTimer.Stop();

        if (_pool == null)
        {
            GD.PrintErr("NoteController: Pool reference is null!");
            QueueFree();
            return;
        }

        Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;

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
