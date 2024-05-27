using Godot;
using System;
using System.Collections.Generic;

public class SolfegeNotesTextureGenerator : Node
{
    public const int SOLFEGE_KEY_OFFSET = 67;
    public const int NOTE_SPRITE_WIDTH = 39;
    public static readonly Color BACKGROUND_COLOR = new Color("fdfbe7");

    public static readonly Texture SOLFEGE_TEXTURE = (Texture)GD.Load("res://Assets/Sprites/Piano/sheet/key-sol.png");

    private static readonly Dictionary<string, Texture> NOTES_TEXTURES = new Dictionary<string, Texture>
    {
        { "do", (Texture)GD.Load("res://Assets/Sprites/Piano/sheet/do.png") },
        { "re", (Texture)GD.Load("res://Assets/Sprites/Piano/sheet/re.png") },
        { "mi", (Texture)GD.Load("res://Assets/Sprites/Piano/sheet/mi.png") },
        { "fa", (Texture)GD.Load("res://Assets/Sprites/Piano/sheet/fa.png") },
        { "sol", (Texture)GD.Load("res://Assets/Sprites/Piano/sheet/sol.png") },
        { "la", (Texture)GD.Load("res://Assets/Sprites/Piano/sheet/la.png") },
        { "si", (Texture)GD.Load("res://Assets/Sprites/Piano/sheet/si.png") }
    };

    public override void _Ready()
    {
        // Initialization code if needed
    }

    public Texture CreateFromNotes(String[] notesArray, Vector2 textureSize)
    {
        var notesTextures = GenerateNotesTextureArray(notesArray);
        var texture = GenerateTexture(notesTextures, textureSize);
        return texture;
    }

    private List<Texture> GenerateNotesTextureArray(String[] notesArray)
    {
        var textureArray = new List<Texture>();
        foreach (string note in notesArray)
        {
            var texture = NOTES_TEXTURES[note];
            textureArray.Add(texture);
        }
        return textureArray;
    }

    private Texture GenerateTexture(List<Texture> notesTextures, Vector2 textureSize)
    {
        var imageTexture = new ImageTexture();
        var image = new Image();
        var format = SOLFEGE_TEXTURE.GetData().GetFormat();
        image.Create((int)textureSize.x, (int)textureSize.y, false, format);
        image.Fill(BACKGROUND_COLOR);
        var offsetX = (textureSize.x - SOLFEGE_TEXTURE.GetWidth()) * 0.5f;
        var offsetY = (textureSize.y - SOLFEGE_TEXTURE.GetHeight()) * 0.5f;
        ImageUtils.BlitTexture(image, SOLFEGE_TEXTURE.GetData(), new Vector2(offsetX, offsetY));
        var noteOffset = SOLFEGE_KEY_OFFSET;
        foreach (var note in notesTextures)
        {
            ImageUtils.BlitTexture(image, note.GetData(), new Vector2(offsetX + noteOffset, offsetY));
            noteOffset += NOTE_SPRITE_WIDTH;
        }
        imageTexture.CreateFromImage(image);
        return imageTexture;
    }
}
