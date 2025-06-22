namespace Wfc.Entities.World.Player.Explosion;

using Godot;

public partial class ExplosionInfo : GodotObject {
  public Node2D BlocksContainer { get; set; }
  public bool CanDetonate { get; set; }
  public Vector2 CollisionExtents { get; set; }
  public Timer DebrisTimer { get; set; }
  public bool Detonate { get; set; }
  public bool HasDetonated { get; set; }
  public int Height { get; set; }
  public int HFrames { get; set; }
  public Vector2 Offset { get; set; }
  public int VFrames { get; set; }
  public int Width { get; set; }
  public ExplosionInfo() {
    BlocksContainer = new Node2D();
    CanDetonate = true;
    CollisionExtents = new Vector2();
    DebrisTimer = new Timer();
    Detonate = false;
    HasDetonated = false;
    Height = 0;
    HFrames = 1;
    Offset = new Vector2();
    VFrames = 1;
    Width = 0;
  }
}
