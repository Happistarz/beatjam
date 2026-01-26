using Godot;
using System;

public partial class BeatLightsController : Node
{
    [Export] public Texture2D OffTexture;
    [Export] public Texture2D OnTexture;

    [ExportGroup("Lights (order 1..4)")]
    [Export] public TextureRect Light1;
    [Export] public TextureRect Light2;
    [Export] public TextureRect Light3;
    [Export] public TextureRect Light4;

    private TextureRect[] _lights;
    private int _lastBeatIndex = -1;

    public override void _Ready()
    {
        _lights = new[] { Light1, Light2, Light3, Light4 };
        SetAllOff();
    }

    public void UpdateFromMusicTime(float musicTime, float beatDuration)
    {
        if (musicTime < 0f || beatDuration <= 0f) return;

        int beatIndex = Mathf.FloorToInt(musicTime / beatDuration) % 4;
        if (beatIndex == _lastBeatIndex) return;

        _lastBeatIndex = beatIndex;
        ApplyBeat(beatIndex);
    }

    public void SetAllOff()
    {
        _lastBeatIndex = -1;
        foreach (var l in _lights)
        {
            if (l != null) l.Texture = OffTexture;
        }
    }

    public void ApplyBeat(int beatIndex)
    {
        for (int i = 0; i < _lights.Length; i++)
        {
            var l = _lights[i];
            if (l == null) continue;
            l.Texture = (i == beatIndex) ? OnTexture : OffTexture;
        }
    }
}
