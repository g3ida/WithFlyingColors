namespace Wfc.Image;
using System.Collections.Generic;
using Godot;
using Wfc.Skin;

public static partial class TextureGenerator {

  public static Texture2D GenerateTexture(Vector2I outTextureSize, IEnumerable<TextureGenRecipe> recipe) {
    if (SkinManager.Instance.CurrentSkin == null) {
      GD.PushError("There are no selected skin!!!");
    }
    return TransformRecipeIntoTexture(outTextureSize, recipe);
  }

  private static ImageTexture TransformRecipeIntoTexture(Vector2I outTextureSize, IEnumerable<TextureGenRecipe> recipe) {
    var image = TransformRecipeIntoImage(outTextureSize, recipe);
    return ImageTexture.CreateFromImage(image);
  }

  private static Image TransformRecipeIntoImage(Vector2I outTextureSize, IEnumerable<TextureGenRecipe> recipe) {
    var format = Image.Format.Rgba8;
    var image = Image.Create(outTextureSize.X, outTextureSize.Y, false, format);
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

  private static Image CreateColoredCopyFromImage(Image srcImage, Color color) {
    var width = srcImage.GetWidth();
    var height = srcImage.GetHeight();
    var format = srcImage.GetFormat();
    var image = Image.CreateEmpty(width, height, false, format);
    for (var i = 0; i < width; i++) {
      for (var j = 0; j < height; j++) {
        var pix = srcImage.GetPixel(i, j);
        var col = new Color(color.R, color.G, color.B, pix.A);
        image.SetPixel(i, j, col);
      }
    }
    return image;
  }
}
