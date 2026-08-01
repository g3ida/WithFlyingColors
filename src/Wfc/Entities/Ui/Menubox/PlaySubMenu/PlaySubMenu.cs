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
    // The box only opens this sub-menu when at least one slot is occupied, so
    // Continue and New Game always have an answer. Load Game only earns a row
    // once there is a second save to choose between - with a single one it
    // could only ever repeat what Continue already does.
    List<ButtonDef> subMenuButtonsDef = [
        new() {
            Text = LocalizationService.GetLocalizedString(TranslationKey.menu_button_continue),
            MenuAction = MenuAction.ContinueGame,
        },
        new() {
            Text = LocalizationService.GetLocalizedString(TranslationKey.menu_button_newGame),
            MenuAction = MenuAction.NewGame,
        },
    ];
    if (SaveManager.CountFilledSlots() >= 2) {
      subMenuButtonsDef.Add(new() {
        Text = LocalizationService.GetLocalizedString(TranslationKey.menu_button_loadGame),
        MenuAction = MenuAction.LoadGame,
      });
    }
    var subMenuNode = SubMenuSceneBuilder.Create(this, subMenuButtonsDef, ColorGroup.Blue);
    CustomMinimumSize = subMenuNode.CustomMinimumSize;
    Size = subMenuNode.Size;
  }
}
