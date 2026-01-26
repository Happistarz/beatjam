using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class ScoreController : Node
{ 
    
    public struct ScoreData {
        public int score;
        public int score_visible;
        public int score_combo;
        public int combo;
    }

    public static ScoreController Instance { get; private set; }
    [Export] public Label ScoreLabel;
    [Export] public Label ComboLabel;
    private Dictionary<MusicData.PlayerRole, ScoreData> _playerScores = new();

    public void ResetPlayerScores()
    {
        foreach (var playerRole in _playerScores.Keys.ToList())
        {
            _playerScores[playerRole] = new ScoreData();
        }
        UpdateLabels();
    }
    
    public override void _Ready()
    {
        Instance = this;

        UpdateLabels();
    }

    public void AddPlayerScore(MusicData.PlayerRole playerRole, int points)
    {
        if (!_playerScores.ContainsKey(playerRole))
        {
            _playerScores[playerRole] = new ScoreData();
        }

        var scoreData = _playerScores[playerRole];

        if (points == 0)
        {
            scoreData.score += scoreData.score_combo * scoreData.combo;
            scoreData.score_visible = scoreData.score;
            scoreData.score_combo = 0;
            scoreData.combo = 0;
        }
        else
        {
            scoreData.score_combo += points;
            scoreData.score_visible += points;
            scoreData.combo += 1;
        }

        _playerScores[playerRole] = scoreData;
        UpdateLabels();

        GD.Print($"Player {playerRole} - Score: {scoreData.score} (Visible: {scoreData.score_visible}), Combo: {scoreData.combo}, Combo Score: {scoreData.score_combo}");
    }

    private void UpdateLabels()
    {
        ScoreLabel.Text = string.Join("\n", _playerScores.Select(kvp => $"{kvp.Key}: {kvp.Value.score_visible}"));
        ComboLabel.Text = string.Join("\n", _playerScores.Select(kvp => $"x{kvp.Value.combo}"));
    }
}