namespace Wfc.Entities.Ui;

using Wfc.Core.Persistence;
using Wfc.Screens.MenuManager;

public class ButtonDef {
  // Conditions look across every slot, not just the selected one: Continue must
  // appear when any slot holds a game, whichever slot happens to be selected.
  public enum ButtonCondition {
    None,
    HasAnyPlayedSlot,
    HasMultiplePlayedSlots,
  }

  public required string Text;
  public ButtonCondition DisplayCondition;
  public ButtonCondition DisableCondition = ButtonCondition.None;

  public MenuAction MenuAction;
}

public static class ButtonsConditionsExtensions {
  // None has to answer these two the opposite way round, which is why they are named
  // rather than both going through Verify. A button with no display condition is one
  // that is always offered; a button with no disable condition is one that is never
  // greyed out. Sharing Verify meant None said "no" to both, and the play sub-menu's
  // slot button was filtered out before it was ever built.
  public static bool ShouldDisplay(this ButtonDef.ButtonCondition condition, ISaveManager saveManager) =>
    condition == ButtonDef.ButtonCondition.None || condition.Verify(saveManager);

  public static bool ShouldDisable(this ButtonDef.ButtonCondition condition, ISaveManager saveManager) =>
    condition != ButtonDef.ButtonCondition.None && condition.Verify(saveManager);

  // The raw predicate: is this condition true of the current save state?
  public static bool Verify(this ButtonDef.ButtonCondition buttonDef, ISaveManager saveManager) => buttonDef switch {
    ButtonDef.ButtonCondition.HasAnyPlayedSlot => saveManager.CountPlayedSlots() > 0,
    ButtonDef.ButtonCondition.HasMultiplePlayedSlots => saveManager.CountPlayedSlots() >= 2,
    _ => false,
  };
}
