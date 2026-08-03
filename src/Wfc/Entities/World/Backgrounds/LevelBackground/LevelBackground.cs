namespace Wfc.Entities.World.Backgrounds;

using Godot;

// A level's backdrop, dropped under the level root as a scene instance. Being
// a CanvasLayer keeps it behind the world on its own layer, out of world
// z-index space, and pins it to the screen: distant scenery stays put no
// matter where the camera goes. A future background that wants depth can put
// Parallax2D layers inside instead of screen-fixed content.
public abstract partial class LevelBackground : CanvasLayer {
}
