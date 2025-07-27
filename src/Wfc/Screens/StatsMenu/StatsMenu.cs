namespace Wfc.Screens;

using Wfc.Core.Event;
using Wfc.Screens.MenuManager;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class StatsMenu : GameMenu {
  public override void _Ready() {
    base._Ready();
  }

  private void OnBackButtonPressed() {
    EventHandler.EmitMenuActionPressed(MenuAction.GoBack);
  }
}
