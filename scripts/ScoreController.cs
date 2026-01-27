using System;
using System.Collections.Generic;
using Godot;

public partial class ScoreController : Node
{
    public struct ScoreData
    {
        public int score;
        public int score_visible;
        public int score_combo;
        public int combo;

        public Label ScoreLabel;
        public Label ComboLabel;
    }

    public static ScoreController Instance { get; private set; }
    private Dictionary<MusicData.PlayerRole, ScoreData> _playerScores = new();

    public void RegisterTrack(MusicData.PlayerRole role, Label scoreLabel, Label comboLabel)
    {
        ScoreData data = new()
        {
            score = 0,
            score_visible = 0,
            score_combo = 0,
            combo = 0,
            ScoreLabel = scoreLabel,
            ComboLabel = comboLabel
        };
        _playerScores[role] = data;
    }

    public void ResetPlayerScores()
    {
        // Destroy scoreData for all players
        _playerScores.Clear();
        UpdateLabels();
    }

    private void AssignLabels()
    {
        foreach (var kvp in _playerScores)
        {
            var playerRole = kvp.Key;
            var scoreData = kvp.Value;

            // Recherchez les labels dans la hiérarchie par nom
            var scoreLabel = FindChild($"{playerRole}_ScoreLabel", true, false) as Label;
            var comboLabel = FindChild($"{playerRole}_ComboLabel", true, false) as Label;

            if (scoreLabel == null || comboLabel == null)
            {
                GD.PrintErr($"ScoreController: Could not find labels for player role {playerRole}");
                continue;
            }

            // Assignez les labels trouvés
            scoreData.ScoreLabel = scoreLabel;
            scoreData.ComboLabel = comboLabel;
        }
    }

    public override void _Ready()
    {
        Instance = this;

        // Initialisation des scores pour tous les rôles
        foreach (MusicData.PlayerRole role in Enum.GetValues(typeof(MusicData.PlayerRole)))
        {
            if (!_playerScores.ContainsKey(role))
            {
                _playerScores[role] = new ScoreData
                {
                    score = 0,
                    combo = 0,
                    score_visible = 0,
                    score_combo = 0,
                    ScoreLabel = null,
                    ComboLabel = null
                };
            }
        }

        GD.Print("Current player scores:");
        foreach (var kvp in _playerScores)
        {
            GD.Print($"Role: {kvp.Key}, Score: {kvp.Value.score}, Combo: {kvp.Value.combo}");
        }

        // Assignez les labels dynamiquement
        AssignLabels();

        UpdateLabels();
    }

    public void AddPlayerScore(MusicData.PlayerRole playerRole, int points)
    {
        foreach (var kvp in _playerScores)
        {
            GD.Print($"PlayerRole: {kvp.Key}, Score: {kvp.Value.score}, Combo: {kvp.Value.combo}");
        }
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

        GD.Print($"Player {playerRole} - Score: {scoreData.score} (Visible: {scoreData.score_visible}), Combo: {scoreData.combo}, Combo Score: {scoreData.score_combo}");
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

        if (scoreData.ScoreLabel == null || scoreData.ComboLabel == null)
        {
            GD.PrintErr($"ScoreController: Labels are not assigned for player role {playerRole}");
            return;
        }

        scoreData.ScoreLabel.Text = scoreData.score_visible.ToString();
        scoreData.ComboLabel.Text = $"X{scoreData.combo}";
    }
}