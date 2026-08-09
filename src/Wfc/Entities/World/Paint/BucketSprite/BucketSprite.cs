namespace Wfc.Entities.World.Paint;

using Godot;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;

// The bucket as it is drawn: a cream body and a steel bail that belong to no colour, and the
// paint, the label and the grip on the bail, which are the level's colour taken through the skin.
// Every shaded part is one step darker than the part it shades, which is how the pieces were cut.
//
// The origin is the middle of the bucket's base, so a bucket is stood on a surface rather than
// centred over it, and the corner it turns on when it topples is half its width to either side.
//
// Runs in the editor because everything that stands one in a level does: a scene whose script is
// not a tool script has no instance of that script while the editor holds it, so the bucket a
// [Tool] parent reaches for is a bare Node2D and cannot be assigned to the field expecting this.
[Tool]
[ScenePath]
public partial class BucketSprite : Node2D {
  #region Constants
  private const SkinColorIntensity PAINT = SkinColorIntensity.Basic;
  private const SkinColorIntensity PAINT_SHADE = SkinColorIntensity.Dark;
  private const SkinColorIntensity TRIM = SkinColorIntensity.Background;
  private const SkinColorIntensity TRIM_SHADE = SkinColorIntensity.VeryLight;

  private const float IMPACT_SQUASH = 0.72f;
  private const float IMPACT_SQUASH_DURATION = 0.08f;
  private const float IMPACT_RECOVERY_DURATION = 0.34f;
  #endregion Constants

  #region Exports
  // Which colour the bucket is full of. Left as an export rather than pushed in by the bucket so
  // the sprite can be stood in a scene on its own.
  [Export(PropertyHint.Enum, "blue,pink,yellow,purple")]
  public string Group {
    get => _group;
    set {
      _group = value;
      _applyColor();
    }
  }
  private string _group = "purple";
  #endregion Exports

  #region Nodes
  [NodePath("Bucket")]
  private Sprite2D _bucketNode = default!;
  [NodePath("Paint")]
  private Sprite2D _paintNode = default!;
  [NodePath("PaintShade")]
  private Sprite2D _paintShadeNode = default!;
  [NodePath("BailGrip")]
  private Sprite2D _bailGripNode = default!;
  [NodePath("BailGripShade")]
  private Sprite2D _bailGripShadeNode = default!;
  [NodePath("Card")]
  private Sprite2D _cardNode = default!;
  [NodePath("CardShade")]
  private Sprite2D _cardShadeNode = default!;
  #endregion Nodes

  // The exported setter fires while the scene is still loading, before there are any sprites to
  // push a colour into.
  private bool _isWired;
  private Vector2 _restScale = Vector2.One;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _isWired = true;
    _restScale = Scale;
    _applyColor();
  }

  // How far it is from the middle of the base out to either bottom corner, in whatever units the
  // bucket is standing in. That corner is what a bucket turns on when it topples, so anything
  // tipping one over has to know where it is rather than assuming the middle.
  public float HalfWidth => _isWired ? _bucketNode.Texture.GetWidth() * Scale.X / 2f : 0f;

  // How tall it stands, so that whatever tips it knows where the mouth has got to.
  public float Height => _isWired ? _bucketNode.Texture.GetHeight() * Scale.Y : 0f;

  // The paint has left the bucket. Nothing else about the bucket changes: what is on the floor
  // now is the splat's business.
  public void Empty() {
    _paintNode.Visible = false;
    _paintShadeNode.Visible = false;
  }

  public void Fill() {
    _paintNode.Visible = true;
    _paintShadeNode.Visible = true;
  }

  // The give the bucket has as it hits, squashed along whichever way is down for it by then -
  // the sprite is rotated with the bucket, so its own axes are the right ones to squash on.
  public void Impact() {
    var tween = CreateTween();
    tween.TweenProperty(this, "scale", new Vector2(_restScale.X, _restScale.Y * IMPACT_SQUASH), IMPACT_SQUASH_DURATION)
      .SetTrans(Tween.TransitionType.Quad)
      .SetEase(Tween.EaseType.Out);
    tween.TweenProperty(this, "scale", _restScale, IMPACT_RECOVERY_DURATION)
      .SetTrans(Tween.TransitionType.Elastic)
      .SetEase(Tween.EaseType.Out);
  }

  private void _applyColor() {
    if (!_isWired) {
      return;
    }
    var skin = SkinManager.Instance.CurrentSkin;
    var skinColor = GameSkin.ColorGroupToSkinColor(Group);

    _paintNode.Modulate = skin.GetColor(skinColor, PAINT);
    _paintShadeNode.Modulate = skin.GetColor(skinColor, PAINT_SHADE);
    _cardNode.Modulate = skin.GetColor(skinColor, TRIM);
    _bailGripNode.Modulate = skin.GetColor(skinColor, TRIM);
    _cardShadeNode.Modulate = skin.GetColor(skinColor, TRIM_SHADE);
    _bailGripShadeNode.Modulate = skin.GetColor(skinColor, TRIM_SHADE);
  }
}
