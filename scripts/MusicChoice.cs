using Godot;

public partial class MusicChoice : CanvasLayer
{
    [Export] private BoxContainer musicList;
    [Export] private Button backButton;

    private bool _isOnBackButton = false;
    private int _selectedIndex = 0;
    private MusicItem[] _musicItems = System.Array.Empty<MusicItem>();

    private double _lastMoveTime = 0;
    private const double MoveDelay = 0.2;

    public override void _Ready()
    {
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

            // Up from item goes to Back
            item.SetFocusNeighbor(Side.Top, item.GetPathTo(backButton));

            // Keep index synced with actual focus (mouse, keyboard, pad)
            int index = i;
            item.FocusEntered += () =>
            {
                _selectedIndex = index;
                _isOnBackButton = false;

                // Down from Back returns to current selected item
                backButton.SetFocusNeighbor(Side.Bottom, backButton.GetPathTo(_musicItems[_selectedIndex]));
            };
        }

        // Down from Back returns to selected item
        backButton.SetFocusNeighbor(Side.Bottom, backButton.GetPathTo(_musicItems[_selectedIndex]));

        // Initial focus
        _isOnBackButton = false;
        _selectedIndex = 0;
        _musicItems[_selectedIndex].GrabFocus();
    }

    public override void _Process(double delta)
    {
        double now = Time.GetTicksMsec() / 1000.0;

        // Left/Right handled by Godot via focus neighbors
        if (now - _lastMoveTime > MoveDelay)
        {
            if (Input.IsActionPressed("ui_up"))
            {
                HandleUp();
                _lastMoveTime = now;
            }
            else if (Input.IsActionPressed("ui_down"))
            {
                HandleDown();
                _lastMoveTime = now;
            }
        }

        if (Input.IsActionJustPressed("ui_accept"))
        {
            if (_isOnBackButton || (backButton != null && backButton.HasFocus()))
                return;

            if (_musicItems.Length == 0)
                return;

            GameManager.Instance.CurrentTrack = GameManager.LoadedTracks[_selectedIndex];
            GetTree().ChangeSceneToPacked(Refs.Instance.GameScene);
        }
    }

    private void HandleUp()
    {
        if (_musicItems.Length == 0 || backButton == null)
            return;

        if (_isOnBackButton)
            return;

        _isOnBackButton = true;
        backButton.GrabFocus();
    }

    private void HandleDown()
    {
        if (_musicItems.Length == 0)
            return;

        if (!_isOnBackButton)
            return;

        _isOnBackButton = false;
        _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _musicItems.Length - 1);
        _musicItems[_selectedIndex].GrabFocus();
    }

    public void _on_back_pressed()
    {
        GetTree().ChangeSceneToPacked(Refs.Instance.MenuScene);
    }
}
