namespace Wfc.test.instrumented;

using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using GodotTestDriver;
using Shouldly;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.Entities.World.Player;
using Wfc.test;
using Wfc.Utils.Layers;

public class InputManagerTests(Node testScene) : TestClass(testScene) {
  private Fixture _fixture = null!;
  [Setup]
  public void Setup() {
    _fixture = new Fixture(TestScene.GetTree());
  }


  [Cleanup]
  public void Cleanup() {
    _fixture.Cleanup();
  }

  [Test]
  public void InputManagerActions_ShouldMatchProjectSettingsInputMap() {

    var engineActions = InputMap.GetActions().ToHashSet();
    foreach (var action in InputManager.Actions) {
      engineActions.Contains(action.Value)
        .ShouldBeTrue($"Action: '{action.Value}' doesn't exist in the engine's input map");
    }
  }

  // The hat and the left stick both sit on ui_down, so a hat press reaches _Input
  // alongside whatever the resting stick emits in the same frame. Only the press may
  // count as one: the menus move a row per press, and a second event passing for a
  // press is what let a single hat press skip a row.
  [Test]
  public async Task CountsOnlyThePressAmongTheEventsSharingItsFrame() {
    var manager = new InputManager();
    var action = InputManager.Actions[IInputManager.Action.UIDown];
    var events = InputMap.ActionGetEvents(action);
    var hat = events.OfType<InputEventJoypadButton>().First();
    var stick = events.OfType<InputEventJoypadMotion>().First();

    var press = new InputEventJoypadButton { ButtonIndex = hat.ButtonIndex, Pressed = true };
    var release = new InputEventJoypadButton { ButtonIndex = hat.ButtonIndex, Pressed = false };
    var drift = new InputEventJoypadMotion {
      Axis = stick.Axis,
      AxisValue = Mathf.Sign(stick.AxisValue) * InputMap.ActionGetDeadzone(action) * 0.5f
    };

    // Unbuffered so the press lands in the frame it is parsed in rather than the next,
    // and inside a process frame because a press parsed during a physics tick does not
    // read as just-pressed until one. The three events then share a frame, as they do
    // on a real pad.
    var accumulated = Input.UseAccumulatedInput;
    Input.UseAccumulatedInput = false;
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    try {
      Input.ParseInputEvent(press);

      manager.IsEventActionJustPressed(IInputManager.Action.UIDown, press).ShouldBeTrue();
      manager.IsEventActionJustPressed(IInputManager.Action.UIDown, drift).ShouldBeFalse();
      manager.IsEventActionJustPressed(IInputManager.Action.UIDown, release).ShouldBeFalse();
    }
    finally {
      Input.ParseInputEvent(release);
      Input.UseAccumulatedInput = accumulated;
    }
  }

  // One push of the stick is one row. It reaches the menu as a burst of motion events
  // rather than a press — several land in the frame the push is flushed in, and more
  // arrive as the axis wobbles over the deadzone — and taking each of them for a press
  // is what moved two rows at a time.
  [Test]
  public async Task MovesOneRowPerStickPush() {
    var navigation = new UINavigationInput(new InputManager());
    var action = InputManager.Actions[IInputManager.Action.UIDown];
    var stick = InputMap.ActionGetEvents(action).OfType<InputEventJoypadMotion>().First();
    var deadzone = InputMap.ActionGetDeadzone(action);
    var direction = Mathf.Sign(stick.AxisValue);

    var accumulated = Input.UseAccumulatedInput;
    Input.UseAccumulatedInput = false;
    var tree = TestScene.GetTree();

    // One frame's worth of the axis, given as how far it is pushed, and answering with
    // the rows the menu would have moved.
    async Task<int> Frame(params float[] deflections) {
      await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
      var rows = 0;
      foreach (var deflection in deflections) {
        var motion = new InputEventJoypadMotion { Axis = stick.Axis, AxisValue = direction * deflection };
        Input.ParseInputEvent(motion);
        if (navigation.IsJustPressed(IInputManager.Action.UIDown, motion)) {
          rows++;
        }
      }
      return rows;
    }

    try {
      await Frame(0f);
      (await Frame(0.6f, 0.9f)).ShouldBe(1, "a push flushed as several events in one frame");
      await Frame(0f);

      var chatter = await Frame(deadzone + 0.05f)
                  + await Frame(deadzone - 0.05f)
                  + await Frame(deadzone + 0.05f);
      chatter.ShouldBe(0, "a stick wobbling over the deadzone is not a push");
      await Frame(0f);

      var held = await Frame(0.6f) + await Frame(0.9f) + await Frame(1f);
      held.ShouldBe(1, "a stick pushed further while held is still the one push");
      await Frame(0f);

      var pushedTwice = await Frame(0.4f, 0.8f, 1f)
                      + await Frame(0.2f, 0f)
                      + await Frame(0.4f, 0.9f, 1f);
      pushedTwice.ShouldBe(2, "letting go and pushing again has to move another row");
    }
    finally {
      await Frame(0f);
      Input.UseAccumulatedInput = accumulated;
    }
  }
}

