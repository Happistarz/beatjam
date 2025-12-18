using Godot;

public partial class TrackSetView : Control
{
    [Export] public NodePath TrackSetPath;
    private Node2D _trackSet;

    public override void _Ready()
    {
        _trackSet = GetNode<Node2D>(TrackSetPath);
        UpdateLayout();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
            UpdateLayout();
    }

    private void UpdateLayout()
    {
        if (_trackSet == null)
            return;

        // Top-left inside ScreenMargin:
        _trackSet.Position = Vector2.Zero;

        // If you prefer center inside ScreenMargin:
        // _trackSet.Position = Size * 0.5f;
    }
}
