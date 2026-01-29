using System;
using System.Collections.Generic;
using Godot;

public partial class ScoreController : Node
{
    public class ScoreData
    {
        public string playerName;
        public int score;
        public int score_visible;
        public int score_combo;
        public int combo;

        public Label ScoreLabel;
        public Label ComboLabel;

        public override string ToString()
        {
            return $"Player name: {playerName}, Score: {score}, Visible: {score_visible}, Combo: {combo}, Combo Score: {score_combo}, ScoreLabel: {ScoreLabel}, ComboLabel: {ComboLabel}";
        }
    }

    public static ScoreController Instance { get; private set; }
    private Dictionary<MusicData.PlayerRole, ScoreData> _playerScores = new();

    public void RegisterTrack(MusicData.PlayerRole role, Label scoreLabel, Label comboLabel, string playerName = "")
    {
        ScoreData data = new()
        {
            playerName = playerName,
            score = 0,
            score_visible = 0,
            score_combo = 0,
            combo = 0,
            ScoreLabel = scoreLabel,
            ComboLabel = comboLabel
        };
        _playerScores[role] = data;
        UpdateLabelForPlayer(role);
    }

    public void ResetPlayerScores()
    {
        GD.Print("ScoreController: Resetting player scores.");
        _playerScores.Clear();
    }

    public Dictionary<MusicData.PlayerRole, ScoreData> GetAllScores()
    {
        return _playerScores;
    }

    public override void _Ready()
    {
        Instance = this;
    }

    public void AddPlayerScore(MusicData.PlayerRole playerRole, int points)
    {
        if (!_playerScores.ContainsKey(playerRole))
        {
            GD.PrintErr($"ScoreController: No score data found for player role {playerRole}");
            return;
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

        UpdateLabels();
    }

    public void FinaliseScores()
    {
        foreach (var kvp in _playerScores)
        {
            var scoreData = kvp.Value;
            if (scoreData.combo == 0)
                continue;

            scoreData.score += scoreData.score_combo * scoreData.combo;
            scoreData.score_visible = scoreData.score;
            scoreData.score_combo = 0;
            scoreData.combo = 0;
        }

        UpdateLabels();
    }

    private void UpdateLabels()
    {
        foreach (var kvp in _playerScores)
        {
            UpdateLabelForPlayer(kvp.Key);
        }
    }

    private void UpdateLabelForPlayer(MusicData.PlayerRole playerRole)
    {
        if (!_playerScores.ContainsKey(playerRole))
        {
            GD.PrintErr($"ScoreController: No score data found for player role {playerRole}");
            return;
        }

        var scoreData = _playerScores[playerRole];

        if (IsInstanceValid(scoreData.ScoreLabel))
        {
            scoreData.ScoreLabel.Text = $"{scoreData.playerName}: {scoreData.score_visible}";
        }
        if (IsInstanceValid(scoreData.ComboLabel))
        {
            scoreData.ComboLabel.Text = $"X{scoreData.combo}";
        }
    }
}