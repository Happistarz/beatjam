using Godot;

public partial class Background : Sprite2D
{
    public override void _Ready()
    {
        // Create a 1x1 white texture
       	var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
        image.Fill(Colors.White);

        var texture = ImageTexture.CreateFromImage(image);
        Texture = texture;

        ResizeToViewport();
    }

    private void ResizeToViewport()
    {
        // Resize sprite to cover the viewport
        var viewportSize = GetViewportRect().Size;
        var textureSize = Texture.GetSize();

        Scale = viewportSize / textureSize;
        Position = Vector2.Zero;
        Centered = false;
    }
}
