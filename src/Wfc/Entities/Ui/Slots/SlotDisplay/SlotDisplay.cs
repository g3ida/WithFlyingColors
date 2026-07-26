namespace Wfc.Entities.Ui.Slots;

using Wfc.Core.Localization;
using Wfc.Core.Persistence;

// Slots are stored 0-based but shown to the player 1-based, and there may be no
// slot at all. Every screen that prints a slot number goes through here, so the
// main menu and the play sub-menu can't drift apart on either count again.
public static class SlotDisplay {
  public static string GetSelectedSlotText(this ISaveManager saveManager, ILocalizationService localizationService) =>
    saveManager.HasSelectedSlot()
      ? $"{saveManager.GetSelectedSlotIndex() + 1}"
      : localizationService.GetLocalizedString(TranslationKey.menu_label_noSlot);

  // "<caption>: <slot>", the one line the main menu and the select-slot screen both
  // show. Kept here so the two can't come to word it differently.
  public static string GetCurrentSlotLine(this ISaveManager saveManager, ILocalizationService localizationService) =>
    $"{localizationService.GetLocalizedString(TranslationKey.menu_label_currentSlot)}: " +
    saveManager.GetSelectedSlotText(localizationService);
}
