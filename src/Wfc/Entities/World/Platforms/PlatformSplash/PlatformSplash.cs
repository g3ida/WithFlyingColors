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
}
