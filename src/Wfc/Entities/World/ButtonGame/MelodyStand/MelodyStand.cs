namespace Wfc.Entities.World.ButtonGame;

using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

// The pad the melody is played from. It blinks for as long as the room is waiting to be asked,
// and the player stepping onto it is what asks.
[ScenePath]
public partial class MelodyStand : Node2D {
  #region Constants
  private const float BLINK_PERIOD = 0.55f;
  private const float BLINK_DIM = 0.42f;
  private const float HALO_PEAK = 0.95f;
  #endregion Constants

  #region Signals
  [Signal]
  public delegate void SteppedOnEventHandler();
  #endregion Signals

  // Whether the player is on the pad right now, so a room that starts waiting under their feet
  // does not sit there blinking at somebody already standing on it.
  public bool IsOccupied { get; private set; }

  public bool IsBlinking { get; private set; }

  private Tween? _blinkTween;

  #region Nodes
  [NodePath("Halo")]
  private Sprite2D _haloNode = default!;
  [NodePath("Spr")]
  private Sprite2D _spriteNode = default!;
  #endregion Nodes

  public override void _EnterTree() {
    base._EnterTree();
    this.WireNodes();
  }

  public void SetBlinking(bool blinking) {
    IsBlinking = blinking;
    _blinkTween?.Kill();
    _blinkTween = null;
    if (!blinking) {
      _spriteNode.Modulate = Colors.White;
      _haloNode.Modulate = new Color(Colors.White, 0.0f);
      return;
    }
    // One loop is dim-to-bright and back, so the pad reads as breathing rather than as a lamp
    // switching on and off - and the halo swells with it instead of on its own clock.
    _blinkTween = CreateTween().SetLoops();
    _blinkTween.SetParallel(true);
    _blinkTween.TweenProperty(_spriteNode, "modulate", Colors.White, BLINK_PERIOD)
      .From(new Color(BLINK_DIM, BLINK_DIM, BLINK_DIM));
    _blinkTween.TweenProperty(_haloNode, "modulate", new Color(Colors.White, HALO_PEAK), BLINK_PERIOD)
      .From(new Color(Colors.White, 0.0f));
    _blinkTween.SetParallel(false);
    _blinkTween.TweenProperty(_spriteNode, "modulate", new Color(BLINK_DIM, BLINK_DIM, BLINK_DIM), BLINK_PERIOD);
    _blinkTween.Parallel().TweenProperty(_haloNode, "modulate", new Color(Colors.White, 0.0f), BLINK_PERIOD);
  }

  public void _onDetectionAreaBodyEntered(Node body) {
    if (body is not Player.Player) {
      return;
    }
    IsOccupied = true;
    EmitSignal(SignalName.SteppedOn);
  }

  public void _onDetectionAreaBodyExited(Node body) {
    if (body is Player.Player) {
      IsOccupied = false;
    }
  }
}
