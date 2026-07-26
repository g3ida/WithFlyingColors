namespace Wfc.Entities.World.BrickBreaker;

using System;
using Godot;
using Wfc.Utils.Colors;

public partial class BricksLevelTilemap : TileMap {
  [Signal]
  public delegate void levelBricksClearedEventHandler(int id);

  private const string BrickScenePath = "res://src/Wfc/Entities/World/BrickBreaker/Brick/Brick.tscn";
  private PackedScene _brickScene = GD.Load<PackedScene>(BrickScenePath);

  [Export]
  public int id { get; set; } = 0;

  private int _bricksCount = 0;
  private bool _isCleared;

  private BricksTileMap _parent = default!;

  public override void _Ready() {
    base._Ready();
    _parent = GetParent<BricksTileMap>();
    FillGrid();
  }

  private void FillGrid() {
    for (int i = 0; i < ColorUtils.COLOR_GROUPS.Length; i++) {
      foreach (Vector2I cell in GetUsedCellsById(0, i)) {
        Vector2 pos = MapToLocal(cell);
        SetCell(0, cell, -1); //Layer cell value

        if (_parent.should_instance_bricks) {
          var brick = _brickScene.Instantiate<Brick>();
          brick.ColorGroup = ColorUtils.COLOR_GROUPS[i];
          _parent.CallDeferred(Node2D.MethodName.AddChild, brick);
          brick.CallDeferred(Node2D.MethodName.SetOwner, _parent);
          brick.Connect(Brick.SignalName.brickBroken, new Callable(this, nameof(OnBrickBroken)));
          brick.Position = pos;
          _bricksCount++;
        }
      }
    }
  }

  // Testing for exactly zero made a single miscount unrecoverable: one extra decrement
  // stepped over the terminal value and the room could never be cleared. The latch on
  // Brick should make that impossible now, but the counter is what the player is locked
  // behind, so it does not get to depend on being counted perfectly.
  private void OnBrickBroken() {
    _bricksCount--;
    if (!_isCleared && _bricksCount <= 0) {
      _isCleared = true;
      EmitSignal(BricksLevelTilemap.SignalName.levelBricksCleared, id);
    }
  }
}
