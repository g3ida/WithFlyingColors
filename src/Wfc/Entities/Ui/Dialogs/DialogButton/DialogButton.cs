namespace Wfc.Entities.Ui.Dialogs;

using Godot;
using Wfc.Core.Event;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Fonts;

// A dialog action button: a caption on the menu's pill, with a focus tint that
// covers the whole of it.
//
// The button wears no glyph of its own. Only one of a dialog's buttons answers to
// the confirm key at a time - whichever holds focus - so a glyph on each of them
// promised bindings that were not there. The screen's InputHintBar is the one
// place that can state the truth for the whole dialog.
public partial class DialogButton : PanelContainer {
  [Signal]
  public delegate void PressedEventHandler();

  #region Nodes
  [NodePath("CaptionBox")]
  private MarginContainer _captionBoxNode = default!;
  [NodePath("CaptionBox/Caption")]
  private Label _captionNode = default!;
  [NodePath("Button")]
  private Button _buttonNode = default!;
  #endregion Nodes

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _centerCaption();
    // The screen's focus poll sits in GameMenu._Process, which the modal pause
    // stops for exactly as long as a dialog is up - so the dialog's buttons report
    // their own focus moves, or arrowing between Confirm and Cancel is silent.
    _buttonNode.FocusEntered += GameEvents.Instance.OnFocusChanged;
  }

  public void SetCaption(string caption) => _captionNode.Text = caption;

  public void GrabButtonFocus() => _buttonNode.GrabFocus();

  // Focus must not escape an open dialog: without explicit neighbors, arrow keys
  // walk off to whatever focusable control happens to sit behind the dim layer.
  // Left/right bounce between the dialog's buttons (or stay put for a lone one),
  // up/down and tab stay on the button itself.
  public void ConfineFocusWith(DialogButton other) {
    var self = _buttonNode.GetPath();
    var partner = other._buttonNode.GetPath();
    _buttonNode.FocusNeighborLeft = partner;
    _buttonNode.FocusNeighborRight = partner;
    _buttonNode.FocusNeighborTop = self;
    _buttonNode.FocusNeighborBottom = self;
    _buttonNode.FocusNext = partner;
    _buttonNode.FocusPrevious = partner;
  }

  private void _onButtonPressed() => EmitSignal(SignalName.Pressed);

  // Sits the caption's capitals on the pill's centre line. Taken off the padding
  // below as much as it is added above, so the button keeps its height.
  private void _centerCaption() {
    var nudge = Mathf.RoundToInt(FontUtils.OpticalCenterOffset(
        _captionNode.GetThemeFont("font"), _captionNode.GetThemeFontSize("font_size")));
    _captionBoxNode.AddThemeConstantOverride(
        "margin_top", _captionBoxNode.GetThemeConstant("margin_top") + nudge);
    _captionBoxNode.AddThemeConstantOverride(
        "margin_bottom", _captionBoxNode.GetThemeConstant("margin_bottom") - nudge);
  }
}
