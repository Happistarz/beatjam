using System.IO;
using Godot;

public partial class TracksLoader : Node
{
  public static TracksLoader Instance;

  public override void _Ready()
  {
    if (Instance == null)
    {
      Instance = this;
      GD.Print("TracksLoader initialized.");
      LoadAllTracks();
    }
    else
    {
      QueueFree();
    }
  }

  public void LoadAllTracks()
  {
    var dir = DirAccess.Open(Refs.Instance.TracksDirectory);
    if (dir == null)
    {
      GD.PrintErr("Failed to open tracks directory: " + Refs.Instance.TracksDirectory);
      return;
    }
    string[] files = dir.GetFiles();
    if (files.Length == 0)
    {
      GD.Print("No tracks found in directory: " + Refs.Instance.TracksDirectory);
      return;
    }
    foreach (var file in files)
    {
      if (file.EndsWith(".json"))
      {
        GD.Print("Loading track: " + Path.Combine(Refs.Instance.TracksDirectory, file));
        LoadTrack(Path.Combine(Refs.Instance.TracksDirectory, file));
      }
    }
  }

  public void LoadTrack(string path)
  {
    var File = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
    var json = new Json();
    var result = json.Parse(File.GetAsText());
    if (result != Error.Ok)
    {
      GD.PrintErr("Failed to parse track JSON: " + json.GetErrorMessage());
      return;
    }
    var trackData = (Godot.Collections.Dictionary)json.Data;

    GD.Print("Track Title: " + trackData["title"]);
    // GD.Print("Artist 0: " + trackData["players"][0]["id"]);
    GD.Print("BPM: " + trackData["bpm"]);
    GD.Print("Notes: ");
  }
}
