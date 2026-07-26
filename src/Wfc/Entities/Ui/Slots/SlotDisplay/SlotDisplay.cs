namespace Wfc.Entities.Ui.Slots;

using Wfc.Core.Persistence;

// Slots are stored 0-based but shown to the player 1-based, and there may be no
// slot at all. Every screen that prints a slot number goes through here, so the
// main menu and the play sub-menu can't drift apart on either count again.
public static class SlotDisplay {
  // Not localized yet: the select-slot screen has always shown this literal, and
  // giving it a TranslationKey means a new entry in every locale file.
  private const string NO_SLOT_TEXT = "None";

  public static string GetSelectedSlotText(this ISaveManager saveManager) =>
    saveManager.HasSelectedSlot() ? $"{saveManager.GetSelectedSlotIndex() + 1}" : NO_SLOT_TEXT;
}
