using Godot;

public partial class NotesEffect : TextureRect
{
    [Export] public Texture2D Frame1;
    [Export] public Texture2D Frame2;
    [Export] public Texture2D Frame3;

    [Export] public float FrameIntervalSeconds = 0.12f;

    private Texture2D[] _frames;
    private int _frameIndex = 0;
    private double _acc = 0.0;
    private bool _playing = false;

    public override void _Ready()
    {
        _frames = new[] { Frame1, Frame2, Frame3 };

        Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;
    }

    public override void _Process(double delta)
    {
        if (!_playing)
            return;

        _acc += delta;
        if (_acc < FrameIntervalSeconds)
            return;

        _acc = 0.0;
        _frameIndex = (_frameIndex + 1) % _frames.Length;

        Texture = _frames[_frameIndex];
    }

    public void Play()
    {
        if (_playing)
            return;

        if (_frames[0] == null || _frames[1] == null || _frames[2] == null)
            return;

        _playing = true;
        _frameIndex = 0;
        _acc = 0.0;

        Texture = _frames[0];

        Visible = true;
        ProcessMode = ProcessModeEnum.Inherit;
    }

    public void Stop()
    {
        _playing = false;
        Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;
    }
}
