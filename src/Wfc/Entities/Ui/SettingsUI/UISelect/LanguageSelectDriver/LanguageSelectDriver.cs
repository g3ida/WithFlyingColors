namespace Wfc.Entities.Ui.SettingsUI.UISelect;

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using Wfc.Core.Localization;
using Wfc.Core.Settings;
using Wfc.Entities.Ui;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class LanguageSelectDriver : UISelectDriver {

  private List<Language> _languages = LanguageExtensions.Languages;

  public LanguageSelectDriver() {
    foreach (var el in _languages) {
      Items.Add(el.GetLanguageNativeName());
      ItemValues.Add(el.GetLanguageCode());
    }
  }

  public override void onItemSelected(Variant? item) {
    if (item == null) {
      return;
    }

    var language = ((string)item).LanguageCodeToLanguage();
    // Also called while the select builds itself, to show the language already in
    // use. Only a real change is the player picking one, so only that is worth
    // announcing - the sfx that goes with it is wired to this event.
    if (language == GameSettings.Language) {
      return;
    }
    GameSettings.Language = language;
    SettingsRepo.Instance.OnLanguageChanged(language);
  }

  public override int GetDefaultSelectedIndex() {
    var index = _languages.FindIndex(x => x == GameSettings.Language);
    return index == -1 ? 0 : index;
  }

  public override void _Ready() {
    base._Ready();
  }
}
