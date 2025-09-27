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

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_musicItems = new MusicItem[3];
		for (int i = 0; i < 3; i++)
		{
			var item = Refs.Instance.MusicItemScene.Instantiate<MusicItem>();
			item.Initialize($"Song {i + 1}", $"{i + 1} Players", $"{120 + i * 10} BPM", $"Difficulty {i + 1}", Refs.Instance.DefaultCoverImage);
			musicList.AddChild(item);
			_musicItems[i] = item;
		}

		if (_musicItems.Length > 0)
		{
			CallDeferred(nameof(SetInitialSelection));
		}
	}

	private void SetInitialSelection()
	{
		_musicItems[0].SetSelected(true);
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
			if (backButton.HasFocus()) return;

			GD.Print($"Selected: {_musicItems[_selectedIndex].TitleLabel.Text}");
			// Transition to game scene with selected music
		}

	}

	private void HandleUp()
	{
		if (_isOnBackButton) return;

		if (_selectedIndex == 0)
		{
			// Move to back button from first music item
			_musicItems[_selectedIndex].SetSelected(false);
			backButton.GrabFocus();
			_isOnBackButton = true;
			return;
		}

		_musicItems[_selectedIndex].SetSelected(false);
		_selectedIndex--;
		_musicItems[_selectedIndex].SetSelected(true);
		_musicItems[_selectedIndex].GrabFocus();
	}

	private void HandleDown()
	{
		if (_isOnBackButton)
		{
			// Move to first music item from back button
			_selectedIndex = 0;
			_musicItems[_selectedIndex].SetSelected(true);
			_musicItems[_selectedIndex].GrabFocus();
			_isOnBackButton = false;
			return;
		}

		_musicItems[_selectedIndex].SetSelected(false);
		_selectedIndex = (_selectedIndex + 1) % _musicItems.Length;
		_musicItems[_selectedIndex].SetSelected(true);
		_musicItems[_selectedIndex].GrabFocus();
	}

	private void FocusNext(Control current)
	{
		int index = GetIndex(current);
		int next = (index + 1) % musicList.GetChildCount();
		if (musicList.GetChild(next) is Control nextControl)
			nextControl.GrabFocus();
	}

	private void FocusPrevious(Control current)
	{
		int index = GetIndex(current);
		int prev = (index - 1 + musicList.GetChildCount()) % musicList.GetChildCount();
		if (musicList.GetChild(prev) is Control prevControl)
			prevControl.GrabFocus();
	}

	private int GetIndex(Node node)
	{
		for (int i = 0; i < musicList.GetChildCount(); i++)
		{
			if (musicList.GetChild(i) == node)
				return i;
		}
		return -1;
	}

	public void _on_back_pressed()
	{
		GetTree().ChangeSceneToPacked(Refs.Instance.MenuScene);
	}
}
