using Godot;
using Wfc.Image;

namespace Wfc.Entities.Ui.Menubox
{
  public static class MenuboxTextureGenerator
  {
    const string BASE_TEXURE_PATH = "res://Assets/Sprites/Menu/Menubox/";
    private static Vector2I _s_textureSize = new Vector2I(936, 936);
    private static TextureGenRecepie[] _s_recepies = [

        new TextureGenRecepie(
            GD.Load<Texture2D>($"{BASE_TEXURE_PATH}menu-box-left-face.png"),
            Skin.SkinColor.LeftFace,
            Skin.SkinColorIntensity.Basic,
            ImageAlignment.MiddleLeft
        ),
        new TextureGenRecepie(
            GD.Load<Texture2D>($"{BASE_TEXURE_PATH}menu-box-top-face.png"),
            Skin.SkinColor.TopFace,
            Skin.SkinColorIntensity.Basic,
            ImageAlignment.TopRight
        ),
        new TextureGenRecepie(
            GD.Load<Texture2D>($"{BASE_TEXURE_PATH}menu-box-bottom-face.png"),
            Skin.SkinColor.BottomFace,
            Skin.SkinColorIntensity.Basic,
            ImageAlignment.BottomRight
        ),
        new TextureGenRecepie(
            GD.Load<Texture2D>($"{BASE_TEXURE_PATH}menu-box-right-face.png"),
            Skin.SkinColor.RightFace,
            Skin.SkinColorIntensity.Basic,
            ImageAlignment.MiddleRight
        ),
        new TextureGenRecepie(
            GD.Load<Texture2D>($"{BASE_TEXURE_PATH}menu-box-left-edge.png"),
            Skin.SkinColor.LeftFace,
            Skin.SkinColorIntensity.Dark,
            ImageAlignment.MiddleLeft
        ),
        new TextureGenRecepie(
            GD.Load<Texture2D>($"{BASE_TEXURE_PATH}menu-box-right-edge.png"),
            Skin.SkinColor.RightFace,
            Skin.SkinColorIntensity.Dark,
            ImageAlignment.MiddleRight
        ),
        new TextureGenRecepie(
            GD.Load<Texture2D>($"{BASE_TEXURE_PATH}menu-box-bottom-edge.png"),
            Skin.SkinColor.BottomFace,
            Skin.SkinColorIntensity.Dark,
            ImageAlignment.BottomCenter
        ),
        new TextureGenRecepie(
            GD.Load<Texture2D>($"{BASE_TEXURE_PATH}menu-box-top-edge.png"),
            Skin.SkinColor.TopFace,
            Skin.SkinColorIntensity.Dark,
            ImageAlignment.TopCenter
        ),
    ];

    public static Texture2D GenerateTexture()
    {
      return TextureGenerator.GenerateTexture(_s_textureSize, _s_recepies);
    }
  }
}