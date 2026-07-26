namespace Wfc.test.instrumented.Minigames;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.Tetris;
using Wfc.Entities.World.Piano;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// Both minigames are armed by walking into an Area2D that only the player can enter, and
// the port turned "ignore everything that is not the player" into its own negation. The
// rooms then never started: no piece ever spawned, no note was ever expected. A guard
// this cheap to invert deserves a test that walks a body in and looks at the result.
public class MinigameTriggerTests(Node testScene) : TestClass(testScene) {
  private FakeDependenciesProvider _provider = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await _idle();
  }

  [Cleanup]
  public void Cleanup() => _provider.QueueFree();

  [Test]
  public async Task ThePianoStartsWhenThePlayerEntersTheTrigger() {
    var scene = await _add(SceneHelpers.InstantiateNode<PianoScene>());
    var piano = scene.GetNode<Piano>("Piano");
    piano.IsStopped().ShouldBeTrue("the puzzle should be idle before the player arrives");

    _walkIntoTrigger(scene.GetNode<Area2D>("TriggerArea"), _player());

    piano.IsStopped().ShouldBeFalse("the puzzle should be running");
  }

  // Player death debris is on the player's collision layer too, so the trigger does get
  // handed bodies that are not the player and has to keep ignoring them.
  [Test]
  public async Task ThePianoIgnoresABodyThatIsNotThePlayer() {
    var scene = await _add(SceneHelpers.InstantiateNode<PianoScene>());
    var piano = scene.GetNode<Piano>("Piano");

    _walkIntoTrigger(scene.GetNode<Area2D>("TriggerArea"), new RigidBody2D());

    piano.IsStopped().ShouldBeTrue();
  }

  // TetrisPool boots paused and the trigger is what unpauses it. Consuming the trigger
  // is the visible half of that: nothing else in the class frees it.
  [Test]
  public async Task TheTetrisPoolStartsWhenThePlayerEntersTheTrigger() {
    var pool = await _add(SceneHelpers.InstantiateNode<TetrisPool>());
    var trigger = pool.GetNode<Area2D>("TriggerEnterArea");

    _walkIntoTrigger(trigger, _player());

    trigger.IsQueuedForDeletion().ShouldBeTrue("the trigger should have been consumed");
  }

  [Test]
  public async Task TheTetrisPoolIgnoresABodyThatIsNotThePlayer() {
    var pool = await _add(SceneHelpers.InstantiateNode<TetrisPool>());
    var trigger = pool.GetNode<Area2D>("TriggerEnterArea");

    _walkIntoTrigger(trigger, new RigidBody2D());

    trigger.IsQueuedForDeletion().ShouldBeFalse();
  }

  // A bare Player rather than the scene: the guard is a type test, and instancing the
  // whole player would drag in a level for it to depend on.
  private static Wfc.Entities.World.Player.Player _player() => new();

  private async Task<T> _add<T>(T node) where T : Node {
    _provider.AddChild(node);
    // Both scenes expect to sit inside a GameLevel and read its camera every frame. There
    // is no level here, and the trigger does not need one, so let them idle instead.
    node.PropagateCall(Node.MethodName.SetProcess, new Godot.Collections.Array { false });
    await _idle();
    return node;
  }

  // The handler is wired to body_entered in the .tscn, so raising the signal exercises
  // the connection and the guard together without needing the physics server to report
  // an overlap on a node parked at the origin. The emit is synchronous: the caller has to
  // assert before the next frame, because the tetris trigger frees itself on the way out.
  private static void _walkIntoTrigger(Area2D trigger, Node2D body) {
    trigger.EmitSignal(Area2D.SignalName.BodyEntered, body);
    body.QueueFree();
  }

  private async Task _idle() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
  }
}
