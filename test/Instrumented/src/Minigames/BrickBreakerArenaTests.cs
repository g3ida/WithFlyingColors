namespace Wfc.test.instrumented.Minigames;

using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Event;
using Wfc.Entities.World;
using Wfc.Entities.World.BrickBreaker;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using EventHandler = Wfc.Core.Event.EventHandler;

// The room the brick-breaker game is played in, taken through the one thing it does most often:
// being started, being lost, and being started again. What has to hold is that a round begins in an
// empty room - two walls standing at once is the last round's bricks left hanging while this
// round's descend through them.
public class BrickBreakerArenaTests(Node testScene) : TestClass(testScene) {
  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";
  private const string ARENA_SCENE = "res://src/Wfc/Entities/World/BrickBreaker/BrickBreaker/BrickBreaker.tscn";
  private const string BRICKS_SCENE = "res://src/Wfc/Entities/World/BrickBreaker/BricksTileMap/BricksTileMap.tscn";
  private const double TIMEOUT = 5.0;

  private FakeDependenciesProvider? _services;

  [Cleanup]
  public void Cleanup() {
    _services?.QueueFree();
    _services = null;
  }

  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task LosingAndStartingAgainLeavesOneWallStanding() {
    var arena = await _openArena();

    (await PhysicsFrames.WaitFor(TestScene, () => _walls(arena) == 1, TIMEOUT))
      .ShouldBeTrue("the room never laid a wall to play against");

    GameEvents.Instance.OnPlayerDying(Vector2.Zero, EntityType.BrickBreaker);
    await PhysicsFrames.Advance(TestScene, 4);
    _walls(arena).ShouldBe(0, "losing left the wall standing in the room");

    EventHandler.Instance.EmitCheckpointLoaded();
    (await PhysicsFrames.WaitFor(TestScene, () => _walls(arena) == 1, TIMEOUT))
      .ShouldBeTrue("starting again laid no wall");
    await PhysicsFrames.Advance(TestScene, 10);

    _walls(arena).ShouldBe(1, "the round started in a room that still had the last round's bricks in it");
  }

  // Restarting from the pause menu puts the level back without anybody dying first, so nothing has
  // told the room its round is over: it starts another one on top of the round already in progress.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task RestartingWithoutDyingDoesNotLeaveTheOldWallStanding() {
    var arena = await _openArena();

    (await PhysicsFrames.WaitFor(TestScene, () => _walls(arena) == 1, TIMEOUT))
      .ShouldBeTrue("the room never laid a wall to play against");

    EventHandler.Instance.EmitCheckpointLoaded();
    await PhysicsFrames.Advance(TestScene, 10);

    _walls(arena).ShouldBe(1, "the round started again through the wall the last one left standing");
  }

  // Whatever leads the room to start a round twice - a respawn arriving twice over, a trigger that
  // fires again - the second start must not descend a wall through the first one.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task StartingTwiceOverDoesNotStackTwoWalls() {
    var arena = await _openArena();

    (await PhysicsFrames.WaitFor(TestScene, () => _walls(arena) == 1, TIMEOUT))
      .ShouldBeTrue("the room never laid a wall to play against");

    GameEvents.Instance.OnPlayerDying(Vector2.Zero, EntityType.BrickBreaker);
    await PhysicsFrames.Advance(TestScene, 4);
    EventHandler.Instance.EmitCheckpointLoaded();
    await PhysicsFrames.Advance(TestScene, 4);
    EventHandler.Instance.EmitCheckpointLoaded();
    await PhysicsFrames.Advance(TestScene, 10);

    _walls(arena).ShouldBe(1, "two walls were left standing in the room at once");
  }

  // The white bricks are tiles the tilemap draws for itself and the coloured ones are nodes it lays
  // out by hand, so the two only line up if a brick stands on the corner of its cell. Half a brick
  // out is a wall that reads as two walls slightly apart - and a right-hand column standing in the
  // room's wall.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task EveryBrickStandsOnTheCellItWasPaintedInto() {
    var map = GD.Load<PackedScene>(BRICKS_SCENE).Instantiate<Node2D>();
    TestScene.AddChild(map);
    await PhysicsFrames.Advance(TestScene, 4);

    var cell = map.GetNode<TileMapLayer>("level0").TileSet.TileSize;
    var bricks = map.GetChildren().OfType<Brick>().ToList();
    bricks.ShouldNotBeEmpty("the map laid no bricks, so this checks nothing");
    foreach (var brick in bricks) {
      (brick.Position.X % cell.X).ShouldBe(0.0f, $"{brick.Name} stands {brick.Position.X % cell.X} into its cell");
      (brick.Position.Y % cell.Y).ShouldBe(0.0f, $"{brick.Name} stands {brick.Position.Y % cell.Y} into its cell");
    }

    map.QueueFree();
  }

  private static int _walls(Node arena) =>
    arena.GetChildren().OfType<BricksTileMap>().Count(GodotObject.IsInstanceValid);

  private async Task<Node2D> _openArena() {
    _services = new FakeDependenciesProvider();
    TestScene.AddChild(_services);
    var level = new FakeGameLevelProvider();
    _services.AddChild(level);
    await PhysicsFrames.Frame(TestScene);

    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    level.AddChild(player);
    level.PlayerNode = player;

    var arena = GD.Load<PackedScene>(ARENA_SCENE).Instantiate<Node2D>();
    level.AddChild(arena);
    await PhysicsFrames.Advance(TestScene, 2);

    // The way in is walking into the room, which is a collision the test has no floor to make.
    arena.Call("_onTriggerEnterAreaBodyEntered", player);
    await PhysicsFrames.Advance(TestScene, 4);
    return arena;
  }
}
