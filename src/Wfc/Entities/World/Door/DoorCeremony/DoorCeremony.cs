namespace Wfc.Entities.World.Door;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

// The light the four arch gems give up on their way to becoming the comet: one stream out of
// each socket, bowed out to the side so the four arcs stay apart, all arriving together over
// the door where they flash into one.
//
// Drawn rather than authored: the streams start wherever a door happens to set its gems, so
// there is no shape to keep in a scene file - only the flare texture they are made of.
[ScenePath]
public partial class DoorCeremony : Node2D {
  #region Constants
  private const float TRAIL_WIDTH = 14.0f;
  // A stream is three lines over each other, all adding their light together: a wide faint
  // halo, the gem's own color at full strength, and a hot white core. One line on its own
  // reads as a drawn stroke rather than as something giving off light.
  private const float HALO_WIDTH_SCALE = 3.4f;
  private const float HALO_ALPHA = 0.22f;
  private const float CORE_WIDTH_SCALE = 0.34f;
  private const float CORE_ALPHA = 0.6f;
  // How far out each stream bows on its way up, as a share of how far off the door's centre
  // line it starts: the gems low on the arch sweep wider than the ones beside the keystone.
  private const float TRAIL_BOW = 2.6f;
  private const float HEAD_SCALE = 0.3f;
  private const float TRAIL_FADE = 0.4f;
  private const float BURST_START_SCALE = 0.15f;
  private const float BURST_END_SCALE = 1.3f;
  #endregion Constants

  #region Nodes
  [NodePath("Burst")]
  private Sprite2D _burstNode = default!;
  #endregion Nodes

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _burstNode.Visible = false;
  }

  // Every stream is launched at once and they all reach the meeting point together, so the
  // merge has one moment rather than four.
  public async Task RunPhotons(IReadOnlyList<(Vector2 From, Color Color)> photons, Vector2 target, float duration) {
    Tween? longest = null;
    foreach (var (from, color) in photons) {
      longest = _launchPhoton(from, color, target, duration);
    }
    await _waitFor(longest);
  }

  public async Task Flash(Vector2 target, Color color, float duration) {
    _burstNode.Position = target;
    _burstNode.Modulate = new Color(color, 1.0f);
    _burstNode.Scale = Vector2.One * BURST_START_SCALE;
    _burstNode.Visible = true;

    var tween = CreateTween();
    tween.SetParallel(true);
    tween.TweenProperty(_burstNode, "scale", Vector2.One * BURST_END_SCALE, duration)
      .SetTrans(Tween.TransitionType.Quad)
      .SetEase(Tween.EaseType.Out);
    tween.TweenProperty(_burstNode, "modulate:a", 0.0f, duration)
      .SetTrans(Tween.TransitionType.Quad)
      .SetEase(Tween.EaseType.In);
    await _waitFor(tween);
    if (IsInstanceValid(_burstNode)) {
      _burstNode.Visible = false;
    }
  }

  // Nothing of a ceremony survives the door being told something new: the streams are only
  // ever a moment, and a second one starting over the first would leave the first's light
  // hanging on the arch.
  public void Clear() {
    foreach (var child in GetChildren()) {
      if (child != _burstNode) {
        child.QueueFree();
      }
    }
    _burstNode.Visible = false;
  }

  private Tween _launchPhoton(Vector2 from, Color color, Vector2 target, float duration) {
    Line2D[] trails = [
      _addTrail(new Color(color, HALO_ALPHA), TRAIL_WIDTH * HALO_WIDTH_SCALE),
      _addTrail(color, TRAIL_WIDTH),
      _addTrail(new Color(1.0f, 1.0f, 1.0f, CORE_ALPHA), TRAIL_WIDTH * CORE_WIDTH_SCALE),
    ];

    var head = new Sprite2D {
      Texture = _burstNode.Texture,
      Material = _burstNode.Material,
      Modulate = color,
      Scale = Vector2.One * HEAD_SCALE,
      Position = from,
    };
    AddChild(head);

    // Bowed away from the door's centre line, so a stream leaving a socket on the left
    // swings further left before it turns up towards the meeting point.
    var control = new Vector2(from.X * TRAIL_BOW, Mathf.Lerp(from.Y, target.Y, 0.5f));
    var tween = CreateTween();
    tween.TweenMethod(
      Callable.From<float>(t => _advance(trails, head, from, control, target, t)),
      0.0f,
      1.0f,
      duration
    ).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
    tween.SetParallel(true);
    foreach (var trail in trails) {
      tween.TweenProperty(trail, "modulate:a", 0.0f, TRAIL_FADE).SetDelay(duration);
    }
    tween.TweenProperty(head, "modulate:a", 0.0f, TRAIL_FADE).SetDelay(duration);
    tween.Chain().TweenCallback(Callable.From(() => {
      foreach (var trail in trails) {
        trail.QueueFree();
      }
      head.QueueFree();
    }));
    return tween;
  }

  private Line2D _addTrail(Color color, float width) {
    var trail = new Line2D {
      Width = width,
      DefaultColor = color,
      Material = _burstNode.Material,
      JointMode = Line2D.LineJointMode.Round,
      BeginCapMode = Line2D.LineCapMode.Round,
      EndCapMode = Line2D.LineCapMode.Round,
    };
    AddChild(trail);
    return trail;
  }

  private static void _advance(Line2D[] trails, Sprite2D head, Vector2 from, Vector2 control, Vector2 to, float t) {
    if (!IsInstanceValid(head)) {
      return;
    }
    var point = from.Lerp(control, t).Lerp(control.Lerp(to, t), t);
    head.Position = point;
    foreach (var trail in trails) {
      if (IsInstanceValid(trail)) {
        trail.AddPoint(point);
      }
    }
  }

  // A door taken out of the tree mid-ceremony takes its tweens with it, and awaiting one of
  // those is awaiting something that will never finish.
  private async Task _waitFor(Tween? tween) {
    if (tween == null) {
      return;
    }
    try {
      await ToSignal(tween, Tween.SignalName.Finished);
    }
    catch (ObjectDisposedException) {
      // The door is gone; so is the ceremony.
    }
  }
}
