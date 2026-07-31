namespace Wfc.Entities.Ui.Menubox;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Localization;
using Wfc.Core.Persistence;
using Wfc.Core.Types;
using Wfc.Screens.MenuManager;

[Meta(typeof(IAutoNode))]
public partial class PlaySubMenu : Control {

  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();

  [Dependency]
  public ISaveManager SaveManager => this.DependOn<ISaveManager>();

  public void OnResolved() {
    // Continue resumes instantly, so it only appears once there is a game to resume;
    // Load Game exists to choose between games, so one played slot is not enough for
    // it to earn a row. New Game is always a valid thing to want.
    List<ButtonDef> subMenuButtonsDef = [
            new() {
            Text = LocalizationService.GetLocalizedString(TranslationKey.menu_button_continue),
            MenuAction = MenuAction.ContinueGame,
            DisplayCondition = ButtonDef.ButtonCondition.HasAnyPlayedSlot
        },
        new()
        {
            Text = LocalizationService.GetLocalizedString(TranslationKey.menu_button_newGame),
            MenuAction = MenuAction.NewGame,
            DisplayCondition = ButtonDef.ButtonCondition.None
        },
        new()
        {
            Text = LocalizationService.GetLocalizedString(TranslationKey.menu_button_loadGame),
            MenuAction = MenuAction.LoadGame,
            DisplayCondition = ButtonDef.ButtonCondition.HasMultiplePlayedSlots
        },
    ];
    var subMenuNode = SubMenuSceneBuilder.Create(this, subMenuButtonsDef, ColorGroup.Blue, SaveManager);
    CustomMinimumSize = subMenuNode.CustomMinimumSize;
    Size = subMenuNode.Size;
  }
}
