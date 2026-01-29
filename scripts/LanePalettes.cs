using Godot;

public static class LanePalettes
{
    // Godot Color uses 0..1 floats (not 0..255)
    public static readonly Color[] Drums =
    {
        new Color(0f, 0f, 0f),
        new Color(1f, 0.8f, 0f), // yellow
        new Color(1f, 0f, 0f), // red
    };

    public static readonly Color[] Guitar =
    {
        new Color(1f, 0f, 0f), // red
        new Color(0f, 1f, 0.5f), // green
        new Color(0f, 0f, 1f), // blue
    };

    public static readonly Color[] Keyboard =
    {
        new Color(0f, 0f, 0f),
        new Color(0f, 0f, 1f), // blue
        new Color(0f, 1f, 0.5f), // green
    };

    public static Color[] ForRole(MusicData.PlayerRole role)
    {
        return role switch
        {
            MusicData.PlayerRole.Drums => Drums,
            MusicData.PlayerRole.Guitar => Guitar,
            MusicData.PlayerRole.Keyboard => Keyboard,
            _ => Drums
        };
    }
}
