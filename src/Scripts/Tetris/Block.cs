using System;
using System.Collections.Generic;
using Godot;
using Wfc.Utils;

public partial class Block : Node2D {
  [Export]
  public string color_group { get; set; }

  [Export]
  public int I { get; set; } = 0;

  [Export]
  public int J { get; set; } = 0;

  [Signal]
  public delegate void BlockDestroyedEventHandler();

  public const float BLINK_ANIMATION_DURATION = 0.5f;
  public List<List<Block>> grid;

  private BlockSprite spriteNode;
  private AnimationPlayer spriteAnimationNode;
  private Area2D areaNode;
  private CollisionShape2D areaShapeNode;

  public override void _Ready() {
    spriteNode = GetNode<BlockSprite>("BlockSprite");
    spriteAnimationNode = GetNode<AnimationPlayer>("BlockSprite/AnimationPlayer");
    areaNode = GetNode<Area2D>("Area2D");
    areaShapeNode = GetNode<CollisionShape2D>("Area2D/CollisionShape2D");

    if (color_group != null) {
      areaNode.AddToGroup(color_group);
      spriteNode.color_group = color_group;
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

  public bool IsTouchingInactiveBlocks() => grid[I][J] != null;

  public void AddToGrid(bool permissiveMode = true) {
    grid[I][J] = this;
    if (permissiveMode) {
      AddPermissivenessBoundsIfNeeded();
    }
  }

  public void RemoveFromGrid() {
    if (grid[I][J] == this) {
      grid[I][J] = null;
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
        grid[I + 1][J] != null &&
        grid[I + 1][J].color_group == color_group;
    bool leftEdge = I > 0 &&
        grid[I - 1][J] != null &&
        grid[I - 1][J].color_group == color_group;

    if (leftEdge) {
      AddPermissivenessBounds(DIR_LEFT);
      grid[I - 1][J]?.AddPermissivenessBounds(DIR_RIGHT);
    }

    if (rightEdge) {
      AddPermissivenessBounds(DIR_RIGHT);
      grid[I + 1][J]?.AddPermissivenessBounds(DIR_LEFT);
    }
  }

  private const int DIR_LEFT = -1;
  private const int DIR_RIGHT = 1;
  private const int DIR_BOTH = 2;

  private void AddPermissivenessBounds(int dir) {
    var group = grid[I + dir][J].color_group;
    var edgeArea = new EdgeArea();
    edgeArea.AddToGroup(group);
    edgeArea.AddToGroup(color_group);
    AddChild(edgeArea);
    edgeArea.Owner = this;

    var areaShape = areaShapeNode.Shape as RectangleShape2D;
    edgeArea.Position = new Vector2(
        dir == DIR_LEFT ? areaShape.Size.X : Constants.TETRIS_BLOCK_SIZE - areaShape.Size.X,
        areaShape.Size.Y
    );

    float edgeAreaX = ((RectangleShape2D)edgeArea.collisionShapeNode.Shape).Size.X;
    float areaX = areaShape.Size.X;
    float factor = 1 - (edgeAreaX / areaX);
    areaShapeNode.Scale = new Vector2(factor - (1 - areaShapeNode.Scale.X), areaShapeNode.Scale.Y);
    areaShapeNode.Position -= new Vector2(dir * 0.5f * (1 - factor) * Constants.TETRIS_BLOCK_SIZE, 0);
  }
}
