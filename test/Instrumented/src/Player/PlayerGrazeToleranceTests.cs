namespace Wfc.test.instrumented.Player;

using System.Collections.Generic;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Event;
using Wfc.Entities.World.Piano;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// The piano is the sharpest case of a coloured run the cube cannot walk to its own edge: the keys
// sit a few pixels apart, each wearing a colour that kills the one beside it, and the cube is
// two thirds the width of a key. Reaching the end of the key it is standing on used to put a
// sliver of the cube over the next one and kill it there, well before it had stepped onto it.
public class PlayerGrazeToleranceTests(Node testScene) : TestClass(testScene) {
  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";

  // The cube wears purple underneath, so this is the key it may stand on and the one beside it
  // is the one that kills.
  private const string A_KEY_THE_CUBE_MAY_STAND_ON = "PianoNote3";
  private const string THE_KEY_BESIDE_IT = "PianoNote4";

  private const float A_SLIVER = 5.0f;
  private const float A_STEP_ONTO_THE_NEXT_KEY = 25.0f;
  private const int A_CONTACT_AND_THE_STATE_THAT_TAKES_IT = 4;

  private FakeDependenciesProvider _services = default!;
  private PianoScene _piano = default!;
  private float _restingSurfaceY;

  [Setup]
  public async Task Setup() {
    _services = new FakeDependenciesProvider();
    TestScene.AddChild(_services);
    _piano = SceneHelpers.InstantiateNode<PianoScene>();
    _services.AddChild(_piano);
    // The room reads a camera off the level it expects to sit in, and there is none here.
    _piano.PropagateCall(Node.MethodName.SetProcess, new Godot.Collections.Array { false });
    await PhysicsFrames.Frame(TestScene);
  }

  [Cleanup]
  public void Cleanup() => _services.QueueFree();

  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ReachingOverTheEdgeOfAKeyOntoTheNextIsSurvived() {
    var player = _cubeOnItsKeyReachingOver(A_SLIVER);

    await PhysicsFrames.Advance(TestScene, A_CONTACT_AND_THE_STATE_THAT_TAKES_IT);

    player.IsDying().ShouldBeFalse("a sliver over the next key killed the cube");
  }

  // The rule this has to leave standing: the next key is still lethal, it just has to be stood on.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task SteppingOntoTheNextKeyStillKills() {
    var player = _cubeOnItsKeyReachingOver(A_STEP_ONTO_THE_NEXT_KEY);

    await PhysicsFrames.Advance(TestScene, A_CONTACT_AND_THE_STATE_THAT_TAKES_IT);

