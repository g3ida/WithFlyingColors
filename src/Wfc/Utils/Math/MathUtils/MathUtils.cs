namespace Wfc.Utils;

using System.Numerics;
using Godot;

public static class MathUtils {
  public static bool IsPositive<T>(T value) where T : INumber<T> => value > T.Zero;

  public const float DEGREES_TO_RAD = Mathf.Pi / 180.0f;
  public const float RAD_TO_DEGREES = 180.0f / Mathf.Pi;
  public const float PI2 = Mathf.Pi * 0.5f;
  public const float PI3 = Mathf.Pi * 0.3333f;
  public const float PI4 = Mathf.Pi * 0.25f;
  public const float PI6 = Mathf.Pi * 0.166667f;
  public const float PI8 = Mathf.Pi * 0.125f;
  public const float PI10 = Mathf.Pi * 0.1f;
  public const float EPSILON = 0.001f;
  public const float EPSILON2 = 0.0001f;
}
