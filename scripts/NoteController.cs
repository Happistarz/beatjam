using System.ComponentModel;
using Godot;

public partial class NoteController : Sprite2D
{
	[Export] public Timer DeleteTimer;

	[Export] public Refs.NoteType NoteType;

	[Export] public float ThresholdY = 775f;

	private float Speed = 200f;
	public bool hasPassed = false;

	private ObjectPool<NoteController> _pool;

	public override void _Process(double delta)
	{
		// Move the note downwards
		GlobalPosition += new Vector2(0, Speed * (float)delta);

		if (GlobalPosition.Y > ThresholdY && !hasPassed)
		{
			hasPassed = true;
			ReturnToPool();
		}
	}

	public void Initialize(Refs.NoteType type, Vector2 position, float speed, ObjectPool<NoteController> pool)
	{
		NoteType = type;
		GlobalPosition = position;
		Speed = speed > 0 ? speed : Refs.Instance.NoteSpeed;

		_pool = pool;
		hasPassed = false;
		ProcessMode = ProcessModeEnum.Inherit;
		Visible = true;
		DeleteTimer.Stop();
	}

	public void ReturnToPool()
	{
		if (_pool == null)
		{
			GD.PrintErr("NoteController: Pool reference is null!");
			QueueFree();
			return;
		}

		DeleteTimer.Stop();
		_pool.Return(this);
	}

	public void _on_delete_timer_timeout()
	{
		ReturnToPool();
	}

	public void Reset()
	{
		DeleteTimer.Stop();
		hasPassed = false;
		_pool = null;
		ProcessMode = ProcessModeEnum.Disabled;
	}
}
