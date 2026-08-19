namespace Wfc.Entities.World.Cutscenes;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Entities.World.Camera;
using Wfc.Screens.Levels;
using Wfc.Utils.Attributes;
using Wfc.Utils.Layers;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class CutsceneTrigger : Area2D {
  public override void _Notification(int what) => this.Notify(what);

  #region Exports
  // How long the camera takes to reach the marker, and the same again on the way back.
  [Export]
  public float TravelTime = 1.5f;

  // How long it rests on the marker once it has arrived, before it turns around.
  [Export]
  public float HoldTime = 1.5f;

  // Held after the stripes are in and the player is locked, before the camera pulls back.
  [Export]
  public float StartDelay = 0.0f;

  // How much of the world the shot shows, pulled back to over the beat after StartDelay and before
  // the camera moves. The stripes eat into the view, so a shot usually needs to see wider than the
  // room it plays in. Left at zero it opens on the room's own view, or on the one the camera has.
  [Export(PropertyHint.Range, "0,4,0.0001,or_greater")]
  public float Zoom = 0.0f;

  // The curve the camera travels on, out and back alike.
  [Export]
  public CameraEasing Easing = CameraEasing.Quad;

  // Where the curve spends its slow part: In leaves gently, Out settles onto the
  // marker, InOut does both.
  [Export]
  public Tween.EaseType Ease = Tween.EaseType.InOut;
  #endregion Exports

  private CutsceneShot _shot() => new(
    TravelTime: TravelTime,
    HoldTime: HoldTime,
    StartDelay: StartDelay,
    Zoom: Zoom,
    Easing: Easing,
    Ease: Ease
  );

  private Marker2D? _followChild = null;
  private bool _triggered = false;

  [Dependency]
  public IGameLevel GameLevel => this.DependOn<IGameLevel>();

  [Dependency]
  public IGameRepo GameRepo => this.DependOn<IGameRepo>();

  public override void _Ready() {
    // A trigger that reports no body is a shot that silently never plays, so the mask is enforced
    // here rather than left to each scene to remember. A localizer whose doorway this is sets the
    // same value again; a trigger standing on its own has nobody else to set it.
    CollisionMask = PhysicsLayers.Player.Mask;
    var children = GetChildren();
    foreach (var ch in children) {
      if (ch is Marker2D position2D) {
        _followChild = position2D;
      }
    }
  }

  private void _onBodyEntered(Node body) {
    if (!_triggered && body == GameRepo.Player.Value && _followChild != null) {
      _triggered = true;
      GameLevel.CutsceneNode.ShowSomeNode(_followChild, _shot());
    }
  }

  public override void _EnterTree() {
    base._EnterTree();
    Connect(
      Area2D.SignalName.BodyEntered,
      new Callable(this, nameof(_onBodyEntered)),
      (uint)ConnectFlags.Persist
    );

  }

  public override void _ExitTree() {
    base._ExitTree();
    Disconnect(
      Area2D.SignalName.BodyEntered,
      new Callable(this, nameof(_onBodyEntered))
    );
  }
}
