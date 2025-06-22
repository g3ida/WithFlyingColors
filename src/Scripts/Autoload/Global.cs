using System;
using System.Collections.Generic;
using Godot;
using Wfc.Screens;

public partial class Global : Node2D {
  private static Global _instance = null;

  public override void _Ready() {
    base._Ready();
    _instance = GetTree().Root.GetNode<Global>("GlobalCS");
    SetProcess(false);
  }

  public static Global Instance() {
    return _instance;
  }

  public GameCamera Camera;

  public Player Player;

  public Cutscene Cutscene;
  public GemsHUDContainer GemHUD;

  public PauseMenuImpl PauseMenu;

  public Texture2D _playerSprite;

  private Dictionary<string, string[]> selectedSkin = SkinLoader.GOOGL_SKIN;

  public Texture2D GetPlayerSprite() {
    if (_playerSprite == null) {
      _playerSprite = PlayerSpriteGenerator.GetTexture();
    }
    return _playerSprite;
  }

  public void SetSelectedSkin(Dictionary<string, string[]> skin) {
    if (selectedSkin != skin) {
      selectedSkin = skin;
      _playerSprite = PlayerSpriteGenerator.GetTexture();
    }
  }

  public Dictionary<string, string[]> GetSelectedSkin() {
    return selectedSkin;
  }
}
