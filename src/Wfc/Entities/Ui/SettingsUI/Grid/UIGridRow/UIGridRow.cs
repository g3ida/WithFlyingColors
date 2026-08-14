namespace Wfc.Entities.Ui.SettingsUI.Grid;

using System.Collections.Generic;
using System.Diagnostics;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Localization;
using Wfc.Core.Settings;
using Wfc.Entities.Ui;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class UIGridRow : PanelContainer {

  #region Dependencies
  // The label holds a string that was already translated when the row was built,
  // so the engine's own auto-translation has nothing left to redo once the player
  // picks another language. Writing it again is what keeps the row in step.
  public override void _Notification(int what) {
    this.Notify(what);
    if (what == NotificationTranslationChanged) {
      _refreshLabel();
    }
  }

  [Dependency]
  public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();
  #endregion Dependencies


  private const string THEME_OVERRIDE_NAME = "panel";

  // The shades a settings row is drawn in. Public because the resolution row's
  // dropdown is a stack of rows lifted off the panel, and a list that picked its own
  // shades stopped looking like the settings it was opened from.

  // The shade text and art are drawn in on a light surface, matching what the
  // settings themes write their labels in. On a dark one they turn white.
  public static readonly Color CONTENT_INK = new(0.176471f, 0.176471f, 0.176471f);

  // The wash that tells one row from the next, laid down in the shade the panel
  // is not so it reads the same on either surface.
  public static readonly Color ALTERNATE_WASH_ON_LIGHT = new(0f, 0f, 0f, 0.05f);
  public static readonly Color ALTERNATE_WASH_ON_DARK = new(1f, 1f, 1f, 0.07f);

  public static readonly Color ROW_HIGHLIGHT_INK = new(0x2d2d2dff);

  // Keeps the label and the value clear of the panel's edges. Without it a value
  // that happened to fill its half of the row - the full name of a connected
  // gamepad, say - sat flush against the side of the settings panel.
  private const int SIDE_MARGIN = 30;

  [Export]
  public TranslationKey TranslationKey { get; set; }

  [Export]
  public bool IsDark { get; set; }// alternate background

  // The row's shades are black washes over a light panel. Drawn over the level
  // instead (the pause overlay), they swap to white washes, and the row drops its
  // own light theme so the host's dark one reaches the chrome its widgets take
  // from the theme rather than from here.
  public bool OnDarkBackground {
    get => _onDarkBackground;
    set {
      _onDarkBackground = value;
      if (value) {
        Theme = null;
      }
      _setStyle(hasFocus: _focusState > 0);
    }
  }

  [NodePath("Content")]
  public HBoxContainer _contentNode = default!;

  private bool _onDarkBackground;
  private Control? _attachedNode = null;
  private Label? _labelNode = null;
  private Control? _decorationBalanceNode = null;
  private readonly List<Control> _decorationNodes = [];
  private int _focusState = 0;
  private void _setStyle(bool hasFocus) {
    var style = new StyleBoxFlat {
      BgColor = _surfaceColor(hasFocus),
      ContentMarginTop = 5,
      ContentMarginBottom = 5,
      ContentMarginLeft = SIDE_MARGIN,
      ContentMarginRight = SIDE_MARGIN,
      // Fixme: this hack is to fix line spacing between items in the parent grid.
      ExpandMarginTop = 4,
    };
    if (HasThemeStyleboxOverride(THEME_OVERRIDE_NAME)) {
      RemoveThemeStyleboxOverride(THEME_OVERRIDE_NAME);
    }
    AddThemeStyleboxOverride(THEME_OVERRIDE_NAME, style);
    // A focused row is filled solid, so what it holds is standing on the opposite
    // surface for as long as it keeps the focus.
    _setContentOnDarkBackground(_onDarkBackground != hasFocus);
  }

  // At rest the row is a wash the panel shows through. Focused it is filled with
  // the shade the panel is not, which is what carries the row against it.
  private Color _surfaceColor(bool hasFocus) {
    if (hasFocus) {
      return _onDarkBackground ? Colors.White : ROW_HIGHLIGHT_INK;
    }
    if (!IsDark) {
      return Colors.Transparent;
    }
    return _onDarkBackground ? ALTERNATE_WASH_ON_DARK : ALTERNATE_WASH_ON_LIGHT;
  }

  // Text is ink on the surface underneath, wherever in the row it is written -
  // the row's own caption, and the ones its widgets draw inside themselves. Art
  // has no colour to be given, so every widget that ships a light and a dark set
  // is told which of the two to show.
  private void _setContentOnDarkBackground(bool onDarkBackground) {
    var ink = onDarkBackground ? Colors.White : CONTENT_INK;
    foreach (var label in _contentNode.FindDescendants<Label>()) {
      label.AddThemeColorOverride("font_color", ink);
    }
    foreach (var control in _contentNode.FindDescendants<IDarkBackgroundAware>()) {
      control.OnDarkBackground = onDarkBackground;
    }
  }

  private void _addContentLabel() {
    // Create label
    _labelNode = new Label {
      Text = LocalizationService.GetLocalizedString(TranslationKey),
      HorizontalAlignment = HorizontalAlignment.Right,
      SizeFlagsHorizontal = SizeFlags.ExpandFill,
      SizeFlagsVertical = SizeFlags.ShrinkCenter
    };
    _contentNode.AddChild(_labelNode);
  }

  // Null until the row has been built, which only happens once its dependencies
  // have resolved, so this doubles as the guard for reading LocalizationService.
  private void _refreshLabel() {
    if (_labelNode != null) {
      _labelNode.Text = LocalizationService.GetLocalizedString(TranslationKey);
    }
  }

  private void _addContentValue() {
    var attached = _getAttachedNodes();
    _attachedNode = attached[0];
    if (_attachedNode is Label label) {
      label.HorizontalAlignment = HorizontalAlignment.Left;
    }
    _attachedNode.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    _attachedNode.SizeFlagsVertical = SizeFlags.ShrinkCenter;
    _attachedNode.FocusEntered += _onAttachedNodeFocusEntered;
    _attachedNode.FocusExited += _onAttachedNodeFocusExited;

    foreach (var node in attached) {
      if (node != _attachedNode) {
        // Trailing decoration keeps its own width and rides at the end of the row, out
        // of the way of the value, which is the part that takes the slack.
        node.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        node.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        _decorationNodes.Add(node);
      }
      node.Reparent(_contentNode);
    }
    _addDecorationBalance();
  }

  // The label and the value split what the row does not otherwise spend, so trailing
  // decoration drags the seam between them off the row's centre. An empty stand-in of the
  // same width at the head puts it back, and every row reads as the same pair of columns.
  private void _addDecorationBalance() {
    if (_decorationNodes.Count == 0) {
      return;
    }
    _decorationBalanceNode = new Control { MouseFilter = MouseFilterEnum.Ignore };
    _contentNode.AddChild(_decorationBalanceNode);
    _contentNode.MoveChild(_decorationBalanceNode, 0);
    foreach (var node in _decorationNodes) {
      node.MinimumSizeChanged += _updateDecorationBalance;
    }
    _updateDecorationBalance();
  }

  private void _updateDecorationBalance() {
    if (_decorationBalanceNode == null) {
      return;
    }
    var separation = _contentNode.GetThemeConstant("separation");
    var width = (float)-separation;
    foreach (var node in _decorationNodes) {
      width += node.GetCombinedMinimumSize().X + separation;
    }
    _decorationBalanceNode.CustomMinimumSize = new Vector2(Mathf.Max(0f, width), 0f);
  }

  private void _onAttachedNodeFocusEntered() {
    _focusState++;
    _setStyle(hasFocus: true);
  }

  private void _onAttachedNodeFocusExited() {
    _focusState = Mathf.Max(0, _focusState - 1);
    if (_focusState == 0) {
      _setStyle(hasFocus: false);
    }
  }

  private void _onMouseEntered() {
    _focusState++;
    _setStyle(hasFocus: true);
    _attachedNode?.GrabFocus();
  }

  private void _onMouseExited() {
    _focusState = Mathf.Max(0, _focusState - 1);
    if (_focusState == 0) {
      _setStyle(hasFocus: false);
    }
  }

  private void _addContentSpacer() {
    // Spacer between label and value
    var spacer = new Control {
      CustomMinimumSize = new Vector2(40, 0),
      SizeFlagsHorizontal = SizeFlags.ShrinkCenter
    };
    _contentNode.AddChild(spacer);
  }

  // Everything the scene hung on the row besides its content box, in the order it hung
  // them. The first is the value the row exists for, and the one focus lands on;
  // anything after it rides along beside it - the swatches that show what a palette
  // looks like rather than only what it is called.
  private List<Control> _getAttachedNodes() {
    var attached = new List<Control>();
    foreach (var child in GetChildren()) {
      if (child != _contentNode && child is Control control) {
        attached.Add(control);
      }
    }
    Debug.Assert(attached.Count > 0, "UIGridRow should have at least one attached child");
    return attached;
  }

  // Gets the focusable control within this row.
  /// This is the control that should receive focus when navigating to this row.
  public Control? GetFocusableControl() {
    return _attachedNode;
  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
  }

  public void OnResolved() {
    SizeFlagsHorizontal = SizeFlags.ExpandFill;
    SizeFlagsVertical = SizeFlags.Fill;
    // Set minimum height to ensure all rows have consistent size
    CustomMinimumSize = new Vector2(0, 70);
    // Make this row stretch across the parent
    _contentNode.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    _contentNode.SizeFlagsVertical = SizeFlags.ShrinkCenter;
    // Set content
    _addContentLabel();
    _addContentSpacer();
    _addContentValue();
    // Styled last: the surface is handed to the content, which is only there to
    // receive it once the row has been built.
    _setStyle(hasFocus: _focusState > 0);
  }
}
