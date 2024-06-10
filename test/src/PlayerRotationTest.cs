namespace WithFlyingColors;

using System.Threading.Tasks;
using Godot;
using Chickensoft.GoDotTest;
using GodotTestDriver;
using GodotTestDriver.Drivers;
using Shouldly;
using Wfc.Entities.World.Player;

public class PlayerRotationActionTest : TestClass {
  private PlayerRotation _playerRotation = default!;
  private CharacterBody2D _playerRotationParent = default!;
  private Fixture _fixture = default!;

  public PlayerRotationActionTest(Node testScene) : base(testScene) { }

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
    await _playerRotation.GetTree().CreateTimer(duration).ToSignal(_playerRotation, "timeout");
    _playerRotationParent.Rotation.ShouldBe(Mathf.Pi);
  }
}
