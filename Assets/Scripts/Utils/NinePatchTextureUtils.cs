using Godot;
using System.Diagnostics;

public class NinePatchTextureUtils : Node
{
    public /*static*/ void ScaleTexture(NinePatchRect ninePatchRect, Vector2 scale)
    {
        Debug.Assert(scale.x >= 1 && scale.y >= 1);
        ninePatchRect.RectScale = new Vector2(1 / scale.x, 1 / scale.y);
        Vector2 size = ninePatchRect.RectSize;

        Vector2 newSize = new Vector2(size.x * scale.x, size.y * scale.y);
        ninePatchRect.RectSize = new Vector2(newSize.x, newSize.y);

        Vector2 rectPos = new Vector2(
            ninePatchRect.RectPosition.x - (newSize.x - size.x) / (scale.x * 2),
            ninePatchRect.RectPosition.y - (newSize.y - size.y) / (scale.y * 2));
        ninePatchRect.RectPosition = rectPos;
    }

    public /*static*/ void SetTexture(NinePatchRect ninePatchRect, Texture texture)
    {
        ninePatchRect.Texture = texture;
    }
}
