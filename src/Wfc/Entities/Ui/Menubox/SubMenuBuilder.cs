namespace Wfc.Entities.Ui.Menubox;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Wfc.Core.Types;
using Wfc.Entities.Ui.Menu;
using Wfc.Utils;


public class SubMenuSceneBuilder {
  public static SubMenu Create(Node instantiator, IEnumerable<ButtonDef> buttons, ColorGroup colorGroup) {
    var subMenuButtons = buttons
      .Where(button => button.DisplayCondition.Verify())
      .Select(button => new SubMenuButton {
        Text = button.Text,
        OnClick = () => button.MenuAction.Emit(),
        IsDisabled = button.DisableCondition.Verify(),
      });
    var instance = SceneHelpers.InstantiateNode<SubMenu>();
    instance.Ready += () => instance.PopulateWith(subMenuButtons, colorGroup);
    instantiator.AddChild(instance);
    instance.Owner = instantiator;
    return instance;
  }
}