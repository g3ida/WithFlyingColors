namespace Wfc.Screens;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Persistence;
using Wfc.Entities.Ui;
using Wfc.Entities.Ui.Slots;
using Wfc.Screens.Levels;
using Wfc.Screens.MenuManager;
using Wfc.Utils;
using Wfc.Utils.Attributes;

// One screen, two jobs, told apart by the mode the caller left on the menu manager:
// picking where a new game lives (empty slots welcome, filled ones ask before being
// wiped) or picking which existing game to load.
[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class SelectSlotMenu : GameMenu {
  [NodePath("BackButton")]
  private Button _backButtonNode = default!;
  [NodePath("SlotsContainer")]
  private SlotsContainer _slotsContainer = default!;
  [NodePath("ResetDialogContainer")]
  private DialogContainer _resetDialogContainerNode = default!;
  [NodePath("NewGameDialogContainer")]
  private DialogContainer _newGameDialogContainerNode = default!;
  [NodePath("CurrentSlotLabel")]
  private Label CurrentSlotLabelNode = default!;
  // The slot the player last acted on: the one a confirmed dialog will wipe.
  private int _currentSlotOnFocus;

  private SlotPickerMode _pickerMode = SlotPickerMode.Load;

  public void OnResolved() {

  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _pickerMode = MenuManager.GetSlotPickerMode();
    _slotsContainer.SetAllowSelectingEmptySlots(_pickerMode == SlotPickerMode.NewGame);
    _currentSlotOnFocus = SaveManager.GetSelectedSlotIndex();
    _slotsContainer.SetGameCurrentSelectedSlot(SaveManager.GetSelectedSlotIndex());
    SetSelectedSlotLabel();
  }

  private void OnBackButtonPressed() => EventHandler.EmitMenuActionPressed(MenuAction.GoBack);

  public override bool OnMenuButtonPressed(MenuAction menuAction) {
    switch (menuAction) {
      case MenuAction.DeleteSlot:
      case MenuAction.SelectSlot:
        return true;
      default:
        // Backing out without picking is fine in both modes: every write path
        // refuses a slot that holds nothing, and the play sub-menu only offers
        // what the slots can actually answer for.
        return false;
    }
  }

  private void _updateSlotsYPos(float posY) {
    _slotsContainer.Position = new Vector2(_slotsContainer.Position.X, posY);
  }

  private void _on_SlotsContainer_SlotPressed(int id, string action) {
    _currentSlotOnFocus = id;
    if (action == "select") {
      if (_pickerMode == SlotPickerMode.NewGame) {
        if (SaveManager.IsSLotFilled(id)) {
          _newGameDialogContainerNode.ShowDialog();
        }
        else {
          _startNewGameInSlot(id);
        }
      }
      else {
        SaveManager.SelectSlot(id);
        _slotsContainer.SetGameCurrentSelectedSlot(id);
        SetSelectedSlotLabel();
        EventHandler.EmitMenuActionPressed(MenuAction.SelectSlot);
        NavigateToScreen(GameMenus.GAME);
      }
    }
    else if (action == "delete") {
      _resetDialogContainerNode.ShowDialog();
      EventHandler.EmitMenuActionPressed(MenuAction.DeleteSlot);
    }
  }

  private void _onNewGameOverwriteConfirmed() => _startNewGameInSlot(_currentSlotOnFocus);

  private void _startNewGameInSlot(int slotIndex) {
    // Wiping the slot clears the selection when it was the selected one, so the
    // reselect puts the new game exactly where the player pointed rather than
    // letting the first save land in slot 0.
    SaveManager.RemoveSaveSlot(slotIndex);
    SaveManager.SelectSlot(slotIndex);
    // A blank but real save: the meta file is what lets every later checkpoint
    // write into this slot.
    SaveManager.SaveGame(GetTree(), slotIndex);
    NavigateToLevelScreen(LevelDispatcher.LEVELS[0].Id);
  }

  private void OnResetSlotConfirmed() {
    SaveManager.RemoveSaveSlot(_currentSlotOnFocus);
    _slotsContainer.UpdateSlot(_currentSlotOnFocus, true);
    _slotsContainer.SetGameCurrentSelectedSlot(SaveManager.GetSelectedSlotIndex());
    SetSelectedSlotLabel();
  }

  // The label holds a string that was already translated when the screen was built,
  // so the engine's own auto-translation has nothing left to redo once the player
  // picks another language.
  public override void _Notification(int what) {
    base._Notification(what);
    if (what == NotificationTranslationChanged && IsNodeReady()) {
      SetSelectedSlotLabel();
    }
  }

  private void SetSelectedSlotLabel() =>
    CurrentSlotLabelNode.Text = SaveManager.GetCurrentSlotLine(LocalizationService);
}
