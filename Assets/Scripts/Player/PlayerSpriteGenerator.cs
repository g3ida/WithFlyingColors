using Godot;
using System;
using System.Collections.Generic;

public class PlayerSpriteGenerator : Node
{
    public static readonly Vector2 SPRITE_SIZE = new Vector2(96, 96);

    public enum ImageAlignment
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public static readonly Dictionary<string, Dictionary<string, object>> FACES_TEXTURES = new Dictionary<string, Dictionary<string, object>>
    {
        {
            "left", new Dictionary<string, object>
            {
                { "texture", GD.Load<Texture>("res://Assets/Sprites/Player/player-left.png") },
                { "color", "3-basic" },
                { "align", ImageAlignment.TopLeft }
            }
        },
        {
            "top", new Dictionary<string, object>
            {
                { "texture", GD.Load<Texture>("res://Assets/Sprites/Player/player-top.png") },
                { "color", "2-basic" },
                { "align", ImageAlignment.TopRight }
            }
        },
        {
            "bottom", new Dictionary<string, object>
            {
                { "texture", GD.Load<Texture>("res://Assets/Sprites/Player/player-bottom.png") },
                { "color", "0-basic" },
                { "align", ImageAlignment.BottomRight }
            }
        },
        {
            "right", new Dictionary<string, object>
            {
                { "texture", GD.Load<Texture>("res://Assets/Sprites/Player/player-right.png") },
                { "color", "1-basic" },
                { "align", ImageAlignment.TopRight }
            }
        },
        {
            "left-edge", new Dictionary<string, object>
            {
                { "texture", GD.Load<Texture>("res://Assets/Sprites/Player/player-left-edge.png") },
                { "color", "3-dark" },
                { "align", ImageAlignment.TopLeft }
            }
        },
        {
            "right-edge", new Dictionary<string, object>
            {
                { "texture", GD.Load<Texture>("res://Assets/Sprites/Player/player-right-edge.png") },
                { "color", "1-dark" },
                { "align", ImageAlignment.TopRight }
            }
        },
        {
            "bottom-edge", new Dictionary<string, object>
            {
                { "texture", GD.Load<Texture>("res://Assets/Sprites/Player/player-bottom-edge.png") },
                { "color", "0-dark" },
                { "align", ImageAlignment.BottomLeft }
            }
        },
        {
            "top-edge", new Dictionary<string, object>
            {
                { "texture", GD.Load<Texture>("res://Assets/Sprites/Player/player-top-edge.png") },
                { "color", "2-dark" },
                { "align", ImageAlignment.TopLeft }
            }
        }
    };

    public static Texture GetTexture()
    {
        if (Global.Instance().GetSelectedSkin() == null)
        {
            GD.PushError("There are no selected skin!!!");
            return null;
        }
        return MergeIntoSingleTexture();
    }

    private static Texture MergeIntoSingleTexture()
    {
        var imageTexture = new ImageTexture();
        var image = MergeIntoSingleImage();
        imageTexture.CreateFromImage(image, (uint)Texture.FlagsEnum.Filter); // Flag 5 corresponds to the FILTER flag, as FLAG_REPEAT is not needed
        return imageTexture;
    }

    private static Image MergeIntoSingleImage()
    {
        var image = new Image();
        var format = Image.Format.Rgba8;
        image.Create((int)SPRITE_SIZE.x, (int)SPRITE_SIZE.y, false, format);
        image.Fill(new Color(0, 0, 0, 0));

        foreach (var entry in FACES_TEXTURES)
        {
            var value = entry.Value;
            var texture = (Texture)value["texture"];
            var color = ColorUtils.GetColor((string)value["color"]);
            var alignment = (ImageAlignment)value["align"];
            var img = CreateColoredCopyFromImage(texture.GetData(), color);
            var pos = GetPositionFromAlignment(texture, alignment);
            ImageUtils.BlitTexture(image, img, pos);
        }
        return image;
    }

    private static Vector2 GetPositionFromAlignment(Texture texture, ImageAlignment alignment)
    {
        switch (alignment)
        {
            case ImageAlignment.TopLeft:
                return Vector2.Zero;
            case ImageAlignment.TopRight:
                return new Vector2(SPRITE_SIZE.x - texture.GetWidth(), 0);
            case ImageAlignment.BottomLeft:
                return new Vector2(0, SPRITE_SIZE.y - texture.GetHeight());
            case ImageAlignment.BottomRight:
                return SPRITE_SIZE - new Vector2(texture.GetWidth(), texture.GetHeight());
            default:
                return Vector2.Zero;
        }
    }

    private static Image CreateColoredCopyFromImage(Image srcImage, Color color)
    {
        int width = srcImage.GetWidth();
        int height = srcImage.GetHeight();
        var image = new Image();
        var format = srcImage.GetFormat();
        image.Create(width, height, false, format);
        srcImage.Lock();
        image.Lock();

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                var pix = srcImage.GetPixel(i, j);
                var col = new Color(color.r, color.g, color.b, pix.a);
                image.SetPixel(i, j, col);
            }
        }

        srcImage.Unlock();
        image.Unlock();
        return image;
    }
}
