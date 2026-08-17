namespace Wfc.Screens;

using Godot;
using Wfc.Core.Event;
using Wfc.Core.Localization;
using Wfc.Core.Persistence;
using Wfc.Entities.Ui.Dialogs;
using Wfc.Entities.Ui.InputHint;
using Wfc.Entities.Ui.Slots;
using Wfc.Screens.MenuManager;
using Wfc.Utils;
using Wfc.Utils.Attributes;

// One screen, two jobs, told apart by the mode the caller left on the menu manager:
// picking where a new game lives (every slot welcome, filled ones ask before being
// wiped) or picking which existing game to load (empty slots greyed out and out of
// focus reach). A single press on a slot performs that mode's action.
[ScenePath]
public partial class SelectSlotMenu : GameMenu {
  [NodePath("BackButton")]
  private Button _backButtonNode = default!;
  [NodePath("SlotsContainer")]
  private SlotsContainer _slotsContainer = default!;
  [NodePath("OverwriteDialogContainer")]
  private DialogContainer _overwriteDialogContainerNode = default!;
  [NodePath("InstructionLabel")]
  private Label _instructionLabelNode = default!;
  [NodePath("InputHintBar")]
  private InputHintBar _inputHintBarNode = default!;

  // The slot the player last pressed: the one a confirmed overwrite will wipe.
  private int _pendingSlot = ISaveManager.NO_SLOT;

  private SlotPickerMode _pickerMode = SlotPickerMode.Load;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _pickerMode = MenuManager.GetSlotPickerMode();

    var isNewGame = _pickerMode == SlotPickerMode.NewGame;
    _slotsContainer.SetAllowSelectingEmptySlots(isNewGame);
    _instructionLabelNode.Text = LocalizationService.GetLocalizedString(
        isNewGame
            ? TranslationKey.menu_label_selectSlotNewGame
            : TranslationKey.menu_label_selectSlotLoad);
    // Same card, mode's own verb: a press means "select a home for the new game" in
    // one mode and "load this save" in the other.
    if (!isNewGame) {
      _inputHintBarNode.RelabelCard("SelectCard", TranslationKey.menu_hint_load);
    }

    // Load mode puts every empty slot out of focus reach, so with nothing saved no
    // card can take focus at all. The back button stays out of the focus chain the
    // rest of the time - UICancel is how a screen is left - but here it is the only
    // thing left to point at.
    if (!isNewGame && SaveManager.HasNoSaves()) {
      _backButtonNode.FocusMode = FocusModeEnum.All;
      _backButtonNode.GrabFocus();
    }
  }

  private static void OnBackButtonPressed() => GameEvents.Instance.OnMenuActionPressed(MenuAction.GoBack);

  // Backing out without picking is fine in both modes: every write path refuses a slot
  // that holds nothing, and the play sub-menu only offers what the slots can actually
  // answer for.
  public override bool OnMenuButtonPressed(MenuAction menuAction) =>
      menuAction == MenuAction.SelectSlot;

  private void _onSlotsContainerSlotPressed(int id) {
    _pendingSlot = id;
    if (_pickerMode == SlotPickerMode.NewGame) {
      if (SaveManager.IsSLotFilled(id)) {
        _overwriteDialogContainerNode.ShowDialog();
      }
      else {
        StartNewGameInSlot(id);
      }
    }
    else {
      SaveManager.SelectSlot(id);
      GameEvents.Instance.OnMenuActionPressed(MenuAction.SelectSlot);
      NavigateToScreen(GameMenus.GAME);
    }
  }

  private void _onNewGameOverwriteConfirmed() => StartNewGameInSlot(_pendingSlot);
}
