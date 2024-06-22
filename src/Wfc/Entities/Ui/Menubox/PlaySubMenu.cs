namespace Wfc.Entities.Ui.Menubox;
using Godot;
using System.Collections.Generic;
using Wfc.Core.Types;

public partial class PlaySubMenu : Control {
  private readonly List<ButtonDef> _subMenuButtonsDef = [
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

  public override void _Ready() {
    var subMenuNode = SubMenuSceneBuilder.Create(this, _subMenuButtonsDef, ColorGroup.Blue);
    CustomMinimumSize = subMenuNode.CustomMinimumSize;
    Size = subMenuNode.Size;
  }
}
