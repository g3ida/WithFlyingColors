namespace Wfc.Entities.World.Platforms;

using Godot;

// Whether a platform under power is driving a body into something, or just carrying it.
//
// Nothing in the engine answers this. A moving platform is kinematic and so is the cube, so
// neither gives way to the other: what actually happens is that the cube's own depenetration
// shoves it along in front of the platform, straight through the floor it was standing on, and
// waiting for the overlap to deepen waits for something that never comes. So the overlap is only
// ever read as "the platform has arrived", and the question that decides a crush is asked of the
// cube instead - whether it has anywhere left to be pushed.
public static class PlatformCrush {
  // How far the platform's edge has to be into the body before the contact counts as an arrival.
  // Barely at all: what a crush costs the player is one that lands a frame late, with the cube
  // already being carried off through the floor. A resting contact is inside the solver's own
  // margin, so this only has to clear that.
  public const float TOUCH_DEPTH = 1.0f;

  // ...and how much of the body the platform has to have come down on. A platform whose corner
  // clips a pixel of the cube's is not a platform the cube was standing under.
  public const float MIN_COVERAGE = 0.2f;

  // Added to the escape so that clearing the platform means clearing it, rather than coming to
  // rest against it for the same test to run again next frame.
  public const float ESCAPE_MARGIN = 2.0f;

  // How deep into the body the platform has reached, measured along its own direction of travel.
  public static float PinchDepth(Rect2 crusher, Rect2 body, Vector2 travel) {
    var overlap = crusher.Intersection(body);
    if (overlap.Size.X <= 0.0f || overlap.Size.Y <= 0.0f) {
      return 0.0f;
    }
    return _travelsVertically(travel) ? overlap.Size.Y : overlap.Size.X;
  }

  // Which side of the platform the body is on. A body the platform is driving at is ahead of it;
  // a body it is carrying downwards is behind, and that one has to be left alone however deeply
  // it happens to be dipped into the platform's back.
  public static bool IsAhead(Rect2 crusher, Rect2 body, Vector2 travel) =>
    (body.GetCenter() - crusher.GetCenter()).Dot(travel) > 0.0f;

  // How much of the body's width the platform has come down across, as a share of that width.
  public static float Coverage(Rect2 crusher, Rect2 body, Vector2 travel) {
    var overlap = crusher.Intersection(body);
    if (overlap.Size.X <= 0.0f || overlap.Size.Y <= 0.0f) {
      return 0.0f;
    }
    return _travelsVertically(travel)
      ? overlap.Size.X / body.Size.X
      : overlap.Size.Y / body.Size.Y;
  }

  // The platform has arrived: it is into the body, it is squarely over the body, and the body is
  // the one in its way.
  public static bool HasArrivedInto(Rect2 crusher, Rect2 body, Vector2 travel) =>
    IsAhead(crusher, body, travel)
      && PinchDepth(crusher, body, travel) > TOUCH_DEPTH
      && Coverage(crusher, body, travel) > MIN_COVERAGE;

  // The move that would take the body clear of the platform. Whether it can be made is the whole
  // difference between being shoved along and being crushed, and only the body can answer that.
  public static Vector2 EscapeMotion(Rect2 crusher, Rect2 body, Vector2 travel) =>
    travel * (PinchDepth(crusher, body, travel) + ESCAPE_MARGIN);

  // Where the platform's leading edge stands, taken across the middle of the body: the plane the
  // two of them are sharing. Whoever reads it also reads which side of the body it lies on to know
  // where the crush is coming from, and it lies on the platform's own side because an arrival is
  // reported the moment the edge is in rather than once it is through.
  public static Vector2 ContactPoint(Rect2 crusher, Rect2 body, Vector2 travel) {
    var centre = body.GetCenter();
    if (_travelsVertically(travel)) {
      return new Vector2(centre.X, travel.Y > 0.0f ? crusher.End.Y : crusher.Position.Y);
    }
    return new Vector2(travel.X > 0.0f ? crusher.End.X : crusher.Position.X, centre.Y);
  }

  private static bool _travelsVertically(Vector2 travel) => Mathf.Abs(travel.Y) >= Mathf.Abs(travel.X);
}
