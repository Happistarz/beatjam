using Godot;
using System;

public partial class AudioManager : Node
{
	private AudioStreamPlayer _audioFocus;
	private AudioStreamPlayer _audioClick;

	public override void _Ready()
	{
		// Récupération des deux lecteurs enfants
		_audioFocus = GetNode<AudioStreamPlayer>("AudioFocus");
		_audioClick = GetNode<AudioStreamPlayer>("AudioClick");
	}

	// Fonction pour le survol
	public void JouerSonFocus(AudioStream nouveauSon = null)
	{
		if (nouveauSon != null)
		{
			_audioFocus.Stream = nouveauSon;
		}
		_audioFocus.Play();
	}

	// Fonction pour le clic
	public void JouerSonClick(AudioStream nouveauSon = null)
	{
		if (nouveauSon != null)
		{
			_audioClick.Stream = nouveauSon;
		}
		_audioClick.Play();
	}
}
