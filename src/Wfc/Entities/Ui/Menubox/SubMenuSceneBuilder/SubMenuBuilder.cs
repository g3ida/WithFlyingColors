namespace Wfc.Entities.Ui.Menubox;

using System.Collections.Generic;
using System.Linq;
using Godot;
using Wfc.Core.Persistence;
using Wfc.Core.Types;
using Wfc.Screens.MenuManager;
using Wfc.Utils;


public class SubMenuSceneBuilder {
  public static SubMenu Create(Node instantiator, IEnumerable<ButtonDef> buttons, ColorGroup colorGroup, ISaveManager saveManager) {
    var subMenuButtons = buttons
      .Where(button => button.DisplayCondition.Verify(saveManager))
      .Select(button => new SubMenuButton {
        Text = button.Text,
        OnClick = () => button.MenuAction.Emit(),
        IsDisabled = button.DisableCondition.Verify(saveManager),
      });
    var instance = SceneHelpers.InstantiateNode<SubMenu>();
    instance.Ready += () => instance.PopulateWith(subMenuButtons, colorGroup);
    instantiator.AddChild(instance);
    instance.Owner = instantiator;
    return instance;
  }
}
