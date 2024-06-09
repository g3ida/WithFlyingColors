using Godot;
using System.Collections.Generic;
using Wfc.Core.Types;

namespace Wfc.Entities.Ui.Menubox
{
  public partial class PlaySubMenu : Control
  {
    private readonly List<ButtonDef> SubMenuButtonsDef = [
        new() {
            Text = "Continue",
            MenuAction = Menu.MenuAction.ContinueGame,
            DisplayCondition = ButtonDef.ButtonCondition.IsDirtySlot
        },
        new()
        {
            Text = "New Game",
            MenuAction = Menu.MenuAction.NewGame,
            DisplayCondition = ButtonDef.ButtonCondition.IsVirginSlot
        },
        new()
        {
            Text = $"Current Slot: {SaveGame.Instance().currentSlotIndex + 1}",
            MenuAction = Menu.MenuAction.GoToSlotSelect,
            DisplayCondition = ButtonDef.ButtonCondition.None
        },
    ];

    public override void _Ready()
    {
      var SubMenuNode = SubMenuSceneBuilder.Create(this, SubMenuButtonsDef, ColorGroup.Blue);
      CustomMinimumSize = SubMenuNode.CustomMinimumSize;
      Size = SubMenuNode.Size;
    }
  }
}
