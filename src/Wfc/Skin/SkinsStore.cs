namespace Wfc.Skin;
using System.Collections.Generic;
using System.Linq;

internal partial class SkinsStore {

  private Dictionary<string, GameSkin> Skins { get; }

  public SkinsStore() {
    Skins = [];
  }

  public GameSkin GetSkin(string name) {
    Skins.TryGetValue(name, out var skin);
    return skin ?? throw new KeyNotFoundException("No skin found for name: " + name);
  }

  public bool AddSkin(string name, GameSkin skin) {
    if (Skins.ContainsKey(name)) {
      return false;
    }
    Skins.Add(name, skin);
    return true;
  }

  public bool RemoveSkin(string name) => Skins.Remove(name);

  public List<GameSkin> GetAllSkins() => Skins.Select(s => s.Value).Distinct().ToList();

  public void ClearSkins() => Skins.Clear();

  public bool ContainsSkin(string name) => Skins.ContainsKey(name);

  public bool ContainsSkin(GameSkin skin) => Skins.ContainsValue(skin);
}