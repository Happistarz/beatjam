using System;
using System.Linq;
using Godot;

public partial class HitZoneController : TextureRect
{
    [Export]
    public Node2D NoteContainer;

    [Export]
    public TrackController Track;

    [Export]
    public Refs.NoteType NoteType;

    [Export]
    public float StartY = -50f;

    [Export]
    public float ScaleOnHit = 1.15f;
    private const float ScaleReturnSpeed = 20f;

    [Signal]
    public delegate void NoteHitEventHandler(int noteType, int accuracy);

    private ObjectPool<NoteController> _notePool;

    private string inputAction;

    public override void _Ready()
    {
        InitializePool();
    }

    public void Initialize()
    {
        if (Track == null)
            return;

        inputAction = Refs.GetInputAction(Track.Role, NoteType);
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

            if (NoteContainer == null || NoteContainer.GetChildren().Count < 1)
                return;

            var note = GetClosestNote();
            if (note == null)
                return;

            var distance = Math.Abs(note.GlobalPosition.Y - GlobalPosition.Y - Size.Y / 2);
            var accuracy = Refs.Instance.GetNoteAccuracy(distance);

            EmitSignal(SignalName.NoteHit, (int)NoteType, (int)accuracy);

            note.ReturnToPool();
        }
    }

    private NoteController GetClosestNote()
    {
        return NoteContainer
            .GetChildren()
            .OfType<NoteController>()
            .Where(n => n.NoteType == NoteType && !n.hasPassed)
            .OrderBy(n => Math.Abs(n.GlobalPosition.Y - GlobalPosition.Y - Size.Y / 2))
            .FirstOrDefault();
    }

    public void SpawnNote(float speed = -1f)
    {
        if (speed < 0f)
            speed = Refs.Instance.NoteSpeed;

        var note = _notePool.Get();
        NoteContainer.AddChild(note);

        var spawnPosition = new Vector2(GlobalPosition.X + Size.X / 2, GlobalPosition.Y + StartY);

        note.Initialize(NoteType, NoteContainer.ToLocal(spawnPosition), speed, _notePool);
    }

    public override void _ExitTree()
    {
        _notePool?.ReturnAll();
    }
}
