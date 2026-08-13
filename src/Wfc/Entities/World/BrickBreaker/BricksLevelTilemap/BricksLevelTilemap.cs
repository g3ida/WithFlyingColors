namespace Wfc.Entities.World.BrickBreaker;

using System;
using Godot;
using Wfc.Utils.Colors;

public partial class BricksLevelTilemap : TileMapLayer {
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
    for (int tileSourceId = 0; tileSourceId < ColorUtils.TILE_SOURCE_ID_COUNT; tileSourceId++) {
      // The tile source id is level data, so the color it means goes through the named mapping
      // rather than through whatever order an array of color names happens to be declared in.
      var colorGroup = ColorUtils.FromTileSourceId(tileSourceId);
      if (colorGroup == null) {
        continue;
      }

      foreach (Vector2I cell in GetUsedCellsById(tileSourceId)) {
        // MapToLocal answers with the middle of the cell, and a brick is drawn and shaped from its
        // own top-left corner. Stood on the middle, every colored brick sat half a brick right and
        // down of the cell it was painted into: out of step with the white bricks the tilemap draws
        // for itself, and over the room's wall at the right-hand end of the wall.
        Vector2 pos = MapToLocal(cell) - ((Vector2)TileSet.TileSize / 2.0f);
        SetCell(cell, -1);

        if (_parent.should_instance_bricks) {
          var brick = _brickScene.Instantiate<Brick>();
          brick.ColorGroup = colorGroup;
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
