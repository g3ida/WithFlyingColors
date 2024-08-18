namespace Wfc.Entities.Ui;
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
  public static bool Verify(this ButtonDef.ButtonCondition buttonDef) => buttonDef switch {
    ButtonDef.ButtonCondition.IsDirtySlot => SaveGame.Instance().DoesSlotHaveProgress(SaveGame.Instance().currentSlotIndex),
    ButtonDef.ButtonCondition.IsVirginSlot => !SaveGame.Instance().DoesSlotHaveProgress(SaveGame.Instance().currentSlotIndex),
    ButtonDef.ButtonCondition.None => false,
    _ => false,
  };
}
