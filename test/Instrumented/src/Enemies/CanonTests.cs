namespace Wfc.test.instrumented.Enemies;

using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Enemies;
using Wfc.Utils;
using Wfc.test.instrumented.Helpers;

// The canon on its mount: it turns to whatever it is set to follow and shoots at it. What has to
// hold is that looking at the player and shooting at the player are the same thing - a canon that
// tracks perfectly and never fires is scenery, and that is what an unwrapped angle and a firing
// window narrower than one tick of the turn left behind.
public class CanonTests(Node testScene) : TestClass(testScene) {
  // Comfortably inside the canon's own range, and far enough that the barrel has to swing to hold a
  // target that moves.
  private const float DROP = 400.0f;
  private const double TIMEOUT = 6.0;

  private Canon _canon = default!;
  private Node2D _target = default!;
  private int _stood;

  [Cleanup]
  public void Cleanup() {
    foreach (var bullet in _bullets()) {
      bullet.QueueFree();
    }
    if (GodotObject.IsInstanceValid(_canon)) {
      _canon.QueueFree();
    }
    if (GodotObject.IsInstanceValid(_target)) {
      _target.QueueFree();
    }
  }

  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ItShootsAtAStandingTargetBelowIt() {
    await _stand(new Vector2(0.0f, DROP));

    (await PhysicsFrames.WaitFor(TestScene, () => _bullets().Any(), TIMEOUT))
      .ShouldBeTrue("the canon never fired at a target standing in front of it");
  }

  // A canon that fires once and then stares is the same to the player as one that never fires.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ItGoesOnShootingOnceItsCooldownIsUp() {
    await _stand(new Vector2(0.0f, DROP));

    (await _shotsWithin(240)).ShouldBeGreaterThanOrEqualTo(3, "the canon stopped after its first shot");
  }

  // The one the player reported. A canon that closes the last of its aim in ever smaller steps, or
  // that swings past a firing window narrower than its own turn rate, follows the player across the
  // whole room without ever taking a shot.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ItKeepsUpWithATargetThatIsRunning() {
    await _stand(new Vector2(-300.0f, DROP));

    // Back and forth under the canon at the speed the cube runs, so the barrel is always having to
    // catch up and the target is always somewhere it could be shot at.
    var swept = await _shotsWithin(240, frame => {
      var along = Mathf.Sin(frame / 40.0f) * 260.0f;
      _target.Position = new Vector2(along, DROP);
    });

    swept.ShouldBeGreaterThanOrEqualTo(2, "the canon tracked a running target without shooting at it");
  }

  // The arc the mount allows is the whole of what the canon may do: it hangs pointing down, so a
  // target above it is one it can neither turn to nor fire at.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ItHoldsFireAtATargetItCannotTurnTo() {
    await _stand(new Vector2(0.0f, -DROP));
    await PhysicsFrames.Advance(TestScene, 120);

    _bullets().ShouldBeEmpty("the canon fired through its own mount at something above it");
  }

  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ItHoldsFireAtATargetOutOfRange() {
    await _stand(new Vector2(2000.0f, DROP));
    await PhysicsFrames.Advance(TestScene, 120);

    _bullets().ShouldBeEmpty("the canon fired at something it cannot reach");
  }

  // How many bullets left the barrel over a stretch, counted by which ones have been seen rather
  // than by how many are in the air: a shot fired early is well out of the room by the time the next
  // one leaves.
  private async Task<int> _shotsWithin(int frames, System.Action<int>? move = null) {
    var fired = new System.Collections.Generic.HashSet<ulong>();
    for (var frame = 0; frame < frames; frame++) {
      move?.Invoke(frame);
      await PhysicsFrames.Frame(TestScene);
      foreach (var bullet in _bullets()) {
        fired.Add(bullet.GetInstanceId());
      }
    }
    return fired.Count;
  }

  private System.Collections.Generic.IEnumerable<Bullet> _bullets() =>
    TestScene.GetChildren().OfType<Bullet>().Where(GodotObject.IsInstanceValid);

  private async Task _stand(Vector2 targetOffset) {
    // Named per test: the target of the test before this one is only queued for freeing, so a name
    // they share is a canon that spends the whole test following a corpse.
    var name = $"Target{++_stood}";
    _target = new Node2D { Name = name, Position = targetOffset };
    TestScene.AddChild(_target);

    _canon = SceneHelpers.InstantiateNode<Canon>();
    // Set before the canon enters the tree: it looks its target up once, as it comes up.
    _canon.ObjectToFollow = new NodePath($"../{name}");
    _canon.cooldown = 0.5f;
    TestScene.AddChild(_canon);
    await PhysicsFrames.Frame(TestScene);
  }
}
