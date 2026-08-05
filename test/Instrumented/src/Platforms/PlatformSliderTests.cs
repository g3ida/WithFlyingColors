namespace Wfc.test.instrumented.Platforms;

using System;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Platforms;
using Wfc.Utils;
using Wfc.test.instrumented.Helpers;

// The slider puts a body the level already has on a run - the brick breaker's door, the tetris
// pool's floor - so what it has to get right is the body it drives and the units it drives it in.
public class PlatformSliderTests(Node testScene) : TestClass(testScene) {
  private const float SPEED = 6.0f;
  private const float DISTANCE = 200.0f;
  private const float CLOSE = 0.5f;
  private const double TIMEOUT = 4.0;

  private Node2D _host = default!;

  [Cleanup]
  public void Cleanup() => _host.QueueFree();

  [Test]
  public async Task ItCarriesTheBodyItIsParentedTo() {
    var body = _body(new Vector2(500.0f, 300.0f));
    var start = body.GlobalPosition;
    var slider = await _drive(body, s => s.Axis = PlatformSlide.SlideAxis.Vertical);

    (await _waitFor(() => body.GlobalPosition.Y >= start.Y + DISTANCE - CLOSE))
      .ShouldBeTrue("the body the slider was parented to never moved");
    slider.GlobalPosition.ShouldBe(body.GlobalPosition, "the slider was left behind by the body it drives");
  }

  // The distance is in the units the body was placed in, so a slider inside a scene that was scaled
  // as a whole runs as far as that scene's own measurements say - which is what the level author is
  // reading off when they set it.
  [Test]
  public async Task TheRunIsMeasuredInTheUnitsTheBodyIsPlacedIn() {
    _host = new Node2D { Scale = new Vector2(2.0f, 2.0f) };
    TestScene.AddChild(_host);
    var body = new AnimatableBody2D { Position = new Vector2(100.0f, 100.0f) };
    _host.AddChild(body);
    var start = body.GlobalPosition;

    var slider = SceneHelpers.InstantiateNode<PlatformSlider>();
    slider.Speed = SPEED;
    slider.Distance = DISTANCE;
    slider.WaitTime = 0.05f;
    body.AddChild(slider);
    await PhysicsFrames.Frame(TestScene);

    (await _waitFor(() => body.GlobalPosition.X >= start.X + (DISTANCE * 2.0f) - CLOSE))
      .ShouldBeTrue("a slider inside a scaled scene ran the wrong distance");
  }

  // What a level author gets when they drop a slider straight into a level, which is a slider with
  // nothing to move. Nothing about it is visible otherwise: the level simply has a platform that
  // never goes anywhere.
  [Test]
  public void ASliderWithNoBodyToMoveSaysSo() {
    _host = new Node2D();
    TestScene.AddChild(_host);
    var slider = SceneHelpers.InstantiateNode<PlatformSlider>();
    _host.AddChild(slider);

    slider._GetConfigurationWarnings().ShouldNotBeEmpty(
      "a slider parented to something that cannot carry the player said nothing about it"
    );
  }

  private AnimatableBody2D _body(Vector2 at) {
    var body = new AnimatableBody2D { Position = at };
    body.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(200.0f, 32.0f) } });
    _host = body;
    TestScene.AddChild(body);
    return body;
  }

  private async Task<PlatformSlider> _drive(Node2D body, Action<PlatformSlider> configure) {
    var slider = SceneHelpers.InstantiateNode<PlatformSlider>();
    slider.Speed = SPEED;
    slider.Distance = DISTANCE;
    slider.WaitTime = 0.05f;
    configure(slider);
    body.AddChild(slider);
    await PhysicsFrames.Frame(TestScene);
    return slider;
  }

  private Task<bool> _waitFor(Func<bool> until) => PhysicsFrames.WaitFor(TestScene, until, TIMEOUT);
}
