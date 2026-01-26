using Godot;
using System;

public partial class ButtonAudio : Button 
{
	protected AudioManager _audioManager;
	
	private bool _peutFaireDuSon = false; 

	[Export] public AudioStream SonFocusSpecifique { get; set; }
	[Export] public AudioStream SonClickSpecifique { get; set; }

	public override async void _Ready() // <--- Ajoute 'async' ici
	{
		_audioManager = GetNode<AudioManager>("/root/AudioManager");

		FocusEntered += OnFocusEntered;
		MouseEntered += OnFocusEntered;
		Pressed += OnButtonPressed;

		await ToSignal(GetTree().CreateTimer(0.15f), SceneTreeTimer.SignalName.Timeout);
		
		_peutFaireDuSon = true; 
	}

	private void OnFocusEntered()
	{
		if (_peutFaireDuSon) 
		{
			_audioManager.JouerSonFocus(SonFocusSpecifique);
		}
	}

	protected virtual void OnButtonPressed()
	{
		_audioManager.JouerSonClick(SonClickSpecifique);
	}
}
