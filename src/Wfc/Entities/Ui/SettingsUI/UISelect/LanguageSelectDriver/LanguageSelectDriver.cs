namespace Wfc.Entities.Ui.SettingsUI.UISelect;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Localization;
using Wfc.Core.Settings;
using Wfc.Entities.Ui;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class LanguageSelectDriver : UISelectDriver {
  private List<Language> _languages = LanguageExtensions.Languages;

  public LanguageSelectDriver() {
    var screen_size = DisplayServer.ScreenGetSize();
    foreach (var el in _languages) {
      Items.Add(el.GetLanguageNativeName());
      ItemValues.Add(el.GetLanguageCode());
    }
  }

  public override void onItemSelected(Variant? item) {
    if (item != null) {
      GameSettings.Language = ((string)item).LangaugeCodeToLanguage();
    }
  }

  public override int GetDefaultSelectedIndex() {
    var index = _languages.FindIndex(x => x == GameSettings.Language);
    return index == -1 ? 0 : index;
  }

  public override void _Ready() {
    base._Ready();
  }
}
