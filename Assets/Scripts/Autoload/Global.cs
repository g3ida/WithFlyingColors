using Godot;
using System;
using System.Collections.Generic;

public class Global : Node2D
{
    private static Node _gdInstance = null;
    private static Global _instance = null;

    public override void _Ready() {
      base._Ready();
      _gdInstance = GetTree().Root.GetNode("Global");
      _instance = GetTree().Root.GetNode<Global>("GlobalCS");
      SetProcess(false);
    }

    public static Global Instance() {
      return _instance;
    }

    public GameCamera Camera {
      get { return (GameCamera)_gdInstance.Get("camera"); }
      set { _gdInstance.Set("camera", value); }
    }
    
    public Player Player {
      get { return (Player)_gdInstance.Get("player"); }
      set { _gdInstance.Set("player", value); }
    }

    public Cutscene Cutscene {
      get { return (Cutscene)_gdInstance.Get("cutscene"); }
      set { _gdInstance.Set("cutscene", value); }
    }
    public GemsHUDContainer GemHUD {
      get { return (GemsHUDContainer)_gdInstance.Get("gem_hud"); }
      set { _gdInstance.Set("gem_hud", value); }
    }
    public Node PauseMenu {
      get { return (Node)_gdInstance.Get("pause_menu"); }
      set { _gdInstance.Set("pause_menu", value); }
    }

    public Texture _playerSprite;
    // {
    //   get { return (Texture)_gdInstance.Get("_playerSprite"); }
    //   set { _gdInstance.Set("_playerSprite", value); }
    // }
    
    private Dictionary<string, string[]> selectedSkin = SkinLoader.DEFAULT_SKIN;

    public Texture GetPlayerSprite()
    {
        if (_playerSprite == null)
        {
            _playerSprite = PlayerSpriteGenerator.GetTexture();
        }
        return _playerSprite;
    }

    public void SetSelectedSkin(Dictionary<string, string[]> skin)
    {
        if (selectedSkin != skin)
        {
            selectedSkin = skin;
            _playerSprite = PlayerSpriteGenerator.GetTexture();
        }
    }

    public Dictionary<string, string[]> GetSelectedSkin()
    {
        return selectedSkin;
    }
}
