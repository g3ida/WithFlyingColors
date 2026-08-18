namespace Wfc.test.instrumented.Minigames;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Event;
using Wfc.Entities.World.Piano;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// A wrong note used to be a sound and nothing else. The board is what keeps score, and it is
// mounted well above the keys, so it says so itself now.
public class SolfegeBoardWrongNoteTests(Node testScene) : TestClass(testScene) {
  private FakeDependenciesProvider _services = default!;
  private PianoScene _scene = default!;

  [Setup]
  public async Task Setup() {
    _services = new FakeDependenciesProvider();
    TestScene.AddChild(_services);
    _scene = SceneHelpers.InstantiateNode<PianoScene>();
    _services.AddChild(_scene);
    // The room reads a camera off the level it expects to sit in, and there is none here.
    _scene.PropagateCall(Node.MethodName.SetProcess, new Godot.Collections.Array { false });
    await PhysicsFrames.Frame(TestScene);
  }

  [Cleanup]
  public void Cleanup() => _services.QueueFree();

  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task AWrongNoteReddensThePaperAndThenClearsIt() {
    _startPuzzle();

    GameEvents.Instance.OnPianoNotePressed(_aNoteThatIsNotExpected());

    (await PhysicsFrames.WaitFor(TestScene, () => _paperTint() != Colors.White, 5.0))
      .ShouldBeTrue("the paper never coloured on a wrong note");
    _paperTint().G.ShouldBeLessThan(1.0f, "the flash should read as red, not as a dimming");
    _paperTint().R.ShouldBe(1.0f, 0.01f);

    (await PhysicsFrames.WaitFor(TestScene, () => _paperTint() == Colors.White, 10.0))
      .ShouldBeTrue("the paper stayed coloured after the flash");
  }

  // The sheet of paper is what went wrong, not the stand holding it up. Tinting this node is
  // the one obvious way to colour the page and it takes its siblings with it, the support bar
  // under the board included.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task TheFlashLeavesTheStandAlone() {
    _startPuzzle();

    GameEvents.Instance.OnPianoNotePressed(_aNoteThatIsNotExpected());

    (await PhysicsFrames.WaitFor(TestScene, () => _paperTint() != Colors.White, 5.0))
      .ShouldBeTrue("the paper never coloured on a wrong note");
    _board().Modulate.ShouldBe(Colors.White, "the flash spread past the paper to the whole board");
    _board().GetNode<ColorRect>("BaseRect").Modulate.ShouldBe(Colors.White);
  }

  // A run cut short mid-flash - a death, a checkpoint load - must not hand the next one a red board.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ARestartClearsAFlashThatWasStillRunning() {
    _startPuzzle();
    GameEvents.Instance.OnPianoNotePressed(_aNoteThatIsNotExpected());
    (await PhysicsFrames.WaitFor(TestScene, () => _paperTint() != Colors.White, 5.0))
      .ShouldBeTrue("the paper never coloured on a wrong note");

    GameEvents.Instance.OnCheckpointLoaded();
    await PhysicsFrames.Frame(TestScene);

    _paperTint().ShouldBe(Colors.White);
    // And stays cleared: the killed flash must not resume a frame later.
    await PhysicsFrames.Advance(TestScene, 20);
    _paperTint().ShouldBe(Colors.White);
  }

  private SolfegeBoard _board() => _scene.GetNode<SolfegeBoard>("Piano/SolfegeBoard");

  private Color _paperTint() =>
    (Color)((ShaderMaterial)_board().GetNode<Sprite2D>("MusicPaperRect").Material)
      .GetShaderParameter("modulate_color");

  // A bare cube rather than the scene: the trigger's guard is a type test, and instancing the
  // whole player would drag in a level for it to depend on. Freed straight after, or it outlives
  // the run as an orphan.
  private void _startPuzzle() {
    var cube = new Wfc.Entities.World.Player.Player();
    _scene.GetNode<Area2D>("TriggerArea").EmitSignal(Area2D.SignalName.BodyEntered, cube);
    cube.QueueFree();
    _board().IsStopped().ShouldBeFalse("the puzzle should be running");
  }

  // The sheet opens on Do, so anything else is a miss.
  private static int _aNoteThatIsNotExpected() => (int)MusicNote.Re;
}
