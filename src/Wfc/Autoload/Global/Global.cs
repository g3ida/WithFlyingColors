namespace Wfc.Autoload;

using Godot;
using Wfc.Entities.World.Player;

public partial class Global : Node2D {
  private static Global _instance = null!;

  // Claimed by the player as it enters the tree, and again by the level that owns it.
  public Player Player = null!;

  public override void _Ready() {
    base._Ready();
    _instance = GetTree().Root.GetNode<Global>("GlobalCS");
    SetProcess(false);
  }

  public static Global Instance() {
    return _instance;
  }
}
