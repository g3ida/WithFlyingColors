namespace Wfc.Entities.World.Platforms;

using Godot;

// The landing splash the platforms share: the shader expands a darkened circle from the
// contact point and brightens it back out at a fixed rate.
//
// The uniform names are cached here because writing one by string literal allocates a
// StringName per call, and a splash writes four of them every frame it plays.
public static class PlatformSplash {
  // The shader's own fade term: how much of the darkening it gives back per second.
  private const float FADE_RATE = 0.06f;

  public static readonly StringName ContactPosParam = "u_contact_pos";
  public static readonly StringName TimerParam = "u_timer";
  public static readonly StringName AspectRatioParam = "u_aspect_ratio";
  public static readonly StringName DarknessParam = "darkness";

  // How long a splash of this darkness stays visible: past this the darkened area has
  // brightened back to the plain texture and the shader draws nothing.
  public static float Duration(float darkness) => Mathf.Max(0f, (1f - darkness) / FADE_RATE);

  // The shader places the splash against SCREEN_UV, so the contact point has to arrive as a
  // fraction of the viewport rather than as the world position it was landed on at. Zoom is
  // part of that conversion: a room framed closer puts the same world point somewhere else
  // on screen.
  public static void Write(
    ShaderMaterial material,
    Camera2D camera,
    Vector2 contact,
    float timer,
    float darkness
  ) {
    var resolution = camera.GetViewportRect().Size;
    var onScreen = ((contact - camera.GetScreenCenterPosition()) * camera.Zoom) + (resolution / 2f);

    material.SetShaderParameter(ContactPosParam, onScreen / resolution);
    material.SetShaderParameter(TimerParam, timer);
    material.SetShaderParameter(AspectRatioParam, resolution.Y / resolution.X);
    material.SetShaderParameter(DarknessParam, darkness);
  }
}
