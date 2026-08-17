namespace Wfc.test.instrumented.Platforms;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Event;
using Wfc.Entities.World;
using Wfc.Entities.World.Platforms;
using Wfc.Utils;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;

// The staircase a stretch of level is crossed on. What makes it a staircase rather than weather is
// that the lanes are fixed, the clock is fixed, and each lane is a set fraction of that clock out of
// step with the one before it - so there is always a piece one hop up and to the right.
public class TetrominoRainTests(Node testScene) : TestClass(testScene) {
  // The level's own settings, so what is asserted here is the crossing that ships.
  private const float CELL = 72.0f;
  private const int LANES = 5;
  private const int LANE_SPACING = 5;
  private const float FALL_HEIGHT = 2400.0f;
  private const float STEP = 0.8f;
  private const int MAX_HEIGHT = 2;

  // Counted in rows, which is what makes the spacing between two pieces exact rather than off by
  // part of a row - the clock the curtain runs on is derived from these.
  private const int ROW_SPACING = 7;
  private const int CLIMB = 2;

  // A row either way. The descent is stepped, so nothing about a piece's height is finer than that.
  private const float CLOSE = CELL;

  private FakeDependenciesProvider _services = default!;
  private TetrominoRain _rain = default!;

  [Cleanup]
  public void Cleanup() => _services.QueueFree();

  // A lane the pieces only roughly follow is not a lane: the gap beside it is what the player is
  // jumping across, and a piece standing off centre eats into it.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task EveryPieceStandsSquarelyInALane() {
    var rain = await _add();
    var lanes = Enumerable.Range(0, LANES).Select(rain.LaneX).ToList();

