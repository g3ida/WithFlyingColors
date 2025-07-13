using System;
using System.Collections.Generic;
using Godot;
using Wfc.Utils;
using Wfc.Utils.Images;

public partial class SolfegeNotesTextureGenerator : Node {
  public const int SOLFEGE_KEY_OFFSET = 67;
  public const int NOTE_SPRITE_WIDTH = 39;
  public static readonly Color BACKGROUND_COLOR = new Color(0xfdfbe7);

  public static readonly Texture2D SOLFEGE_TEXTURE = GD.Load<Texture2D>("res://Assets/Sprites/Piano/sheet/key-sol.png");

  private static readonly Dictionary<string, Texture2D> NOTES_TEXTURES = new Dictionary<string, Texture2D>
  {
        { "do", GD.Load<Texture2D>("res://Assets/Sprites/Piano/sheet/do.png") },
        { "re", GD.Load<Texture2D>("res://Assets/Sprites/Piano/sheet/re.png") },
        { "mi", GD.Load<Texture2D>("res://Assets/Sprites/Piano/sheet/mi.png") },
        { "fa", GD.Load<Texture2D>("res://Assets/Sprites/Piano/sheet/fa.png") },
        { "sol", GD.Load<Texture2D>("res://Assets/Sprites/Piano/sheet/sol.png") },
        { "la", GD.Load<Texture2D>("res://Assets/Sprites/Piano/sheet/la.png") },
        { "si", GD.Load<Texture2D>("res://Assets/Sprites/Piano/sheet/si.png") }
    };

  public override void _Ready() {
    // Initialization code if needed
  }

  public Texture2D CreateFromNotes(String[] notesArray, Vector2 textureSize) {
    var notesTextures = GenerateNotesTextureArray(notesArray);
    var texture = GenerateTexture(notesTextures, textureSize);
    return texture;
  }

  private List<Texture2D> GenerateNotesTextureArray(String[] notesArray) {
    var textureArray = new List<Texture2D>();
    foreach (string note in notesArray) {
      var texture = NOTES_TEXTURES[note];
      textureArray.Add(texture);
    }
    return textureArray;
  }

  private Texture2D GenerateTexture(List<Texture2D> notesTextures, Vector2 textureSize) {
    var format = SOLFEGE_TEXTURE.GetImage().GetFormat();
    var image = Image.Create((int)textureSize.X, (int)textureSize.Y, false, format);
    image.Fill(BACKGROUND_COLOR);
    int offsetX = (int)((textureSize.X - SOLFEGE_TEXTURE.GetWidth()) * 0.5f);
    int offsetY = (int)((textureSize.Y - SOLFEGE_TEXTURE.GetHeight()) * 0.5f);
    ImageUtils.BlitTexture(image, SOLFEGE_TEXTURE.GetImage(), new Vector2I(offsetX, offsetY));
    var noteOffset = SOLFEGE_KEY_OFFSET;
    foreach (var note in notesTextures) {
      ImageUtils.BlitTexture(image, note.GetImage(), new Vector2I(offsetX + noteOffset, offsetY));
      noteOffset += NOTE_SPRITE_WIDTH;
    }
    return ImageTexture.CreateFromImage(image);
  }
}
