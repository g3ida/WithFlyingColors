namespace Wfc.test.instrumented.Enemies;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Enemies;
using Wfc.test.instrumented.Helpers;
using Wfc.Utils;

public class SlidingLazerTests(Node testScene) : TestClass(testScene) {
  private const int HELD_FRAMES = 10;

  [Test]
  public async Task ASlidingLazerSweepsAlongItsOffsetAndOnlyItsOffset() {
    var beam = SceneHelpers.InstantiateNode<SlidingLazer>();
    beam.SlideOffset = new Vector2(0f, Constants.WORLD_TO_SCREEN);
    beam.WaitTime = 0f;
    TestScene.AddChild(beam);
    await PhysicsFrames.Frame(TestScene);
    var start = beam.Position;

    for (var frame = 0; frame < HELD_FRAMES; frame++) {
      await PhysicsFrames.Frame(TestScene);
    }

    beam.Position.X.ShouldBe(start.X);
    beam.Position.Y.ShouldBeGreaterThan(start.Y);

    beam.QueueFree();
    await PhysicsFrames.Frame(TestScene);
  }
}
