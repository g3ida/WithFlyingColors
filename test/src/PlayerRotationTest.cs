namespace WithFlyingColors;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using GodotTestDriver;
using Shouldly;
using Wfc.Entities.World.Player;
using Wfc.test;

public class PlayerRotationActionTest(Node testScene) : TestClass(testScene) {
  private PlayerRotation _playerRotation = default!;
  private CharacterBody2D _playerRotationParent = default!;
  private Fixture _fixture = default!;

  [SetupAll]
  public async Task Setup() {
    _fixture = new Fixture(TestScene.GetTree());
    _playerRotationParent = new CharacterBody2D();
    await _fixture.AddToRoot(_playerRotationParent);
    _playerRotation = new PlayerRotation(_playerRotationParent);
  }

  [CleanupAll]
  public void Cleanup() => _fixture.Cleanup();

  [Test]
  public async Task TestExecuteWithPositiveAngle() {
    _playerRotationParent.Rotation.ShouldBe(0);
    var duration = 0.1f;
    _playerRotation.Fire(Mathf.Pi, duration);
    await _playerRotation
      .GetTree()
      .CreateTimer(duration + Mathf.Epsilon)
      .ToSignal(_playerRotation, nameof(PlayerRotation.RotationCompleted));
    _playerRotationParent.Rotation.ShouldBeCloseTo(Mathf.Pi);
  }
}
