namespace Wfc.Image;

using System.Collections.Generic;
using Godot;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Images;

public static partial class TextureGenerator {

  public static Texture2D GenerateTexture(Vector2I outTextureSize, IEnumerable<TextureGenRecipe> recipe) =>
    TransformRecipeIntoTexture(outTextureSize, recipe);

  private static ImageTexture TransformRecipeIntoTexture(Vector2I outTextureSize, IEnumerable<TextureGenRecipe> recipe) {
    var image = TransformRecipeIntoImage(outTextureSize, recipe);
    return ImageTexture.CreateFromImage(image);
  }

  private static Image TransformRecipeIntoImage(Vector2I outTextureSize, IEnumerable<TextureGenRecipe> recipe) {
    var format = Image.Format.Rgba8;
    var image = Image.CreateEmpty(outTextureSize.X, outTextureSize.Y, false, format);
    var skin = SkinManager.Instance.CurrentSkin;
    image.Fill(new Color(0, 0, 0, 0)); // Transparent background

    foreach (var ingredient in recipe) {
      var texture = ingredient.Texture;
      var color = skin.GetColor(ingredient.Color, ingredient.ColorIntensity);
      var alignment = ingredient.Alignment;
      var img = CreateColoredCopyFromImage(texture.GetImage(), color);
      var pos = GetPositionFromAlignment(texture, alignment, outTextureSize);
      ImageUtils.BlitTexture(image, img, pos);
    }
    return image;
  }

  private static Vector2I GetPositionFromAlignment(Texture2D texture, ImageAlignment alignment, Vector2I outTextureSize) {
    var inTextureSize = new Vector2I(texture.GetWidth(), texture.GetHeight());
    return alignment switch {
      ImageAlignment.TopLeft => Vector2I.Zero,
      ImageAlignment.TopRight => new Vector2I(outTextureSize.X - inTextureSize.X, 0),
      ImageAlignment.BottomLeft => new Vector2I(0, outTextureSize.Y - inTextureSize.Y),
      ImageAlignment.BottomRight => outTextureSize - inTextureSize,
      ImageAlignment.TopCenter => new Vector2I((outTextureSize.X / 2) - (inTextureSize.X / 2), 0),
      ImageAlignment.BottomCenter => new Vector2I((outTextureSize.X / 2) - (inTextureSize.X / 2), outTextureSize.Y - inTextureSize.Y),
      ImageAlignment.MiddleLeft => new Vector2I(0, (outTextureSize.Y / 2) - (texture.GetHeight() / 2)),
      ImageAlignment.MiddleRight => new Vector2I(outTextureSize.X - inTextureSize.X, (outTextureSize.Y / 2) - (inTextureSize.Y / 2)),
      ImageAlignment.MiddleCenter => (outTextureSize / 2) - (inTextureSize / 2),
      _ => Vector2I.Zero,
    };
  }

  // Keeps the source alpha and replaces every RGB with one flat color.
  //
  // One buffer round trip rather than a GetPixel/SetPixel pair per pixel: the menu box alone is
  // eight PNGs totalling just over two million pixels, so the old form crossed into the engine
  // about four million times, on the main thread, every time the box was rebuilt.
  private static Image CreateColoredCopyFromImage(Image srcImage, Color color) {
    var source = srcImage;
    if (source.GetFormat() != Image.Format.Rgba8) {
      // BlendRect refuses to mix formats, and the destination is always Rgba8.
      source = (Image)srcImage.Duplicate();
      source.Convert(Image.Format.Rgba8);
    }

    var data = source.GetData();
    // Truncating, which is what Image.SetPixel does for an 8-bit channel.
    var r = (byte)Mathf.Clamp(color.R * 255.0f, 0.0f, 255.0f);
    var g = (byte)Mathf.Clamp(color.G * 255.0f, 0.0f, 255.0f);
    var b = (byte)Mathf.Clamp(color.B * 255.0f, 0.0f, 255.0f);
    for (var i = 0; i < data.Length; i += 4) {
      data[i] = r;
      data[i + 1] = g;
      data[i + 2] = b;
    }

    return Image.CreateFromData(source.GetWidth(), source.GetHeight(), false, Image.Format.Rgba8, data);
  }
}
