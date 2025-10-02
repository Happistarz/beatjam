using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class HitZoneController : Sprite2D
{
	[Export] public Timer RandomNoteTimer;
	[Export] public Node2D NoteContainer;

	[Export] public TrackController Track;
	[Export] public Refs.NoteType NoteType;

	private string inputAction;
	private List<NoteController> notes;

	public override void _Ready()
	{
		inputAction = Refs.Instance.GetInputAction(Track.Role, NoteType);
		notes = new List<NoteController>();
	}

	public override void _Process(double delta)
	{
		if (notes.Count < 1) return;
		if (notes.First().hasPassed) notes.RemoveAt(0);

		if (Input.IsActionJustPressed(inputAction))
		{
			var note = notes.FirstOrDefault();
			note.QueueFree();
			notes.RemoveAt(0);
		}
	}

	public void _on_random_note_timer_timeout()
	{
		var note = Refs.Instance.NoteScene.Instantiate<NoteController>();
		NoteContainer.CallDeferred("add_child", note);
		note.Initialize(NoteType, new Vector2(GlobalPosition.X, note.StartY));
		notes.Add(note);

		RandomNoteTimer.WaitTime = (float)GD.RandRange(0.5, 1.5);
		RandomNoteTimer.Start();
	}
}
