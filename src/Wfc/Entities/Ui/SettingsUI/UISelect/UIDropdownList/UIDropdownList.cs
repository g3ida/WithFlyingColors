namespace Wfc.Entities.Ui.SettingsUI.UISelect;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Input;
using Wfc.Core.Ui;
using Wfc.Entities.Ui.SettingsUI.Grid;
using Wfc.Screens.MenuManager;
using EventHandler = Wfc.Core.Event.EventHandler;

// The list a UIDropdownButton opens: every option at once, one of them highlighted,
// and nothing applied until the player picks one.
//
// Drawn as a stack of settings rows lifted off the panel - the same inks, the same
// alternating wash, the same solid bar under the one in hand - so opening it does
// not feel like leaving the settings. It takes the shades from UIGridRow rather than
// keeping its own so the two cannot drift apart.
//
// Built in code rather than in a scene for the same reason MarqueeLabel is: it
// carries no settings a designer would want to reach. The dependencies are handed in
// by the button that opens it, which already holds them.
public partial class UIDropdownList : Control {
  #region Constants
  // The most of the screen the list may take before it starts scrolling.
  private const float MAX_HEIGHT_RATIO = 0.6f;
  private const float SCREEN_MARGIN = 20f;
  // The size the settings write their rows at, so an option reads as the value of
  // the row it was opened from.
  private const int ITEM_FONT_SIZE = 40;
  private const int ITEM_MARGIN_Y = 8;
  // Drawn top level, the list is parented straight to the canvas and no longer
  // sits after the settings panel in the tree, so it says outright that it is in
  // front rather than relying on the order it happens to be visited in.
  private const int OVERLAY_Z_INDEX = 100;

  // The list carries the focused row's fill on down: the row is always focused when
  // its list is open, so the two read as one piece rather than as a panel that
  // appeared over the settings. Short of opaque, like every other surface here.
  private static readonly Color SURFACE = new(0f, 0f, 0f, 0.72f);
  #endregion Constants

  #region Signals
  [Signal]
  public delegate void ItemChosenEventHandler(int index);
  [Signal]
  public delegate void ClosedEventHandler();
  #endregion Signals

  public IInputManager InputManager { get; set; } = default!;
  public IModalStack ModalStack { get; set; } = default!;

  private readonly List<Button> _itemNodes = [];
  private PanelContainer _panelNode = default!;
  private ScrollContainer _scrollNode = default!;
  private VBoxContainer _itemBoxNode = default!;

  private List<string> _pendingItems = [];
  private Rect2 _anchorRect;
  private float _textColumn;
  private int _index;
  private bool _holdsModal;
  private bool _isClosing;

  // Where the list opens, how wide, and the column its options are written down.
  //
  // The column is where on the screen the value the closed row shows begins, not how
  // far in it sits: the panel is brought inside the screen after the fact, and an
  // indent measured against the row would carry that shift into the text and stop it
  // lining up with the row it was opened from.
  //
  // Set before the node is added to the tree: the list builds itself in _Ready and
  // has nothing to build from otherwise.
  public void Configure(IReadOnlyList<string> items, int selectedIndex, Rect2 anchorRect, float textColumn) {
    _pendingItems = [.. items];
    _index = selectedIndex;
    _anchorRect = anchorRect;
    _textColumn = textColumn;
  }

  public override void _Ready() {
    base._Ready();
    // The modal stack pauses the tree, and a list that stopped with it could
    // neither be moved nor closed.
    ProcessMode = ProcessModeEnum.Always;
    // Top level so the settings row it hangs off cannot clip it and the panel it
    // stands over cannot lay it out.
    TopLevel = true;
    ZIndex = OVERLAY_Z_INDEX;
    SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    _build();
    ModalStack.Push(this);
    _holdsModal = true;
    // Ahead of the entries being given their voice, so opening the list is one
    // sound rather than the open and the first entry landing together.
    _highlight(_index);
    _announceFocusMoves();
  }

  public override void _ExitTree() {
    base._ExitTree();
    _releaseModal();
  }

