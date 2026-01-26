using System;
using System.Collections.Generic;
using Godot;

public partial class CharacterAnimation : TextureRect
{
    
    public enum CharacterState
    {
        Idle,
        Grooving,
        Hyped,
        Shameful,
        Angry,
    }

    [Export] public string BaseDir = "res://assets/sprites/Characters/Cat/";
    [Export] public string FilePrefix = "T_Cat_";
    [Export] public float FrameIntervalSeconds = 0.2f;

    private MusicData.PlayerRole _role;
    private CharacterState _state = CharacterState.Idle;
    private readonly List<Texture2D> _frames = new();
    private int _frameIndex = 0;

    private double _acc = 0.0;

    public override void _Ready()
    {
        SetState(CharacterState.Idle);
    }

    public void SetRole(MusicData.PlayerRole role)
    {
        _role = role;

        if (Refs.Instance == null)
        {
            GD.PushWarning("CharacterAnimation: Refs.Instance is null (cannot resolve character directory).");
            return;
        }

        var dir = Refs.Instance.GetCharacterDirectoryForRole(role);
        var prefix = Refs.Instance.GetCharacterFilePrefixForRole(role);

        SetCharacter(dir, prefix);
    }

    public void SetCharacter(string baseDir, string filePrefix)
    {
        BaseDir = baseDir;
        FilePrefix = filePrefix;
        SetState(_state);
    }

    public void SetState(CharacterState state)
    {
        _state = state;
        LoadFramesForState(state);
        _frameIndex = 0;
        ApplyCurrentFrame();
    }

    public override void _Process(double delta)
    {
        if (_frames.Count <= 1)
            return;

        _acc += delta;
        if (_acc < FrameIntervalSeconds)
            return;

        _acc = 0.0;
        _frameIndex = (_frameIndex + 1) % _frames.Count;
        ApplyCurrentFrame();
    }

    private void LoadFramesForState(CharacterState state)
    {
        _frames.Clear();

        // Naming convention:
        // Idle -> T_Cat_Idle.png
        // Grooving -> T_Cat_Grooving1.png, T_Cat_Grooving2.png (if present)
        // Hyped -> T_Cat_Hyped1.png, T_Cat_Hyped2.png (if present)
        // Shameful -> T_Cat_Shameful.png
        // Angry -> T_Cat_Angry.png

        switch (state)
        {
            case CharacterState.Idle:
                TryAddSingle("Idle");
                break;

            case CharacterState.Grooving:
                TryAddNumbered("Grooving", 1, 8);
                break;

            case CharacterState.Hyped:
                TryAddNumbered("Hyped", 1, 8);
                break;

            case CharacterState.Shameful:
                TryAddSingle("Shameful");
                break;

            case CharacterState.Angry:
                TryAddSingle("Angry");
                break;
        }

        // Fallback safety
        if (_frames.Count == 0)
            TryAddSingle("Idle");
    }

    private void TryAddSingle(string suffix)
    {
        var path = BuildPath(suffix, null);
        var tex = LoadTexture(path);
        if (tex != null)
            _frames.Add(tex);
        else
            GD.PushWarning($"CharacterAnimation: Missing texture at {path}");
    }

    private void TryAddNumbered(string suffix, int start, int max)
    {
        for (int i = start; i <= max; i++)
        {
            var path = BuildPath(suffix, i);
            var tex = LoadTexture(path);
            if (tex == null)
            {
                // Stop at first missing to allow variable frame counts (1..N)
                break;
            }
            _frames.Add(tex);
        }

        // If no numbered frames exist, try non-numbered fallback (optional)
        if (_frames.Count == 0)
            TryAddSingle(suffix);
    }

    private string BuildPath(string suffix, int? number)
    {
        var file = number.HasValue
            ? $"{FilePrefix}{suffix}{number.Value}.png"
            : $"{FilePrefix}{suffix}.png";

        return $"{BaseDir.TrimEnd('/')}/{file}";
    }

    private static Texture2D LoadTexture(string path)
    {
        if (!ResourceLoader.Exists(path))
            return null;

        return ResourceLoader.Load<Texture2D>(path);
    }

    private void ApplyCurrentFrame()
    {
        if (_frames.Count == 0)
            return;

        Texture = _frames[_frameIndex];
    }
}
