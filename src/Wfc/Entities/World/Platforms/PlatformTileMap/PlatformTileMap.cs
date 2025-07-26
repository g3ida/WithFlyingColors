namespace Wfc.Entities.World.Platforms;

using Godot;
using Wfc.Core.Event;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class PlatformTileMap : TileMap {
  [Export]
  public float SplashDarkness { get; set; } = 0.78f;

  private float _animationTimer = 10;
  private Vector2 _contactPosition = new Vector2(0, 0);

  public override void _EnterTree() {
    _connectSignals();
  }

  public override void _ExitTree() {
    _disconnectSignals();
  }

  public void OnPlayerLanded(Node area, Vector2 position) {
    if (area == GetNode<Area2D>("Area2D")) {
      _animationTimer = 0;
      _contactPosition = position;
    }
  }

  public override void _Process(double delta) {
    if (Engine.IsEditorHint())
      return;

    _animationTimer += (float)delta;

    if (Material is ShaderMaterial shaderMaterial) {
      // FIXME: Migration 4.0 - Viewport - there is also SubViewport.Size2DOverride;
      //Vector2 resolution = GetViewport().GetSize2dOverride();
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

  private void _connectSignals() {
    EventHandler.Instance.Events.PlayerLanded += OnPlayerLanded;
  }

  private void _disconnectSignals() {
    EventHandler.Instance.Events.PlayerLanded -= OnPlayerLanded;
  }
}