  public override void _Input(InputEvent @event) {
    if (_isClosing || ModalStack.IsBlockedFor(this)) {
      return;
    }

    if (InputManager.IsEventActionJustPressed(IInputManager.Action.UICancel, @event)) {
      Close();
      GetViewport().SetInputAsHandled();
      return;
    }
    if (InputManager.IsEventActionJustPressed(IInputManager.Action.UIConfirm, @event)) {
      _choose(_index);
      GetViewport().SetInputAsHandled();
      return;
    }
    if (InputManager.IsEventActionJustPressed(IInputManager.Action.UIUp, @event)) {
      _step(-1);
      GetViewport().SetInputAsHandled();
      return;
    }
    if (InputManager.IsEventActionJustPressed(IInputManager.Action.UIDown, @event)) {
      _step(1);
      GetViewport().SetInputAsHandled();
      return;
    }
    // Left and right mean nothing to a list, and letting them through would walk the
    // engine's own focus navigation off it and onto the settings behind.
    if (InputManager.IsEventActionJustPressed(IInputManager.Action.UILeft, @event)
        || InputManager.IsEventActionJustPressed(IInputManager.Action.UIRight, @event)) {
      GetViewport().SetInputAsHandled();
    }
  }

  public void Close() {
    if (_isClosing) {
      return;
    }
    _isClosing = true;
    MenuAction.CloseDropdown.Emit();
    _stopHolding();
    QueueFree();
  }

  // The entry is announced only once the list has let the screen go and handed the
  // focus back, so whatever the announcement sets off - a confirmation of its own -
  // opens over a screen that is no longer holding anything and finds a live control
  // to return the focus to when it closes.
  private void _choose(int index) {
    if (_isClosing || index < 0 || index >= _itemNodes.Count) {
      return;
    }
    _isClosing = true;
    MenuAction.CloseDropdown.Emit();
    _stopHolding();
    EmitSignal(SignalName.ItemChosen, index);
    QueueFree();
  }

  private void _stopHolding() {
    _releaseModal();
    EmitSignal(SignalName.Closed);
  }

  private void _releaseModal() {
    if (!_holdsModal) {
      return;
    }
    _holdsModal = false;
    ModalStack.Pop(this);
  }

  private void _step(int direction) {
    if (_itemNodes.Count == 0) {
      return;
    }
    _highlight((_index + direction + _itemNodes.Count) % _itemNodes.Count);
  }

  private void _highlight(int index) {
    if (index < 0 || index >= _itemNodes.Count) {
      return;
    }
    _index = index;
    _itemNodes[index].GrabFocus();
    _scrollNode.EnsureControlVisible(_itemNodes[index]);
  }

  // The screen's own focus poll sits in GameMenu._Process, which the modal pause
  // stops for exactly as long as the list is up - so the entries report their own
  // focus moves, or moving through the list is silent.
  private void _announceFocusMoves() {
    foreach (var item in _itemNodes) {
      item.FocusEntered += EventHandler.Instance.EmitFocusChanged;
    }
  }

  #region Building
  private void _build() {
    // Nothing is dimmed: the list is one row of the settings opened out, not a
    // screen over them. The layer is here to catch a click aimed past the list,
    // which is how a dropdown is closed with a mouse.
    var backdrop = new ColorRect {
      Color = Colors.Transparent,
      MouseFilter = MouseFilterEnum.Stop,
      AnchorRight = 1f,
      AnchorBottom = 1f,
    };
    backdrop.GuiInput += _onBackdropInput;
    AddChild(backdrop);

    _panelNode = new PanelContainer();
    _panelNode.AddThemeStyleboxOverride("panel", _panelStyle());
    AddChild(_panelNode);

    _scrollNode = new ScrollContainer {
      HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
      FollowFocus = true,
    };
    _panelNode.AddChild(_scrollNode);

    _itemBoxNode = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
    _itemBoxNode.AddThemeConstantOverride("separation", 0);
    _scrollNode.AddChild(_itemBoxNode);

    // Across before down: how wide the panel is and where it sits sideways settles
    // what indent writes the options down the column the row's value is on, and the
    // entries have to be built before there is a height to place it against.
    _spreadPanel();
    for (var i = 0; i < _pendingItems.Count; i++) {
      _addItem(_pendingItems[i], i);
    }
    _confineFocus();
    _dropPanel();
  }

  private void _onBackdropInput(InputEvent @event) {
    if (@event is InputEventMouseButton { Pressed: true }) {
      Close();
    }
  }

