namespace Wfc.Entities.World.Piano;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.Utils;
using Wfc.Utils.Images;

public partial class SolfegeNotesTextureGenerator {
  public const int SOLFEGE_KEY_OFFSET = 67;
  public const int NOTE_SPRITE_WIDTH = 39;
  public static readonly Color BACKGROUND_COLOR = new Color(0xfdfbe700);
  public const string PIANO_RESOURCES_BASE_PATH = @"res://Assets/Sprites/Piano/sheet/";
  private static readonly Texture2D SOLFEGE_TEXTURE = GD.Load<Texture2D>(PIANO_RESOURCES_BASE_PATH + "key-sol.png");
  private static readonly Dictionary<MusicNote, Texture2D> NOTES_TEXTURES = new Dictionary<MusicNote, Texture2D>
  {
        { MusicNote.Do, GD.Load<Texture2D>(PIANO_RESOURCES_BASE_PATH + "do.png") },
        { MusicNote.Re, GD.Load<Texture2D>(PIANO_RESOURCES_BASE_PATH + "re.png") },
        { MusicNote.Mi, GD.Load<Texture2D>(PIANO_RESOURCES_BASE_PATH + "mi.png") },
        { MusicNote.Fa, GD.Load<Texture2D>(PIANO_RESOURCES_BASE_PATH + "fa.png") },
        { MusicNote.Sol, GD.Load<Texture2D>(PIANO_RESOURCES_BASE_PATH + "sol.png") },
        { MusicNote.La, GD.Load<Texture2D>(PIANO_RESOURCES_BASE_PATH + "la.png") },
        { MusicNote.Si, GD.Load<Texture2D>(PIANO_RESOURCES_BASE_PATH + "si.png") }
    };

  public Texture2D CreateFromNotes(MusicNote[] notesArray, Vector2 textureSize) {
    var notesTextures = _generateNotesTextureArray(notesArray);
    var texture = GenerateTexture(notesTextures, textureSize);
    return texture;
  }

  private static List<Texture2D> _generateNotesTextureArray(MusicNote[] notesArray) {
    var textureArray = new List<Texture2D>();
    foreach (MusicNote note in notesArray) {
      var texture = NOTES_TEXTURES[note];
      textureArray.Add(texture);
    }
    return textureArray;
  }

  private static ImageTexture GenerateTexture(List<Texture2D> notesTextures, Vector2 textureSize) {
    var format = SOLFEGE_TEXTURE.GetImage().GetFormat();
    var image = Image.CreateEmpty((int)textureSize.X, (int)textureSize.Y, false, format);
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
