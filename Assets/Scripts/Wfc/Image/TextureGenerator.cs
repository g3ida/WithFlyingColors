using System.Collections.Generic;
using Godot;
using Wfc.Skin;

namespace Wfc.Image
{
  public static partial class TextureGenerator
  {

    public static Texture2D GenerateTexture(Vector2I outTextureSize, IEnumerable<TextureGenRecepie> recepie)
    {
      if (SkinManager.Instance.CurrentSkin == null)
      {
        GD.PushError("There are no selected skin!!!");
      }
      return TransformRecepieIntoTexture(outTextureSize, recepie);
    }

    private static Texture2D TransformRecepieIntoTexture(Vector2I outTextureSize, IEnumerable<TextureGenRecepie> recepie)
    {
      var image = TransformRecepieIntoImage(outTextureSize, recepie);
      return ImageTexture.CreateFromImage(image);
    }

    private static Godot.Image TransformRecepieIntoImage(Vector2I outTextureSize, IEnumerable<TextureGenRecepie> recepie)
    {
      var format = Godot.Image.Format.Rgba8;
      var image = Godot.Image.Create(outTextureSize.X, outTextureSize.Y, false, format);
      var skin = SkinManager.Instance.CurrentSkin;
      image.Fill(new Color(0, 0, 0, 0)); // Transparent background

      foreach (var ingredient in recepie)
      {
        var texture = (Texture2D)ingredient.Texture;
        var color = skin.GetColor(ingredient.Color, ingredient.ColorIntensity);
        var alignment = ingredient.Alignment;
        var img = CreateColoredCopyFromImage(texture.GetImage(), color);
        var pos = GetPositionFromAlignment(texture, alignment, outTextureSize);
        ImageUtils.BlitTexture(image, img, pos);
      }
      return image;
    }

    private static Vector2I GetPositionFromAlignment(Texture2D texture, ImageAlignment alignment, Vector2I outTextureSize)
    {
      var inTextureSize = new Vector2I(texture.GetWidth(), texture.GetHeight());
      switch (alignment)
      {
        case ImageAlignment.TopLeft:
          return Vector2I.Zero;
        case ImageAlignment.TopRight:
          return new Vector2I(outTextureSize.X - inTextureSize.X, 0);
        case ImageAlignment.BottomLeft:
          return new Vector2I(0, outTextureSize.Y - inTextureSize.Y);
        case ImageAlignment.BottomRight:
          return outTextureSize - inTextureSize;
        case ImageAlignment.TopCenter:
          return new Vector2I(outTextureSize.X / 2 - inTextureSize.X / 2, 0);
        case ImageAlignment.BottomCenter:
          return new Vector2I(outTextureSize.X / 2 - inTextureSize.X / 2, outTextureSize.Y - inTextureSize.Y);
        case ImageAlignment.MiddleLeft:
          return new Vector2I(0, outTextureSize.Y / 2 - texture.GetHeight() / 2);
        case ImageAlignment.MiddleRight:
          return new Vector2I(outTextureSize.X - inTextureSize.X, outTextureSize.Y / 2 - inTextureSize.Y / 2);
        case ImageAlignment.MiddleCenter:
          return outTextureSize / 2 - inTextureSize / 2;
        default:
          return Vector2I.Zero;
      }
    }

    private static Godot.Image CreateColoredCopyFromImage(Godot.Image srcImage, Godot.Color color)
    {
      int width = srcImage.GetWidth();
      int height = srcImage.GetHeight();
      var format = srcImage.GetFormat();
      var image = Godot.Image.Create(width, height, false, format);
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
}