namespace Wfc.Entities.Ui.Menubox;
using Godot;
using Wfc.Image;
using Wfc.Skin;

public static class MenuboxTextureGenerator {
  private const string BASE_TEXTURE_PATH = "res://Assets/Sprites/Menu/Menubox/";
  private static Vector2I _textureSize = new(936, 936);
  private static readonly TextureGenRecipe[] _recipes = [

      new TextureGenRecipe(
          GD.Load<Texture2D>($"{BASE_TEXTURE_PATH}menu-box-left-face.png"),
          SkinColor.LeftFace,
          SkinColorIntensity.Basic,
          ImageAlignment.MiddleLeft
      ),
      new TextureGenRecipe(
          GD.Load<Texture2D>($"{BASE_TEXTURE_PATH}menu-box-top-face.png"),
          SkinColor.TopFace,
          SkinColorIntensity.Basic,
          ImageAlignment.TopRight
      ),
      new TextureGenRecipe(
          GD.Load<Texture2D>($"{BASE_TEXTURE_PATH}menu-box-bottom-face.png"),
          SkinColor.BottomFace,
          SkinColorIntensity.Basic,
          ImageAlignment.BottomRight
      ),
      new TextureGenRecipe(
          GD.Load<Texture2D>($"{BASE_TEXTURE_PATH}menu-box-right-face.png"),
          SkinColor.RightFace,
          SkinColorIntensity.Basic,
          ImageAlignment.MiddleRight
      ),
      new TextureGenRecipe(
          GD.Load<Texture2D>($"{BASE_TEXTURE_PATH}menu-box-left-edge.png"),
          SkinColor.LeftFace,
          SkinColorIntensity.Dark,
          ImageAlignment.MiddleLeft
      ),
      new TextureGenRecipe(
          GD.Load<Texture2D>($"{BASE_TEXTURE_PATH}menu-box-right-edge.png"),
          SkinColor.RightFace,
          SkinColorIntensity.Dark,
          ImageAlignment.MiddleRight
      ),
      new TextureGenRecipe(
          GD.Load<Texture2D>($"{BASE_TEXTURE_PATH}menu-box-bottom-edge.png"),
          SkinColor.BottomFace,
          SkinColorIntensity.Dark,
          ImageAlignment.BottomCenter
      ),
      new TextureGenRecipe(
          GD.Load<Texture2D>($"{BASE_TEXTURE_PATH}menu-box-top-edge.png"),
          SkinColor.TopFace,
          SkinColorIntensity.Dark,
          ImageAlignment.TopCenter
      ),
  ];

  private static Texture2D? _cached;
  private static GameSkin? _cachedSkin;

  // Cached, keyed on the skin it was built from.
  //
  // Menubox._Ready calls this, and navigation re-instantiates the menu scene every time, so
  // returning to the main menu used to redo eight synchronous GPU readbacks and a pass over two
  // million pixels for a result that never differs - Settings then Back visibly hitched, the same
  // way, every time. Keying on the skin object means a skin selector invalidates this for free.
  public static Texture2D GenerateTexture() {
    var skin = SkinManager.Instance.CurrentSkin;
    if (_cached != null && ReferenceEquals(_cachedSkin, skin)) {
      return _cached;
    }

    _cachedSkin = skin;
    _cached = TextureGenerator.GenerateTexture(_textureSize, _recipes);
    return _cached;
  }
}