  private void _addItem(string caption, int index) {
    var button = new Button {
      Text = caption,
      Alignment = HorizontalAlignment.Left,
      SizeFlagsHorizontal = SizeFlags.ExpandFill,
      Flat = true,
    };
    button.AddThemeFontSizeOverride("font_size", ITEM_FONT_SIZE);
    button.AddThemeColorOverride("font_color", Colors.White);
    button.AddThemeColorOverride("font_hover_color", UIGridRow.CONTENT_INK);
    button.AddThemeColorOverride("font_focus_color", UIGridRow.CONTENT_INK);
    button.AddThemeColorOverride("font_pressed_color", UIGridRow.CONTENT_INK);
    // Every other entry carries the wash that tells one settings row from the next.
    var rest = index % 2 == 1 ? UIGridRow.ALTERNATE_WASH_ON_DARK : Colors.Transparent;
    button.AddThemeStyleboxOverride("normal", _itemStyle(rest));
    button.AddThemeStyleboxOverride("hover", _itemStyle(Colors.White));
    button.AddThemeStyleboxOverride("focus", _itemStyle(Colors.White));
    button.AddThemeStyleboxOverride("pressed", _itemStyle(Colors.White));
    // Pointing at an entry is what highlights it, so the mouse and the pad agree on
    // which one a press would take.
    button.MouseEntered += () => _highlight(index);
    button.Pressed += () => _choose(index);
    _itemBoxNode.AddChild(button);
    _itemNodes.Add(button);
  }

  // Focus must not escape an open list: without explicit neighbours, a directional
  // event this node did not answer walks the engine's own focus navigation onto
  // whatever settings row happens to sit behind it.
  private void _confineFocus() {
    for (var i = 0; i < _itemNodes.Count; i++) {
      var self = _itemNodes[i].GetPath();
      var previous = _itemNodes[(i - 1 + _itemNodes.Count) % _itemNodes.Count].GetPath();
      var next = _itemNodes[(i + 1) % _itemNodes.Count].GetPath();
      _itemNodes[i].FocusNeighborTop = previous;
      _itemNodes[i].FocusPrevious = previous;
      _itemNodes[i].FocusNeighborBottom = next;
      _itemNodes[i].FocusNext = next;
      _itemNodes[i].FocusNeighborLeft = self;
      _itemNodes[i].FocusNeighborRight = self;
    }
  }

  // Square, with no padding: the list is the row carried on down, and a rounded, inset
  // panel would read as something that landed on top of the settings instead.
  private static StyleBoxFlat _panelStyle() => new() { BgColor = SURFACE };

  private StyleBoxFlat _itemStyle(Color fill) => new() {
    BgColor = fill,
    ContentMarginLeft = Mathf.Max(0f, _textColumn - _panelNode.Position.X),
    ContentMarginTop = ITEM_MARGIN_Y,
    ContentMarginBottom = ITEM_MARGIN_Y,
  };

  // The list is as wide as the button that opened it and stands on the same left
  // edge, brought inside the screen if that would hang it off the side. Measured
  // against the viewport rather than the parent: the list is top level, so its
  // coordinates are the screen's.
  private void _spreadPanel() {
    var screen = GetViewportRect().Size;
    var width = _anchorRect.Size.X;
    _panelNode.Position = new Vector2(
      Mathf.Clamp(_anchorRect.Position.X, 0f, Mathf.Max(0f, screen.X - width)), 0f);
    _panelNode.Size = new Vector2(width, 0f);
  }

  // Hangs the list off the bottom of the button that opened it, and flips it above
  // when there is not the room below.
  //
  // The entries are measured rather than the panel holding them: a ScrollContainer
  // asks for no more room than a single line, which is what lets it scroll, and a
  // panel sized to that is a panel with nothing showing in it.
  private void _dropPanel() {
    var screen = GetViewportRect().Size;
    var height = Mathf.Min(_itemBoxNode.GetCombinedMinimumSize().Y, screen.Y * MAX_HEIGHT_RATIO);

    var below = _anchorRect.End.Y;
    var above = _anchorRect.Position.Y - height;
    var top = below + height <= screen.Y - SCREEN_MARGIN || above < SCREEN_MARGIN ? below : above;

    _panelNode.Position = new Vector2(
      _panelNode.Position.X, Mathf.Clamp(top, 0f, Mathf.Max(0f, screen.Y - height)));
    _panelNode.Size = new Vector2(_panelNode.Size.X, height);
  }
  #endregion Building
}
