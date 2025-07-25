namespace Wfc.Entities.Tetris.Tetrominos;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class Block : Node2D {
  #region Exports
  [Export]
  public string ColorGroup { get; set; } = "blue";
  [Export]
  public int I { get; set; } = 0;
  [Export]
  public int J { get; set; } = 0;
  #endregion Exports

  [Signal]
  public delegate void BlockDestroyedEventHandler();

  public const float BLINK_ANIMATION_DURATION = 0.5f;
  public Block?[,]? Grid = null;

  [NodePath("BlockSprite")]
  private BlockSprite spriteNode = default!;
  [NodePath("BlockSprite/AnimationPlayer")]
  private AnimationPlayer spriteAnimationNode = default!;
  [NodePath("Area2D")]
  private Area2D areaNode = default!;
  [NodePath("Area2D/CollisionShape2D")]
  private CollisionShape2D areaShapeNode = default!;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();

    if (ColorGroup != null) {
      areaNode.AddToGroup(ColorGroup);
      spriteNode.ColorGroup = ColorGroup;
    }
  }

  public void MoveDown() => J += 1;
  public void MoveLeft() => I -= 1;
  public void MoveRight() => I += 1;

  public void MoveBy(int di, int dj) {
    I += di;
    J += dj;
  }

  public void MoveTo(int _i, int _j) {
    I = _i;
    J = _j;
  }

  public bool CanMoveBy(int di, int dj) {
    I += di;
    J += dj;
    bool can = IsInValidPosition();
    I -= di;
    J -= dj;
    return can;
  }

  public bool CanMoveLeft() => CanMoveBy(-1, 0);
  public bool CanMoveRight() => CanMoveBy(1, 0);
  public bool CanMoveDown() => CanMoveBy(0, 1);

  public bool IsInValidPosition() => !IsOffScreen() && !IsTouchingInactiveBlocks();

  public bool IsOffScreen() => I < 0 || I >= Constants.TETRIS_POOL_WIDTH || J < 0 || J >= Constants.TETRIS_POOL_HEIGHT;

  public bool IsTouchingInactiveBlocks() => Grid?[I, J] != null;

  public void AddToGrid(bool permissiveMode = true) {
    if (Grid != null) {
      Grid[I, J] = this;
      if (permissiveMode) {
        AddPermissivenessBoundsIfNeeded();
      }
    }
  }

  public void RemoveFromGrid() {
    if (Grid?[I, J] == this) {
      Grid[I, J] = null;
    }
  }

  public async void Destroy() {
    spriteAnimationNode.Play("Blink");
    await ToSignal(spriteAnimationNode, "animation_finished");
    EmitSignal(nameof(BlockDestroyed));
    QueueFree();
  }

  private void AddPermissivenessBoundsIfNeeded() {
    bool rightEdge = I + 1 < Constants.TETRIS_POOL_WIDTH &&
        Grid?[I + 1, J] != null &&
        Grid[I + 1, J]!.ColorGroup == ColorGroup;
    bool leftEdge = I > 0 &&
        Grid?[I - 1, J] != null &&
        Grid[I - 1, J]!.ColorGroup == ColorGroup;

    if (leftEdge) {
      AddPermissivenessBounds(DIR_LEFT);
      Grid?[I - 1, J]?.AddPermissivenessBounds(DIR_RIGHT);
    }

    if (rightEdge) {
      AddPermissivenessBounds(DIR_RIGHT);
      Grid?[I + 1, J]?.AddPermissivenessBounds(DIR_LEFT);
    }
  }

  private const int DIR_LEFT = -1;
  private const int DIR_RIGHT = 1;
  private const int DIR_BOTH = 2;

  private void AddPermissivenessBounds(int dir) {
    var group = Grid?[I + dir, J]?.ColorGroup;
    var edgeArea = new EdgeArea();
    if (group != null) {
      edgeArea.AddToGroup(group);
    }
    edgeArea.AddToGroup(ColorGroup);
    AddChild(edgeArea);
    edgeArea.Owner = this;

    var areaShape = areaShapeNode.Shape as RectangleShape2D;
    if (areaShape != null) {
      edgeArea.Position = new Vector2(
          dir == DIR_LEFT ? areaShape.Size.X : Constants.TETRIS_BLOCK_SIZE - areaShape.Size.X,
          areaShape.Size.Y
      );

      float edgeAreaX = (edgeArea.CollisionShapeNode?.Shape as RectangleShape2D)?.Size.X ?? 0;
      float areaX = areaShape.Size.X;
      float factor = 1 - (edgeAreaX / areaX);
      areaShapeNode.Scale = new Vector2(factor - (1 - areaShapeNode.Scale.X), areaShapeNode.Scale.Y);
      areaShapeNode.Position -= new Vector2(dir * 0.5f * (1 - factor) * Constants.TETRIS_BLOCK_SIZE, 0);
    }
  }
}
