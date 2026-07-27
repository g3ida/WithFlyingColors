namespace Wfc.test.instrumented.BrickBreaker;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.BrickBreaker;
using Wfc.Entities.World.BrickBreaker.Powerups;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// The shield power-up: a barrier the player earns to catch one ball that gets past them. The layer
// contract is asserted next door; this is the behavior it buys, and it is worth having on its own
// because the shield is spent by the very contact it has to survive long enough to answer.
public class BallShieldTests(Node testScene) : TestClass(testScene) {
  private const float SHIELD_HEIGHT = 400.0f;
  private const float DROP_HEIGHT = 200.0f;
  private const int FRAMES = 40;

  private FakeDependenciesProvider _provider = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await _physicsFrame();
  }

  [Cleanup]
  public void Cleanup() => _provider.QueueFree();

  [Test]
  public async Task AShieldSendsAFallingBallBackUpTheArena() {
    var shield = _addShield();
    var ball = _dropBallAboveTheShield();

    var lowest = ball.GlobalPosition.Y;
    for (var frame = 0; frame < FRAMES; frame++) {
      await _physicsFrame();
      lowest = Mathf.Max(lowest, ball.GlobalPosition.Y);
    }

    ball.BallVelocity.Y.ShouldBeLessThan(0.0f, "the shield should have turned the ball back");
    ball.GlobalPosition.Y.ShouldBeLessThan(lowest, "the ball should be climbing away from the shield");
    ball.GlobalPosition.Y.ShouldBeLessThan(SHIELD_HEIGHT, "the ball should never have got past the shield");

    ball.QueueFree();
    await _physicsFrame();
  }

  // One save, then it is gone - a permanent floor would retire the minigame's only real threat.
  [Test]
  public async Task AShieldIsSpentByTheBallItTurnsBack() {
    var shield = _addShield();
    var ball = _dropBallAboveTheShield();

    GodotObject.IsInstanceValid(shield).ShouldBeTrue("the shield should still be waiting for the ball");

    for (var frame = 0; frame < FRAMES; frame++) {
      await _physicsFrame();
    }

    GodotObject.IsInstanceValid(shield).ShouldBeFalse("the shield should have been spent by the save");

    ball.QueueFree();
    await _physicsFrame();
  }

  // Placed before it joins the tree, as the power-up itself does. A node parented first sits at the
  // origin for as long as it takes to move it, and anything else at the origin has already touched it.
  private ProtectionArea _addShield() {
    var shield = SceneHelpers.InstantiateNode<ProtectionArea>();
    shield.Position = Vector2.Down * SHIELD_HEIGHT;
    _provider.AddChild(shield);
    return shield;
  }

  private BouncingBall _dropBallAboveTheShield() {
    var ball = SceneHelpers.InstantiateNode<BouncingBall>();
    ball.Position = Vector2.Down * (SHIELD_HEIGHT - DROP_HEIGHT);
    _provider.AddChild(ball);
    ball.SetBallVelocity(Vector2.Down);
    return ball;
  }

  private async Task _physicsFrame() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
  }
}
