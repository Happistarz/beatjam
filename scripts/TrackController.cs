using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class TrackController : Node2D
{
	[Export] public BeatController BeatController;
	[Export] public MusicData.PlayerRole Role;
	[Export] public HitZoneController[] HitZones;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		AddToGroup("tracks");
		BeatController.Connect(BeatController.SignalName.Beat, Callable.From<int, int, int>(OnBeat));
	}

	public void OnBeat(int measure, int beat, int sixteenth)
	{
	}
	
	public void SpawnNoteAtTiming(MusicData.Note note)
	{
		var hitZone = HitZones.FirstOrDefault(hz => hz.NoteType == note.Type);

		if (hitZone == null)
		{
			GD.PrintErr($"No HitZone found for NoteType {note.Type} in Role {Role}");
			return;
		}

		hitZone.SpawnNote(Refs.Instance.NoteSpeed);
	}
}