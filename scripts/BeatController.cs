using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class BeatController : Node
{
    private struct TimingInfo
    {
        public int Measure;
        public int Beat;
        public int Sixteenth;
    }

    [Export]
    public AudioStreamPlayer MusicPlayer;

    [Export]
    public AudioStreamPlayer MetronomeClickPlayer;

    [Export]
    public Timer BeatTimer;

    [Export]
    public Timer DelayStartTimer;

    [Export]
    public TrackSet TrackSet;

    private int measureCounter = 0;
    private int beatCounter = 0;
    private int sixteenthCounter = 0;

    private bool inMetronomeMode = false;
    private const int METRONOME_START_MEASURES = 2;
    private int metronomeMeasuresElapsed = 0;
    private int metronomeBeatsElapsed = 0;

    private float leadOffset = 0f;

    private Dictionary<TimingInfo, List<MusicData.Note>> scheduledNotes = new();

    private float sixteenthDuration = 0f;
    private float beatDuration = 0f;

    private int lastSpawnedSixteenth = -1;

    public override void _Ready()
    {
        beatDuration = 60f / GameManager.Instance.CurrentTrack.BPM;
        sixteenthDuration = beatDuration / 4f;

        BeatTimer.Autostart = false;
        BeatTimer.OneShot = false;

        PrepareUpcomingNotes();
        StartGame();
    }

    public void CalculateLeadOffset()
    {
        float noteSpeed = Refs.Instance.NoteSpeed;
        float noteDistance = 800f;

        var hitzone = TrackSet.TrackControllers.FirstOrDefault()?.HitZones.FirstOrDefault();
        if (hitzone != null)
        {
            // Distance du spawn (StartY négatif vers le haut) jusqu’au centre visuel de frappe
            noteDistance = Math.Abs(hitzone.StartY) + (hitzone.Size.Y * 0.5f);
        }

        leadOffset = noteDistance / noteSpeed;
    }

    private void PrepareUpcomingNotes()
    {
        if (GameManager.Instance.CurrentTrack?.Notes == null)
            return;

        scheduledNotes.Clear();

        foreach (var playerNotes in GameManager.Instance.CurrentTrack.Notes)
        {
            foreach (var note in playerNotes.Value)
            {
                var timing = new TimingInfo
                {
                    Measure = note.Measure,
                    Beat = note.Beat,
                    Sixteenth = note.Sixteenth,
                };

                if (!scheduledNotes.ContainsKey(timing))
                {
                    scheduledNotes[timing] = new List<MusicData.Note>();
                }
                scheduledNotes[timing].Add(note);
            }
        }
    }

    public void StartGame()
    {
        DelayStartTimer.Start();
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
        MetronomeClickPlayer.PitchScale = (metronomeBeatsElapsed % 2 == 0) ? 1.2f : 1.0f;

        MetronomeClickPlayer.Play();

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

        int maxSixteenthToSpawn = Mathf.FloorToInt(targetTime / sixteenthDuration);

        if (lastSpawnedSixteenth < 0)
            lastSpawnedSixteenth = maxSixteenthToSpawn - 1;

        while (lastSpawnedSixteenth < maxSixteenthToSpawn)
        {
            lastSpawnedSixteenth++;

            int measure = lastSpawnedSixteenth / 16;
            int beat = lastSpawnedSixteenth % 16 / 4;
            int sixteenthInBeat = lastSpawnedSixteenth % 4;

            var timing = new TimingInfo
            {
                Measure = measure,
                Beat = beat,
                Sixteenth = sixteenthInBeat,
            };

            if (scheduledNotes.TryGetValue(timing, out var notes))
            {
                foreach (var note in notes)
                    TrackSet.SpawnNotesForTiming(note);

                scheduledNotes.Remove(timing);
            }
        }
    }
}
