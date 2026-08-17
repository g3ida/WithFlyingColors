namespace Wfc.Entities.World.Gems.Test;

using Chickensoft.GoDotTest;
using Chickensoft.GodotNodeInterfaces;
using Godot;
using Moq;
using Shouldly;
using Wfc.Entities.World.Gems;

// What the gem does when it is taken or found already banked, with its five child nodes
// standing in as mocks: no scene is instantiated, nothing is added to the tree and no frame
// is stepped. GemWiringTests still covers the wiring itself against the real scene.
public class GemPickupTests(Node testScene) : TestClass(testScene) {
  private Mock<IPointLight2D> _light = default!;
  private Mock<IAnimatedSprite2D> _sprite = default!;
  private Mock<ICollisionPolygon2D> _collisionShape = default!;
  private Gem _gem = default!;

  [Setup]
  public void Setup() {
    _light = new Mock<IPointLight2D>();
    _sprite = new Mock<IAnimatedSprite2D>();
    _collisionShape = new Mock<ICollisionPolygon2D>();
    _gem = new Gem {
      LightNode = _light.Object,
      AnimatedSpriteNode = _sprite.Object,
      CollisionShapeNode = _collisionShape.Object,
    };
  }

  [Cleanup]
  public void Cleanup() => _gem.Free();

  [Test]
  public void AFreshGemIsWorthItsFullShineTest() {
    _gem.IsAlreadyCollected.ShouldBeFalse();
    _gem.IsBeingCollected.ShouldBeFalse();
    _gem.LightEnergyScale.ShouldBe(1f);
  }

  // The two colours come from the palette rather than from the gem, so a gem that reported the
  // same value for both would be painting its core in the shade meant for its light.
  [Test]
  public void TheCoreIsPalerThanTheShineTest() {
    _gem.CoreColor.ShouldNotBe(_gem.ShineColor);
  }

  [Test]
  public void AGemTheSlotHasBankedBurnsLowerTest() {
    _gem.MarkAlreadyCollected();

    _gem.IsAlreadyCollected.ShouldBeTrue();
    _gem.LightEnergyScale.ShouldBeLessThan(1f);
  }

  // The shape can only go away on a deferred call, so the flag is what everything landing on
  // the gem inside the same frame has to read.
  [Test]
  public void TakingAGemClosesItToAnythingElseThatFrameTest() {
    _gem.Take();

    _gem.IsBeingCollected.ShouldBeTrue();
    _collisionShape.Verify(
      shape => shape.SetDeferred(CollisionPolygon2D.PropertyName.Disabled, true),
      Times.Once
    );
  }
}
