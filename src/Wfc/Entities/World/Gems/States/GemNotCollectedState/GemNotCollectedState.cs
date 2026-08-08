namespace Wfc.Entities.World.Gems;

using System;
using System.Linq;
using Godot;
using Wfc.Autoload;
using Wfc.State;
using Wfc.Utils.Animation;
using Wfc.Utils.Colors;
using static Godot.Area2D;

public partial class GemNotCollectedState : GemBaseState {
  private const float AMPLITUDE = 4.0f;
  private const float ANIMATION_DURATION = 4.0f;
  private const float SHINE_VARIANCE = 0.08f;
  private const float ROTATION_SPEED = 0.002f;

  private NodeOscillator? _oscillator;
  private AreaEnteredEventHandler? _areaEnteredEventHandler;
  private IState<Gem>? _requestedState = null;
  private IStatesStore<Gem> _statesStore;

  public GemNotCollectedState(IStatesStore<Gem> statesStore, Gem gem) : base() {
    _statesStore = statesStore;
    _oscillator = new NodeOscillator(gem, AMPLITUDE, ANIMATION_DURATION);
  }

  public override void Enter(Gem o) {
    // The store hands out one instance per state, so this field outlives the visit that
    // set it. Left over, the very first physics frame after a respawn re-requests the
    // transition the gem made before it died - the gem collects itself, silently, before
    // the animation can emit GemCollected.
    _requestedState = null;

    o.AnimationPlayerNode.Play("RESET");
    o.AnimatedSpriteNode.Play("default");
    o.ShineSfxNode.Play();

    _areaEnteredEventHandler = (Area2D area) => {
      _requestedState = _handleAreaEntered(o, area);
    };
    o.AreaEntered += _areaEnteredEventHandler;
  }

  public override void Exit(Gem o) {
    o.ShineSfxNode.Stop();
    if (_areaEnteredEventHandler != null) {
      o.AreaEntered -= _areaEnteredEventHandler;
      _areaEnteredEventHandler = null;
    }
    o.CollisionShapeNode.Disabled = false;
  }

  public override IState<Gem>? PhysicsUpdate(Gem gem, float delta) {
    gem.LightNode.Position = gem.AnimatedSpriteNode.Position;
    _oscillator?.Update(delta);
    var timer = _oscillator?.Timer ?? 0f;
    gem.LightNode.Energy =
      (1 + SHINE_VARIANCE * (float)Math.Sin(2 * Mathf.Pi * timer / ANIMATION_DURATION)) * gem.LightEnergyScale;
    gem.LightNode.Rotate(ROTATION_SPEED);
    return _requestedState;
  }

  public IState<Gem>? _handleAreaEntered(Gem gem, Area2D area) {
    if (_requestedState != null) {
      return _requestedState;
    }
    // FIXME: We should remove the player area or make it inactive instead of doing
    // the check here
    if (Global.Instance().Player.IsDying())
      return null;
    // A gem the level has already given up asks nothing of the player: whichever face
    // reaches it takes it, and taking it is worth nothing.
    if (area.IsInGroup(gem.GroupName) || (gem.IsAlreadyCollected && _isPlayerFace(area))) {
      gem.Take();
      return _statesStore.GetState<GemCollectingState>();
    }
    return null;
  }

  // The cube's faces and corners are the only areas that carry a color group, so this
  // keeps a ghost from being taken by whatever else happens to share their layer.
  private static bool _isPlayerFace(Area2D area) =>
    ColorUtils.COLOR_GROUPS.Any(colorGroup => area.IsInGroup(colorGroup));
}
