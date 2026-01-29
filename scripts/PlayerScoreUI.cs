using Godot;
using System;

public partial class PlayerScoreUI : VBoxContainer
{
	[Export] public Label ScoreLabel;
	[Export] public CharacterAnimation CharacterAnimation;

	public void Initialize(ScoreController.ScoreData scoreData, MusicData.PlayerRole role)
	{
		ScoreLabel.Text = scoreData.score_visible.ToString();

		CharacterAnimation.SetRole(role);
	}
}