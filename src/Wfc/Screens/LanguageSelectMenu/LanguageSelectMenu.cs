namespace Wfc.Screens;

using Wfc.Core.Settings;
using Wfc.Screens.MenuManager;
using Wfc.Utils.Attributes;

// The first thing the game asks on a first launch, since every screen after it is
// drawn in whatever is picked here. From then on the settings menu owns the language.
[ScenePath]
public partial class LanguageSelectMenu : FirstRunMenu {
  // Straight to the main menu for a player who already has a palette on file - one
  // upgrading from a version that only ever asked the language has that flag false and
  // gets the colour question, and nobody is asked either question twice.
  protected override GameMenus NextScreen =>
    GameSettings.HasStoredSkin ? GameMenus.MAIN_MENU : GameMenus.SKIN_SELECT;
}
