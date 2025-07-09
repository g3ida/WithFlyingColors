namespace Wfc.Entities.Ui;

using Wfc.Core.Persistence;
using Wfc.Screens.MenuManager;

public class ButtonDef {
  public enum ButtonCondition {
    None,
    IsDirtySlot, // a slot contains a game in progress not a won game
    IsVirginSlot, // a new slot with progress 0%
  }

  public required string Text;
  public ButtonCondition DisplayCondition;
  public ButtonCondition DisableCondition = ButtonCondition.None;

  public MenuAction MenuAction;
}

public static class ButtonsConditionsExtensions {
  public static bool Verify(this ButtonDef.ButtonCondition buttonDef, ISaveManager saveManager) => buttonDef switch {
    ButtonDef.ButtonCondition.IsDirtySlot => (saveManager.GetSlotMetaData()?.Progress ?? 0) > 0,
    ButtonDef.ButtonCondition.IsVirginSlot => (saveManager.GetSlotMetaData()?.Progress ?? 0) == 0,
    ButtonDef.ButtonCondition.None => false,
    _ => false,
  };
}
