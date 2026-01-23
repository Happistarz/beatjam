using Godot;

public partial class PlayerTrack : Control
{
    [Export] public MusicData.PlayerRole Role;

    [Export] public TrackController TrackUI;
    [Export] public CharacterAnimation CharacterAnimation;

    public override void _Ready()
    {
        // Auto-wire if not set in the inspector
        if (TrackUI == null)
            TrackUI = FindChild("TrackUI", true, false) as TrackController;

        if (CharacterAnimation == null)
            CharacterAnimation = FindChild("CharacterAnimation", true, false) as CharacterAnimation;

        ApplyRole();
    }

    public void ApplyRole()
    {
        if (TrackUI != null)
            TrackUI.Role = Role;

        if (CharacterAnimation != null)
            CharacterAnimation.SetRole(Role);
    }
}
