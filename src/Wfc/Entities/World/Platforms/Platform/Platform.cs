namespace Wfc.Entities.World.Platforms;

using Chickensoft.Sync.Primitives;
using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Screens.Levels;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Images;

[Tool]
[Meta(typeof(IAutoNode))]
public partial class Platform : AnimatableBody2D {
  private AutoChannel.Binding? _landedBinding;

  public override void _Notification(int what) => this.Notify(what);
  [Dependency]
  public IGameLevel GameLevel => this.DependOn<IGameLevel>();
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
    // Nothing to feed the shader until something lands; OnPlayerLanded turns this back on.
    SetProcess(false);
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
      SetProcess(true);
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

      Camera2D cam = GameLevel.CameraNode;

      if (cam != null) {
        Vector2 camPos = cam.GetScreenCenterPosition();
        Vector2 currentPos = new Vector2(
            _contactPosition.X + (resolution.X / 2) - camPos.X,
            _contactPosition.Y + (resolution.Y / 2) - camPos.Y);
        Vector2 pos = new Vector2(currentPos.X / resolution.X, currentPos.Y / resolution.Y);
        Vector2 positionInShaderCoords = new Vector2(pos.X, 1 - pos.Y);

        shaderMaterial.SetShaderParameter(PlatformSplash.ContactPosParam, positionInShaderCoords);
        shaderMaterial.SetShaderParameter(PlatformSplash.TimerParam, _animationTimer);
        shaderMaterial.SetShaderParameter(PlatformSplash.AspectRatioParam, resolution.Y / resolution.X);
        shaderMaterial.SetShaderParameter(PlatformSplash.DarknessParam, SplashDarkness);
      }
    }
    if (_animationTimer > PlatformSplash.Duration(SplashDarkness)) {
      SetProcess(false);
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
    _landedBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.PlayerLandedOn m) => OnPlayerLanded(m.Area, m.Position));
  }

  private void _disconnectSignals() {
    if (Engine.IsEditorHint())
      return;
    _landedBinding?.Dispose();
    _landedBinding = null;
  }
}
