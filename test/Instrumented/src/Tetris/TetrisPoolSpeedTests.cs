namespace Wfc.test.instrumented.Tetris;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.Tetris;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// How fast the pool drops a piece is the whole of its difficulty, and it is decided by the
// level alone. It used to have two writers that disagreed about the first one, so a retry after
// a run that had levelled up came back quicker than the run being retried - the same room,
// measurably harder, with nothing on screen to say why.
public class TetrisPoolSpeedTests(Node testScene) : TestClass(testScene) {
  // Ten cleared lines is one level.
  private const int LINES_PER_LEVEL = 10;

  private FakeDependenciesProvider _provider = default!;
  private TetrisPool _pool = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    _pool = SceneHelpers.InstantiateNode<TetrisPool>();
    _provider.AddChild(_pool);
    await _idle();
  }

  [Cleanup]
  public void Cleanup() => _provider.QueueFree();

  [Test]
  public void AFreshPoolOpensAtTheFirstLevelsSpeedTest() {
    _pool.StepInterval.ShouldBe(Constants.TETRIS_SPEEDS[0]);
  }

  [Test]
  public async Task ClearingEnoughLinesSpeedsThePoolUpTest() {
    await _enterThePool();

    _clearLines(LINES_PER_LEVEL);

    _pool.StepInterval.ShouldBe(Constants.TETRIS_SPEEDS[1]);
  }

  // The regression this file exists for.
  [Test]
  public async Task ARetryOpensAtTheSameSpeedAsTheRunItRetriesTest() {
    await _enterThePool();
    var opening = _pool.StepInterval;
    _clearLines(LINES_PER_LEVEL * 2);
    _pool.StepInterval.ShouldNotBe(opening, "the run should have sped up before it is restarted");

    _pool.reset();

    _pool.StepInterval.ShouldBe(opening);
  }

  // Every level has its own speed until the table runs out, after which the pool stays at its
  // quickest rather than reading past the end of it.
  [Test]
  public async Task TheSpeedFollowsTheLevelAndStopsAtTheQuickestTest() {
    await _enterThePool();

    for (var level = 1; level <= Constants.TETRIS_SPEEDS.Length + 2; level++) {
      var expected = Constants.TETRIS_SPEEDS[Mathf.Min(level, Constants.TETRIS_SPEEDS.Length) - 1];
      _pool.StepInterval.ShouldBe(expected, $"level {level}");
      _clearLines(LINES_PER_LEVEL);
    }
  }

  // The trigger is what starts a run, and until it has fired a reset is refused - the pool must
  // not restart itself behind a player who has never been in it.
  private async Task _enterThePool() {
    var body = new Wfc.Entities.World.Player.Player();
    _pool.GetNode<Area2D>("TriggerEnterArea").EmitSignal(Area2D.SignalName.BodyEntered, body);
    body.QueueFree();
    await _idle();
  }

  private void _clearLines(int count) =>
    _pool.EmitSignal(TetrisPool.SignalName.LinesRemoved, count);

  private async Task _idle() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
  }
}
