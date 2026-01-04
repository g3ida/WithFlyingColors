namespace Wfc.Entities.Ui.SettingsUI.UISelect;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Input.Controllers;
using Wfc.Core.Localization;
using Wfc.Core.Settings;
using Wfc.Entities.Ui;
using Wfc.Utils.Attributes;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class ControllerSelectDriver : UISelectDriver {

  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();

  private List<ControllerType> _controllers = ControllerTypeExtensions.ControllerTypes;

  public ControllerSelectDriver() {

  }

  public override void onItemSelected(Variant? item) {
    if (item != null) {
      // GameSettings.Controller = ((string)item).ToInt().ToControllerType();
    }
  }

  public override int GetDefaultSelectedIndex() {
    // var index = _controllers.FindIndex(x => x == GameSettings.Controller);
    // return index == -1 ? 0 : index;
    return 0;
  }

  public override void _Ready() {
    base._Ready();
  }

  public void OnResolved() {
    foreach (var el in _controllers) {
      Items.Add(el.GetLocalizedName(LocalizationService));
      ItemValues.Add(((int)el).ToString());
    }
  }
}
