using System;
using Godot;

public partial class MusicData
{
  public enum DifficultyLevel
  {
    Easy,
    Medium,
    Hard
  }
  public enum PlayerRole
  {
    Guitar,
    Drums,
    Bass
  }

  public struct Player
  {
    public string Name;
    public PlayerRole Role;
  }

  public struct Note
  {
    public Refs.NoteType Type;
    public double Time; // in seconds
  }

  public string Title;
  public int BPM;
  public AudioStream MusicStream;
  public Texture2D CoverImage;
  public DifficultyLevel Difficulty;
  public Player[] Players;
  public int PlayerCount => Players.Length;
  public Note[] Notes;
  public int NoteCount => Notes.Length;
}