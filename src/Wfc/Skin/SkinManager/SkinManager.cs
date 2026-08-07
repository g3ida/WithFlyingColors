namespace Wfc.Skin;

using System.Collections.Generic;


public partial class SkinManager {
  // The palettes a player may pick between, in the order the picker steps through
  // them. The default leads, and the one next to it is the one that holds up best
  // under colour vision deficiency, so a single step off the default reaches it.
  public static readonly string[] SELECTABLE_SKINS = ["default", "clear", "googl"];

  public const string DEFAULT_SKIN_NAME = "default";

  private static SkinManager? _instance;
  public GameSkin CurrentSkin => _store.GetSkin("current");
  public GameSkin DefaultSkin => _store.GetSkin(DEFAULT_SKIN_NAME);

  // Which of the selectable palettes "current" is a copy of. The skins themselves are
  // values and several may hold the same colours, so the choice is tracked by name
  // rather than read back off the colours.
  public string CurrentSkinName { get; private set; } = DEFAULT_SKIN_NAME;

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

  /// <summary>
  /// What a palette is called on screen. Proper names rather than translated words,
  /// the same way the language picker shows each language in its own language, and
  /// deliberately nothing about who a palette is for.
  /// </summary>
  public static string DisplayName(string skinName) => skinName switch {
    DEFAULT_SKIN_NAME => "Neon",
    "clear" => "Clear",
    "googl" => "Classic",
    _ => skinName,
  };

  public GameSkin GetSkin(string name) => _store.GetSkin(name);

  public bool AddSkin(string name, GameSkin skin) => _store.AddSkin(name, skin);

  public bool RemoveSkin(string name) {
    if (name is "default" or "current") {
      throw new System.ArgumentException("Cannot remove default or current skin");
    }
    return _store.RemoveSkin(name);
  }

  public List<GameSkin> GetAllSkins() => _store.GetAllSkins();

  /// <summary>
  /// Makes one of the stored palettes the one the game draws itself in. False if there
  /// is no such palette, which leaves the current one alone.
  /// </summary>
  public bool SetCurrentSkin(string name) {
    if (name == "current" || !_store.ContainsSkin(name)) {
      return false;
    }
    _store.RemoveSkin("current");
    _store.AddSkin("current", _store.GetSkin(name));
    CurrentSkinName = name;
    return true;
  }

  public void ClearToDefaults() {
    _store.ClearSkins();
    PopulateWithPresetSkins();
    CurrentSkinName = DEFAULT_SKIN_NAME;
  }

  public bool ContainsSkin(string name) => _store.ContainsSkin(name);

  private void PopulateWithPresetSkins() {
    _store.AddSkin(DEFAULT_SKIN_NAME, PresetSkins.DEFAULT_SKIN);
    _store.AddSkin("clear", PresetSkins.CLEAR_SKIN);
    _store.AddSkin("googl", PresetSkins.GOOGL_SKIN);
    _store.AddSkin("current", PresetSkins.DEFAULT_SKIN);
  }

}
