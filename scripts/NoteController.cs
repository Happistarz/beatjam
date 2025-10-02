using Godot;

public partial class NoteController : Sprite2D
{
	[Export] public Timer DeleteTimer;

	[Export] public Refs.NoteType NoteType;

	[Export] public float ThresholdY = 775f;
	[Export] public float Speed = 2f;
	[Export] public float StartY = -50f;

	public bool hasPassed = false;

	public override void _Process(double delta)
	{
		// Move the note downwards
		GlobalPosition += new Vector2(0, Speed);

		if (GlobalPosition.Y > ThresholdY && !DeleteTimer.IsStopped())
		{
			hasPassed = true;
			DeleteTimer.Stop();
		}
	}

	public void Initialize(Refs.NoteType type, Vector2 position)
	{
		NoteType = type;
		GlobalPosition = position;
	}

	public void _on_delete_timer_timeout()
	{
		QueueFree();
	}
}
