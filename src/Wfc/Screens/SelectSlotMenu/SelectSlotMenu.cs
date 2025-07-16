namespace Wfc.Screens;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Persistence;
using Wfc.Utils.Attributes;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class SelectSlotMenu : GameMenu {
  public override void _Notification(int what) => this.Notify(what);
  [Dependency]
  public ISaveManager SaveManager => this.DependOn<ISaveManager>();
  private Button BackButtonNode;
  private SlotsContainer SlotsContainer;
  private DialogContainer ResetDialogContainerNode;
  private DialogContainer NoSelectedSlotDialogContainer;
  private Label CurrentSlotLabelNode;
  private int currentSlotOnFocus;
  private int deleteTmpId = 0; // Used to save the currently deleting slot

  public void OnResolved() {

  }

  public override void _Ready() {
    base._Ready();
    BackButtonNode = GetNode<Button>("BackButton");
    SlotsContainer = GetNode<SlotsContainer>("SlotsContainer");
    ResetDialogContainerNode = GetNode<DialogContainer>("ResetDialogContainer");
    NoSelectedSlotDialogContainer = GetNode<DialogContainer>("NoSelectedSlotDialogContainer");
    CurrentSlotLabelNode = GetNode<Label>("CurrentSlotLabel");
    currentSlotOnFocus = SaveManager.GetSelectedSlotIndex();
    SlotsContainer.SetGameCurrentSelectedSlot(SaveManager.GetSelectedSlotIndex());
    SetSelectedSlotLabel();
  }

  private void OnBackButtonPressed() {
    if (SaveManager.GetSelectedSlotIndex() == -1) {
      EventHandler.EmitMenuButtonPressed(MenuButtons.SHOW_DIALOG);

    }
    else {
      EventHandler.EmitMenuButtonPressed(MenuButtons.BACK);
    }
  }

  public override bool OnMenuButtonPressed(MenuButtons menuButton) {
    base.OnMenuButtonPressed(menuButton);
    switch (menuButton) {
      case MenuButtons.SHOW_DIALOG:
        NoSelectedSlotDialogContainer.ShowDialog();
        return true;
      case MenuButtons.DELETE_SLOT:
        return true;
      case MenuButtons.SELECT_SLOT:
        return true;
      case MenuButtons.BACK:
        return false; // We don't return true here because we want the default behavior to be called
      default:
        return false;
    }
  }

  private void UpdateSlotsYPos(float posY) {
    SlotsContainer.Position = new Vector2(SlotsContainer.Position.X, posY);
  }

  private void _on_SlotsContainer_slot_pressed(int id, string action) {
    currentSlotOnFocus = id;
    if (action == "select") {
      SaveManager.SelectSlot(id);
      _on_confirm_slot_button_selected(id);
      SlotsContainer.SetGameCurrentSelectedSlot(id);
      EventHandler.EmitMenuButtonPressed(MenuButtons.SELECT_SLOT);

    }
    else if (action == "delete") {
      deleteTmpId = id;
      ResetDialogContainerNode.ShowDialog();
      EventHandler.EmitMenuButtonPressed(MenuButtons.DELETE_SLOT);
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
    SaveManager.RemoveSaveSlot(currentSlotOnFocus);
    SlotsContainer.UpdateSlot(deleteTmpId, true);
    SlotsContainer.SetGameCurrentSelectedSlot(SaveManager.GetSelectedSlotIndex());
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
