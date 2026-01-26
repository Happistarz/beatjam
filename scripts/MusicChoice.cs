using Godot;

public partial class MusicChoice : CanvasLayer
{
    [Export] private VBoxContainer musicList;
    [Export] private Button backButton;

    private bool _isOnBackButton = false;
    private int _selectedIndex = 0;
    private MusicItem[] _musicItems;

    private double _lastMoveTime = 0;
    private const double MoveDelay = 0.2;

    public override void _Ready()
    {
        _musicItems = new MusicItem[GameManager.LoadedTracks.Count];

        for (int i = 0; i < GameManager.LoadedTracks.Count; i++)
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

            musicList.AddChild(item);
            _musicItems[i] = item;
        }

        if (_musicItems.Length > 0)
            CallDeferred(nameof(SetInitialSelection));
        else
            backButton?.GrabFocus();
    }

    private void SetInitialSelection()
    {
        _selectedIndex = 0;
        _isOnBackButton = false;
        _musicItems[0].GrabFocus();
    }

    public override void _Process(double delta)
    {
        double now = Time.GetTicksMsec() / 1000.0;

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

            GD.Print($"Selected: {_musicItems[_selectedIndex].TitleLabel.Text}");

            // Set the current track in GameManager so the Game scene knows what to play
            GameManager.Instance.CurrentTrack = GameManager.LoadedTracks[_selectedIndex];

            GetTree().ChangeSceneToPacked(Refs.Instance.GameScene);
        }
    }

    private void HandleUp()
    {
        if (_musicItems == null || _musicItems.Length == 0)
            return;

        if (_isOnBackButton)
            return;

        if (_selectedIndex == 0)
        {
            backButton?.GrabFocus();
            _isOnBackButton = true;
            return;
        }

        _selectedIndex--;
        _musicItems[_selectedIndex].GrabFocus();
    }

    private void HandleDown()
    {
        if (_musicItems == null || _musicItems.Length == 0)
            return;

        if (_isOnBackButton)
        {
            _selectedIndex = 0;
            _isOnBackButton = false;
            _musicItems[_selectedIndex].GrabFocus();
            return;
        }

        _selectedIndex = (_selectedIndex + 1) % _musicItems.Length;
        _musicItems[_selectedIndex].GrabFocus();
    }

    public void _on_back_pressed()
    {
        GetTree().ChangeSceneToPacked(Refs.Instance.MenuScene);
    }
}
