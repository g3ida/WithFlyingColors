namespace Wfc.Entities.Ui.Menubox;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using System.Collections.Generic;
using Wfc.Core.Localization;
using Wfc.Core.Types;
using Wfc.Screens.MenuManager;

[Meta(typeof(IAutoNode))]
public partial class PlaySubMenu : Control
{

  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();

  public void OnResolved()
  {
    List<ButtonDef> subMenuButtonsDef = [
            new() {
            Text = LocalizationService.GetLocalizedString(TranslationKey.Continue),
            MenuAction = MenuAction.ContinueGame,
            DisplayCondition = ButtonDef.ButtonCondition.IsDirtySlot
        },
        new()
        {
            Text = LocalizationService.GetLocalizedString(TranslationKey.NewGame),
            MenuAction = MenuAction.NewGame,
            DisplayCondition = ButtonDef.ButtonCondition.IsVirginSlot
        },
        new()
        {
            Text = LocalizationService.GetLocalizedString(TranslationKey.SelectedSlot) + $": {SaveGame.Instance().currentSlotIndex + 1}",
            MenuAction = MenuAction.GoToSlotSelect,
            DisplayCondition = ButtonDef.ButtonCondition.None
        },
    ];
    var subMenuNode = SubMenuSceneBuilder.Create(this, subMenuButtonsDef, ColorGroup.Blue);
    CustomMinimumSize = subMenuNode.CustomMinimumSize;
    Size = subMenuNode.Size;
  }
}
