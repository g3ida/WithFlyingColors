using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerSpriteGenerator : Node
{
    public static readonly Vector2I SPRITE_SIZE = new Vector2I(96, 96);

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
                { "texture", GD.Load<Texture2D>("res://Assets/Sprites/Player/player-left.png") },
                { "color", "3-basic" },
                { "align", ImageAlignment.TopLeft }
            }
        },
        {
            "top", new Dictionary<string, object>
            {
                { "texture", GD.Load<Texture2D>("res://Assets/Sprites/Player/player-top.png") },
                { "color", "2-basic" },
                { "align", ImageAlignment.TopRight }
            }
        },
        {
            "bottom", new Dictionary<string, object>
            {
                { "texture", GD.Load<Texture2D>("res://Assets/Sprites/Player/player-bottom.png") },
                { "color", "0-basic" },
                { "align", ImageAlignment.BottomRight }
            }
        },
        {
            "right", new Dictionary<string, object>
            {
                { "texture", GD.Load<Texture2D>("res://Assets/Sprites/Player/player-right.png") },
                { "color", "1-basic" },
                { "align", ImageAlignment.TopRight }
            }
        },
        {
            "left-edge", new Dictionary<string, object>
            {
                { "texture", GD.Load<Texture2D>("res://Assets/Sprites/Player/player-left-edge.png") },
                { "color", "3-dark" },
                { "align", ImageAlignment.TopLeft }
            }
        },
        {
            "right-edge", new Dictionary<string, object>
            {
                { "texture", GD.Load<Texture2D>("res://Assets/Sprites/Player/player-right-edge.png") },
                { "color", "1-dark" },
                { "align", ImageAlignment.TopRight }
            }
        },
        {
            "bottom-edge", new Dictionary<string, object>
            {
                { "texture", GD.Load<Texture2D>("res://Assets/Sprites/Player/player-bottom-edge.png") },
                { "color", "0-dark" },
                { "align", ImageAlignment.BottomLeft }
            }
        },
        {
            "top-edge", new Dictionary<string, object>
            {
                { "texture", GD.Load<Texture2D>("res://Assets/Sprites/Player/player-top-edge.png") },
                { "color", "2-dark" },
                { "align", ImageAlignment.TopLeft }
            }
        }
    };

    public static Texture2D GetTexture()
    {
        if (Global.Instance().GetSelectedSkin() == null)
        {
            GD.PushError("There are no selected skin!!!");
            return null;
        }
        return MergeIntoSingleTexture();
    }

    private static Texture2D MergeIntoSingleTexture()
    {
        var image = MergeIntoSingleImage();
        // FIXME: flag filter is 4 ?? could be a bug introduced after c# migration ?
        return ImageTexture.CreateFromImage(image);//, (uint)Texture2D.FlagsEnum.Filter); Flag 5 corresponds to the FILTER flag, as FLAG_REPEAT is not needed
    }

    private static Image MergeIntoSingleImage()
    {
        var format = Image.Format.Rgba8;
        var image = Image.Create((int)SPRITE_SIZE.X, (int)SPRITE_SIZE.Y, false, format);
        image.Fill(new Color(0, 0, 0, 0));

        foreach (var entry in FACES_TEXTURES)
        {
            var value = entry.Value;
            var texture = (Texture2D)value["texture"];
            var color = ColorUtils.GetColor((string)value["color"]);
            var alignment = (ImageAlignment)value["align"];
            var img = CreateColoredCopyFromImage(texture.GetImage(), color);
            var pos = GetPositionFromAlignment(texture, alignment);
            ImageUtils.BlitTexture(image, img, pos);
        }
        return image;
    }

    private static Vector2I GetPositionFromAlignment(Texture2D texture, ImageAlignment alignment)
    {
        switch (alignment)
        {
            case ImageAlignment.TopLeft:
                return Vector2I.Zero;
            case ImageAlignment.TopRight:
                return new Vector2I(SPRITE_SIZE.X - texture.GetWidth(), 0);
            case ImageAlignment.BottomLeft:
                return new Vector2I(0, SPRITE_SIZE.Y - texture.GetHeight());
            case ImageAlignment.BottomRight:
                return SPRITE_SIZE - new Vector2I(texture.GetWidth(), texture.GetHeight());
            default:
                return Vector2I.Zero;
        }
    }

    private static Image CreateColoredCopyFromImage(Image srcImage, Color color)
    {
        int width = srcImage.GetWidth();
        int height = srcImage.GetHeight();
        var format = srcImage.GetFormat();
        var image = Image.Create(width, height, false, format);
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                var pix = srcImage.GetPixel(i, j);
                var col = new Color(color.R, color.G, color.B, pix.A);
                image.SetPixel(i, j, col);
            }
        }
        return image;
    }
}
