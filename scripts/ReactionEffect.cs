using Godot;

public partial class ReactionEffect : TextureRect
{
    [Export] public float LifetimeSeconds = 0.35f;
    [Export] public float FrameIntervalSeconds = 0.10f;

    [Export] public Vector2 PositionOffset = Vector2.Zero;

    [Export] public Texture2D Hyped1;
    [Export] public Texture2D Hyped2;

    [Export] public Texture2D Grooving1;
    [Export] public Texture2D Grooving2;

    [Export] public Texture2D Shameful1;
    [Export] public Texture2D Shameful2;

    [Export] public Texture2D Angry1;
    [Export] public Texture2D Angry2;

    private bool _active = false;
    private double _lifeRemaining = 0.0;

    private double _frameAcc = 0.0;
    private bool _toggle = false;

    private Texture2D _f1;
    private Texture2D _f2;

    private Vector2 _basePosition;

    public override void _Ready()
    {
        _basePosition = Position;
        Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;
    }

    public override void _Process(double delta)
    {
        if (!_active)
            return;

        _lifeRemaining -= delta;
        if (_lifeRemaining <= 0.0)
        {
            Stop();
            return;
        }

        _frameAcc += delta;
        if (_frameAcc >= FrameIntervalSeconds)
        {
            _frameAcc = 0.0;
            _toggle = !_toggle;
            Texture = _toggle ? _f2 : _f1;
        }
    }

    public void Play(CharacterAnimation.CharacterState state)
    {
        if (!TryGetFrames(state, out _f1, out _f2))
            return;

        _toggle = false;
        _frameAcc = 0.0;
        Texture = _f1;

        Position = _basePosition + PositionOffset;

        Visible = true;
        ProcessMode = ProcessModeEnum.Inherit;

        _active = true;
        _lifeRemaining = LifetimeSeconds;
    }

    private bool TryGetFrames(CharacterAnimation.CharacterState state, out Texture2D f1, out Texture2D f2)
    {
        f1 = null;
        f2 = null;

        switch (state)
        {
            case CharacterAnimation.CharacterState.Hyped: f1 = Hyped1; f2 = Hyped2; break;
            case CharacterAnimation.CharacterState.Grooving: f1 = Grooving1; f2 = Grooving2; break;
            case CharacterAnimation.CharacterState.Shameful: f1 = Shameful1; f2 = Shameful2; break;
            case CharacterAnimation.CharacterState.Angry: f1 = Angry1; f2 = Angry2; break;
            default: return false;
        }

        return f1 != null && f2 != null;
    }

    private void Stop()
    {
        _active = false;
        Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;
    }
}
