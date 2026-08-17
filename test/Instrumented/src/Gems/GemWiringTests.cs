namespace Wfc.test.instrumented.Gems;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Chickensoft.GodotNodeInterfaces;
using Godot;
using Shouldly;
using Wfc.Entities.World.Gems;
using Wfc.Utils;

// The gem's five node references come from the [Node] attributes alone, against the real scene.
// Nothing else wires them, so a path that stops naming something has only this to fail it.
public class GemWiringTests(Node testScene) : TestClass(testScene) {
  private Gem _gem = default!;

  [Setup]
  public async Task Setup() {
    _gem = SceneHelpers.InstantiateNode<Gem>();
    TestScene.AddChild(_gem);
    await _idle();
  }

  [Cleanup]
  public void Cleanup() => _gem.QueueFree();

  [Test]
  public void WireNodesFillsEveryNodeFieldTest() {
    _gem.CollisionShapeNode.ShouldNotBeNull();
    _gem.LightNode.ShouldNotBeNull();
    _gem.ShineSfxNode.ShouldNotBeNull();
    _gem.AnimatedSpriteNode.ShouldNotBeNull();
    _gem.AnimationPlayerNode.ShouldNotBeNull();
  }

  // Each field has to be the node the attribute names, not merely something non-null: the two
  // sprite-side fields sit at different depths and are the pair most easily crossed.
  [Test]
  public void EveryNodeFieldIsTheOneItsPathNamesTest() {
    _wrapped(_gem.CollisionShapeNode).ShouldBeSameAs(_gem.GetNode("CollisionShape2D"));
    _wrapped(_gem.LightNode).ShouldBeSameAs(_gem.GetNode("PointLight2D"));
    _wrapped(_gem.ShineSfxNode).ShouldBeSameAs(_gem.GetNode("ShineSfx"));
    _wrapped(_gem.AnimatedSpriteNode).ShouldBeSameAs(_gem.GetNode("AnimatedSprite2D"));
    _wrapped(_gem.AnimationPlayerNode).ShouldBeSameAs(_gem.GetNode("AnimatedSprite2D/AnimationPlayer"));
  }

  // _Ready reads the light and the sprite as soon as it has them, so a field nothing had wired
  // would have taken the gem down on the way in rather than later.
  [Test]
  public void TheGemJoinsItsColorGroupAndPaintsItselfTest() {
    _gem.IsInGroup(_gem.GroupName).ShouldBeTrue();
    _gem.LightNode.Color.ShouldBe(_gem.ShineColor);
  }

  // [Node] hands back an adapter rather than the node itself, so identity has to be asked of
  // what the adapter wraps.
  private static Node _wrapped(IGodotObject adapted) =>
    (Node)((IGodotObjectAdapter)adapted).TargetObj;

  private async Task _idle() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
  }
}
