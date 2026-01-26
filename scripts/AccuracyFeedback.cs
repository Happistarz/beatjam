using Godot;

public partial class AccuracyFeedback : Control
{
    [Export] public float LifetimeSeconds = 0.20f;

    // Offset applied relative to the spawn position (in local UI space)
    [Export] public Vector2 PositionOffset = Vector2.Zero;

    public override void _Ready()
    {
        var timer = GetTree().CreateTimer(LifetimeSeconds);
        timer.Timeout += QueueFree;
    }
}
