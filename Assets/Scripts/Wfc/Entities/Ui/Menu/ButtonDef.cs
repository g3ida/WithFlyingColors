using System;
using Wfc.Entities.Ui.Menu;

namespace Wfc.Entities.Ui
{
  public class ButtonDef
  {
    public enum ButtonCondition
    {
      None,
      IsDirtySlot, // a slot contains a game in progress not a won game
      IsVirginSlot, // a new slot with progress 0%
    }

    public string Text;
    public ButtonCondition DisplayCondition;
    public ButtonCondition DisableCondition = ButtonCondition.None;

    public MenuAction MenuAction;
  }

  public static class ButtonsConditionsExtensions
  {
    public static bool Verify(this ButtonDef.ButtonCondition buttonDef)
    {
      switch (buttonDef)
      {
        case ButtonDef.ButtonCondition.IsDirtySlot:
          return SaveGame.Instance().DoesSlotHaveProgress(SaveGame.Instance().currentSlotIndex);
        case ButtonDef.ButtonCondition.IsVirginSlot:
          return !SaveGame.Instance().DoesSlotHaveProgress(SaveGame.Instance().currentSlotIndex);
        default:
          return false;
      }
    }
  }
}