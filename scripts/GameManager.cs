using Godot;

public partial class GameManager : Node
{
  public static GameManager Instance { get; private set; }

  public static float CurrentTime { get; set; } = 0f;

  public override void _Ready()
  {
    if (Instance != null)
    {
      QueueFree();
      return;
    }

    Instance = this;

    SetPhysicsProcess(false);
  }

  public override void _ExitTree()
  {
    if (Instance == this)
    {
      Instance = null;
    }
  }

  public override void _Process(double delta)
  {
    CurrentTime += (float)delta;
  }
}
