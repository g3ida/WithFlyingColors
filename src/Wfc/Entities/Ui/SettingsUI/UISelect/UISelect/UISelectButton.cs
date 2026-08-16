namespace Wfc.Entities.Ui.SettingsUI.UISelect;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Input;
using Wfc.Core.Logger;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class UISelectButton : Button, IDarkBackgroundAware {

  #region Constants
  // What the arrows are tinted with at rest on a light surface, the shade the
  // animations were authored around.
  private static readonly Color ARROW_REST_TINT = new(0.18f, 0.18f, 0.18f);
  #endregion Constants

  #region Dependencies
  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public IInputManager InputManager => this.DependOn<IInputManager>();
  #endregion Dependencies

  #region Exports
  // On a settings row the arrows sit together ahead of the value, which the eye reads
  // against the caption of the row they share. A picker standing on its own has no
  // caption to read against, so it puts the value between the arrows instead.
  [Export]
  public bool ArrowsFlankValue { get; set; }

  // What the value is drawn at when the picker is a screen of its own rather than one
  // row of a panel. A real font size, not a scale on the control: scaling redraws
  // glyphs the engine has already rasterised at the theme's size, which is what leaves
  // large text soft. Zero keeps the theme's size.
  [Export]
  public int FontSize { get; set; }
  #endregion Exports

  public UISelectDriver SelectDriver = default!;

  #region Nodes
  [NodePath("HBoxContainer")]
  private HBoxContainer ChildContainerNode = default!;
  [NodePath("HBoxContainer/Left")]
  private Button LeftArrowNode = default!;
  [NodePath("HBoxContainer/Left/AnimationPlayer")]
  private AnimationPlayer LeftArrowAnimationNode = default!;
  [NodePath("HBoxContainer/Spacer")]
  private Control LeftSpacerNode = default!;
  [NodePath("HBoxContainer/Spacer2")]
  private Control RightSpacerNode = default!;
  [NodePath("HBoxContainer/Right")]
  private Button RightArrowNode = default!;
  [NodePath("HBoxContainer/Right/AnimationPlayer")]
  private AnimationPlayer RightArrowAnimationNode = default!;
  [NodePath("HBoxContainer/Label")]
  private MarqueeLabel LabelNode = default!;
  [NodePath("HBoxContainer/Label/AnimationPlayer")]
  private AnimationPlayer AnimationPlayerNode = default!;
  #endregion Nodes

  private int _index;
  public Variant? SelectedValue = null;
  private bool _isReady = false;
  private bool _selectDriverSignalsSet = false;
  private bool _onDarkBackground;

  [Signal]
  public delegate void ValueChangedEventHandler(Variant value);

  public override void _EnterTree() {
    base._EnterTree();
    this.ChildEnteredTree += _trySetSelectDriver;
    this.FocusEntered += _onFocusEntered;
    this.FocusExited += _onFocusExited;
    this.WireNodes();
  }

  public override void _ExitTree() {
    base._ExitTree();
    this.ChildEnteredTree -= _trySetSelectDriver;
    this.FocusEntered -= _onFocusEntered;
    this.FocusExited -= _onFocusExited;
    if (_selectDriverSignalsSet) {
      SelectDriver.ItemListChanged -= _onSelectDriverItemListChanged;
    }
  }

  public override void _Ready() {
    base._Ready();

    _makeAnimationsLocal(LeftArrowAnimationNode);
    _makeAnimationsLocal(RightArrowAnimationNode);
    if (FontSize > 0) {
      _growValueTo(FontSize);
    }
    _sizeArrowsToValue();
    if (ArrowsFlankValue) {
      _flankValueWithArrows();
    }
    LabelNode.ReserveWidthFor(SelectDriver.Items);
    _index = SelectDriver.GetDefaultSelectedIndex();
    UpdateSelectedItem();
    UpdateRectSize();
    SetProcess(false);
    this.GrabFocusOnHover();
    this.BlinkWhileFocused(AnimationPlayerNode);
    _isReady = true;
  }

  // Redraws the value at the given size, and grows the room around it by however much
  // taller a line of it got, so the picker keeps its proportions at any size.
  private void _growValueTo(int fontSize) {
    var lineHeightBefore = LabelNode.GetMinimumSize().Y;
    LabelNode.FontSize = fontSize;
    var growth = LabelNode.GetMinimumSize().Y / lineHeightBefore;
    // Without this the longer names would start scrolling instead of simply being
    // shown, since the cap is a width and the text just got wider.
    LabelNode.MaxWidth *= growth;
    LeftSpacerNode.CustomMinimumSize *= growth;
    RightSpacerNode.CustomMinimumSize *= growth;
  }

  // An arrow is drawn as tall as a line of the value, whatever size that is drawn at,
  // so the art it is cut from can be any resolution and the picker holds together at
  // any font size.
  private void _sizeArrowsToValue() {
    var lineHeight = LabelNode.GetMinimumSize().Y;
    _sizeArrow(LeftArrowNode, lineHeight);
    _sizeArrow(RightArrowNode, lineHeight);
  }

  private static void _sizeArrow(Button arrow, float lineHeight) {
    arrow.ExpandIcon = true;
    var icon = arrow.Icon.GetSize();
    arrow.CustomMinimumSize = new Vector2(lineHeight * icon.X / icon.Y, lineHeight);
  }

  // Sends the right arrow past the value to the far end of the row. The two spacers
  // then stand on either side of the value rather than one after the other, so both
  // are widened to the larger of the pair to keep the value centred between them.
  private void _flankValueWithArrows() {
    ChildContainerNode.MoveChild(LabelNode, RightArrowNode.GetIndex());
    ChildContainerNode.MoveChild(RightArrowNode, -1);
    var gap = Mathf.Max(LeftSpacerNode.CustomMinimumSize.X, RightSpacerNode.CustomMinimumSize.X);
    LeftSpacerNode.CustomMinimumSize = new Vector2(gap, LeftSpacerNode.CustomMinimumSize.Y);
    RightSpacerNode.CustomMinimumSize = new Vector2(gap, RightSpacerNode.CustomMinimumSize.Y);
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
    LabelNode.ReserveWidthFor(SelectDriver.Items);
    _index = this.SelectDriver.GetDefaultSelectedIndex();
    UpdateSelectedItem();
  }

  public override void _Process(double delta) {
    if (!HasFocus()) {
      return;
    }

    // A select with nothing to move between (the resolution row in fullscreen, the
    // controller row with no pad plugged in) has no answer to give, and stepping
    // it would land on the item already shown and report that as a change.
    if (SelectDriver.Items.Count < 2) {
      return;
    }

    if (InputManager.IsJustPressed(IInputManager.Action.UILeft)) {
      _onLeftPressed();
      LeftArrowAnimationNode.Play("triggered");
    }
    else if (InputManager.IsJustPressed(IInputManager.Action.UIRight)) {
      _onRightPressed();
      RightArrowAnimationNode.Play("triggered");
    }
  }

  // Left/right is polled, so processing only needs to run while this select is the one
  // being pointed at. The blink that goes with it is wired in _Ready.
  private void _onFocusEntered() => SetProcess(true);

  private void _onFocusExited() => SetProcess(false);

  private void _onLeftPressed() {
    _index = (_index + 1) % SelectDriver.Items.Count;
    _applyUserSelection();
  }

  private void _onRightPressed() {
    _index = (_index - 1 + SelectDriver.Items.Count) % SelectDriver.Items.Count;
    _applyUserSelection();
  }

  // Shows the item the player just moved to. The driver is told that this one
  // came from them, which is not something onItemSelected can say on its own:
  // the same call carries the selections made in code (the first draw, a
  // refreshed item list, the controller select following the active device).
  private void _applyUserSelection() {
    UpdateSelectedItem();
    SelectDriver.OnUserSelectionChanged();
    EmitSignal(nameof(ValueChanged), SelectDriver.ItemValues[_index]);
  }

  private void UpdateSelectedItem() {
    if (_index >= 0 && _index < SelectDriver.Items.Count && _index < SelectDriver.ItemValues.Count) {
      LabelNode.Text = SelectDriver.Items[_index];
      SelectedValue = SelectDriver.ItemValues[_index];
      SelectDriver.onItemSelected(SelectedValue);
      UpdateRectSize();
    }
    else {
      Log.Error($"SelectDriver - invalid index {_index}");
    }

  }

  private void UpdateRectSize() {
    SetDeferred(Button.PropertyName.CustomMinimumSize, ChildContainerNode.Size);
    SetDeferred(Control.PropertyName.Size, ChildContainerNode.Size);
  }

  /// <summary>
  /// Re-reads the driver's default index and shows that item. Call this when the
  /// value the select stands for was changed by something other than this button
  /// (the controller select follows the device the player last touched).
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
    UpdateSelectedItem();
  }

  /// <summary>
  /// Refreshes the items from the select driver. Call this after the driver's items list has changed.
  /// </summary>
  public void RefreshItems() {
    if (!_isReady || SelectDriver == null) {
      return;
    }

    // Clamp index to new bounds
    if (SelectDriver.Items.Count > 0) {
      _index = Math.Clamp(_index, 0, SelectDriver.Items.Count - 1);
      UpdateSelectedItem();
    }
  }

  private void _onLabelResized() {
    if (_isReady) {
      UpdateRectSize();
    }
  }

  // The arrows are authored for a light panel: tinted dark at rest, flashing to
  // white when stepped. On a dark surface the same feedback runs the other way
  // round, so every colour the animations write is swapped for its opposite,
  // along with the tint they rest at.
  public bool OnDarkBackground {
    set {
      if (_onDarkBackground == value) {
        return;
      }
      _onDarkBackground = value;
      _setArrowSurface(LeftArrowNode, LeftArrowAnimationNode);
      _setArrowSurface(RightArrowNode, RightArrowAnimationNode);
    }
  }

  private void _setArrowSurface(Button arrow, AnimationPlayer animationPlayer) {
    arrow.AddThemeColorOverride("icon_normal_color", _onDarkBackground ? Colors.White : ARROW_REST_TINT);
    // Inverting an inverted colour gives the original back, so the same pass
    // carries the animations either way.
    foreach (var libraryName in animationPlayer.GetAnimationLibraryList()) {
      var library = animationPlayer.GetAnimationLibrary(libraryName);
      foreach (var animationName in library.GetAnimationList()) {
        _invertAnimationColors(library.GetAnimation(animationName));
      }
    }
  }

  // The libraries ship with the scene and are shared between every instance of
  // it, so a button takes its own copy before it ever repaints a key: editing
  // one in place would repaint the selects of every other screen too.
  private static void _makeAnimationsLocal(AnimationPlayer animationPlayer) {
    foreach (var libraryName in animationPlayer.GetAnimationLibraryList()) {
      var library = (AnimationLibrary)animationPlayer.GetAnimationLibrary(libraryName).Duplicate(true);
      animationPlayer.RemoveAnimationLibrary(libraryName);
      animationPlayer.AddAnimationLibrary(libraryName, library);
    }
  }

  private static void _invertAnimationColors(Animation animation) {
    for (var track = 0; track < animation.GetTrackCount(); track++) {
      for (var key = 0; key < animation.TrackGetKeyCount(track); key++) {
        var value = animation.TrackGetKeyValue(track, key);
        if (value.VariantType == Variant.Type.Color) {
          var color = value.AsColor();
          animation.TrackSetKeyValue(track, key, new Color(1f - color.R, 1f - color.G, 1f - color.B, color.A));
        }
      }
    }
  }
}
