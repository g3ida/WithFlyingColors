using System;
using Godot;
using Wfc.Utils.Colors;

public partial class BricksLevelTilemap : TileMap {
  [Signal]
  public delegate void level_bricks_clearedEventHandler(int id);

  private const string BrickScenePath = "res://Assets/Scenes/BrickBreaker/Brick.tscn";
  private PackedScene _brickScene = GD.Load<PackedScene>(BrickScenePath);

  [Export]
  public int id { get; set; } = 0;

  private int _bricksCount = 0;

  private BricksTileMap _parent;

  public override void _Ready() {
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
          brick.color_group = ColorUtils.COLOR_GROUPS[i];
          _parent.CallDeferred("add_child", brick);
          brick.CallDeferred("set_owner", _parent);
          brick.Connect(nameof(Brick.brick_broken), new Callable(this, nameof(OnBrickBroken)));
          brick.Position = pos;
          _bricksCount++;
        }
      }
    }
  }

  private void OnBrickBroken() {
    _bricksCount--;
    if (_bricksCount == 0) {
      EmitSignal(nameof(level_bricks_cleared), id);
    }
  }
}
