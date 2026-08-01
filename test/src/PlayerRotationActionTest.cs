namespace WithFlyingColors;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using GodotTestDriver;
using Shouldly;
using Wfc.Entities.World.Player;
using Wfc.test;
using Wfc.Utils;

public class PlayerRotationActionTest(Node testScene) : TestClass(testScene) {
  private const float DURATION = 0.1f;
  private const float DELTA = 1.0f / 60.0f;
  // Enough steps for the rotation to finish and report itself done, however the
  // duration divides into a frame.
  private const int MAX_STEPS = 60;
  // Several whole circles, so the turns land back where they started.
  private const int MANY_TURNS = 100;
  // Far below the step an overshoot would show up as, but above the drift of adding one
  // relative turn to another.
  private const float TOLERANCE = 0.001f;

  private PlayerRotationAction _rotation = default!;
  private CharacterBody2D _bodyNode = default!;
  private Fixture _fixture = default!;

  [SetupAll]
  public async Task Setup() {
    _fixture = new Fixture(TestScene.GetTree());
    _bodyNode = new CharacterBody2D();
    await _fixture.AddToRoot(_bodyNode);
    _rotation = new PlayerRotationAction();
    _rotation.SetBody(_bodyNode);
  }

  [CleanupAll]
  public void Cleanup() => _fixture.Cleanup();

  [Test]
  public void TestExecuteWithPositiveAngle() {
    _startFacingForward();

    _rotation.Execute(1, Mathf.Pi, DURATION);

    _stepUntilDone().ShouldBeTrue("the rotation never reported completion");
    // Half a turn either way is the same facing, and folding names it the negative one.
    Mathf.AngleDifference(_bodyNode.Rotation, Mathf.Pi).ShouldBeCloseTo(0.0f);
  }

  // The clock and the angle advance in the same call, so the body can never be carried
  // past the target by an extra step and then pulled back onto it.
  [Test]
  public void TestNeverTurnsPastItsTarget() {
    _startFacingForward();
    var target = MathUtils.PI2;

    _rotation.Execute(1, MathUtils.PI2, DURATION);

    for (var i = 0; i < MAX_STEPS && !_rotation.CanRotate; i++) {
      _rotation.Step(DELTA);
      _bodyNode.Rotation.ShouldBeLessThanOrEqualTo(target + TOLERANCE);
    }
    _bodyNode.Rotation.ShouldBeCloseTo(target, TOLERANCE);
  }

  // Turning always the same way used to leave the body's own angle climbing a quarter turn
  // at a time forever, and a float that large can no longer say which quarter it is on.
  [Test]
  public void TestTheAngleStaysBoundedAcrossManyTurns() {
    _startFacingForward();

    for (var turn = 0; turn < MANY_TURNS; turn++) {
      _rotation.Execute(1, MathUtils.PI2, DURATION);
      _stepUntilDone().ShouldBeTrue($"turn {turn} never reported completion");
    }

    Mathf.Abs(_bodyNode.Rotation).ShouldBeLessThanOrEqualTo(Mathf.Pi);
    Mathf.Abs(_rotation.CurrentAngle).ShouldBeLessThanOrEqualTo(Mathf.Pi);
    // A whole number of circles, so the body is facing where it started.
    _bodyNode.Rotation.ShouldBeCloseTo(0.0f);
  }

  // These share one body, so each test says where it starts from rather than inheriting
  // whichever way the one before it left the cube facing.
  private void _startFacingForward() {
    _bodyNode.Rotation = 0.0f;
    _rotation.Reset(0.0f);
  }

  private bool _stepUntilDone() {
    for (var i = 0; i < MAX_STEPS; i++) {
      _rotation.Step(DELTA);
      if (_rotation.CanRotate) {
        return true;
      }
    }
    return false;
  }
}
