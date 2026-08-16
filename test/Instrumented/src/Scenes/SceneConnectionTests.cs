namespace Wfc.test.instrumented.Scenes;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.Ui.Slots;
using Wfc.Entities.World.BrickBreaker;
using Wfc.Utils;

// Signals wired in a .tscn name their handler as a string, so renaming one is the one refactor
// the compiler cannot check: the method moves, the scene goes on naming the old one, and the
// signal quietly reaches nobody. These are the handlers that were renamed off the GDScript-era
// _on_Node_signal spelling, asserted against the scenes that call them.
public class SceneConnectionTests(Node testScene) : TestClass(testScene) {
  private const string BRICK = "res://src/Wfc/Entities/World/BrickBreaker/Brick/Brick.tscn";
  private const string BALL = "res://src/Wfc/Entities/World/BrickBreaker/BouncingBall/BouncingBall.tscn";
  private const string ARENA = "res://src/Wfc/Entities/World/BrickBreaker/BrickBreaker/BrickBreaker.tscn";
  private const string SELECT_SLOT = "res://src/Wfc/Screens/SelectSlotMenu/SelectSlotMenu.tscn";

  private readonly System.Collections.Generic.List<Node> _built = [];

  [Cleanup]
  public void Cleanup() {
    foreach (var node in _built) {
      node.QueueFree();
    }
    _built.Clear();
  }

  [Test]
  public async Task ABrickAnswersItsAreaTest() {
    var brick = await _build<Brick>(BRICK);

    _isWired(brick, "Area2D", Area2D.SignalName.AreaEntered, "_onAreaEntered")
      .ShouldBeTrue();
  }

  [Test]
  public async Task ABouncingBallAnswersItsAreaTest() {
    var ball = await _build<BouncingBall>(BALL);

    _isWired(ball, "Area2D", Area2D.SignalName.AreaEntered, "_onAreaEntered")
      .ShouldBeTrue();
  }

  [Test]
  public async Task TheArenaAnswersItsLevelUpTimerTest() {
    var arena = await _build<BrickBreaker>(ARENA);

    _isWired(arena, "BricksContainer/LevelUpTimer", Timer.SignalName.Timeout, "_onLevelUpTimerTimeout")
      .ShouldBeTrue();
  }

  // The spelling the scenes used before, which nothing should answer to any more. Without this
  // the assertions above would pass just as well against a scene still naming the old handler,
  // because a connection to a method that does not exist is still a connection.
  [Test]
  public async Task NothingAnswersTheOldSpellingTest() {
    var brick = await _build<Brick>(BRICK);

    _isWired(brick, "Area2D", Area2D.SignalName.AreaEntered, "_on_Area2D_area_entered")
      .ShouldBeFalse();
  }

  // An [Export] is stored in the scene under the field's own name, so renaming the field
  // renames the key the scene has to use. Get it wrong and Godot drops the unknown property
  // without a word: the build stays green, and the slots quietly stop centring.
  //
  // Instantiated but never added to the tree - exported values are applied by Instantiate, and
  // the menu wants a dependency provider it has no business having here.
  [Test]
  public void TheSlotPickerKeepsItsExportedCentringTest() {
    var menu = GD.Load<PackedScene>(SELECT_SLOT).ShouldNotBeNull().Instantiate();
    try {
      var slots = menu.GetNode<SlotsContainer>("SlotsContainer");

      slots.CenterVertically.ShouldBeTrue("the slot picker centres itself down the screen");
      slots.CenterHorizontally.ShouldBeFalse("and is left where the scene put it across");
    }
    finally {
      menu.Free();
    }
  }

  private static bool _isWired(Node owner, string childPath, string signal, string method) =>
    owner.GetNode(childPath).IsConnected(signal, new Callable(owner, method));

  // Loaded by path rather than through SceneHelpers: none of these three carries a [ScenePath],
  // because each is placed by whatever builds it rather than instanced by name.
  private async Task<T> _build<T>(string scenePath) where T : Node {
    var scene = GD.Load<PackedScene>(scenePath).ShouldNotBeNull($"no scene at '{scenePath}'");
    var node = scene.Instantiate<T>();
    _built.Add(node);
    TestScene.AddChild(node);
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    return node;
  }
}
