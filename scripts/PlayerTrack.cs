using Godot;

public partial class PlayerTrack : Control
{
    [Export] public MusicData.PlayerRole Role;

    [Export] public TrackController TrackUI;
    [Export] public CharacterAnimation CharacterAnimation;

    [Export] public NotesEffect Notes;
    [Export] public ReactionEffect Reaction;

    public override void _Ready()
    {
        // Auto-wire if not set in the inspector
        if (TrackUI == null)
            TrackUI = FindChild("TrackUI", true, false) as TrackController;

        if (CharacterAnimation == null)
            CharacterAnimation = FindChild("CharacterAnimation", true, false) as CharacterAnimation;

        if (Notes == null)
            Notes = FindChild("Notes", true, false) as NotesEffect;

        if (Reaction == null)
            Reaction = FindChild("Reaction", true, false) as ReactionEffect;

        ApplyRole();

        // Subscribe to character state changes to start/stop notes animation
        if (CharacterAnimation != null)
            CharacterAnimation.StateChanged += OnCharacterStateChanged;

        // Apply initial state
        if (CharacterAnimation != null)
            OnCharacterStateChanged((int)CharacterAnimation.CurrentState);

        Label scoreLabel = null;
        Label comboLabel = null;

        var scoreContainer = FindChild("ScoreContainer", true, false) as TextureRect;
        if (scoreContainer != null)
        {
            scoreLabel = scoreContainer.FindChild("ScoreLabel", true, false) as Label;
            comboLabel = scoreContainer.FindChild("ComboLabel", true, false) as Label;
        }

        if (scoreLabel == null)
            GD.PrintErr($"PlayerTrack: ScoreLabel not found for role {Role}");

        if (comboLabel == null)
            GD.PrintErr($"PlayerTrack: ComboLabel not found for role {Role}");

        GD.Print($"Listing children for PlayerTrack with role {Role}:");
        foreach (Node child in GetChildren())
        {
            GD.Print($"Child: {child.Name}, Type: {child.GetType()}");
        }

        ScoreController.Instance?.RegisterTrack(Role, scoreLabel, comboLabel);
    }

    public void ApplyRole()
    {
        if (TrackUI != null)
            TrackUI.Role = Role;

        if (CharacterAnimation != null)
            CharacterAnimation.SetRole(Role);
    }

    // Called by HitZoneController when a note is judged (Perfect/Great/Miss)
    public void OnJudgement(Refs.Accuracy accuracy)
    {
        if (CharacterAnimation == null)
            return;

        // Update character animation/state first
        CharacterAnimation.OnJudgement(accuracy);

        // Reaction depends on resulting state
        if (Reaction != null)
            Reaction.Play(CharacterAnimation.CurrentState);
    }

    // Receives int because Godot signals use Variant
    private void OnCharacterStateChanged(int state)
    {
        var s = (CharacterAnimation.CharacterState)state;

        if (Notes == null)
            return;

        if (s == CharacterAnimation.CharacterState.Grooving || s == CharacterAnimation.CharacterState.Hyped)
            Notes.Play();
        else
            Notes.Stop();
    }
}
