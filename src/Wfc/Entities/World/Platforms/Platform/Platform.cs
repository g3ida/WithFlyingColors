namespace Wfc.Entities.World.Platforms;

using System;
using Chickensoft.AutoInject;
using Godot;
using GodotTestDriver.Util;
using Wfc.Core.Event;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Images;
using EventHandler = Wfc.Core.Event.EventHandler;

[Tool]
public partial class Platform : AnimatableBody2D {
  private static readonly Texture2D GearedTexture = GD.Load<Texture2D>("res://Assets/Sprites/Platforms/geared-platform.png");
  private static readonly Texture2D SimpleTexture = GD.Load<Texture2D>("res://Assets/Sprites/Platforms/platform.png");

  #region Exports
  [Export]
  public string Group { get; set; } = "blue";

  [Export]
  public bool Geared { get; set; } = true;

  [Export]
  public float SplashDarkness { get; set; } = 0.78f;
  #endregion Exports

  private float _animationTimer = 10;
  private Vector2 _contactPosition = new Vector2(0, 0);

  #region Nodes
  [NodePath("NinePatchRect")]
  private NinePatchRect _ninePatchRectNode = default!;
  [NodePath("Area2D")]
  private Area2D _areaNode = default!;
  #endregion Nodes

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _setPlatformTexture();
    _ninePatchRectNode.ScaleTexture(Scale);
    if (!string.IsNullOrEmpty(Group)) {
      Color color = SkinManager.Instance.CurrentSkin.GetColor(
        GameSkin.ColorGroupToSkinColor(Group),
        SkinColorIntensity.Basic
      );
      _ninePatchRectNode.Modulate = color;
      _areaNode.AddToGroup(Group);
    }
  }

  public override void _EnterTree() {
    base._EnterTree();
    _connectSignals();
  }

  public override void _ExitTree() {
    base._ExitTree();
    _disconnectSignals();
  }

  public void OnPlayerLanded(Node area, Vector2 position) {
    if (area == _areaNode) {
      _animationTimer = 0;
      _contactPosition = position;
    }
  }

  public override void _Process(double delta) {
    if (Engine.IsEditorHint())
      return;

    _animationTimer += (float)delta;

    if (_ninePatchRectNode.Material is ShaderMaterial shaderMaterial) {
      // FIXME: Migration 4.0 - Viewport
      // Vector2 resolution = GetViewport().GetSize2dOverride();
      Vector2 resolution = new Vector2(1, 1);

      Camera2D cam = Global.Instance().Camera;

      if (cam != null) {
        Vector2 camPos = cam.GetScreenCenterPosition();
        Vector2 currentPos = new Vector2(
            _contactPosition.X + (resolution.X / 2) - camPos.X,
            _contactPosition.Y + (resolution.Y / 2) - camPos.Y);
        Vector2 pos = new Vector2(currentPos.X / resolution.X, currentPos.Y / resolution.Y);
        Vector2 positionInShaderCoords = new Vector2(pos.X, 1 - pos.Y);

        shaderMaterial.SetShaderParameter("u_contact_pos", positionInShaderCoords);
        shaderMaterial.SetShaderParameter("u_timer", _animationTimer);
        shaderMaterial.SetShaderParameter("u_aspect_ratio", resolution.Y / resolution.X);
        shaderMaterial.SetShaderParameter("darkness", SplashDarkness);
      }
    }
  }

  private void _setPlatformTexture() {
    if (Geared) {
      _ninePatchRectNode.SetTexture(GearedTexture);
    }
    else {
      _ninePatchRectNode.SetTexture(SimpleTexture);
    }
  }

  private void _connectSignals() {
    if (Engine.IsEditorHint())
      return;
    EventHandler.Instance.Events.PlayerLanded += OnPlayerLanded;
  }

  private void _disconnectSignals() {
    base._ExitTree();
    if (Engine.IsEditorHint())
      return;
    EventHandler.Instance.Events.PlayerLanded -= OnPlayerLanded;
  }
}
