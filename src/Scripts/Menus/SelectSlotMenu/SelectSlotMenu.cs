using Godot;
using System;
using Wfc.Screens;

public partial class SelectSlotMenu : GameMenu
{
  private Button BackButtonNode;
  private SlotsContainer SlotsContainer;
  private DialogContainer ResetDialogContainerNode;
  private DialogContainer NoSelectedSlotDialogContainer;
  private Label CurrentSlotLabelNode;
  private int currentSlotOnFocus;
  private int deleteTmpId = 0; // Used to save the currently deleting slot

  public override void _Ready()
  {
    base._Ready();
    BackButtonNode = GetNode<Button>("BackButton");
    SlotsContainer = GetNode<SlotsContainer>("SlotsContainer");
    ResetDialogContainerNode = GetNode<DialogContainer>("ResetDialogContainer");
    NoSelectedSlotDialogContainer = GetNode<DialogContainer>("NoSelectedSlotDialogContainer");
    CurrentSlotLabelNode = GetNode<Label>("CurrentSlotLabel");
    currentSlotOnFocus = SaveGame.Instance().currentSlotIndex;

    SlotsContainer.SetGameCurrentSelectedSlot(SaveGame.Instance().currentSlotIndex);
    SetSelectedSlotLabel();
  }

  private void OnBackButtonPressed()
  {
    if (SaveGame.Instance().currentSlotIndex == -1)
    {
      Event.Instance.EmitMenuButtonPressed(MenuButtons.SHOW_DIALOG);
    }
    else
    {
      Event.Instance.EmitMenuButtonPressed(MenuButtons.BACK);
    }
  }

  public override bool OnMenuButtonPressed(MenuButtons menuButton)
  {
    base.OnMenuButtonPressed(menuButton);
    switch (menuButton)
    {
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

  private void UpdateSlotsYPos(float posY)
  {
    SlotsContainer.Position = new Vector2(SlotsContainer.Position.X, posY);
  }

  private void _on_SlotsContainer_slot_pressed(int id, string action)
  {
    currentSlotOnFocus = id;
    if (action == "select")
    {
      SaveGame.Instance().currentSlotIndex = id;
      _on_confirm_slot_button_selected(id);
      SlotsContainer.SetGameCurrentSelectedSlot(id);
      Event.Instance.EmitMenuButtonPressed(MenuButtons.SELECT_SLOT);
    }
    else if (action == "delete")
    {
      deleteTmpId = id;
      ResetDialogContainerNode.ShowDialog();
      Event.Instance.EmitMenuButtonPressed(MenuButtons.DELETE_SLOT);
    }
  }

  private void _on_confirm_slot_button_selected(int slotIndex)
  {
    if (SaveGame.Instance().IsSlotFilled(slotIndex))
    {
      SaveGame.Instance().currentSlotIndex = slotIndex;
    }
    else
    {
      SaveGame.Instance().Save(slotIndex, true);
      SaveGame.Instance().Refresh();
    }
    SetSelectedSlotLabel();
  }

  private void OnResetSlotConfirmed()
  {
    SaveGame.Instance().RemoveSaveSlot(currentSlotOnFocus);
    SaveGame.Instance().Refresh();
    SlotsContainer.UpdateSlot(deleteTmpId, true);
    SlotsContainer.SetGameCurrentSelectedSlot(SaveGame.Instance().currentSlotIndex);
    SetSelectedSlotLabel();
  }

  private void SetSelectedSlotLabel()
  {
    if (SaveGame.Instance().currentSlotIndex != -1)
    {
      CurrentSlotLabelNode.Text = $"{SaveGame.Instance().currentSlotIndex + 1}";
    }
    else
    {
      CurrentSlotLabelNode.Text = "None";
    }
  }
}
