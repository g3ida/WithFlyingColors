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
  // The slot the player last acted on, and the one the reset dialog will wipe if it
  // is confirmed.
  private int _currentSlotOnFocus;

  public void OnResolved() {

  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _currentSlotOnFocus = SaveManager.GetSelectedSlotIndex();
    _slotsContainer.SetGameCurrentSelectedSlot(SaveManager.GetSelectedSlotIndex());
    SetSelectedSlotLabel();
  }

  private void OnBackButtonPressed() => EventHandler.EmitMenuActionPressed(MenuAction.GoBack);

  public override bool OnMenuButtonPressed(MenuAction menuAction) {
    switch (menuAction) {
      case MenuAction.ShowDialog:
        _noSelectedSlotDialogContainer.ShowDialog();
        return true;
      case MenuAction.DeleteSlot:
      case MenuAction.SelectSlot:
        return true;
      case MenuAction.GoBack:
        // Leaving with no slot selected would strand the game with nowhere to save,
        // so the screen holds the player here. The guard lives on the action rather
        // than on the Back button so UICancel goes through it too.
        if (!SaveManager.HasSelectedSlot()) {
          _noSelectedSlotDialogContainer.ShowDialog();
          return true;
        }
        return false; // Let GameMenu run the default back navigation.
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
    _slotsContainer.UpdateSlot(_currentSlotOnFocus, true);
    _slotsContainer.SetGameCurrentSelectedSlot(SaveManager.GetSelectedSlotIndex());
    SetSelectedSlotLabel();
  }

  private void SetSelectedSlotLabel() => CurrentSlotLabelNode.Text = SaveManager.GetSelectedSlotText();
}
