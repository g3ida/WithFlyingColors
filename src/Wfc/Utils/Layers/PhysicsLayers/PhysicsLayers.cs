namespace Wfc.Utils.Layers;

using Godot;

static class PhysicsLayers {
  public static readonly LayerInfo Default = new LayerInfo("Default", 1);
  public static readonly LayerInfo Player = new LayerInfo("Player", 2);
  public static readonly LayerInfo Platform = new LayerInfo("Platform", 4);
  public static readonly LayerInfo FallZone = new LayerInfo("FallZone", 8);
  public static readonly LayerInfo BoxFace = new LayerInfo("BoxFace", 16);
  public static readonly LayerInfo Gems = new LayerInfo("Gems", 32);
  public static readonly LayerInfo Bullets = new LayerInfo("Bullets", 64);
  public static readonly LayerInfo Tetris = new LayerInfo("Tetris", 128);
  public static readonly LayerInfo PowerUp = new LayerInfo("PowerUp", 256);
  public static readonly LayerInfo BouncingBall = new LayerInfo("BouncingBall", 512);
  public static readonly LayerInfo Bricks = new LayerInfo("Bricks", 1024);
}
