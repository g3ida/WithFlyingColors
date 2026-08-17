namespace Wfc.Entities.World.Player.Test;

using System;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Chickensoft.GodotTestDriver;
using Godot;
using Shouldly;
using Wfc.Core.Event;
using Wfc.Entities.World.Player;
using Wfc.test;
using Wfc.Utils;
using EventHandler = Wfc.Core.Event.EventHandler;

public class PlayerSlipperingStateTest(Node testScene) : TestClass(testScene) {
  private Fixture _fixture = null!;
  private SignalsCounter _signalsCounter = new SignalsCounter();

  private static readonly string BASE_FIXTURE_PATH = "test/src/Wfc/Entities/World/Player/State/Fixture/";

  // These fixtures run real physics: the cube has to overbalance, rotate and fall out of
  // the world. Generous, because a loaded CI runner stretches the wall clock more than
  // the physics steps - but bounded, which is the whole point.
  private const double DEATH_TIMEOUT = 10.0;

  [Setup]
  public void Setup() {
    _fixture = new Fixture(TestScene.GetTree());
  }


  [Cleanup]
  public void Cleanup() {
    _fixture.Cleanup();
    _signalsCounter.Clear();
  }

  [Test]
  public async Task PlayerOnRightEdge_ShouldSlipperAndRotate() {
    _signalsCounter.Connect("slippering", EventHandler.Instance.Events, Events.SignalName.PlayerSlippering);

    var PlayerOnEdgeNode = await _fixture.LoadAndAddScene<Node>(BASE_FIXTURE_PATH + "PlayerOnRightEdge.tscn");
    var player = PlayerOnEdgeNode.GetNode<Player>("Player");
    player.Rotation.ShouldBeCloseTo(0f, epsilon: 0.15f);

    var died = await PlayerOnEdgeNode.GetTree()
      .ExpectSignal(EventHandler.Instance.Events, Events.SignalName.PlayerDied, DEATH_TIMEOUT);

    died.ShouldBeTrue("the player never slipped off the edge and died");
    player.Rotation.ShouldBeCloseTo(MathUtils.PI2);
    _signalsCounter.getCallCount("slippering").ShouldBe(1);
  }

  [Test]
  public async Task PlayerOnLeftEdge_ShouldSlipperAndRotate() {
    _signalsCounter.Connect("slippering", EventHandler.Instance.Events, Events.SignalName.PlayerSlippering);
    var PlayerOnEdgeNode = await _fixture.LoadAndAddScene<Node>(BASE_FIXTURE_PATH + "PlayerOnLeftEdge.tscn");

    var player = PlayerOnEdgeNode.GetNode<Player>("Player");
    player.Rotation.ShouldBeCloseTo(0f, epsilon: 0.1f);

    var died = await PlayerOnEdgeNode.GetTree()
      .ExpectSignal(EventHandler.Instance.Events, Events.SignalName.PlayerDied, DEATH_TIMEOUT);

    died.ShouldBeTrue("the player never slipped off the edge and died");
    player.Rotation.ShouldBeCloseTo(-MathUtils.PI2);
    _signalsCounter.getCallCount("slippering").ShouldBe(1);
  }

  [Test]
  public async Task PlayerOnLeftEdgeStairs_ShouldJustSlipper() {
    _signalsCounter.Connect("slippering", EventHandler.Instance.Events, Events.SignalName.PlayerSlippering);
    var PlayerOnEdgeNode = await _fixture.LoadAndAddScene<Node>(BASE_FIXTURE_PATH + "PlayerOnLeftEdgeStairs.tscn");

    var player = PlayerOnEdgeNode.GetNode<Player>("Player");
    player.Rotation.ShouldBeCloseTo(0f, epsilon: 0.1f);

    await PlayerOnEdgeNode.GetTree().SleepFor(2.0);

    player.Rotation.ShouldBeCloseTo(0f);
    _signalsCounter.getCallCount("slippering").ShouldBe(1);
  }

  [Test]
  public async Task PlayerOnLeftEdgeGroundNear_ShouldJustSlipper() {
    _signalsCounter.Connect("slippering", EventHandler.Instance.Events, Events.SignalName.PlayerSlippering);
    var PlayerOnEdgeNode = await _fixture.LoadAndAddScene<Node>(BASE_FIXTURE_PATH + "PlayerOnLeftEdgeGroundNear.tscn");

    var player = PlayerOnEdgeNode.GetNode<Player>("Player");
    player.Rotation.ShouldBeCloseTo(0f, epsilon: 0.1f);

    await PlayerOnEdgeNode.GetTree().SleepFor(2.0);

    player.Rotation.ShouldBeCloseTo(0f);
    _signalsCounter.getCallCount("slippering").ShouldBe(1);
  }
}
