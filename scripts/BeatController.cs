using System.Collections.Generic;
using Godot;

public partial class BeatController : Node
{
	private struct TimingInfo
	{
		public int Measure;
		public int Beat;
		public int Sixteenth;
	}

	[Export] public AudioStreamPlayer MusicPlayer;

	[Export] public Timer BeatTimer;

	private int measureCounter = 0;
	private int beatCounter = 0;
	private int sixteenthCounter = 0;

	private float leadOffset = 0f;

	private Dictionary<TimingInfo, List<MusicData.Note>> scheduledNotes = new();

	private List<TrackController> trackControllers = new();
	private float sixteenthDuration = 0f;

	public override void _Ready()
	{
		trackControllers = new List<TrackController>(Refs.Instance.MaxPlayers); // Cached references
		sixteenthDuration = 60f / (GameManager.Instance.CurrentTrack.BPM * 4f);
		BeatTimer.WaitTime = sixteenthDuration;

		CacheControllers();

		CalculateLeadOffset();
		PrepareUpcomingNotes();
		StartTrack();
	}

	private void CacheControllers()
	{
		trackControllers.Clear();

		var trackNodes = GetTree().GetNodesInGroup("tracks");
		foreach (var node in trackNodes)
		{
			if (node is TrackController trackController)
			{
				trackControllers.Add(trackController);
			}
		}
	}

	private void CalculateLeadOffset()
	{
		const float noteDistance = 800f;

		float travelTime = noteDistance / Refs.Instance.NoteSpeed;

		leadOffset = travelTime;
	}

	private void PrepareUpcomingNotes()
    {
		if (GameManager.Instance.CurrentTrack?.Notes == null) return;

		scheduledNotes.Clear();
		
		foreach (var playerNotes in GameManager.Instance.CurrentTrack.Notes)
		{
			foreach (var note in playerNotes.Value)
			{
				var timing = new TimingInfo
				{
					Measure = note.Measure,
					Beat = note.Beat,
					Sixteenth = note.Sixteenth
				};

				if (!scheduledNotes.ContainsKey(timing))
				{
					scheduledNotes[timing] = new List<MusicData.Note>();
				}
				scheduledNotes[timing].Add(note);
			}
		}
	}

	public void StartTrack()
	{
		MusicPlayer.Play();
		BeatTimer.Start();
	}

	public void StopTrack()
	{
		MusicPlayer.Stop();
		BeatTimer.Stop();
	}

	public void _on_BeatTimer_timeout()
	{
		CheckAndSpawnNotes();

	
		sixteenthCounter++;
		if (sixteenthCounter >= 4)
		{
			sixteenthCounter = 0;
			beatCounter++;
			if (beatCounter >= 4)
			{
				beatCounter = 0;
				measureCounter++;
			}
		}
	}

	private void CheckAndSpawnNotes()
	{
		float currentTime = MusicPlayer.GetPlaybackPosition();
		float targetTime = currentTime + leadOffset;

		int targetSixteenth = Mathf.FloorToInt(targetTime / sixteenthDuration);

		int targetMeasure = targetSixteenth / 16;
		int targetBeat = targetSixteenth % 16 / 4;
		int targetSixteenthInBeat = targetSixteenth % 4;

		var timing = new TimingInfo
		{
			Measure = targetMeasure,
			Beat = targetBeat,
			Sixteenth = targetSixteenthInBeat
		};

		if (scheduledNotes.TryGetValue(timing, out var notes))
		{
			foreach (var note in notes)
			{
				SpawnNotesForTiming(note);
			}
			scheduledNotes.Remove(timing);
		}
	}

	private void SpawnNotesForTiming(MusicData.Note note)
	{
		foreach (TrackController trackController in trackControllers)
		{
			if (trackController != null && trackController.Role == GetPlayerRoleFromNote(note))
			{
				trackController.SpawnNoteAtTiming(note);
				break;
			}
		}
	}

	private MusicData.PlayerRole GetPlayerRoleFromNote(MusicData.Note note)
	{
		foreach (var playerNotes in GameManager.Instance.CurrentTrack.Notes)
		{
			if (playerNotes.Value.Contains(note))
			{
				return playerNotes.Key;
			}
		}
		return MusicData.PlayerRole.Guitar;
	}
	// TODO:
	// faire le leadOffset pour spawner les notes en avance (en fonction de la vitesse des notes et du BPM)
	// leadOffset = (noteSpeed / (BPM / 60)) * 4;
	// spawn les vrais notes en fonction de la track data
	// test le hit avec les vraies notes
	// fix le timing des notes sur le beat si besoin
}
