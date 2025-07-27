namespace Wfc.Screens;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Persistence;
using Wfc.Entities.Ui;
using Wfc.Entities.Ui.Slots;
using Wfc.Screens.MenuManager;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class SelectSlotMenu : GameMenu {
  [NodePath("BackButton")]
  private Button _backButtonNode = default!;
  [NodePath("SlotsContainer")]
  private SlotsContainer _slotsContainer = default!;
  [NodePath("ResetDialogContainer")]
  private DialogContainer _resetDialogContainerNode = default!;
  [NodePath("NoSelectedSlotDialogContainer")]
  private DialogContainer _noSelectedSlotDialogContainer = default!;
  [NodePath("CurrentSlotLabel")]
  private Label CurrentSlotLabelNode = default!;
  private int _currentSlotOnFocus;
  private int _deleteTmpId = 0; // Used to save the currently deleting slot

  public void OnResolved() {

  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _currentSlotOnFocus = SaveManager.GetSelectedSlotIndex();
    _slotsContainer.SetGameCurrentSelectedSlot(SaveManager.GetSelectedSlotIndex());
    SetSelectedSlotLabel();
  }

  private void OnBackButtonPressed() {
    if (SaveManager.GetSelectedSlotIndex() == -1) {
      EventHandler.EmitMenuActionPressed(MenuAction.ShowDialog);

    }
    else {
      EventHandler.EmitMenuActionPressed(MenuAction.GoBack);
    }
  }

  public override bool OnMenuButtonPressed(MenuAction menuAction) {
    base.OnMenuButtonPressed(menuAction);
    switch (menuAction) {
      case MenuAction.ShowDialog:
        _noSelectedSlotDialogContainer.ShowDialog();
        return true;
      case MenuAction.DeleteSlot:
        return true;
      case MenuAction.SelectSlot:
        return true;
      case MenuAction.GoBack:
        return false; // We don't return true here because we want the default behavior to be called
      default:
        return false;
    }
  }

  private void _updateSlotsYPos(float posY) {
    _slotsContainer.Position = new Vector2(_slotsContainer.Position.X, posY);
  }

  private void _on_SlotsContainer_SlotPressed(int id, string action) {
    _currentSlotOnFocus = id;
    if (action == "select") {
      SaveManager.SelectSlot(id);
      _on_confirm_slot_button_selected(id);
      _slotsContainer.SetGameCurrentSelectedSlot(id);
      EventHandler.EmitMenuActionPressed(MenuAction.SelectSlot);

    }
    else if (action == "delete") {
      _deleteTmpId = id;
      _resetDialogContainerNode.ShowDialog();
      EventHandler.EmitMenuActionPressed(MenuAction.DeleteSlot);
    }
  }

  private void _on_confirm_slot_button_selected(int slotIndex) {
    if (SaveManager.IsSLotFilled(slotIndex)) {
      SaveManager.SelectSlot(slotIndex);
    }
    else {
      SaveManager.SaveGame(GetTree(), slotIndex);
    }
    SetSelectedSlotLabel();
  }

  private void OnResetSlotConfirmed() {
    SaveManager.RemoveSaveSlot(_currentSlotOnFocus);
    _slotsContainer.UpdateSlot(_deleteTmpId, true);
    _slotsContainer.SetGameCurrentSelectedSlot(SaveManager.GetSelectedSlotIndex());
    SetSelectedSlotLabel();
  }

  private void SetSelectedSlotLabel() {
    if (SaveManager.GetSelectedSlotIndex() != -1) {
      CurrentSlotLabelNode.Text = $"{SaveManager.GetSelectedSlotIndex() + 1}";
    }
    else {
      CurrentSlotLabelNode.Text = "None";
    }
  }
}
