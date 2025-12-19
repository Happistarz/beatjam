using Godot;

public partial class MusicItem : Button
{
    [ExportGroup("UI Elements")]
    [Export] public TextureRect CoverImage;

    [Export] public Label TitleLabel;
    [Export] public Label PlayersLabel;
    [Export] public Label BPMLabel;
    [Export] public Label TrackLengthLabel;

    public void Initialize(string title, string players, string bpm, string trackLength, Texture2D coverImage)
    {
        if (TitleLabel != null) TitleLabel.Text = title;
        if (PlayersLabel != null) PlayersLabel.Text = players;
        if (BPMLabel != null) BPMLabel.Text = bpm;
        if (TrackLengthLabel != null) TrackLengthLabel.Text = trackLength;
        if (CoverImage != null) CoverImage.Texture = coverImage;
    }
}
