using Godot;
using System.Diagnostics;

public static class NinePatchTextureUtils
{
    public static void ScaleTexture(NinePatchRect ninePatchRect, Vector2 scale)
    {
        Debug.Assert(scale.X >= 1 && scale.Y >= 1);
        ninePatchRect.Scale = new Vector2(1 / scale.X, 1 / scale.Y);
        Vector2 size = ninePatchRect.Size;

        Vector2 newSize = new Vector2(size.X * scale.X, size.Y * scale.Y);
        ninePatchRect.Size = new Vector2(newSize.X, newSize.Y);

        Vector2 rectPos = new Vector2(
            ninePatchRect.Position.X - (newSize.X - size.X) / (scale.X * 2),
            ninePatchRect.Position.Y - (newSize.Y - size.Y) / (scale.Y * 2));
        ninePatchRect.Position = rectPos;
    }

    public static void SetTexture(NinePatchRect ninePatchRect, Texture2D texture)
    {
        ninePatchRect.Texture = texture;
    }
}
