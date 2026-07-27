namespace Wfc.test.instrumented.BrickBreaker;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.BrickBreaker;
using Wfc.test;

// Breaking is a one-way door, but the brick used to be able to walk through it several
// times in one frame: QueueFree is deferred, so its Area2D keeps monitoring for the rest
// of the tick, and the mask admits the balls and all eight of the player's face and
// corner areas at once. Each extra report decremented the room's counter, which tested
// for exactly zero - so the arena emptied and never reported itself cleared.
public class BrickTests(Node testScene) : TestClass(testScene) {
  private const string BRICK_SCENE = "res://src/Wfc/Entities/World/BrickBreaker/Brick/Brick.tscn";

  private Node2D _root = default!;
  private SignalsCounter _signalsCounter = new();

  [Setup]
  public async Task Setup() {
    _root = new Node2D();
    TestScene.AddChild(_root);
    await _idle();
  }

  [Cleanup]
  public void Cleanup() {
    _signalsCounter.Clear();
    _root.QueueFree();
  }

  [Test]
  public async Task ReportsItselfBrokenOnce() {
    var brick = await _addBrick();
    var area = brick.GetNode<Area2D>("Area2D");
    _signalsCounter.Connect("broken", brick, Brick.SignalName.brickBroken);

    _hit(area);

    _signalsCounter.getCallCount("broken").ShouldBe(1);
  }

  [Test]
  public async Task ReportsItselfBrokenOnceEvenWhenHitRepeatedlyInOneFrame() {
    var brick = await _addBrick();
    var area = brick.GetNode<Area2D>("Area2D");
    _signalsCounter.Connect("broken", brick, Brick.SignalName.brickBroken);

    // A cube landing square on a brick delivers a face and two corners at once.
    _hit(area);
    _hit(area);
    _hit(area);

    _signalsCounter.getCallCount("broken").ShouldBe(1, "the room's brick counter is decremented once per report");
  }

  private async Task<Brick> _addBrick() {
    var brick = GD.Load<PackedScene>(BRICK_SCENE).Instantiate<Brick>();
    _root.AddChild(brick);
    await _idle();
    return brick;
  }

  // The contact itself, not the physics that would produce it: the bug is in what the
  // brick does with a report it has already acted on.
  private static void _hit(Area2D area) => area.EmitSignal(Area2D.SignalName.AreaEntered, new Area2D());

  private async Task _idle() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
  }
}
