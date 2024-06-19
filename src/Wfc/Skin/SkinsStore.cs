using System.Collections.Generic;
using System.Linq;

namespace Wfc.Skin
{

  internal partial class SkinsStore
  {

    private Dictionary<string, Skin> Skins { get; }

    public SkinsStore()
    {
      Skins = new Dictionary<string, Skin>();
    }

    public Skin GetSkin(string name)
    {
      Skins.TryGetValue(name, out var skin);
      return skin;
    }

    public bool AddSkin(string name, Skin skin)
    {
      if (Skins.ContainsKey(name))
      {
        return false;
      }
      Skins.Add(name, skin);
      return true;
    }

    public bool RemoveSkin(string name)
    {
      return Skins.Remove(name);
    }

    public List<Skin> GetAllSkins()
    {
      return Skins.Select(s => s.Value).Distinct().ToList();
    }

    public void ClearSkins()
    {
      Skins.Clear();
    }

    public bool ContainsSkin(string name)
    {
      return Skins.ContainsKey(name);
    }

    public bool ContainsSkin(Skin skin)
    {
      return Skins.ContainsValue(skin);
    }
  }
}