using System;
using System.Collections.Generic;
using Godot;
using Wfc.Entities.HUD;
using Wfc.Entities.World.Camera;
using Wfc.Entities.World.Cutscenes;
using Wfc.Entities.World.Player;
using Wfc.Screens;

public partial class Global : Node2D {
  private static Global _instance = null!;

  public override void _Ready() {
    base._Ready();
    _instance = GetTree().Root.GetNode<Global>("GlobalCS");
    SetProcess(false);
  }

  public static Global Instance() {
    return _instance;
  }
  public Player Player;
}
