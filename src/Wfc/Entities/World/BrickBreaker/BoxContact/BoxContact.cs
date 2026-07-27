namespace Wfc.Entities.World.BrickBreaker;

using Godot;
using Wfc.Utils;

// Where a circle meets a box, in the box's own frame and with the box centered on the origin.
//
// Worked out arithmetically rather than asked of the physics server, which has nothing to say
// about a pair that deliberately do not collide, and nothing to sweep once one of them has
// already climbed inside the other.
public readonly record struct BoxContact(Vector2 Normal, Vector2 Point, float Depth) {
  // Normal points out of the box towards the circle, Point sits on the box's surface, and Depth is
  // how far along the normal the circle has to travel to come to rest against it. `approach` is how
  // the circle is moving relative to the box, which is the only thing left to go on once the circle
  // is far enough in that its position no longer says which side it arrived from.
  public static bool Find(Vector2 center, float radius, Vector2 halfExtents, Vector2 approach, out BoxContact contact) {
    var surface = new Vector2(
      Mathf.Clamp(center.X, -halfExtents.X, halfExtents.X),
      Mathf.Clamp(center.Y, -halfExtents.Y, halfExtents.Y)
    );
    var outward = center - surface;
    var gap = outward.LengthSquared();

    if (gap > radius * radius) {
      contact = default;
      return false;
    }

    if (gap > MathUtils.EPSILON2) {
      var distance = Mathf.Sqrt(gap);
      contact = new BoxContact(outward / distance, surface, radius - distance);
      return true;
    }

    contact = _fromInside(center, radius, halfExtents, approach);
    return true;
  }

  // The center is inside the box, so the face it came in through has to be named rather than
  // measured. Taking the shallowest way out instead sent a ball that a dash had driven deep in from
  // the side back out through the top or bottom face, which then judged its color.
  private static BoxContact _fromInside(Vector2 center, float radius, Vector2 halfExtents, Vector2 approach) {
    var normal = approach.LengthSquared() > MathUtils.EPSILON
      ? _facing(-approach)
      : _shallowestWayOut(center, halfExtents);

    var reach = (Mathf.Abs(normal.X) * halfExtents.X) + (Mathf.Abs(normal.Y) * halfExtents.Y);
    var along = center.Dot(normal);
    return new BoxContact(normal, center + (normal * (reach - along)), reach - along + radius);
  }

  // The axis-aligned face `direction` points at.
  private static Vector2 _facing(Vector2 direction) =>
    Mathf.Abs(direction.X) >= Mathf.Abs(direction.Y)
      ? new Vector2(direction.X < 0.0f ? -1.0f : 1.0f, 0.0f)
      : new Vector2(0.0f, direction.Y < 0.0f ? -1.0f : 1.0f);

  // Nothing is moving, so there is no side it came in from and the nearest way out will do.
  private static Vector2 _shallowestWayOut(Vector2 center, Vector2 halfExtents) =>
    halfExtents.X - Mathf.Abs(center.X) <= halfExtents.Y - Mathf.Abs(center.Y)
      ? new Vector2(center.X < 0.0f ? -1.0f : 1.0f, 0.0f)
      : new Vector2(0.0f, center.Y < 0.0f ? -1.0f : 1.0f);
}
