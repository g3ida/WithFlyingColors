using System;
using Godot;
using Wfc.Core.Event;
using Wfc.Skin;
using Wfc.Utils.Images;
using EventHandler = Wfc.Core.Event.EventHandler;

[Tool]
public partial class Platform : AnimatableBody2D {
  private static readonly Texture2D GearedTexture = (Texture2D)GD.Load("res://Assets/Sprites/Platforms/geared-platform.png");
  private static readonly Texture2D SimpleTexture = (Texture2D)GD.Load("res://Assets/Sprites/Platforms/platform.png");

  [Export]
  public string group { get; set; }

  [Export]
  public bool geared { get; set; } = true;

  [Export]
  public float Splash_darkness { get; set; } = 0.78f;

  private float animationTimer = 10;
  private Vector2 contactPosition = new Vector2(0, 0);

  private NinePatchRect ninePatchRectNode;
  private Area2D areaNode;

  public override void _Ready() {
    base._Ready();
    ninePatchRectNode = GetNode<NinePatchRect>("NinePatchRect");
    areaNode = GetNode<Area2D>("Area2D");

    SetPlatformTexture();
    ninePatchRectNode.ScaleTexture(Scale);

    if (!string.IsNullOrEmpty(group)) {
      Color color = SkinManager.Instance.CurrentSkin.GetColor(
        GameSkin.ColorGroupToSkinColor(group),
        SkinColorIntensity.Basic
      );
      ninePatchRectNode.Modulate = color;
      areaNode.AddToGroup(group);
    }
  }

  public override void _EnterTree() {
    base._EnterTree();
    ConnectSignals();
  }

  public override void _ExitTree() {
    DisconnectSignals();
  }

  public void OnPlayerLanded(Node area, Vector2 position) {
    if (area == areaNode) {
      animationTimer = 0;
      contactPosition = position;
    }
  }

  public override void _Process(double delta) {
    if (Engine.IsEditorHint())
      return;

    animationTimer += (float)delta;

    if (ninePatchRectNode.Material is ShaderMaterial shaderMaterial) {
      // FIXME: Migration 4.0 - Viewport
      // Vector2 resolution = GetViewport().GetSize2dOverride();
      Vector2 resolution = new Vector2(1, 1);

      Camera2D cam = Global.Instance().Camera;

      if (cam != null) {
        Vector2 camPos = cam.GetScreenCenterPosition();
        Vector2 currentPos = new Vector2(
            contactPosition.X + (resolution.X / 2) - camPos.X,
            contactPosition.Y + (resolution.Y / 2) - camPos.Y);
        Vector2 pos = new Vector2(currentPos.X / resolution.X, currentPos.Y / resolution.Y);
        Vector2 positionInShaderCoords = new Vector2(pos.X, 1 - pos.Y);

        shaderMaterial.SetShaderParameter("u_contact_pos", positionInShaderCoords);
        shaderMaterial.SetShaderParameter("u_timer", animationTimer);
        shaderMaterial.SetShaderParameter("u_aspect_ratio", resolution.Y / resolution.X);
        shaderMaterial.SetShaderParameter("darkness", Splash_darkness);
      }
    }
  }

  private void SetPlatformTexture() {
    if (geared) {
      ninePatchRectNode.SetTexture(GearedTexture);
    }
    else {
      ninePatchRectNode.SetTexture(SimpleTexture);
    }
  }

  private void ConnectSignals() {
    if (Engine.IsEditorHint())
      return;
    EventHandler.Instance.Events.PlayerLanded += OnPlayerLanded;
  }

  private void DisconnectSignals() {
    base._ExitTree();
    if (Engine.IsEditorHint())
      return;
    EventHandler.Instance.Events.PlayerLanded -= OnPlayerLanded;
  }
}
