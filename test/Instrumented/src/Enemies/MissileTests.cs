namespace Wfc.test.instrumented.Enemies;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Enemies;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// What separates a missile from a bullet is that it corrects course, and what keeps it fair
// is that the correction is a rate rather than a snap. Both halves are checked here.
public class MissileTests(Node testScene) : TestClass(testScene) {
  private const int FLIGHT_FRAMES = 30;
  private const float SIDEWAYS = 600.0f;

  private FakeDependenciesProvider _provider = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await PhysicsFrames.Frame(TestScene);
  }

  [Cleanup]
  public void Cleanup() => _provider.QueueFree();

  [Test]
  public async Task AMissileClosesOnATargetItWasNotAimedAt() {
    var target = new Node2D { Position = new Vector2(0.0f, -SIDEWAYS) };
    _provider.AddChild(target);

    var chasing = await _fly(Vector2.Right, target);
    var straight = await _fly(Vector2.Right, null);

    chasing.Y.ShouldBeLessThan(straight.Y);
  }

  [Test]
  public async Task AMissileTurnsNoFasterThanItsTurnRate() {
    // Behind the muzzle, so a missile free to snap around would already be heading back.
    var target = new Node2D { Position = new Vector2(-SIDEWAYS, 0.0f) };
    _provider.AddChild(target);

    var chasing = await _fly(Vector2.Right, target);

    chasing.X.ShouldBeGreaterThan(0.0f);
  }

  [Test]
  public async Task AMissileWithNoTargetHoldsItsHeading() {
    var straight = await _fly(Vector2.Right, null);

    straight.X.ShouldBeGreaterThan(0.0f);
    straight.Y.ShouldBe(0.0f, 0.01f);
  }

  // Where the missile body ends up, relative to the muzzle it left, after a fixed flight.
  private async Task<Vector2> _fly(Vector2 direction, Node2D? target) {
    var missile = SceneHelpers.InstantiateNode<Missile>();
    _provider.AddChild(missile);
    missile.GlobalPosition = Vector2.Zero;
    if (target is not null) {
      missile.SetTarget(target);
    }
    missile.Shoot(direction);

    var body = missile.GetNode<CharacterBody2D>("CharacterBody2D");
    await PhysicsFrames.Advance(TestScene, FLIGHT_FRAMES);
    var travelled = body.GlobalPosition;

    missile.QueueFree();
    await PhysicsFrames.Frame(TestScene);
    return travelled;
  }
}
