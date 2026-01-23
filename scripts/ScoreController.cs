using Godot;

public partial class ScoreController : Node
{ 
    public static ScoreController Instance { get; private set; }
    [Export] public Label ScoreLabel;
    [Export] public Label ComboLabel;
    private int _score = 0;
    private int _score_visible = 0;
    private int _score_combo = 0;
    private int _combo = 0;

    public void ResetScore()
    {
        _score = 0;
        _score_visible = 0;
        _score_combo = 0;
        _combo = 0;
        UpdateLabels();
    }
    
    public override void _Ready()
    {
        Instance = this;

        UpdateLabels();
    }

    public void AddScore(int points)
    {
        if (points == 0)
        {
            _score += _score_combo * _combo;
            _score_visible = _score;
            _score_combo = 0;
            _combo = 0;
        }
        else
        {
            _score_combo += points;
            _score_visible += points;
            _combo += 1;
        }
        UpdateLabels();

        GD.Print($"Score: {_score} (Visible: {_score_visible}), Combo: {_combo}, Combo Score: {_score_combo}");

    }

    private void UpdateLabels()
    {
        ScoreLabel.Text = $"Score: {_score_visible}";
        ComboLabel.Text = $"Combo: {_combo}";
    }
}