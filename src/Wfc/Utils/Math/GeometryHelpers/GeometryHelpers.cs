namespace Wfc.Utils;

using Godot;

public static class GeometryHelpers {
  public static bool Intersects(Vector2 circlePos, float circleRadius, Vector2 rectPos, Vector2 rect) {
    Vector2 circleDist = new Vector2 {
      X = Mathf.Abs(circlePos.X - rectPos.X),
      Y = Mathf.Abs(circlePos.Y - rectPos.Y)
    };

    if (circleDist.X > (rect.X / 2.0f + circleRadius))
      return false;
    if (circleDist.Y > (rect.Y / 2.0f + circleRadius))
      return false;

    if (circleDist.X <= (rect.X / 2.0f))
      return true;
    if (circleDist.Y <= (rect.Y / 2.0f))
      return true;

    float cornerDistanceSq = Mathf.Pow(circleDist.X - rect.X / 2.0f, 2) +
                             Mathf.Pow(circleDist.Y - rect.Y / 2.0f, 2);
    return (cornerDistanceSq <= (circleRadius * circleRadius));
  }
}
