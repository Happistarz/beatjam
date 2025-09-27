using Godot;

public partial class MusicChoice : CanvasLayer
{

	[Export] private VBoxContainer musicList;
	[Export] private Button backButton;

	private bool isOnBackButton = false;
	private int selectedIndex = 0;
	private MusicItem[] musicItems;

	private double _lastMoveTime = 0;
	private const double MoveDelay = 0.2;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		musicItems = new MusicItem[3];
		for (int i = 0; i < 3; i++)
		{
			var item = Refs.Instance.MusicItemScene.Instantiate<MusicItem>();
			item.Initialize($"Song {i + 1}", $"{i + 1} Players", $"{120 + i * 10} BPM", $"Difficulty {i + 1}", null);
			musicList.AddChild(item);
			musicItems[i] = item;
		}

		if (musicItems.Length > 0)
		{
			CallDeferred(nameof(SetInitialSelection));
		}
	}

	private void SetInitialSelection()
	{
		musicItems[0].SetSelected(true);
		musicItems[0].GrabFocus();
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

			GD.Print($"Selected: {musicItems[selectedIndex].TitleLabel.Text}");
			// Transition to game scene with selected music
		}

	}

	private void HandleUp()
	{
		if (isOnBackButton) return;

		if (selectedIndex == 0)
		{
			// Move to back button from first music item
			musicItems[selectedIndex].SetSelected(false);
			backButton.GrabFocus();
			isOnBackButton = true;
			return;
		}

		musicItems[selectedIndex].SetSelected(false);
		selectedIndex--;
		musicItems[selectedIndex].SetSelected(true);
		musicItems[selectedIndex].GrabFocus();
	}

	private void HandleDown()
	{
		if (isOnBackButton)
		{
			// Move to first music item from back button
			selectedIndex = 0;
			musicItems[selectedIndex].SetSelected(true);
			musicItems[selectedIndex].GrabFocus();
			isOnBackButton = false;
			return;
		}

		musicItems[selectedIndex].SetSelected(false);
		selectedIndex = (selectedIndex + 1) % musicItems.Length;
		musicItems[selectedIndex].SetSelected(true);
		musicItems[selectedIndex].GrabFocus();
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
