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

}

