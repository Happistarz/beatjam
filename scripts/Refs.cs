using Godot;

public partial class Refs : Node
{
    public static Refs Instance;

    public enum NoteType
    {
        High,
        Medium,
        Low,
    }

    public enum Accuracy
    {
        Perfect,
        Great,
        Miss,
    }

    [ExportGroup("Game Settings")]
    [Export]
    public int MaxPlayers = 4;

    [Export]
    public int MinPlayers = 1;

    [Export]
    public int DefaultBPM = 120;

    [Export]
    public string GameTitle = "BeatJam";

    [Export]
    public float NoteHitWindow = 0.2f; // seconds

    [Export]
    public float NoteSpeed = 200f; // units per second

    [Export]
    public float perfectThreshold = 10f; // units

    [Export]
    public float greatThreshold = 30f; // units

    [ExportGroup("Scenes")]
    [Export]
    public PackedScene MusicItemScene;

    [Export]
    public PackedScene MenuScene;

    [Export]
    public PackedScene MusicChoiceScene;

    [Export]
    public PackedScene GameScene;

    [Export]
    public PackedScene TrackScene;

    [Export]
    public PackedScene NoteScene;

    [ExportGroup("Textures")]
    [Export]
    public Texture2D DefaultCoverImage;

    [ExportGroup("Configs")]
    [Export]
    public string TracksDirectory = "res://tracks/";

    [Export]
    public string AudioDirectory = "res://assets/musics/";

    [Export]
    public string CoverDirectory = "res://assets/covers/";

    [ExportGroup("Inputs")]
    [Export]
    public string UI_LEFT = "ui_left";

    [Export]
    public string UI_RIGHT = "ui_right";

    [Export]
    public string UI_UP = "ui_up";

    [Export]
    public string UI_DOWN = "ui_down";

    [Export]
    public string UI_SELECT = "ui_select";

    public static string GetInputAction(MusicData.PlayerRole role, NoteType noteType)
    {
        return (role, noteType) switch
        {
            (MusicData.PlayerRole.Guitar, NoteType.High) => "INSTRU1_H",
            (MusicData.PlayerRole.Guitar, NoteType.Medium) => "INSTRU1_M",
            (MusicData.PlayerRole.Guitar, NoteType.Low) => "INSTRU1_L",
            (MusicData.PlayerRole.Bass, NoteType.High) => "INSTRU2_H",
            (MusicData.PlayerRole.Bass, NoteType.Medium) => "INSTRU2_M",
            (MusicData.PlayerRole.Bass, NoteType.Low) => "INSTRU2_L",
            (MusicData.PlayerRole.Drums, NoteType.High) => "INSTRU3_H",
            (MusicData.PlayerRole.Drums, NoteType.Medium) => "INSTRU3_M",
            (MusicData.PlayerRole.Drums, NoteType.Low) => "INSTRU3_L",
            _ => "",
        };
    }

    public Accuracy GetNoteAccuracy(float distance)
    {
        if (distance <= perfectThreshold)
            return Accuracy.Perfect;
        else if (distance <= greatThreshold)
            return Accuracy.Great;
        else
            return Accuracy.Miss;
    }

    public override void _Ready()
    {
        if (Instance == null)
        {
            Instance = this;
            GD.Print("Refs initialized.");
        }
        else
        {
            QueueFree();
        }
    }
}
