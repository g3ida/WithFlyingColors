using Godot;
using Wfc.Skin;

namespace Wfc.Image {
  public partial class TextureGenRecipe {

    public Texture2D Texture { get; }
    public SkinColor Color { get; }
    public SkinColorIntensity ColorIntensity { get; }
    public ImageAlignment Alignment { get; }

    public TextureGenRecipe(Texture2D texture, SkinColor Color, SkinColorIntensity colorIntensity, ImageAlignment alignment) {
      Texture = texture;
      this.Color = Color;
      ColorIntensity = colorIntensity;
      Alignment = alignment;
    }
  }
}