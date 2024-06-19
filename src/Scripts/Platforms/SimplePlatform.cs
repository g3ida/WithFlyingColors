using Godot;
using System;

[Tool]
public partial class SimplePlatform : StaticBody2D {
  private static readonly Texture2D GearedTexture = (Texture2D)GD.Load("res://Assets/Sprites/Platforms/geared-platform.png");
  private static readonly Texture2D SimpleTexture = (Texture2D)GD.Load("res://Assets/Sprites/Platforms/platform.png");

  [Export]
  public string group { get; set; }

  [Export]
  public float splash_darkness { get; set; } = 0.78f;

  [Export]
  public bool geared { get; set; } = true;

  private float animationTimer = 10;
  private Vector2 contactPosition = new Vector2(0, 0);

  private NinePatchRect _ninePatchRectNode;
  private Area2D _areaNode;
  private CollisionShape2D _collisionShape;
  private CollisionShape2D _colorAreaShape;

  public override void _Ready() {
    base._Ready();
    _ninePatchRectNode = GetNode<NinePatchRect>("NinePatchRect");
    _areaNode = GetNode<Area2D>("Area2D");
    _collisionShape = GetNode<CollisionShape2D>("CollisionShape");
    _colorAreaShape = GetNode<CollisionShape2D>("Area2D/ColorAreaShape");

    SetPlatformTexture();
    NinePatchTextureUtils.ScaleTexture(_ninePatchRectNode, Scale);
    correctAreaSize();

    if (!string.IsNullOrEmpty(group)) {
      int colorIndex = ColorUtils.GetGroupColorIndex(group);
      _ninePatchRectNode.Modulate = ColorUtils.GetBasicColor(colorIndex);
      _areaNode.AddToGroup(group);
    }
    else {
      foreach (var colorGroup in Constants.COLOR_GROUPS) {
        _areaNode.AddToGroup(colorGroup);
      }
    }
  }

  public void correctAreaSize() {
    var A = (_colorAreaShape.Shape as RectangleShape2D).Size;
    var B = (_collisionShape.Shape as RectangleShape2D).Size;
    var scaleCoefficient = (A + (B - A) / Scale) / B;
    (_collisionShape.Shape as RectangleShape2D).Size *= scaleCoefficient;
  }

  public override void _EnterTree() {
    base._EnterTree();
    ConnectSignals();
  }

  public override void _ExitTree() {
    base._ExitTree();
    DisconnectSignals();
  }

  public void OnPlayerLanded(Node area, Vector2 position) {
    if (area == _areaNode) {
      animationTimer = 0;
      contactPosition = position;
    }
  }

  public override void _Process(double delta) {
    if (Engine.IsEditorHint())
      return;

    animationTimer += (float)delta;

    if (_ninePatchRectNode.Material is ShaderMaterial shaderMaterial) {
      Vector2 resolution = GetViewport().GetVisibleRect().Size; // must be 1920x1080
      Camera2D cam = Global.Instance().Camera;
      if (cam != null) {
        Vector2 camPos = cam.GetScreenCenterPosition();
        Vector2 currentPos = new Vector2(
            contactPosition.X + (resolution.X / 2) - camPos.X,
            contactPosition.Y + (resolution.Y / 2) - camPos.Y);
        Vector2 pos = new Vector2(currentPos.X / resolution.X, currentPos.Y / resolution.Y);
        // Position in shader coords is now Y instead of 1-Y as it was in Godot 3.x
        Vector2 positionInShaderCoords = new Vector2(pos.X, pos.Y);

        shaderMaterial.SetShaderParameter("u_contact_pos", positionInShaderCoords);
        shaderMaterial.SetShaderParameter("u_timer", animationTimer);
        shaderMaterial.SetShaderParameter("u_aspect_ratio", resolution.Y / resolution.X);
        shaderMaterial.SetShaderParameter("darkness", splash_darkness);
      }
    }
  }

  private void SetPlatformTexture() {
    if (geared) {
      NinePatchTextureUtils.SetTexture(_ninePatchRectNode, GearedTexture);
    }
    else {
      NinePatchTextureUtils.SetTexture(_ninePatchRectNode, SimpleTexture);
    }
  }

  private void ConnectSignals() {
    if (Engine.IsEditorHint())
      return;
    Event.Instance.Connect("player_landed", new Callable(this, nameof(OnPlayerLanded)));
  }

  private void DisconnectSignals() {
    if (Engine.IsEditorHint())
      return;
    Event.Instance.Disconnect("player_landed", new Callable(this, nameof(OnPlayerLanded)));
  }
}