    for (var sample = 0; sample < 90; sample++) {
      foreach (var piece in _piecesOf(rain)) {
        lanes.ShouldContain(
          lane => Mathf.Abs(lane - piece.Bounds.GetCenter().X) < 0.5f,
          $"a piece is centred at {piece.Bounds.GetCenter().X}, which is no lane"
        );
      }
      await PhysicsFrames.Frame(TestScene);
    }
  }

  // The curtain has to be standing when the player arrives at it. Filling from an empty sky costs a
  // whole fall before the first piece is reachable - on the first attempt and on every retry after
  // it, which is the one the player is already frustrated by.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ItOpensWithPiecesAlreadyOnTheirWayDown() {
    var rain = await _add();
    var pieces = _piecesOf(rain);

    pieces.Count.ShouldBeGreaterThanOrEqualTo(LANES, "the curtain opened with lanes that had nothing in them");
    pieces.ShouldContain(
      piece => piece.Descended > FALL_HEIGHT / 2.0f,
      "every piece started at the top, so the curtain opened empty where the player is standing"
    );
    foreach (var lane in Enumerable.Range(0, LANES)) {
      _inLane(rain, lane).ShouldNotBeEmpty($"lane {lane} opened with nothing in it");
    }
  }

  // The headroom the player jumps out of. Fixed timing is the whole point of it: a gap that changed
  // from drop to drop could not be jumped out of on purpose.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task PiecesInOneLaneAreAlwaysOneSpacingApart() {
    var rain = await _add();
    await PhysicsFrames.Advance(TestScene, 30);

    for (var lane = 0; lane < LANES; lane++) {
      var descents = _inLane(rain, lane).Select(piece => piece.Descended).OrderBy(descent => descent).ToList();
      descents.Count.ShouldBeGreaterThan(1, $"lane {lane} is holding one piece, so there is no spacing to check");

      foreach (var (above, below) in descents.Zip(descents.Skip(1))) {
        (below - above).ShouldBe(rain.LanePitch, CLOSE, $"lane {lane} is dropping unevenly");
      }
    }
  }

  // What makes the crossing climb. Every lane is a set fraction of the clock behind the one before
  // it, so the piece to the right always stands that much higher than the one being stood on - and
  // a hop rightwards is a hop upwards rather than a slow ride down.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task EachLaneStandsAStaggerHigherThanTheOneBeforeIt() {
    var rain = await _add();

    for (var sample = 0; sample < 60; sample++) {
      for (var lane = 0; lane + 1 < LANES; lane++) {
        var here = _inLane(rain, lane).Min(piece => piece.Descended);
        var next = _inLane(rain, lane + 1).Min(piece => piece.Descended);

        // Both wrap round a spacing: which piece of the next lane is the higher one changes as the
        // curtain turns over, and what holds is the offset between the two lanes' clocks.
        var rise = _wrap(here - next, rain.LanePitch);
        rise.ShouldBe(rain.StaggerRise, CLOSE, $"lane {lane + 1} is not standing a stagger above lane {lane}");
      }
      await PhysicsFrames.Frame(TestScene);
    }
  }

  // Two pieces in the same place are two colliders in the same place: the player lands on a surface
  // that is also a wall. Lanes are spaced wider than the widest piece, which is what rules it out.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task NoTwoPiecesEverOverlap() {
    var rain = await _add();

    for (var sample = 0; sample < 90; sample++) {
      var bounds = _piecesOf(rain).Select(piece => piece.Bounds).ToList();
      for (var left = 0; left < bounds.Count; left++) {
        for (var right = left + 1; right < bounds.Count; right++) {
          bounds[left].Intersects(bounds[right]).ShouldBeFalse(
            $"two pieces are occupying {bounds[left]} and {bounds[right]}"
          );
        }
      }
      await PhysicsFrames.Frame(TestScene);
    }
  }

  // What the player is retrying is the jump they died on, and that jump was onto a particular shape
  // arriving at a particular moment. A curtain that rolled fresh dice on every death would be a
  // stretch of level nobody could learn, only outlast.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ARespawnDropsTheSamePiecesAgain() {
    var rain = await _add();
    var first = _describe(rain);

    await PhysicsFrames.Advance(TestScene, 40);
    GameEvents.Instance.OnCheckpointLoaded();
    await PhysicsFrames.Frame(TestScene);

    _describe(rain).ShouldBe(first, "the curtain came back holding different pieces");
  }

  // The pieces in the air belong to the attempt that just ended. Left there, the player respawns
  // into a curtain half full of pieces at heights nothing about the retry explains.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ARespawnClearsThePiecesTheLastAttemptLeftFalling() {
    var rain = await _add();
    await PhysicsFrames.Advance(TestScene, 40);

    var lastAttempt = _piecesOf(rain).Select(piece => piece.GetInstanceId()).ToList();
    lastAttempt.Count.ShouldBeGreaterThan(0);

    GameEvents.Instance.OnCheckpointLoaded();
    await PhysicsFrames.Advance(TestScene, 2);

    _piecesOf(rain).Select(piece => piece.GetInstanceId()).Intersect(lastAttempt).ShouldBeEmpty(
      "the last attempt's pieces are still coming down on the retry"
    );
  }

  // A death is followed by an animation the player is watching, not playing. A curtain that kept
  // dropping through it buries the spot they are about to respawn onto.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ItStopsDroppingOnceThePlayerIsDying() {
    var rain = await _add();
    GameEvents.Instance.OnPlayerDying(rain, Vector2.Zero, EntityType.FallZone);
    await PhysicsFrames.Frame(TestScene);
    var held = _piecesOf(rain).Count;

    await PhysicsFrames.Advance(TestScene, (int)(rain.SpawnInterval * 1.5f * Engine.PhysicsTicksPerSecond));

    _piecesOf(rain).Count.ShouldBeLessThanOrEqualTo(held, "the curtain kept dropping pieces over the death");
  }

  // The I on its end is four cells of tower one cell wide: a wall between two lanes rather than a
  // step between them, and a ledge narrower than the cube that has to land on it. Capping the height
  // is also what lets the lanes pack closer together, since two stacked pieces need room for the
  // taller of them plus the jump out.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ItNeverDropsAPieceTallerThanItIsAllowed() {
    var rain = await _add();
    var seen = new HashSet<TetrominoShape.Kind>();

    for (var sample = 0; sample < 240; sample++) {
      foreach (var piece in _piecesOf(rain)) {
        seen.Add(piece.Kind);
        TetrominoShape.HeightOf(piece.Kind, piece.RotationIndex).ShouldBeLessThanOrEqualTo(
          MAX_HEIGHT,
          $"a {piece.Kind} came down standing taller than the room between two pieces"
        );
      }
      await PhysicsFrames.Frame(TestScene);
    }

    // Capping the height takes rotations away from the I and from nothing else, so every shape - and
    // so every colour - still comes down.
    seen.Count.ShouldBeGreaterThan(3, "the cap narrowed the rain down to a handful of shapes");
  }

  // An author who sets a spacing the crossing cannot be made on is told so in the editor rather than
  // in a playtest.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ItWarnsWhenTheLanesAreTooCloseOrTooFlat() {
    var rain = await _add();
    rain._GetConfigurationWarnings().ShouldBeEmpty("the level's own settings are being warned about");

    rain.LaneSpacing = 1;
    rain._GetConfigurationWarnings().ShouldContain(
      warning => warning.Contains("LaneSpacing"),
      "lanes narrower than a piece were accepted"
    );

    rain.LaneSpacing = LANE_SPACING;
    rain.Climb = 0;
    rain._GetConfigurationWarnings().ShouldContain(
      warning => warning.Contains("Climb"),
      "lanes in step with each other were accepted, so the crossing only ever loses height"
    );

    // The pairing the curtain cannot be asked to avoid: the flattest piece to leave and the tallest
    // to land on. Letting pieces stand taller widens it, and it is the hop that decides whether the
    // crossing can be made at all - the earlier settings cleared every other check and still dealt
    // hops nobody could take.
    rain.Climb = CLIMB;
    rain.MaxPieceHeight = TetrominoShape.MAX_SPAN_CELLS;
    rain._GetConfigurationWarnings().ShouldContain(
      warning => warning.Contains("worst pairing"),
      "a curtain whose worst pairing needs nearly the whole jump was accepted"
    );
  }

  // The hop the player is actually asked to make is between the tops of two pieces, so how tall each
  // of them stands is part of it. Held well inside the cube's jump so that the hop is a step rather
  // than a thing to be played perfectly.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task TheWorstHopItCanDealIsWellInsideTheCubesJump() {
    var rain = await _add();

    rain.WorstHopRise.ShouldBe(rain.StaggerRise + ((MAX_HEIGHT - 1) * CELL), 1.0f);
    rain.WorstHopRise.ShouldBeLessThan(
      rain.ReachableRise * 0.8f,
      "the worst hop asks for more climb than a jump across the widest gap can buy"
    );
  }

  private static List<TetrominoPlatform> _piecesOf(TetrominoRain rain) =>
    rain.GetChildren().OfType<TetrominoPlatform>().Where(GodotObject.IsInstanceValid).ToList();

  private static List<TetrominoPlatform> _inLane(TetrominoRain rain, int lane) =>
    _piecesOf(rain).Where(piece => Mathf.Abs(piece.Bounds.GetCenter().X - rain.LaneX(lane)) < 0.5f).ToList();

  // What the curtain is holding, said in a way two runs can be compared by.
  private static List<string> _describe(TetrominoRain rain) =>
    _piecesOf(rain)
      .Select(piece => $"{piece.Kind}/{piece.RotationIndex}@{piece.Bounds.GetCenter().X}")
      .OrderBy(description => description)
      .ToList();

  private static float _wrap(float value, float period) {
    var wrapped = value % period;
    return wrapped < 0.0f ? wrapped + period : wrapped;
  }

  private async Task<TetrominoRain> _add() {
    _services = new FakeDependenciesProvider();
    TestScene.AddChild(_services);
    var level = new FakeGameLevelProvider();
    _services.AddChild(level);
    await PhysicsFrames.Frame(TestScene);

    _rain = SceneHelpers.InstantiateNode<TetrominoRain>();
    _rain.LaneCount = LANES;
    _rain.LaneSpacing = LANE_SPACING;
    _rain.RowSpacing = ROW_SPACING;
    _rain.Climb = CLIMB;
    _rain.FallHeight = FALL_HEIGHT;
    _rain.CellSize = CELL;
    _rain.StepInterval = STEP;
    _rain.MaxPieceHeight = MAX_HEIGHT;
    _rain.Seed = 7;

    level.AddChild(_rain);
    await PhysicsFrames.Frame(TestScene);
    return _rain;
  }
}
