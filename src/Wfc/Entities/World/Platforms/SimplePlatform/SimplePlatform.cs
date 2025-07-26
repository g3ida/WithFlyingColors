namespace Wfc.Entities.World.Platforms;

using Chickensoft.AutoInject;
using Godot;
using Wfc.Core.Event;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;
using Wfc.Utils.Images;
using EventHandler = Wfc.Core.Event.EventHandler;

[Tool]
[ScenePath]
public partial class SimplePlatform : StaticBody2D {
  private static readonly Texture2D GearedTexture = (Texture2D)GD.Load("res://Assets/Sprites/Platforms/geared-platform.png");
  private static readonly Texture2D SimpleTexture = (Texture2D)GD.Load("res://Assets/Sprites/Platforms/platform.png");

  [Export]
  public string Group { get; set; } = "blue";

  [Export]
  public float splash_darkness { get; set; } = 0.78f;

  [Export]
  public bool geared { get; set; } = true;

  private float animationTimer = 10;
  private Vector2 contactPosition = new Vector2(0, 0);

  [NodePath("NinePatchRect")]
  private NinePatchRect _ninePatchRectNode = default!;
  [NodePath("Area2D")]
  private Area2D _areaNode = default!;
  [NodePath("CollisionShape")]
  private CollisionShape2D _collisionShape = default!;
  [NodePath("Area2D/ColorAreaShape")]
  private CollisionShape2D _colorAreaShape = default!;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();

    _setPlatformTexture();
    _ninePatchRectNode.ScaleTexture(Scale);
    correctAreaSize();

    if (!string.IsNullOrEmpty(Group)) {
      Color color = SkinManager.Instance.CurrentSkin.GetColor(
        GameSkin.ColorGroupToSkinColor(Group),
        SkinColorIntensity.Basic
      );
      _ninePatchRectNode.Modulate = color;
      _areaNode.AddToGroup(Group);
    }
    else {
      foreach (var colorGroup in ColorUtils.COLOR_GROUPS) {
        _areaNode.AddToGroup(colorGroup);
      }
    }
  }

  public void correctAreaSize() {
    if (_collisionShape.Shape is RectangleShape2D rectangleShape) {
      var A = (_colorAreaShape.Shape as RectangleShape2D)?.Size ?? Vector2.Zero;
      var B = rectangleShape.Size;
      var scaleCoefficient = (A + (B - A) / Scale) / B;
      rectangleShape.Size *= scaleCoefficient;
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

  private void _setPlatformTexture() {
    if (geared) {
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
    if (Engine.IsEditorHint())
      return;
    EventHandler.Instance.Events.PlayerLanded -= OnPlayerLanded;
  }
}
