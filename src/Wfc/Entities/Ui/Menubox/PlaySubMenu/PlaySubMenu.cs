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
    List<ButtonDef> subMenuButtonsDef = [
            new() {
            Text = LocalizationService.GetLocalizedString(TranslationKey.menu_button_continue),
            MenuAction = MenuAction.ContinueGame,
            DisplayCondition = ButtonDef.ButtonCondition.IsDirtySlot
        },
        new()
        {
            Text = LocalizationService.GetLocalizedString(TranslationKey.menu_button_newGame),
            MenuAction = MenuAction.NewGame,
            DisplayCondition = ButtonDef.ButtonCondition.IsVirginSlot
        },
        new()
        {
            Text = LocalizationService.GetLocalizedString(TranslationKey.menu_button_selectedSlot) + $": {SaveManager.GetSelectedSlotIndex() + 1}",
            MenuAction = MenuAction.GoToSlotSelect,
            DisplayCondition = ButtonDef.ButtonCondition.None
        },
    ];
    var subMenuNode = SubMenuSceneBuilder.Create(this, subMenuButtonsDef, ColorGroup.Blue, SaveManager);
    CustomMinimumSize = subMenuNode.CustomMinimumSize;
    Size = subMenuNode.Size;
  }
}
