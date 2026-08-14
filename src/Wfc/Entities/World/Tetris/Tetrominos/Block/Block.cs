namespace Wfc.Entities.Tetris.Tetrominos;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.Autoload;
using Wfc.Entities.World;
using Wfc.Entities.World.Platforms;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

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

  private float _pendingDrop = 0.0f;

  [NodePath("BlockSprite")]
  private BlockSprite spriteNode = default!;
  [NodePath("BlockSprite/AnimationPlayer")]
  private AnimationPlayer spriteAnimationNode = default!;
  [NodePath("Area2D")]
  private Area2D areaNode = default!;
  [NodePath("Area2D/CollisionShape2D")]
  private CollisionShape2D areaShapeNode = default!;
  [NodePath("CharacterBody2D")]
  private CharacterBody2D bodyNode = default!;
  [NodePath("CharacterBody2D/CollisionShape2D")]
  private CollisionShape2D bodyShapeNode = default!;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    SetPhysicsProcess(false);

    // Fitted to the cell it occupies rather than drawn at the size the art happens to be, so a
    // block covers its own cell and nobody else's.
    spriteNode.Scale = Vector2.One * (Constants.TETRIS_BLOCK_SIZE / Constants.TETRIS_BLOCK_ART_SIZE);
    spriteNode.Position = Vector2.One * (Constants.TETRIS_BLOCK_SIZE * 0.5f);

    if (ColorGroup != null) {
      areaNode.AddToGroup(ColorGroup);
      spriteNode.ColorGroup = ColorGroup;
    }
  }

  // A cleared line moves every row above it down in the grid at once, but the body under a
  // settled block only follows its transform: covering the distance in one frame lands it
  // inside anything standing on the stack. Successive clears stack up rather than race.
  public void QueueDrop(float distance) {
    _pendingDrop += distance;
    SetPhysicsProcess(true);
  }

  public override void _PhysicsProcess(double delta) {
    var step = Math.Min(_pendingDrop, Constants.TETRIS_MAX_FALL_SPEED * (float)delta);
    Position += new Vector2(0.0f, step);
    _pendingDrop -= step;
    if (_pendingDrop <= 0.0f) {
      _pendingDrop = 0.0f;
      SetPhysicsProcess(false);
    }
    if (step > 0.0f) {
      ResolveDescentContact([bodyNode.GetRid()]);
    }
  }

  internal Rid BodyRid => bodyNode.GetRid();

  // A block descends by transform, so nothing in the engine stops it at the player: left to
  // itself it sweeps its kill area through every face of a cube it comes down on, and the
  // death reported is whichever color happened to be touched second. So a block that has
  // arrived into the cube does what the engine would have done for a real kinematic pusher:
  // carries the cube along ahead of it while the cube has anywhere to go, and squashes it
  // once it has not. Either way the overlap never grows past the cube's top edge band, so
  // the only color reading ever taken is the one face actually being touched.
  internal void ResolveDescentContact(Godot.Collections.Array<Rid> movingBodies) {
    var player = Global.Instance()?.Player;
    if (player is null || !IsInstanceValid(player) || !player.IsInsideTree() || player.IsDying()) {
      return;
    }
    if (bodyShapeNode.Shape is not RectangleShape2D rect) {
      return;
    }

    var travel = Vector2.Down;
    var size = rect.Size * bodyShapeNode.GlobalScale.Abs();
    var crusher = new Rect2(bodyShapeNode.GlobalPosition - (size * 0.5f), size);
    var half = player.GetCollisionHalfExtents();
    var body = new Rect2(player.GlobalPosition - half, half * 2.0f);

    // Everything below works on the whole guard band under the block, not just on contact.
    // Penetration cannot be corrected after the fact: the cube's own move this frame is swept
    // against the block's stale pre-descent transform, and the overlap the physics step then
    // records is delivered as kill signals before anyone can undo it. So the cube is kept
    // clear of the leading edge the whole way down - the band is wider than the two of them
    // can close in one frame.
    var separation = body.Position.Y - crusher.End.Y;
    if (separation >= GUARD_BAND || !PlatformCrush.IsAhead(crusher, body, travel)) {
      return;
    }
    var overlapX = Math.Min(crusher.End.X, body.End.X) - Math.Max(crusher.Position.X, body.Position.X);
    if (overlapX <= body.Size.X * PlatformCrush.MIN_COVERAGE) {
      return;
    }

    // The carry only shields a face the block cannot kill. Met with any other color, the
    // touch itself is the death - the same rule as walking into the piece's side - so the
    // block stands aside and lets the color areas read the contact.
    if (!player.WearsColorToward(ColorGroup, -travel)) {
      return;
    }

    // Meeting a descending block from below is a ceiling hit; the upward speed ends here.
    if (player.Velocity.Y < 0f) {
      player.Velocity = new Vector2(player.Velocity.X, 0f);
    }

    if (-separation > PlatformCrush.TOUCH_DEPTH &&
        !_hasSomewhereToGo(player, PlatformCrush.EscapeMotion(crusher, body, travel), movingBodies)) {
      EventHandler.Instance.EmitPlayerDying(
        this,
        PlatformCrush.ContactPoint(crusher, body, travel),
        EntityType.Crusher
      );
      return;
    }

    var shortfall = PlatformCrush.ESCAPE_MARGIN - separation;
    if (shortfall > 0f && _hasSomewhereToGo(player, travel * shortfall, movingBodies)) {
      player.GlobalPosition += travel * shortfall;
    }
  }

  // Wider than the piece's descent plus a jumping cube's rise in one frame, so the band is
  // entered before the two of them can meet inside it.
  private const float GUARD_BAND = 40f;

  // Every body descending this frame is left out of the escape probe, not just this block: the
  // engine answers an overlap the cube starts out in with a collision whichever way the motion
  // points, and a sibling block one cell over is exactly such an overlap while the piece is
  // into the cube.
  private static bool _hasSomewhereToGo(CharacterBody2D body, Vector2 escape, Godot.Collections.Array<Rid> movingBodies) {
    var probe = new PhysicsTestMotionParameters2D {
      From = body.GlobalTransform,
      Motion = escape,
      ExcludeBodies = movingBodies,
    };
    return !PhysicsServer2D.BodyTestMotion(body.GetRid(), probe);
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

  // A seam only needs an edge area when the two blocks disagree on color: the area
  // joins both groups so a face straddling the seam matches whichever color it wears
  // instead of being killed by the neighbor it is not touching yet.
  internal static bool NeedsEdgeBetween(string? colorGroup, string? neighborColorGroup) =>
    neighborColorGroup != null && neighborColorGroup != colorGroup;

  private void AddPermissivenessBoundsIfNeeded() {
    // Against the array rather than the playfield width: the escape wall lives in a column past
    // the last one a piece can be placed in, and a block landing beside it still needs the seam.
    bool rightEdge = Grid != null && I + 1 < Grid.GetLength(0) &&
        Grid[I + 1, J] != null &&
        NeedsEdgeBetween(ColorGroup, Grid[I + 1, J]!.ColorGroup);
    bool leftEdge = I > 0 &&
        Grid?[I - 1, J] != null &&
        NeedsEdgeBetween(ColorGroup, Grid[I - 1, J]!.ColorGroup);

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

  private bool _hasEdgeLeft;
  private bool _hasEdgeRight;

  private void AddPermissivenessBounds(int dir) {
    // Once per side. A block is asked for its edge by its own AddToGrid and again by each
    // neighbor that lands beside it, and TetrisPool never nulls _shape when a piece locks,
    // so the same side really does come round twice. The subtraction below is only correct
    // once per side: run twice it takes the color area to nothing and then past it, into a
    // negative scale that mirrors the shape onto the wrong half of the block.
    if (dir == DIR_LEFT ? _hasEdgeLeft : _hasEdgeRight) {
      return;
    }

    var group = Grid?[I + dir, J]?.ColorGroup;
    var edgeArea = SceneHelpers.InstantiateNode<EdgeArea>();
    if (group != null) {
      edgeArea.AddToGroup(group);
    }
    edgeArea.AddToGroup(ColorGroup);
    AddChild(edgeArea);
    edgeArea.Owner = this;

    if (dir == DIR_LEFT) {
      _hasEdgeLeft = true;
    }
    else {
      _hasEdgeRight = true;
    }

    if (areaShapeNode.Shape is not RectangleShape2D areaShape) {
      return;
    }

    // Godot 3 stored a rectangle as its half size and the port kept that arithmetic while
    // the property became the full size, which is how a left-hand edge ended up at
    // (74, 74) - diagonally outside the 72-wide block it belongs to. Halving explicitly is
    // what that missing conversion looks like.
    var edgeWidth = edgeArea.Width;
    var seamX = dir == DIR_LEFT ? 0f : Constants.TETRIS_BLOCK_SIZE;
    edgeArea.Position = new Vector2(seamX - (dir * edgeWidth * 0.5f), areaShapeNode.Position.Y);

    // The band is only permissive if this block's own color area stops short of it. A face
    // is killed by contact with any area it shares no group with, so a two-color seam laid
    // on top of an unchanged single-color area would still be fatal. Subtracting rather
    // than multiplying keeps a block with edges on both sides from shrinking twice over.
    var shrunk = (areaShape.Size.X - edgeWidth) / areaShape.Size.X;
    areaShapeNode.Scale = new Vector2(shrunk - (1 - areaShapeNode.Scale.X), areaShapeNode.Scale.Y);
    areaShapeNode.Position -= new Vector2(dir * edgeWidth * 0.5f, 0);
  }
}
