using Godot;

public partial class TrackController : Node2D
{
	[Export] public AudioStreamPlayer MusicPlayer;

	[Export] public MusicData.PlayerRole Role;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
