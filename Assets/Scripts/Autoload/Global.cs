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

    public Camera2D Camera {
      get { return (Camera2D)_gdInstance.Get("camera"); }
      set { _gdInstance.Set("camera", value); }
    }
    
    public KinematicBody2D Player {
      get { return (KinematicBody2D)_gdInstance.Get("player"); }
      set { _gdInstance.Set("player", value); }
    }

    private Node cutscene {
      get { return (Node)_gdInstance.Get("cutscene"); }
      set { _gdInstance.Set("cutscene", value); }
    }
    private Node gemHud {
      get { return (Node)_gdInstance.Get("gemHud"); }
      set { _gdInstance.Set("gemHud", value); }
    }
    private Node pauseMenu {
      get { return (Node)_gdInstance.Get("pauseMenu"); }
      set { _gdInstance.Set("pauseMenu", value); }
    }

    private Texture _playerSprite {
      get { return (Texture)_gdInstance.Get("_playerSprite"); }
      set { _gdInstance.Set("_playerSprite", value); }
    }
    
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
