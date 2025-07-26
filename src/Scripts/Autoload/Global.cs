using System;
using System.Collections.Generic;
using Godot;
using Wfc.Entities.HUD;
using Wfc.Entities.World.Camera;
using Wfc.Entities.World.Cutscenes;
using Wfc.Entities.World.Player;
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

  public Texture2D GetPlayerSprite() {
    if (_playerSprite == null) {
      _playerSprite = PlayerSpriteGenerator.GetTexture();
    }
    return _playerSprite;
  }
}
