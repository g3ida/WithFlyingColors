namespace Wfc.Entities.World.Player;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Images;

public partial class PlayerSpriteGenerator : Node {
  public static readonly Vector2I SPRITE_SIZE = new Vector2I(96, 96);

  public enum ImageAlignment {
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
  }

  public struct FaceData {

    public FaceData(Texture2D texture, SkinColor color, SkinColorIntensity intensity, ImageAlignment alignment) {
      Texture = texture;
      Color = color;
      Intensity = intensity;
      Align = alignment;
    }

    public Texture2D Texture { get; set; }
    public SkinColor Color { get; set; }
    public SkinColorIntensity Intensity { get; set; }
    public ImageAlignment Align { get; set; }
  }

  public static readonly Dictionary<string, FaceData> FacesTextures = new() {
    ["left"] = new(
              texture: GD.Load<Texture2D>("res://Assets/Sprites/Player/player-left.png"),
              color: SkinColor.LeftFace,
              intensity: SkinColorIntensity.Basic,
              alignment: ImageAlignment.TopLeft
          ),
    ["top"] = new(
              texture: GD.Load<Texture2D>("res://Assets/Sprites/Player/player-top.png"),
              color: SkinColor.TopFace,
              intensity: SkinColorIntensity.Basic,
              alignment: ImageAlignment.TopRight
          ),
    ["bottom"] = new(
              texture: GD.Load<Texture2D>("res://Assets/Sprites/Player/player-bottom.png"),
              color: SkinColor.BottomFace,
              intensity: SkinColorIntensity.Basic,
              alignment: ImageAlignment.BottomRight
          ),
    ["right"] = new(
              texture: GD.Load<Texture2D>("res://Assets/Sprites/Player/player-right.png"),
              color: SkinColor.RightFace,
              intensity: SkinColorIntensity.Basic,
              alignment: ImageAlignment.TopRight
          ),
    ["left-edge"] = new(
              texture: GD.Load<Texture2D>("res://Assets/Sprites/Player/player-left-edge.png"),
              color: SkinColor.LeftFace,
              intensity: SkinColorIntensity.Dark,
              alignment: ImageAlignment.TopLeft
          ),
    ["right-edge"] = new(
              texture: GD.Load<Texture2D>("res://Assets/Sprites/Player/player-right-edge.png"),
              color: SkinColor.RightFace,
              intensity: SkinColorIntensity.Dark,
              alignment: ImageAlignment.TopRight
          ),
    ["bottom-edge"] = new(
              texture: GD.Load<Texture2D>("res://Assets/Sprites/Player/player-bottom-edge.png"),
              color: SkinColor.BottomFace,
              intensity: SkinColorIntensity.Dark,
              alignment: ImageAlignment.BottomLeft
          ),
    ["top-edge"] = new(
              texture: GD.Load<Texture2D>("res://Assets/Sprites/Player/player-top-edge.png"),
              color: SkinColor.TopFace,
              intensity: SkinColorIntensity.Dark,
              alignment: ImageAlignment.TopLeft
          )
  };


  public static Texture2D GetTexture() {
    return _mergeIntoSingleTexture();
  }

  private static ImageTexture _mergeIntoSingleTexture() {
    var image = _mergeIntoSingleImage();
    return ImageTexture.CreateFromImage(image);
  }

  private static Image _mergeIntoSingleImage() {
    var format = Image.Format.Rgba8;
    var image = Image.CreateEmpty((int)SPRITE_SIZE.X, (int)SPRITE_SIZE.Y, false, format);
    image.Fill(new Color(0, 0, 0, 0));

    foreach (var entry in FacesTextures) {
      var faceData = entry.Value;
      var texture = faceData.Texture;
      var color = SkinManager.Instance.CurrentSkin.GetColor(faceData.Color, faceData.Intensity);
      var alignment = faceData.Align;
      var img = _createColoredCopyFromImage(texture.GetImage(), color);
      var pos = _getPositionFromAlignment(texture, alignment);
      ImageUtils.BlitTexture(image, img, pos);
    }
    return image;
  }

  private static Vector2I _getPositionFromAlignment(Texture2D texture, ImageAlignment alignment) {
    switch (alignment) {
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

  private static Image _createColoredCopyFromImage(Image srcImage, Color color) {
    int width = srcImage.GetWidth();
    int height = srcImage.GetHeight();
    var format = srcImage.GetFormat();
    var image = Image.CreateEmpty(width, height, false, format);
    for (int i = 0; i < width; i++) {
      for (int j = 0; j < height; j++) {
        var pix = srcImage.GetPixel(i, j);
        var col = new Color(color.R, color.G, color.B, pix.A);
        image.SetPixel(i, j, col);
      }
    }
    return image;
  }
}
