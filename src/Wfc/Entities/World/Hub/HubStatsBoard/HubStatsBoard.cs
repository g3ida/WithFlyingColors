namespace Wfc.Entities.World.Hub;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Input;
using Wfc.Core.Localization;
using Wfc.Entities.World.Door;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;
using DoorEntity = Wfc.Entities.World.Door.Door;

// Where the run's numbers are read back in the hub. Answered the same way a door is - walk
// up to it and press the same button - so the room has one verb rather than two.
//
// The statue itself says nothing legible: the plaque is a block of symbols, and the four
// colours running down it are the only writing the game ever asks the player to read.
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
  [NodePath("Statue/Symbols/Pink")]
  private Node2D _pinkSymbolsNode = default!;
  [NodePath("Statue/Symbols/Blue")]
  private Node2D _blueSymbolsNode = default!;
  [NodePath("Statue/Symbols/Yellow")]
  private Node2D _yellowSymbolsNode = default!;
  [NodePath("Statue/Symbols/Purple")]
  private Node2D _purpleSymbolsNode = default!;
  #endregion Nodes

  private Player.Player? _playerInside;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _paintSymbols();
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

  // The plaque is one texture repeated, so the colour is all that separates a column from
  // its neighbour: the dark tones because the stone behind them is nearly white.
  private void _paintSymbols() {
    _paintColumn(_pinkSymbolsNode, ColorUtils.PINK);
    _paintColumn(_blueSymbolsNode, ColorUtils.BLUE);
    _paintColumn(_yellowSymbolsNode, ColorUtils.YELLOW);
    _paintColumn(_purpleSymbolsNode, ColorUtils.PURPLE);
  }

  private static void _paintColumn(Node2D column, string colorGroup) =>
    column.Modulate = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(colorGroup),
      SkinColorIntensity.VeryDark
    );

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
