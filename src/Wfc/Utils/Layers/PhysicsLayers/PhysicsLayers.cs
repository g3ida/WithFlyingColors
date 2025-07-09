namespace Wfc.Utils.Layers;

using Godot;

static class PhysicsLayers {
  public static LayerInfo Default = new LayerInfo("Default", 1);
  public static LayerInfo Player = new LayerInfo("Player", 2);
  public static LayerInfo Platform = new LayerInfo("Platform", 4);
  public static LayerInfo FallZone = new LayerInfo("FallZone", 8);
  public static LayerInfo BoxFace = new LayerInfo("BoxFace", 16);
  public static LayerInfo Gems = new LayerInfo("Gems", 32);
  public static LayerInfo Bullets = new LayerInfo("Bullets", 64);
  public static LayerInfo Tetris = new LayerInfo("Tetris", 128);
  public static LayerInfo PowerUp = new LayerInfo("PowerUp", 256);
  public static LayerInfo BouncingBall = new LayerInfo("BouncingBall", 512);
  public static LayerInfo Bricks = new LayerInfo("Bricks", 1024);
}