    player.IsDying().ShouldBeTrue("the wrong key stopped being lethal to stand on");
  }

  // Surviving the reach is half of it. A key answers the cube standing on it, and a key the cube
  // is only reaching across would sound a note the player never played - and hand it to the sheet
  // as their answer, which is a wrong one and costs them the run.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ReachingOverTheNextKeyDoesNotSoundIt() {
    var sounded = new List<int>();
    using var binding = GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.PianoNotePressed message) => sounded.Add(message.NoteIndex));

    _cubeOnItsKeyReachingOver(A_SLIVER);

    (await PhysicsFrames.WaitFor(TestScene, () => sounded.Count > 0, 5.0))
      .ShouldBeTrue("the key the cube is standing on never sounded");
    sounded.ShouldContain(_note(A_KEY_THE_CUBE_MAY_STAND_ON).Index);
    // Long enough that the other key would have finished its own press had it started one.
    await PhysicsFrames.Advance(TestScene, 40);
    sounded.ShouldNotContain(
      _note(THE_KEY_BESIDE_IT).Index, "the key the cube was only reaching over sounded anyway");
  }

  // A key is watched after the cube arrives, because the cube can still be walking onto it. That
  // watch has to end the moment the key answers: a key that kept answering for as long as the
  // cube was over it would re-press itself the instant its own release let go, and stand there
  // sounding the same note over and over under a cube that was not moving at all.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task AKeyStoodOnSoundsOnce() {
    var sounded = new List<int>();
    using var binding = GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.PianoNotePressed message) => sounded.Add(message.NoteIndex));

    var player = _cubeStandingSquarelyOnItsKey();
    var key = _note(A_KEY_THE_CUBE_MAY_STAND_ON).Index;

    (await PhysicsFrames.WaitFor(TestScene, () => sounded.Contains(key), 5.0))
      .ShouldBeTrue("the key the cube is standing on never sounded");
    await PhysicsFrames.Advance(TestScene, 120);

    sounded.FindAll(note => note == key).Count
      .ShouldBe(1, "the key kept sounding under a cube that never moved");
    player.IsDying().ShouldBeFalse();
  }

  // Walked into rather than arrived at, in steps small enough that the contact never ends and no
  // second arrival is announced. Nothing but the contact being measured again each frame can
  // catch this one, and a graze forgiven once and then forgotten would let the cube walk the
  // whole keyboard.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task AGrazeWalkedOnIntoStillKills() {
    var player = _cubeOnItsKeyReachingOver(A_SLIVER);
    await PhysicsFrames.Advance(TestScene, A_CONTACT_AND_THE_STATE_THAT_TAKES_IT);
    player.IsDying().ShouldBeFalse("a sliver over the next key killed the cube");

    for (var reach = A_SLIVER; reach <= A_STEP_ONTO_THE_NEXT_KEY && !player.IsDying(); reach += 2.0f) {
      player.GlobalPosition = _positionReachingOver(player, reach);
      await PhysicsFrames.Frame(TestScene);
    }

    // Held where the walk ended rather than asserted the moment it ends: the contact is reported
    // by the face and taken up by the cube, which are not the same frame.
    for (var frame = 0; frame < A_CONTACT_AND_THE_STATE_THAT_TAKES_IT && !player.IsDying(); frame++) {
      player.GlobalPosition = _positionReachingOver(player, A_STEP_ONTO_THE_NEXT_KEY);
      await PhysicsFrames.Frame(TestScene);
    }

    player.IsDying().ShouldBeTrue("the cube walked on into the next key and was never charged for it");
  }

  private Wfc.Entities.World.Player.Player _cubeOnItsKeyReachingOver(float reach) {
    var player = _addCube();
    player.GlobalPosition = _positionReachingOver(player, reach);
    return player;
  }

  private Wfc.Entities.World.Player.Player _cubeStandingSquarelyOnItsKey() {
    var player = _addCube();
    player.GlobalPosition =
      new Vector2(_note(A_KEY_THE_CUBE_MAY_STAND_ON).GlobalPosition.X, _restingSurfaceY);
    return player;
  }

  private Wfc.Entities.World.Player.Player _addCube() {
    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    _services.AddChild(player);
    // Taken once, off the resting key, and held for the rest of the test. A key the cube stands
    // on sinks under it, and a cube that rode that down would leave the height where the keys
    // wear their colours - on a slower machine, before it had walked far enough to be charged.
    var key = _note(A_KEY_THE_CUBE_MAY_STAND_ON);
    _restingSurfaceY = key.GlobalPosition.Y - _keySurfaceDrop(key) - player.GetCollisionHalfExtents().Y;
    return player;
  }

  // `reach` is how far the cube's own edge passes the near edge of the next key's colour, which is
  // the width of cube that key covers - the quantity the tolerance is written in terms of.
  private Vector2 _positionReachingOver(Wfc.Entities.World.Player.Player player, float reach) {
    var next = _note(THE_KEY_BESIDE_IT);
    var nextColorLeft = next.GlobalPosition.X - (_colorAreaSize(next).X * 0.5f);
    return new Vector2(nextColorLeft - player.GetCollisionHalfExtents().X + reach, _restingSurfaceY);
  }

  private static Vector2 _colorAreaSize(PianoNote note) =>
    ((RectangleShape2D)note.GetNode<CollisionShape2D>("ColorArea/CollisionShape2D").Shape).Size;

  // The key's own body is what the cube rests on, and its top edge is where the shape puts it.
  private static float _keySurfaceDrop(PianoNote note) {
    var shape = note.GetNode<CollisionShape2D>("CollisionShape2D");
    return -shape.Position.Y + (((RectangleShape2D)shape.Shape).Size.Y * 0.5f);
  }

  private PianoNote _note(string name) => _piano.GetNode<PianoNote>($"Piano/NotesContainer/{name}");
}
