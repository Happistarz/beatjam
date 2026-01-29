using System.Collections.Generic;
using Godot;

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    public static List<MusicData> LoadedTracks { get; private set; } = new List<MusicData>();

#nullable enable
    public MusicData? CurrentTrack { get; set; } = null;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Hidden;

        if (Instance != null)
        {
            QueueFree();
            return;
        }

        Instance = this;

        LoadedTracks = TracksLoader.Instance.LoadAllTracks();
        CurrentTrack = LoadedTracks.Count > 0
            ? LoadedTracks[0]
            : null;

        SetPhysicsProcess(false);
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("QUIT"))
        {
            ReturnToMainMenu();
        }
    }

    public string? GetPlayerName(MusicData.PlayerRole role)
    {
        return CurrentTrack?.Players.Find(p => p.Role == role).Name;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ReturnToMainMenu()
    {
        GetTree().ChangeSceneToPacked(Refs.Instance.MenuScene);
    }
}
