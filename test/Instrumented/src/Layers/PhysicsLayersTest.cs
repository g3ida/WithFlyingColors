namespace Wfc.test.instrumented;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using GodotTestDriver;
using Shouldly;
using Wfc.Core.Event;
using Wfc.Entities.World.Player;
using Wfc.test;
using Wfc.Utils.Layers;

public class PhysicsLayersTest(Node testScene) : TestClass(testScene) {
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
  public void LayerNamesShouldMatchProjectSettings() {
    // Tuple of (LayerInfo, layer index in project settings)
    var layers = new (LayerInfo Info, int Index)[] {
      (PhysicsLayers.Default, 1),
      (PhysicsLayers.Player, 2),
      (PhysicsLayers.Platform, 3),
      (PhysicsLayers.FallZone, 4),
      (PhysicsLayers.BoxFace, 5),
      (PhysicsLayers.Gems, 6),
      (PhysicsLayers.Bullets, 7),
      (PhysicsLayers.Tetris, 8),
      (PhysicsLayers.PowerUp, 9),
      (PhysicsLayers.BouncingBall, 10),
      (PhysicsLayers.Bricks, 11),
    };

    foreach (var (info, idx) in layers) {
      var settingName = $"layer_names/2d_physics/layer_{idx}";
      var layerName = ProjectSettings.GetSetting(settingName).AsString();
      layerName.ShouldBe(info.Name, $"Layer {idx} name mismatch");
      // Optionally, also check that the bit value matches the expected value
      var expectedBit = 1 << (idx - 1);
      ((int)info.Mask).ShouldBe(expectedBit, $"Layer {info.Name} bit value mismatch");
    }
  }

}

