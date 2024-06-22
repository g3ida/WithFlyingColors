namespace Wfc.Image;
using Godot;
using Wfc.Skin;


public partial class TextureGenRecipe(Texture2D texture, SkinColor color, SkinColorIntensity colorIntensity, ImageAlignment alignment) {

  public Texture2D Texture { get; } = texture;
  public SkinColor Color { get; } = color;
  public SkinColorIntensity ColorIntensity { get; } = colorIntensity;
  public ImageAlignment Alignment { get; } = alignment;
}