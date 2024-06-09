using System.Collections.Generic;

namespace Wfc.Skin
{
  public partial class SkinManager
  {
    private static SkinManager _s_instance;
    public Skin CurrentSkin
    {
      get
      {
        return store.GetSkin("current");
      }
    }
    public Skin DefaultSkin
    {
      get
      {
        return store.GetSkin("default");
      }
    }

    public static SkinManager Instance
    {
      get
      {
        if (_s_instance == null)
        {
          _s_instance = new SkinManager();
        }
        return _s_instance;
      }
    }

    private SkinsStore store;

    private SkinManager()
    {
      store = new SkinsStore();
      _PopulateWithPresetSkins();
    }

    public Skin GetSkin(string name)
    {
      return store.GetSkin(name);
    }

    public bool AddSkin(string name, Skin skin)
    {
      return store.AddSkin(name, skin);
    }

    public bool RemoveSkin(string name)
    {
      if (name == "default" || name == "current")
      {
        throw new System.ArgumentException("Cannot remove default or current skin");
      }
      return store.RemoveSkin(name);
    }

    public List<Skin> GetAllSkins()
    {
      return store.GetAllSkins();
    }

    public void ClearToDefaults()
    {
      store.ClearSkins();
      _PopulateWithPresetSkins();
    }

    public bool ContainsSkin(string name)
    {
      return store.ContainsSkin(name);
    }

    private void _PopulateWithPresetSkins()
    {
      store.AddSkin("default", PresetSkins.DEFAULT_SKIN);
      store.AddSkin("googl", PresetSkins.GOOGL_SKIN);
      store.AddSkin("current", PresetSkins.DEFAULT_SKIN);
    }

  }
}