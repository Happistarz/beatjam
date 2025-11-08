using Godot;

public partial class MusicItem : PanelContainer
{
    [ExportGroup("UI Elements")]
    [Export]
    public TextureRect CoverImage;

    [Export]
    public Label TitleLabel;

    [Export]
    public Label PlayersLabel;

    [Export]
    public Label BPMLabel;

    [Export]
    public Label DifficultyLabel;

    [ExportGroup("Styles")]
    [Export]
    private StyleBoxFlat normalStyle;

    [Export]
    private StyleBoxFlat selectedStyle;

    public void SetSelected(bool selected)
    {
        AddThemeStyleboxOverride("panel", selected ? selectedStyle : normalStyle);
    }

    public void Initialize(
        string title,
        string players,
        string bpm,
        string difficulty,
        Texture2D coverImage
    )
    {
        TitleLabel.Text = title;
        PlayersLabel.Text = players;
        BPMLabel.Text = bpm;
        DifficultyLabel.Text = difficulty;
        CoverImage.Texture = coverImage;
    }
}
