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

    public enum CharacterAnimal
    {
        Cat,
        Elephant,
        Frog,
    }

    public enum CharacterState
    {
        Idle,
        Grooving,
        Hyped,
        Shameful,
        Angry,
    }

    [ExportGroup("Game Settings")]
    [Export] public int MaxPlayers = 4;
    [Export] public int MinPlayers = 1;
    [Export] public int DefaultBPM = 120;
    [Export] public string GameTitle = "BeatJam";
    [Export] public float NoteHitWindow = 0.2f; // seconds
    [Export] public float NoteSpeed = 200f; // units per second
    [Export] public float perfectThreshold = 10f; // units
    [Export] public float greatThreshold = 30f; // units
    [Export] public int MinimumNoteSize = 80; // pixels

    [ExportGroup("Scenes")]
    [Export] public PackedScene MusicItemScene;
    [Export] public PackedScene MenuScene;
    [Export] public PackedScene MusicChoiceScene;
    [Export] public PackedScene GameScene;
    [Export] public PackedScene TrackScene;
    [Export] public PackedScene HitZoneLayerScene;
    [Export] public PackedScene NoteScene;

    [ExportGroup("Textures")]
    [Export] public Texture2D DefaultCoverImage;

    [ExportGroup("Configs")]
    [Export] public string TracksDirectory = "res://tracks/";
    [Export] public string AudioDirectory = "res://assets/musics/";
    [Export] public string CoverDirectory = "res://assets/covers/";

    [ExportGroup("Inputs")]
    [Export] public string UI_LEFT = "ui_left";
    [Export] public string UI_RIGHT = "ui_right";
    [Export] public string UI_UP = "ui_up";
    [Export] public string UI_DOWN = "ui_down";
    [Export] public string UI_SELECT = "ui_select";

    [ExportGroup("Characters")]
    [Export] public string CharactersBaseDirectory = "res://assets/sprites/Characters/";

    // Which animal is used for each instrument
    [Export] public CharacterAnimal GuitarAnimal = CharacterAnimal.Frog;
    [Export] public CharacterAnimal DrumsAnimal = CharacterAnimal.Elephant;
    [Export] public CharacterAnimal KeyboardAnimal = CharacterAnimal.Cat;

    public CharacterAnimal GetAnimalForRole(MusicData.PlayerRole role)
    {
        return role switch
        {
            MusicData.PlayerRole.Guitar => GuitarAnimal,
            MusicData.PlayerRole.Drums => DrumsAnimal,
            MusicData.PlayerRole.Keyboard => KeyboardAnimal,
            _ => CharacterAnimal.Cat,
        };
    }

    public string GetCharacterDirectory(CharacterAnimal animal)
    {
        // Example: res://assets/sprites/Characters/Cat
        return $"{CharactersBaseDirectory.TrimEnd('/')}/{animal}";
    }

    public string GetCharacterFilePrefix(CharacterAnimal animal)
    {
        // Example: T_Cat_
        return $"T_{animal}_";
    }

    public string GetCharacterDirectoryForRole(MusicData.PlayerRole role)
    {
        return GetCharacterDirectory(GetAnimalForRole(role));
    }

    public string GetCharacterFilePrefixForRole(MusicData.PlayerRole role)
    {
        return GetCharacterFilePrefix(GetAnimalForRole(role));
    }

    public static string GetInputAction(MusicData.PlayerRole role, NoteType noteType)
    {
        return (role, noteType) switch
        {
            (MusicData.PlayerRole.Guitar, NoteType.High) => "INSTRU1_H",
            (MusicData.PlayerRole.Guitar, NoteType.Medium) => "INSTRU1_M",
            (MusicData.PlayerRole.Guitar, NoteType.Low) => "INSTRU1_L",

            (MusicData.PlayerRole.Keyboard, NoteType.High) => "INSTRU2_H",
            (MusicData.PlayerRole.Keyboard, NoteType.Medium) => "INSTRU2_M",
            (MusicData.PlayerRole.Keyboard, NoteType.Low) => "INSTRU2_L",

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
        if (distance <= greatThreshold)
            return Accuracy.Great;
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
