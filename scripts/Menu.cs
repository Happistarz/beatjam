using Godot;

public partial class Menu : VBoxContainer
{
    [Export]
    public Button StartButton;

    [Export]
    public Button QuitButton;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GetChild<Button>(0).GrabFocus();
    }

    private void _on_start_pressed()
    {
        StartButton.GrabFocus();
        GetTree().ChangeSceneToPacked(Refs.Instance.MusicChoiceScene);
    }

    private void _on_quit_pressed()
    {
        QuitButton.GrabFocus();
        GetTree().Quit();
    }
}
