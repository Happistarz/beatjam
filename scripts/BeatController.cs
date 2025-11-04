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
	[Export] public AudioStreamPlayer MetronomeClickPlayer;

	[Export] public Timer BeatTimer;
	[Export] public Timer DelayStartTimer;

	private int measureCounter = 0;
	private int beatCounter = 0;
	private int sixteenthCounter = 0;

	private bool inMetronomeMode = false;
	private const int METRONOME_START_MEASURES = 2;
	private int metronomeMeasuresElapsed = 0;
	private int metronomeBeatsElapsed = 0;

	private float leadOffset = 0f;

	private Dictionary<TimingInfo, List<MusicData.Note>> scheduledNotes = new();

	private List<TrackController> trackControllers = new();
	private float sixteenthDuration = 0f;
	private float beatDuration = 0f;

	public override void _Ready()
	{
		trackControllers = new List<TrackController>(Refs.Instance.MaxPlayers); // Cached references
		beatDuration = 60f / GameManager.Instance.CurrentTrack.BPM;
		sixteenthDuration = beatDuration / 4f;

		BeatTimer.Autostart = false;
		BeatTimer.OneShot = false;

		CacheControllers();

		CalculateLeadOffset();
		PrepareUpcomingNotes();
		DelayStartTimer.Start();
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

	private void StartMetronomeMode()
	{
		StopTrack();
		inMetronomeMode = true;
		metronomeMeasuresElapsed = 0;
		metronomeBeatsElapsed = 0;

		BeatTimer.Stop();
		BeatTimer.WaitTime = beatDuration;
		BeatTimer.Start();
	}

	public void StartTrack()
	{
		inMetronomeMode = false;

		BeatTimer.Stop();
		BeatTimer.WaitTime = sixteenthDuration;
		BeatTimer.Start();

		MusicPlayer.Play();

		measureCounter = 0;
		beatCounter = 0;
		sixteenthCounter = 0;
	}

	public void StopTrack()
	{
		MusicPlayer.Stop();
		BeatTimer.Stop();

		measureCounter = 0;
		beatCounter = 0;
		sixteenthCounter = 0;

		metronomeMeasuresElapsed = 0;
		metronomeBeatsElapsed = 0;
	}

	public void _on_DelayStartTimer_timeout()
	{
		StartMetronomeMode();
		DelayStartTimer.Stop();
	}

	public void _on_BeatTimer_timeout()
	{
		if (inMetronomeMode)
		{
			OnMetronomeClick();
			return;
		}

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

	private void _on_MusicPlayer_finished()
	{
		StopTrack();
	}

	private void OnMetronomeClick()
	{
		// Pitch mapping:
		// Mesure 1: 1.2, 1.0, 1.2, 1.0
		// Mesure 2: cycle 1.2, 1.0, 0.9 across 8 clicks (2 notes/beat)
		float pitch;
		if (metronomeMeasuresElapsed == 0)
		{
			pitch = (metronomeBeatsElapsed % 2 == 0) ? 1.2f : 1.0f;
		}
		else
		{
			// Mesure 2: motif 1,3,1,3 ... (8 clics): 1.2f puis 0.9f en alternance
			pitch = (metronomeBeatsElapsed % 2 == 0) ? 1.2f : 0.9f;
		}
		MetronomeClickPlayer.PitchScale = pitch;
		MetronomeClickPlayer.Play();

		// Mesure 0: 4 clics (noires), Mesure 1: 8 clics (croches = 2/beat)
		int beatsInThisMeasure = (metronomeMeasuresElapsed == 1) ? 8 : 4;

		metronomeBeatsElapsed++;

		if (metronomeBeatsElapsed >= beatsInThisMeasure)
		{
			metronomeBeatsElapsed = 0;
			metronomeMeasuresElapsed++;

			if (metronomeMeasuresElapsed >= METRONOME_START_MEASURES)
			{
				BeatTimer.Stop();
				MetronomeClickPlayer.Stop();
				StartTrack();
				return;
			}

			int nextBeatsInMeasure = (metronomeMeasuresElapsed == 1) ? 8 : 4;
			BeatTimer.WaitTime = (nextBeatsInMeasure == 8) ? beatDuration / 2f : beatDuration;
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
