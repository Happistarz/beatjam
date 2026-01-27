using Godot;

public partial class MusicItem : ButtonAudio
{
    [ExportGroup("UI Elements")]
    [Export] public TextureRect CoverImage;

    [Export] public Label TitleLabel;
    [Export] public Label PlayersLabel;
    [Export] public Label BPMLabel;
    [Export] public Label TrackLengthLabel;

    public string Title => TitleLabel?.Text ?? string.Empty;

    public override void _Ready()
    {
        base._Ready();

        FocusMode = FocusModeEnum.All;

        if (CoverImage != null) CoverImage.FocusMode = FocusModeEnum.None;
        if (TitleLabel != null) TitleLabel.FocusMode = FocusModeEnum.None;
        if (PlayersLabel != null) PlayersLabel.FocusMode = FocusModeEnum.None;
        if (BPMLabel != null) BPMLabel.FocusMode = FocusModeEnum.None;
        if (TrackLengthLabel != null) TrackLengthLabel.FocusMode = FocusModeEnum.None;
    }

    public void Initialize(string title, string players, string bpm, string trackLength, Texture2D coverImage)
    {
        if (TitleLabel != null) TitleLabel.Text = title;
        if (PlayersLabel != null) PlayersLabel.Text = players;
        if (BPMLabel != null) BPMLabel.Text = bpm;
        if (TrackLengthLabel != null) TrackLengthLabel.Text = trackLength;
        if (CoverImage != null) CoverImage.Texture = coverImage;
    }
}
