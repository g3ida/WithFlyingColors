namespace Wfc.Entities.Ui.Dialogs;

using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

// An in-canvas replacement for the engine's dialog windows: a centred panel with
// a message and one or two DialogButtons. Being an ordinary Control, it centres
// with anchors, tweens predictably and never fights the window system over
// position or scale.
//
// Wording and modality stay with the DialogContainer that shows it; this node
// only owns the layout and reports which button was pressed.
public partial class ConfirmDialog : Control {
  [Signal]
  public delegate void ConfirmedEventHandler();
  [Signal]
  public delegate void CancelledEventHandler();

  // An information dialog has nothing to cancel; hiding the second button turns
  // the panel into an accept-only note without a second scene.
  [Export]
  public bool ShowCancelButton { get; set; } = true;

  #region Nodes
  [NodePath("CenterContainer/Panel/VBox/Text")]
  private Label _textNode = default!;
  [NodePath("CenterContainer/Panel/VBox/Buttons/ConfirmButton")]
  private DialogButton _confirmButtonNode = default!;
  [NodePath("CenterContainer/Panel/VBox/Buttons/CancelButton")]
  private DialogButton _cancelButtonNode = default!;
  #endregion Nodes

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _cancelButtonNode.Visible = ShowCancelButton;
    if (ShowCancelButton) {
      _confirmButtonNode.ConfineFocusWith(_cancelButtonNode);
      _cancelButtonNode.ConfineFocusWith(_confirmButtonNode);
    }
    else {
      _confirmButtonNode.ConfineFocusWith(_confirmButtonNode);
    }
  }

  public void SetText(string text) => _textNode.Text = text;

  public void SetConfirmCaption(string caption) => _confirmButtonNode.SetCaption(caption);

  public void SetCancelCaption(string caption) => _cancelButtonNode.SetCaption(caption);

  // A confirmation opens on its cancel button: these dialogs guard destructive
  // answers, so the harmless one is what a hasty press should land on.
  public void FocusDefaultButton() =>
      (ShowCancelButton ? _cancelButtonNode : _confirmButtonNode).GrabButtonFocus();

  private void _onConfirmPressed() => EmitSignal(SignalName.Confirmed);

  private void _onCancelPressed() => EmitSignal(SignalName.Cancelled);
}
