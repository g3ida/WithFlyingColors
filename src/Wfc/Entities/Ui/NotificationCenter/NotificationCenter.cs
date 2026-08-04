namespace Wfc.Entities.Ui;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Localization;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

// Everything the game wants to tell the player in passing arrives here as a translation key and
// leaves as a bar in the corner. It hangs off the orchestrator rather than a level, so what a
// notification says is never tied to which level happened to be loaded when it was raised.
[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class NotificationCenter : CanvasLayer {
  #region Dependencies
  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();
  #endregion Dependencies

  #region Constants
  // Past this the corner is a wall of text nobody reads, so the oldest is sent away early to make
  // room rather than the newest being dropped: what just happened is what the player is looking for.
  private const int MAX_STACKED = 3;

  // The bar is edged in one of the cube's own four colours, taken at random. Nothing about a
  // notification is tied to a colour group, and picking one anyway keeps the corner in the game's
  // palette instead of introducing a fifth colour to mean "message".
  private static readonly SkinColor[] FACES = {
    SkinColor.TopFace,
    SkinColor.LeftFace,
    SkinColor.BottomFace,
    SkinColor.RightFace,
  };
  #endregion Constants

  #region Nodes
  [NodePath("Stack")]
  private VBoxContainer _stackNode = default!;
  #endregion Nodes

  private bool _isSubscribed;

  public void OnResolved() { }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
  }

  public override void _EnterTree() {
    base._EnterTree();
    if (!_isSubscribed) {
      EventHandler.Instance.Events.NotificationRaised += _onNotificationRaised;
      _isSubscribed = true;
    }
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (_isSubscribed) {
      EventHandler.Instance.Events.NotificationRaised -= _onNotificationRaised;
      _isSubscribed = false;
    }
  }

  private void _onNotificationRaised(int translationKey) {
    _makeRoom();

    var card = SceneHelpers.InstantiateNode<NotificationCard>();
    // Upper case is the bar's own voice rather than the translation's: every language in the file
    // is written in its normal case, and the corner shouts in all of them.
    card.Configure(
      LocalizationService.GetLocalizedString((TranslationKey)translationKey).ToUpperInvariant(),
      SkinManager.Instance.CurrentSkin.GetColor(_randomFace(), SkinColorIntensity.Basic)
    );
    _stackNode.AddChild(card);
    // Newest at the top, so a bar the player is already reading is never the one that moves.
    _stackNode.MoveChild(card, 0);
  }

  private void _makeRoom() {
    var standing = _stackNode.GetChildCount();
    for (var i = standing - 1; i >= MAX_STACKED - 1; i--) {
      if (_stackNode.GetChild(i) is NotificationCard oldest) {
        oldest.Dismiss();
      }
    }
  }

  private static SkinColor _randomFace() => FACES[GD.Randi() % (uint)FACES.Length];
}
