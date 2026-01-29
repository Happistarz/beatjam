using Godot;

public partial class MusicChoice : CanvasLayer
{
    [Export] private BoxContainer musicList;
    [Export] private Button backButton;

    private int _selectedIndex = 0;
    private MusicItem[] _musicItems = System.Array.Empty<MusicItem>();

    public override void _Ready()
    {
        ScoreController.Instance?.ResetPlayerScores();

        int count = GameManager.LoadedTracks.Count;
        _musicItems = new MusicItem[count];

        for (int i = 0; i < count; i++)
        {
            var track = GameManager.LoadedTracks[i];
            var item = Refs.Instance.MusicItemScene.Instantiate<MusicItem>();

            item.Initialize(
                track.Title,
                $"{track.Players.Count} Players",
                $"{track.BPM} BPM",
                track.Length,
                track.CoverImage ?? Refs.Instance.DefaultCoverImage
            );

            item.FocusMode = Control.FocusModeEnum.All;

            musicList.AddChild(item);
            _musicItems[i] = item;
        }

        if (backButton != null)
            backButton.FocusMode = Control.FocusModeEnum.All;

        if (_musicItems.Length == 0)
        {
            backButton?.GrabFocus();
            return;
        }

        CallDeferred(nameof(SetupFocusGraph));
    }

    private void SetupFocusGraph()
    {
        if (backButton == null || _musicItems.Length == 0)
            return;

        for (int i = 0; i < _musicItems.Length; i++)
        {
            var item = _musicItems[i];

            int left = (i - 1 + _musicItems.Length) % _musicItems.Length;
            int right = (i + 1) % _musicItems.Length;

            // Loop left/right
            item.SetFocusNeighbor(Side.Left, item.GetPathTo(_musicItems[left]));
            item.SetFocusNeighbor(Side.Right, item.GetPathTo(_musicItems[right]));

            // DOWN from item goes to Back
            item.SetFocusNeighbor(Side.Bottom, item.GetPathTo(backButton));

            // Optional: disable Up navigation by clearing Top neighbor
            item.SetFocusNeighbor(Side.Top, new NodePath());

            // Keep index synced with actual focus (mouse, keyboard, pad)
            int index = i;
            item.FocusEntered += () =>
            {
                _selectedIndex = index;

                // DOWN from Back returns to current selected item
                backButton.SetFocusNeighbor(Side.Bottom, backButton.GetPathTo(_musicItems[_selectedIndex]));
            };
        }

        // DOWN from Back returns to selected item
        backButton.SetFocusNeighbor(Side.Bottom, backButton.GetPathTo(_musicItems[_selectedIndex]));

        // Optional: disable Up navigation from Back too
        backButton.SetFocusNeighbor(Side.Top, new NodePath());

        // Initial focus
        _selectedIndex = 0;
        _musicItems[_selectedIndex].GrabFocus();
    }

    public override void _Process(double delta)
    {
        // Let Godot handle focus navigation via neighbors (Left/Right/Down).
        // Only handle Accept here.
        if (Input.IsActionJustPressed("ui_accept"))
        {
            if (_musicItems.Length == 0)
                return;

            // If Back has focus, do nothing here (button's pressed signal handles it)
            if (backButton != null && backButton.HasFocus())
                return;

            GameManager.Instance.CurrentTrack = GameManager.LoadedTracks[_selectedIndex];
            GetTree().ChangeSceneToPacked(Refs.Instance.GameScene);
        }
    }

    public void _on_back_pressed()
    {
        GetTree().ChangeSceneToPacked(Refs.Instance.MenuScene);
    }
}
