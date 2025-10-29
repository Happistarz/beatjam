using System;
using System.Linq;
using Godot;

public partial class HitZoneController : Sprite2D
{
	[Export] public Node2D NoteContainer;

	[Export] public TrackController Track;
	[Export] public Refs.NoteType NoteType;

	[Export] public float StartY = -50f;

	[Signal] public delegate void NoteHitEventHandler(int noteType, int accuracy);

	private string inputAction;
	public override void _Ready()
	{
		inputAction = Refs.Instance.GetInputAction(Track.Role, NoteType);
	}

	public override void _Process(double delta)
	{
		if (NoteContainer.GetChildren().Count < 1) return;

		if (Input.IsActionJustPressed(inputAction))
		{
			var note = GetClosestNote();
			if (note == null) return;

			var distance = Math.Abs(note.GlobalPosition.Y - GlobalPosition.Y);
			var accuracy = Refs.Instance.GetNoteAccuracy(distance);
			GD.Print($"Note distance: {distance:F1}, Accuracy: {accuracy}");

			EmitSignal(SignalName.NoteHit, (int)NoteType, (int)accuracy);

			note.QueueFree();
		}
	}
	
	private NoteController GetClosestNote()
    {
        return NoteContainer.GetChildren()
			.OfType<NoteController>()
			.Where(n => n.NoteType == NoteType)
			.OrderBy(n => Math.Abs(n.GlobalPosition.Y - GlobalPosition.Y))
			.FirstOrDefault();
    }

	public void SpawnNote(float speed = -1f)
	{
		if (speed < 0f) speed = Refs.Instance.NoteSpeed;

		var note = Refs.Instance.NoteScene.Instantiate<NoteController>();
		NoteContainer.CallDeferred("add_child", note);
		note.Initialize(NoteType, new Vector2(GlobalPosition.X, StartY), speed);
	}
}
