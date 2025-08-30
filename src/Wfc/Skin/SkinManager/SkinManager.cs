namespace Wfc.Skin;

using System.Collections.Generic;


public partial class SkinManager {
  private static SkinManager? _instance;
  public GameSkin CurrentSkin => _store.GetSkin("current");
  public GameSkin DefaultSkin => _store.GetSkin("default");

  public static SkinManager Instance {
    get {
      _instance ??= new SkinManager();
      return _instance;
    }
  }

  private readonly SkinsStore _store;

  private SkinManager() {
    _store = new SkinsStore();
    PopulateWithPresetSkins();
  }

  public GameSkin GetSkin(string name) => _store.GetSkin(name);

  public bool AddSkin(string name, GameSkin skin) => _store.AddSkin(name, skin);

  public bool RemoveSkin(string name) {
    if (name is "default" or "current") {
      throw new System.ArgumentException("Cannot remove default or current skin");
    }
    return _store.RemoveSkin(name);
  }

  public List<GameSkin> GetAllSkins() => _store.GetAllSkins();

  public void ClearToDefaults() {
    _store.ClearSkins();
    PopulateWithPresetSkins();
  }

  public bool ContainsSkin(string name) => _store.ContainsSkin(name);

  private void PopulateWithPresetSkins() {
    _store.AddSkin("default", PresetSkins.DEFAULT_SKIN);
    _store.AddSkin("googl", PresetSkins.GOOGL_SKIN);
    _store.AddSkin("current", PresetSkins.DEFAULT_SKIN);
  }

}
