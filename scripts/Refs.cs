using Godot;

public partial class Refs : Node
{
  public static Refs Instance;

  [ExportGroup("Game Settings")]
  [Export] public int MaxPlayers = 4;
  [Export] public int MinPlayers = 1;
  [Export] public int DefaultBPM = 120;
  [Export] public string GameTitle = "BeatJam";

  [ExportGroup("Scenes")]
  [Export] public PackedScene MusicItemScene;
  [Export] public PackedScene MenuScene;
  [Export] public PackedScene MusicChoiceScene;

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
