using Godot;
using System.ComponentModel;
using System.Linq;

public partial class ScoreScene : Control
{
	[Export] public HBoxContainer PlayerScoresContainer;
	[Export] public Label ScoreLabel;
	[Export] public TextureRect TrackCoverRect;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{


		var scores = ScoreController.Instance.GetAllScores();

		foreach (var kvpScore in scores)
		{
			var playerScoreUI = Refs.Instance.PlayerScoreScene.Instantiate<PlayerScoreUI>();
			playerScoreUI.Initialize(kvpScore.Value, kvpScore.Key);
			PlayerScoresContainer.AddChild(playerScoreUI);
		}

		ScoreLabel.Text = scores.Sum(s => s.Value.score_visible).ToString();

		// Set the track cover image
		SetTrackCoverImage();
	}

	private void SetTrackCoverImage()
	{
		var selectedTrack = GameManager.Instance.CurrentTrack;
		if (selectedTrack != null && selectedTrack.CoverImage != null)
		{
			TrackCoverRect.Texture = selectedTrack.CoverImage;
		}
		else
		{
			GD.PrintErr("No cover image found for the selected track.");
		}
	}

	public void _on_MenuButton_pressed()
	{
		GetTree().ChangeSceneToPacked(Refs.Instance.MenuScene);
	}
}
