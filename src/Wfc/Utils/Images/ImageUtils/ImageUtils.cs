namespace Wfc.Utils.Images;

using System;
using Godot;

public static class ImageUtils {
  public static void BlitTexture(Image destImage, Image srcImage, Vector2I pos) {
    Vector2 bounds = new Vector2(destImage.GetWidth(), destImage.GetHeight());
    int srcBoundX = (int)Math.Min(pos.X + srcImage.GetWidth(), bounds.X);
    int srcBoundY = (int)Math.Min(pos.Y + srcImage.GetHeight(), bounds.Y);
    Rect2I srcRect = new Rect2I(Vector2I.Zero, new Vector2I(srcBoundX, srcBoundY));
    destImage.BlendRect(srcImage, srcRect, pos);
  }

  // 4 times percent slower than built-in blend function.
  // maybe it should be better implemented in C++
  public static void AlphaBlend(Image src, Image dst, Color color, Vector2I pos) {
    if (pos.X < 0 || pos.Y < 0)
      throw new ArgumentException("Expected pos coords to be positive");

    int width = Math.Min(src.GetWidth(), dst.GetWidth() - (int)pos.X);
    int height = Math.Min(src.GetHeight(), dst.GetHeight() - (int)pos.Y);

    for (int i = 0; i < width; i++) {
      for (int j = 0; j < height; j++) {
        Vector2I dstPos = new Vector2I(i + pos.X, j + pos.Y);
        Color srcPix = src.GetPixel(i, j);
        Color dstPix = dst.GetPixelv(dstPos);

        srcPix = new Color(color.R, color.G, color.B, srcPix.A); // Edit src color
        Color col = AlphaBlendColors(srcPix, dstPix);
        dst.SetPixelv(dstPos, col);
      }
    }
  }

  // Blending c1 on c2
  public static Color AlphaBlendColors(Color c1, Color c2) {
    Color c = new Color();
    float invC1A = 1 - c1.A;
    c.A = c1.A + c2.A * invC1A;
    if (c.A > 0.01) {
      c.R = (c1.R * c1.A + c2.R * c2.A * invC1A) / c.A;
      c.G = (c1.G * c1.A + c2.G * c2.A * invC1A) / c.A;
      c.B = (c1.B * c1.A + c2.B * c2.A * invC1A) / c.A;
    }
    return c;
  }
}
