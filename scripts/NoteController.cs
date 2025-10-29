using Godot;

public partial class NoteController : Sprite2D
{
	[Export] public Timer DeleteTimer;

	[Export] public Refs.NoteType NoteType;

	[Export] public float ThresholdY = 775f;

	private float Speed = Refs.Instance.NoteSpeed;
	public bool hasPassed = false;

	public override void _Process(double delta)
	{
		// Move the note downwards
		GlobalPosition += new Vector2(0, Speed * (float)delta);

		if (GlobalPosition.Y > ThresholdY && !DeleteTimer.IsStopped())
		{
			hasPassed = true;
			DeleteTimer.Stop();
		}
	}

	public void Initialize(Refs.NoteType type, Vector2 position, float speed = -1f)
	{
		NoteType = type;
		GlobalPosition = position;
		if (speed > 0f) Speed = speed;
	}

	public void _on_delete_timer_timeout()
	{
		QueueFree();
	}
}
