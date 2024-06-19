using Godot;
using Wfc.Image;

namespace Wfc.Entities.World.Player {
    public static class PlayerTextureGenerator {

        const string BASE_TEXTURE_PATH = "res://Assets/Sprites/Player/";
        private static Vector2I _s_textureSize = new Vector2I(96, 96);
        private static TextureGenRecipe[] _s_recipes = [
        new TextureGenRecipe(
                GD.Load<Texture2D>($"{BASE_TEXTURE_PATH}player-left.png"),
                Skin.SkinColor.LeftFace,
                Skin.SkinColorIntensity.Basic,
                ImageAlignment.TopLeft
            ),
            new TextureGenRecipe(
                GD.Load<Texture2D>($"{BASE_TEXTURE_PATH}player-top.png"),
                Skin.SkinColor.TopFace,
                Skin.SkinColorIntensity.Basic,
                ImageAlignment.TopRight
            ),
            new TextureGenRecipe(
                GD.Load<Texture2D>($"{BASE_TEXTURE_PATH}player-bottom.png"),
                Skin.SkinColor.BottomFace,
                Skin.SkinColorIntensity.Basic,
                ImageAlignment.BottomRight
            ),
            new TextureGenRecipe(
                GD.Load<Texture2D>($"{BASE_TEXTURE_PATH}player-right.png"),
                Skin.SkinColor.RightFace,
                Skin.SkinColorIntensity.Basic,
                ImageAlignment.TopRight
            ),
            new TextureGenRecipe(
                GD.Load<Texture2D>($"{BASE_TEXTURE_PATH}player-left-edge.png"),
                Skin.SkinColor.LeftFace,
                Skin.SkinColorIntensity.Dark,
                ImageAlignment.TopLeft
            ),
            new TextureGenRecipe(
                GD.Load<Texture2D>($"{BASE_TEXTURE_PATH}player-right-edge.png"),
                Skin.SkinColor.RightFace,
                Skin.SkinColorIntensity.Dark,
                ImageAlignment.TopRight
            ),
            new TextureGenRecipe(
                GD.Load<Texture2D>($"{BASE_TEXTURE_PATH}player-bottom-edge.png"),
                Skin.SkinColor.BottomFace,
                Skin.SkinColorIntensity.Dark,
                ImageAlignment.BottomLeft
            ),
            new TextureGenRecipe(
                GD.Load<Texture2D>($"{BASE_TEXTURE_PATH}player-top-edge.png"),
                Skin.SkinColor.TopFace,
                Skin.SkinColorIntensity.Dark,
                ImageAlignment.TopLeft
            ),
    ];

        public static Texture2D GenerateTexture() {
            return TextureGenerator.GenerateTexture(_s_textureSize, _s_recipes);
        }
    }
}