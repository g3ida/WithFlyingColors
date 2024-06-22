namespace Wfc.Entities.World.Player;
using Godot;
using Wfc.Image;
using Wfc.Skin;

public static class PlayerTextureGenerator {
  private const string BASE_TEXTURE_PATH = "res://Assets/Sprites/Player/";
  private static Vector2I _textureSize = new(96, 96);
  private static readonly TextureGenRecipe[] _recipes = [
    new TextureGenRecipe(
              GD.Load<Texture2D>($"{BASE_TEXTURE_PATH}player-left.png"),
              SkinColor.LeftFace,
              SkinColorIntensity.Basic,
              ImageAlignment.TopLeft
          ),
          new TextureGenRecipe(
              GD.Load<Texture2D>($"{BASE_TEXTURE_PATH}player-top.png"),
              SkinColor.TopFace,
              SkinColorIntensity.Basic,
              ImageAlignment.TopRight
          ),
          new TextureGenRecipe(
              GD.Load<Texture2D>($"{BASE_TEXTURE_PATH}player-bottom.png"),
              SkinColor.BottomFace,
              SkinColorIntensity.Basic,
              ImageAlignment.BottomRight
          ),
          new TextureGenRecipe(
              GD.Load<Texture2D>($"{BASE_TEXTURE_PATH}player-right.png"),
              SkinColor.RightFace,
              SkinColorIntensity.Basic,
              ImageAlignment.TopRight
          ),
          new TextureGenRecipe(
              GD.Load<Texture2D>($"{BASE_TEXTURE_PATH}player-left-edge.png"),
              SkinColor.LeftFace,
              SkinColorIntensity.Dark,
              ImageAlignment.TopLeft
          ),
          new TextureGenRecipe(
              GD.Load<Texture2D>($"{BASE_TEXTURE_PATH}player-right-edge.png"),
              SkinColor.RightFace,
              SkinColorIntensity.Dark,
              ImageAlignment.TopRight
          ),
          new TextureGenRecipe(
              GD.Load<Texture2D>($"{BASE_TEXTURE_PATH}player-bottom-edge.png"),
              SkinColor.BottomFace,
              SkinColorIntensity.Dark,
              ImageAlignment.BottomLeft
          ),
          new TextureGenRecipe(
              GD.Load<Texture2D>($"{BASE_TEXTURE_PATH}player-top-edge.png"),
              SkinColor.TopFace,
              SkinColorIntensity.Dark,
              ImageAlignment.TopLeft
          ),
  ];

  public static Texture2D GenerateTexture() => TextureGenerator.GenerateTexture(_textureSize, _recipes);
}