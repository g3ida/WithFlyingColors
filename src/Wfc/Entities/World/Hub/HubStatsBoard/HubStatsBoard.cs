namespace Wfc.Entities.World.Hub;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Input;
using Wfc.Core.Localization;
using Wfc.Entities.World.Door;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using DoorEntity = Wfc.Entities.World.Door.Door;

// Where the run's numbers are read back in the hub. Answered the same way a door is - walk
// up to it and press the same button - so the room has one verb rather than two.
//
// A blank slab for now: the sprite is still to come, and the board's own title is all it
// says until it is stood at.
[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class HubStatsBoard : Node2D {
  #region Dependencies
  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();
  [Dependency]
  public IInputManager InputManager => this.DependOn<IInputManager>();
  #endregion Dependencies

  #region Nodes
  [NodePath("TitleLabel")]
  private Label _titleLabelNode = default!;
  [NodePath("ReadArea")]
  private Area2D _readAreaNode = default!;
  [NodePath("DoorPrompt")]
  private DoorPrompt _promptNode = default!;
  [NodePath("HubStatsMenu")]
  private HubStatsMenu _menuNode = default!;
  #endregion Nodes

  private Player.Player? _playerInside;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _readAreaNode.BodyEntered += _onReadAreaBodyEntered;
    _readAreaNode.BodyExited += _onReadAreaBodyExited;
    _menuNode.Closed += _refreshPrompt;
    _refreshPrompt();
  }

  public void OnResolved() =>
    _titleLabelNode.Text = LocalizationService
      .GetLocalizedString(TranslationKey.menu_header_gameStats)
      .ToUpperInvariant();

  public override void _Input(InputEvent @event) {
    base._Input(@event);
    if (_playerInside is not { } player || _menuNode.IsOpen) {
      return;
    }
    if (!InputManager.IsEventActionJustPressed(DoorEntity.ENTER_ACTION, @event)) {
      return;
    }
    // A dash in mid air stays a dash, exactly as at a doorway: only a grounded press in
    // front of the board is a request to read it.
    if (!player.IsOnFloor()) {
      return;
    }
    GetViewport().SetInputAsHandled();
    _menuNode.Open();
    _refreshPrompt();
  }

  // Nothing to advertise while the board is being read: the overlay carries its own hints,
  // and the prompt underneath would be offering a press that does nothing.
  private void _refreshPrompt() => _promptNode.Visible = _playerInside != null && !_menuNode.IsOpen;

  private void _onReadAreaBodyEntered(Node2D body) {
    if (body is Player.Player player) {
      _playerInside = player;
      _refreshPrompt();
    }
  }

  private void _onReadAreaBodyExited(Node2D body) {
    if (body == _playerInside) {
      _playerInside = null;
      _refreshPrompt();
    }
  }
}
