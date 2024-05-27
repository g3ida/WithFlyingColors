using Godot;
using System;

public static class ImageUtils
{
    public static void BlitTexture(Image destImage, Image srcImage, Vector2 pos)
    {
        Vector2 bounds = new Vector2(destImage.GetWidth(), destImage.GetHeight());
        float srcBoundX = Math.Min(pos.x + srcImage.GetWidth(), bounds.x);
        float srcBoundY = Math.Min(pos.y + srcImage.GetHeight(), bounds.y);
        Rect2 srcRect = new Rect2(Vector2.Zero, new Vector2(srcBoundX, srcBoundY));
        destImage.BlendRect(srcImage, srcRect, pos);
    }

    // 4 times percent slower than built-in blend function.
    // maybe it should be better implemented in C++
    public static void AlphaBlend(Image src, Image dst, Color color, Vector2 pos)
    {
        if (pos.x < 0 || pos.y < 0)
            throw new ArgumentException("Expected pos coords to be positive");

        int width = Math.Min(src.GetWidth(), dst.GetWidth() - (int)pos.x);
        int height = Math.Min(src.GetHeight(), dst.GetHeight() - (int)pos.y);
        
        src.Lock();
        dst.Lock();

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Vector2 dstPos = new Vector2(i + pos.x, j + pos.y);
                Color srcPix = src.GetPixel(i, j);
                Color dstPix = dst.GetPixelv(dstPos);
                
                srcPix = new Color(color.r, color.g, color.b, srcPix.a); // Edit src color
                Color col = AlphaBlendColors(srcPix, dstPix);
                dst.SetPixelv(dstPos, col);
            }
        }

        dst.Unlock();
        src.Unlock();
    }

    // Blending c1 on c2
    public static Color AlphaBlendColors(Color c1, Color c2)
    {
        Color c = new Color();
        float invC1A = 1 - c1.a;
        c.a = c1.a + c2.a * invC1A;
        if (c.a > 0.01)
        {
            c.r = (c1.r * c1.a + c2.r * c2.a * invC1A) / c.a;
            c.g = (c1.g * c1.a + c2.g * c2.a * invC1A) / c.a;
            c.b = (c1.b * c1.a + c2.b * c2.a * invC1A) / c.a;
        }
        return c;
    }
}
