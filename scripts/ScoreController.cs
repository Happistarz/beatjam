using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class ScoreController : Node
{ 
    public MusicData.PlayerRole Role { get; set; }
    // public struct ScoreData {
    public int score;
    public int score_visible;
    public int score_combo;
    public int combo;
    // }

    public static ScoreController Instance { get; private set; }
    [Export] public Label ScoreLabel;
    [Export] public Label ComboLabel;
    // private Dictionary<MusicData.PlayerRole, ScoreData> _playerScores = new();

    public void ResetPlayerScores()
    {
        score = 0;
        score_visible = 0;
        score_combo = 0;
        combo = 0;
        UpdateLabels();
    }
    
    public override void _Ready()
    {
        Instance = this;
        UpdateLabels();
    }

    public void AddPlayerScore(MusicData.PlayerRole playerRole, int points)
    {
        if (points == 0)
        {
            score += score_combo * combo;
            score_visible = score;
            score_combo = 0;
            combo = 0;
        }
        else
        {
            score_combo += points;
            score_visible += points;
            combo += 1;
        }
        UpdateLabels();

        GD.Print($"Player {playerRole} - Score: {score} (Visible: {score_visible}), Combo: {combo}, Combo Score: {score_combo}");
    }

    private void UpdateLabels()
    {
        ScoreLabel.Text = score_visible.ToString();
        ComboLabel.Text = $"X{combo}";
    }
}