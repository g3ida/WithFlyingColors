namespace Wfc.Entities.Ui.SettingsUI.UISelect;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Input;
using Wfc.Core.Ui;
using Wfc.Screens.MenuManager;
using Wfc.Utils;
using Wfc.Utils.Attributes;

// A settings row that shows one value and opens the rest of them on demand, for
// settings a UISelectButton cannot serve: stepping through those applies every
// value on the way past, and a window resize is too heavy a thing to do to a
// player who is only looking.
//
// The options come from a child UISelectDriver, the same one the stepping select
// reads, so a driver can be moved between the two without being touched. Nothing
// is announced until the player picks an entry out of the open list.
[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class UIDropdownButton : Button, IDarkBackgroundAware {
  #region Constants
  // What the arrow and the box are drawn in at rest on a light surface.
  private static readonly Color ARROW_REST_TINT = new(0.18f, 0.18f, 0.18f);

  // The value is boxed so that the row reads as the closed head of the list rather
  // than as a caption that happens to have an arrow beside it. The box is drawn
  // outwards from the content: the row inside is laid out at the button's own edges,
  // so padding it any other way would move what it holds.
  private const int BOX_BORDER_WIDTH = 3;
  private const int BOX_PADDING = 12;

  // How thick the arrow lies down as once the list is open, against the height of
  // the arrow itself.
  private const float OPEN_MARK_THICKNESS = 0.2f;

  private static readonly StringName[] BOX_STATES = ["normal", "hover", "pressed", "focus"];
  #endregion Constants

  #region Dependencies
  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public IInputManager InputManager => this.DependOn<IInputManager>();
  [Dependency]
  public IModalStack ModalStack => this.DependOn<IModalStack>();
  #endregion Dependencies

  #region Signals
  // The player picked an entry out of the open list. Not raised for the value the
  // row was already showing, and never raised by simply moving through the list.
  [Signal]
  public delegate void ValueCommittedEventHandler(Variant value);
  #endregion Signals

  #region Nodes
  [NodePath("HBoxContainer")]
  private HBoxContainer _childContainerNode = default!;
  [NodePath("HBoxContainer/Arrow")]
  private Button _arrowNode = default!;
  [NodePath("HBoxContainer/Spacer")]
  private Control _arrowSpacerNode = default!;
  [NodePath("HBoxContainer/Label")]
  private MarqueeLabel _labelNode = default!;
  [NodePath("HBoxContainer/Label/AnimationPlayer")]
  private AnimationPlayer _animationPlayerNode = default!;
  #endregion Nodes

  public UISelectDriver SelectDriver = default!;

  private int _index;
  private UIDropdownList? _openList;
  private Texture2D _closedMark = default!;
  private Texture2D _openMark = default!;
  private bool _isReady;
  private bool _selectDriverSignalsSet;
  private bool _onDarkBackground;

  public Variant? SelectedValue { get; private set; }

  public override void _EnterTree() {
    base._EnterTree();
    ChildEnteredTree += _trySetSelectDriver;
    this.WireNodes();
  }

  public override void _ExitTree() {
    base._ExitTree();
    ChildEnteredTree -= _trySetSelectDriver;
    if (_selectDriverSignalsSet) {
      SelectDriver.ItemListChanged -= _onSelectDriverItemListChanged;
    }
  }

  public override void _Ready() {
    base._Ready();
    _buildMarks();
    _sizeArrowToValue();
    // The value keeps the column the list writes its options down, rather than
    // drifting with its own width inside the room kept for the longest of them.
    _labelNode.AlignLeft = true;
    _labelNode.ReserveWidthFor(SelectDriver.Items);
    _index = SelectDriver.GetDefaultSelectedIndex();
    _showSelectedItem();
    _updateRectSize();
    this.GrabFocusOnHover();
    this.BlinkWhileFocused(_animationPlayerNode);
    Pressed += Open;
    _arrowNode.Pressed += Open;
    _isReady = true;
  }

  // Opens the list of options over the settings panel. Nothing to open when the
  // driver has a single answer to give - the resolution row in fullscreen - and a
  // list of one would only be a way of confirming what the row already says.
  public void Open() {
    if (!_isReady || _openList != null || SelectDriver.Items.Count < 2 || ModalStack.IsAnyOpen) {
      return;
    }

    var list = new UIDropdownList {
      InputManager = InputManager,
      ModalStack = ModalStack,
    };
    // The list hangs off the box rather than off the content inside it, so the two
    // share an edge, and its options are written down the column the value is on
    // rather than under the arrow - so the one the row shows sits directly above its
    // own entry.
    list.Configure(SelectDriver.Items, _index, GetGlobalRect().Grow(BOX_PADDING),
        _labelNode.GlobalPosition.X);
    list.ItemChosen += _onListItemChosen;
    list.Closed += _onListClosed;
    _openList = list;
    _arrowNode.Icon = _openMark;
    _applyInk();
    MenuAction.OpenDropdown.Emit();
    AddChild(list);
  }

  private void _onListItemChosen(int index) {
    if (index == _index || index < 0 || index >= SelectDriver.ItemValues.Count) {
      return;
    }
    _index = index;
    _showSelectedItem();
    SelectDriver.OnUserSelectionChanged();
    EmitSignal(SignalName.ValueCommitted, SelectDriver.ItemValues[_index]);
  }

  private void _onListClosed() {
    _openList = null;
    _arrowNode.Icon = _closedMark;
    _applyInk();
    // The list took the focus off this row to put it on an entry, and freeing it
    // would otherwise leave the settings with nothing focused at all.
    if (FocusMode != FocusModeEnum.None) {
      GrabFocus();
    }
  }

  /// <summary>
  /// Re-reads the driver's default index and shows that item. Call this when the
  /// value the row stands for was changed by something other than this button.
  /// </summary>
  public void SyncSelectionToDefault() {
    if (!_isReady || SelectDriver == null) {
      return;
    }
    var index = SelectDriver.GetDefaultSelectedIndex();
    if (index == _index) {
      return;
    }
    _index = index;
    _showSelectedItem();
  }

  private void _trySetSelectDriver(Node child) {
    if (child is UISelectDriver driver) {
      SelectDriver = driver;
      SelectDriver.ItemListChanged += _onSelectDriverItemListChanged;
      _selectDriverSignalsSet = true;
    }
  }

  private void _onSelectDriverItemListChanged() {
    // A different set of options is a different longest one to keep room for.
    _labelNode.ReserveWidthFor(SelectDriver.Items);
    _index = SelectDriver.GetDefaultSelectedIndex();
    _showSelectedItem();
    _openList?.Close();
  }

  private void _showSelectedItem() {
    // The arrow is a promise that there is a list behind the value. With a single
    // answer to give - the resolution row in fullscreen - there is nothing to open,
    // so the row states its value plainly instead. The gap the arrow was held clear
    // by goes with it, or the value keeps an indent nothing is standing in and stops
    // lining up with the checkboxes above and below it.
    var hasChoice = SelectDriver.Items.Count > 1;
    _arrowNode.Visible = hasChoice;
    _arrowSpacerNode.Visible = hasChoice;
    _applyInk();
    if (_index < 0 || _index >= SelectDriver.Items.Count || _index >= SelectDriver.ItemValues.Count) {
      GD.PrintErr("UIDropdownButton - invalid index ", _index);
      return;
    }
    _labelNode.Text = SelectDriver.Items[_index];
    SelectedValue = SelectDriver.ItemValues[_index];
    SelectDriver.onItemSelected(SelectedValue);
    _updateRectSize();
  }

  private void _updateRectSize() {
    SetDeferred(PropertyName.CustomMinimumSize, _childContainerNode.Size);
    SetDeferred(Control.PropertyName.Size, _childContainerNode.Size);
  }

  // Closed, the row wears the stepping select's arrow turned a quarter turn, so it
  // reads as something that opens downwards rather than something that steps. Open,
  // the arrow lies down: a row already showing everything it has must not go on
  // promising to open.
  private void _buildMarks() {
    var image = _arrowNode.Icon.GetImage();
    image.Rotate90(ClockDirection.Clockwise);
    _closedMark = ImageTexture.CreateFromImage(image);
    _openMark = _flatten(image.GetSize());
    _arrowNode.Icon = _closedMark;
  }

  // Drawn at the arrow's own size so the two marks take up the same room and the
  // value beside them does not shift as the list opens and closes.
  private static ImageTexture _flatten(Vector2I size) {
    var image = Image.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8);
    image.Fill(Colors.Transparent);
    var thickness = Mathf.Max(1, Mathf.RoundToInt(size.Y * OPEN_MARK_THICKNESS));
    image.FillRect(new Rect2I(0, (size.Y - thickness) / 2, size.X, thickness), Colors.White);
    return ImageTexture.CreateFromImage(image);
  }

  private StyleBoxFlat _boxStyle(Color ink) => new() {
    BgColor = Colors.Transparent,
    BorderWidthLeft = BOX_BORDER_WIDTH,
    BorderWidthTop = BOX_BORDER_WIDTH,
    BorderWidthRight = BOX_BORDER_WIDTH,
    BorderWidthBottom = BOX_BORDER_WIDTH,
    BorderColor = ink,
    ExpandMarginLeft = BOX_PADDING,
    ExpandMarginTop = BOX_PADDING,
    ExpandMarginRight = BOX_PADDING,
    ExpandMarginBottom = BOX_PADDING,
  };

  // The arrow and the box are drawn in the same ink, and both swap with the surface
  // the row is standing on.
  //
  // The box is only there while the list is: closed, the row is one of the settings
  // and is drawn like the rest of them; open, the box is what joins it to the list
  // hanging off its bottom edge.
  private void _applyInk() {
    var ink = _onDarkBackground ? Colors.White : ARROW_REST_TINT;
    _arrowNode.AddThemeColorOverride("icon_normal_color", ink);
    foreach (var state in BOX_STATES) {
      AddThemeStyleboxOverride(state, _openList == null ? new StyleBoxEmpty() : _boxStyle(ink));
    }
  }

  // The arrow is drawn as tall as a line of the value, so the art it is cut from
  // can be any resolution and the row holds together at any font size.
  private void _sizeArrowToValue() {
    var lineHeight = _labelNode.GetMinimumSize().Y;
    var icon = _arrowNode.Icon.GetSize();
    _arrowNode.ExpandIcon = true;
    _arrowNode.CustomMinimumSize = new Vector2(lineHeight * icon.X / icon.Y, lineHeight);
  }

  private void _onLabelResized() {
    if (_isReady) {
      _updateRectSize();
    }
  }

  // The arrow is authored for a light panel. On a dark surface it turns white, the
  // same swap every other widget in a focused row makes.
  public bool OnDarkBackground {
    set {
      if (_onDarkBackground == value) {
        return;
      }
      _onDarkBackground = value;
      _applyInk();
    }
  }
}
