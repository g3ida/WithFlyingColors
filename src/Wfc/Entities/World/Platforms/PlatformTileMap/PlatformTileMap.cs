namespace Wfc.Entities.World.Platforms;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Chickensoft.Sync.Primitives;
using Godot;
using Wfc.Core.Event;
using Wfc.Screens.Levels;

[Meta(typeof(IAutoNode))]
public partial class PlatformTileMap : TileMapLayer {
  private AutoChannel.Binding? _landedBinding;

  public override void _Notification(int what) => this.Notify(what);
  [Dependency]
  public IGameLevel GameLevel => this.DependOn<IGameLevel>();

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

  public override void _Ready() {
    base._Ready();
    // Nothing to feed the shader until something lands; OnPlayerLanded turns this back on.
    SetProcess(false);
  }

  public void OnPlayerLanded(Node area, Vector2 position) {
    if (area == GetNode<Area2D>("Area2D")) {
      _animationTimer = 0;
      _contactPosition = position;
      SetProcess(true);
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

  private void _connectSignals() {
    _landedBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.PlayerLandedOn m) => OnPlayerLanded(m.Area, m.Position));
  }

  private void _disconnectSignals() {
    _landedBinding?.Dispose();
    _landedBinding = null;
  }
}
