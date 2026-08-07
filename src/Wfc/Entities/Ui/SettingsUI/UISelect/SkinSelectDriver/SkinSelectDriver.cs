namespace Wfc.Entities.Ui.SettingsUI.UISelect;

using System.Collections.Generic;
using System.Linq;
using Godot;
using Wfc.Core.Settings;
using Wfc.Skin;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

[ScenePath]
public partial class SkinSelectDriver : UISelectDriver {

  private readonly List<string> _skins = [.. SkinManager.SELECTABLE_SKINS];

  public SkinSelectDriver() {
    foreach (var skin in _skins) {
      Items.Add(SkinManager.DisplayName(skin));
      ItemValues.Add(skin);
    }
  }

  public override void onItemSelected(Variant? item) {
    if (item == null) {
      return;
    }

    var skin = (string)item;
    // Also called while the select builds itself, to show the palette already in use.
    // Only a real change is the player picking one, and only that is worth announcing
    // to everything already drawn in the old colours.
    if (skin == GameSettings.Skin) {
      return;
    }
    GameSettings.Skin = skin;
    EventHandler.Instance.EmitSkinChanged(skin);
  }

  public override int GetDefaultSelectedIndex() {
    var index = _skins.IndexOf(GameSettings.Skin);
    return index == -1 ? 0 : index;
  }
}
