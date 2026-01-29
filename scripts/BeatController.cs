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

        public override string ToString() => $"M{Measure}:B{Beat}:S{Sixteenth}";
    }

    [ExportGroup("Audio")]
    [Export] public AudioStreamPlayer MusicPlayer;
    [Export] public AudioStreamPlayer MetronomeClickPlayer;

    [ExportGroup("Timers")]
    [Export] public Timer BeatTimer;
    [Export] public Timer DelayStartTimer;

    [ExportGroup("References")]
    [Export] public TrackSet TrackSet;

    public List<BeatLightsController> BeatLights = new();

    public static BeatController Instance { get; private set; }
    public bool InMetronomeMode { get; private set; } = false;

    // Timing
    private float beatDuration;
    private float sixteenthDuration;
    private float leadOffset;

    // Metronome
    private const int METRONOME_MEASURES = 2;
    private int metronomeMeasuresElapsed;
    private int metronomeBeatsElapsed;
    private ulong metronomeStartTime;
    private int totalMetronomeClicks;
    private int metronomeBeatVisual = 0;

    // Note scheduling
    private readonly Dictionary<TimingInfo, List<MusicData.Note>> scheduledNotes = new();
    private int lastSpawnedSixteenth = -1;

    public override void _Ready()
    {
        Instance = this;

        beatDuration = 60f / GameManager.Instance.CurrentTrack.BPM;
        sixteenthDuration = beatDuration / 4f;

        BeatTimer.Autostart = false;
        BeatTimer.OneShot = false;

        PrepareUpcomingNotes();
        StartGame();
    }

    public override void _Process(double delta)
    {
        float currentMusicTime = GetCurrentMusicTime();
        if (currentMusicTime > float.MinValue)
        {
            CheckAndSpawnNotesForTime(currentMusicTime);

            // Update lights only during real music playback
            if (!InMetronomeMode)
            {
                BeatLights.ForEach(bl => bl.UpdateFromMusicTime(currentMusicTime, beatDuration));
            }
        }
    }

    public float GetCurrentMusicTime()
    {
        if (InMetronomeMode)
        {
            float elapsed = (Time.GetTicksMsec() - metronomeStartTime) / 1000f;
            float totalDuration = GetMetronomeTotalDuration();
            float musicTime = -(totalDuration - elapsed);

            return musicTime;
        }

        if (MusicPlayer.Playing)
        {
            float playbackPos = MusicPlayer.GetPlaybackPosition();

            return playbackPos;
        }

        return float.MinValue;
    }

    public void CalculateLeadOffset()
    {
        float noteSpeed = Refs.Instance.NoteSpeed;
        float noteDistance = 800f; // Fallback

        var hitzone = TrackSet.TrackControllers.FirstOrDefault()?.HitZones.FirstOrDefault();
        if (hitzone?.SpawnPoint != null)
        {
            float spawnY = hitzone.SpawnPoint.GetGlobalTransformWithCanvas().Origin.Y;
            float hitzoneY = hitzone.GetGlobalTransformWithCanvas().Origin.Y + (hitzone.Size.Y * 0.5f);
            noteDistance = Math.Abs(hitzoneY - spawnY);
        }

        leadOffset = noteDistance / Mathf.Max(1f, noteSpeed);
    }

    private void PrepareUpcomingNotes()
    {
        var notes = GameManager.Instance.CurrentTrack?.Notes;
        if (notes == null) return;

        scheduledNotes.Clear();

        int totalNotes = 0;
        foreach (var playerNotes in notes)
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
                    scheduledNotes[timing] = new List<MusicData.Note>();

                scheduledNotes[timing].Add(note);
                totalNotes++;
            }
        }
    }

    public void StartGame()
    {
        DelayStartTimer.Start();
    }

    private void StartMetronomeMode()
    {
        CalculateLeadOffset();
        StopTrack();

        InMetronomeMode = true;
        metronomeMeasuresElapsed = 0;
        metronomeBeatsElapsed = 0;
        totalMetronomeClicks = 0;
        metronomeStartTime = Time.GetTicksMsec();
        lastSpawnedSixteenth = -1;

        BeatLights.ForEach(bl => bl.SetAllOff());

        BeatTimer.WaitTime = beatDuration;
        BeatTimer.Start();
    }

    public void StartTrack()
    {
        InMetronomeMode = false;

        BeatLights.ForEach(bl => bl.SetAllOff());

        BeatTimer.WaitTime = sixteenthDuration;
        BeatTimer.Start();

        MusicPlayer.Stream = GameManager.Instance.CurrentTrack.MusicStream;
        MusicPlayer.Play();
    }

    public void StopTrack()
    {
        MusicPlayer.Stop();
        BeatTimer.Stop();
        metronomeMeasuresElapsed = 0;
        metronomeBeatsElapsed = 0;

        BeatLights.ForEach(bl => bl.SetAllOff());
    }

    public void _on_DelayStartTimer_timeout()
    {
        StartMetronomeMode();
        DelayStartTimer.Stop();
    }

    public void _on_BeatTimer_timeout()
    {
        if (InMetronomeMode)
            OnMetronomeClick();
    }

    private void _on_MusicPlayer_finished()
    {
        ScoreController.Instance?.FinaliseScores();
        GetTree().CreateTimer(2f).Timeout += () =>
        {
            GetTree().ChangeSceneToPacked(Refs.Instance.ScoreScene);
            StopTrack();
        };
    }


    private void OnMetronomeClick()
    {
        totalMetronomeClicks++;

        MetronomeClickPlayer.PitchScale =
            (metronomeBeatsElapsed % 2 == 0) ? 1.2f : 1.0f;
        MetronomeClickPlayer.Play();

        int beatsInMeasure = (metronomeMeasuresElapsed == 1) ? 8 : 4;
        metronomeBeatsElapsed++;

        if (metronomeBeatsElapsed >= beatsInMeasure)
        {
            metronomeBeatsElapsed = 0;
            metronomeMeasuresElapsed++;

            if (metronomeMeasuresElapsed >= METRONOME_MEASURES)
            {
                BeatTimer.Stop();
                MetronomeClickPlayer.Stop();

                float totalDuration = GetMetronomeTotalDuration();
                float currentElapsed =
                    (Time.GetTicksMsec() - metronomeStartTime) / 1000f;
                float remainingTime = totalDuration - currentElapsed;

                if (remainingTime > 0.001f)
                    GetTree().CreateTimer(remainingTime).Timeout += StartTrack;
                else
                    StartTrack();

                return;
            }

            int nextBeats = (metronomeMeasuresElapsed == 1) ? 8 : 4;
            float nextWaitTime =
                (nextBeats == 8) ? beatDuration / 2f : beatDuration;

            BeatTimer.WaitTime = nextWaitTime;
        }
    }

    private float GetMetronomeTotalDuration()
    {
        // First measure: 4 beats
        // Second measure: 8 half-beats
        return (4 * beatDuration) + (8 * (beatDuration / 2f));
    }

    private void CheckAndSpawnNotesForTime(float currentTime)
    {
        float targetTime = currentTime + leadOffset;
        int maxSixteenth = Mathf.FloorToInt(targetTime / sixteenthDuration);

        if (maxSixteenth < 0) return;
        if (lastSpawnedSixteenth < 0) lastSpawnedSixteenth = -1;

        while (lastSpawnedSixteenth < maxSixteenth)
        {
            lastSpawnedSixteenth++;
            if (lastSpawnedSixteenth < 0) continue;

            var timing = new TimingInfo
            {
                Measure = lastSpawnedSixteenth / 16,
                Beat = lastSpawnedSixteenth % 16 / 4,
                Sixteenth = lastSpawnedSixteenth % 4,
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
