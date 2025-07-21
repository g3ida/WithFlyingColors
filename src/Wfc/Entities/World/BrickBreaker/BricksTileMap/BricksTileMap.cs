namespace Wfc.Entities.World.BrickBreaker;

using System.Collections.Generic;
using Godot;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class BricksTileMap : Node2D {
  [Signal]
  public delegate void bricksClearedEventHandler();

  [Signal]
  public delegate void levelClearedEventHandler(int level);

  [Export]
  public bool should_instance_bricks { get; set; } = true;

  private Godot.Collections.Array<Node> _levels = new Godot.Collections.Array<Node>();
  private List<bool> _isLevelCleared = new List<bool>();
  private int _leastUnclearedLevel = 0;

  public override void _Ready() {
    base._Ready();
    _levels = GetChildren();
    _InitIsLevelCleared();
  }

  private void _InitIsLevelCleared() {
    _isLevelCleared = new List<bool>();
    for (int i = 0; i < _levels.Count; i++) {
      _isLevelCleared.Add(!should_instance_bricks);
    }
  }

  private int _GetLeastUnclearedLevel() {
    for (int i = 0; i < _isLevelCleared.Count; i++) {
      if (!_isLevelCleared[i]) {
        return i;
      }
    }
    return -1;
  }

  private void _onLevelBricksCleared(int id) {
    _isLevelCleared[id] = true;
    int newUnclearedLevel = _GetLeastUnclearedLevel();
    if (newUnclearedLevel == -1) {
      CallDeferred(nameof(_EmitBricksCleared));
    }
    else if (newUnclearedLevel != _leastUnclearedLevel) {
      CallDeferred(nameof(_EmitLevelCleared), newUnclearedLevel);
    }
  }

  private void _EmitLevelCleared(int newUnclearedLevel) {
    EmitSignal(BricksTileMap.SignalName.levelCleared, newUnclearedLevel);
  }

  private void _EmitBricksCleared() {
    EmitSignal(BricksTileMap.SignalName.bricksCleared);
  }
}
