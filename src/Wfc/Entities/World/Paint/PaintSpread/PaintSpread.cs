namespace Wfc.Entities.World.Paint;

using System.Collections.Generic;
using Godot;
using Wfc.Utils;
using Wfc.Utils.Layers;

// Laying a width of paint onto a level. Anything that throws paint has the same problem: the paint
// is a width, the thing it lands on is whatever width it happens to be, and paint does not hold
// itself up past the end of what it is lying on. Left alone, a narrow platform wears a coat wider
// than itself with the ends of it hanging in the air, which reads as floor the cube can stand on.
//
// So what lands on the surface is cut to it, and what was thrown past either end falls and coats
// whatever is under there - which is what paint poured over the end of a shelf does.
internal static class PaintSpread {
  // How finely the surface is felt along for its ends, and the least paint worth making a splat of.
  // Anything narrower reads as a smear rather than a run of coloured floor, and it would claim a
  // strip of colour too thin for the player to see they had to match.
  private const float EDGE_STEP = 4f;
  private const float MIN_SPLASH = 28f;

  // How far above and below the surface the feeler looks, so that a run is followed along its top
  // rather than lost at the first pixel of slope.
  private const float PROBE_RISE = 4f;
  private const float PROBE_DROP = 14f;

  // How far below the lip paint thrown off the end will look for something to land on. Past this it
  // is falling into the room rather than onto the next shelf down.
  private const float OVERFLOW_DROP = 900f;

  // Lays the paint and reports every splat it made. The caller keeps them, because what happens to
  // them afterwards - whether they are a puzzle piece that stays or paint that dries up and goes -
  // is the caller's business and not the level's.
  public static void Lay(
    Node2D thrower, Vector2 where, Node? surface, string group, float width, float life,
    List<PaintSplat> laid) {
    var half = width / 2f;
    var from = where.X - half;
    var to = where.X + half;
    var space = thrower.GetWorld2D().DirectSpaceState;
    var query = _query();

    var left = where.X - _runsTo(space, query, where, surface, -1f, half);
    var right = where.X + _runsTo(space, query, where, surface, 1f, half);

    // An end the surface cut off runs right up to the edge and stops there; an end the paint simply
    // did not reach thins away as spilt paint does.
    var ends = new Vector2(left > from ? 0f : 1f, right < to ? 0f : 1f);
    _lay(thrower, surface, Mathf.Max(from, left), Mathf.Min(to, right), where.Y, group, life, ends, laid);
    _spillOver(thrower, space, query, from, Mathf.Min(to, left), where.Y, group, life, laid);
    _spillOver(thrower, space, query, Mathf.Max(from, right), to, where.Y, group, life, laid);
  }

  // How far what was hit actually runs, either side of the point the paint struck it. Felt along
  // rather than read off the platform, because what caught the paint is whatever the physics server
  // says it is and need not be something the level knows how to measure.
  private static float _runsTo(
    PhysicsDirectSpaceState2D space, PhysicsRayQueryParameters2D query,
    Vector2 point, Node? surface, float direction, float reach) {
    var run = 0f;
    while (run + EDGE_STEP <= reach) {
      var at = run + EDGE_STEP;
      query.From = new Vector2(point.X + (direction * at), point.Y - PROBE_RISE);
      query.To = query.From + new Vector2(0f, PROBE_RISE + PROBE_DROP);
      using var hit = space.IntersectRay(query);
      if (hit.Count == 0 || hit["collider"].As<Node>() != surface) {
        break;
      }
      run = at;
    }
    return run;
  }

  private static void _spillOver(
    Node2D thrower, PhysicsDirectSpaceState2D space, PhysicsRayQueryParameters2D query,
    float from, float to, float lip, string group, float life, List<PaintSplat> laid) {
    if (to - from < MIN_SPLASH) {
      return;
    }
    query.From = new Vector2((from + to) / 2f, lip);
    query.To = query.From + new Vector2(0f, OVERFLOW_DROP);
    using var hit = space.IntersectRay(query);
    if (hit.Count == 0) {
      return;
    }
    // What fell is a splash of its own where it landed, so both its ends are where the paint ran
    // out rather than where anything cut it.
    _lay(thrower, hit["collider"].As<Node>(), from, to, hit["position"].AsVector2().Y, group, life,
      Vector2.One, laid);
  }

  private static void _lay(
    Node2D thrower, Node? surface, float from, float to, float top,
    string group, float life, Vector2 ends, List<PaintSplat> laid) {
    var width = to - from;
    if (width < MIN_SPLASH) {
      return;
    }
    var host = surface as Node2D ?? thrower.GetParent() as Node2D;
    if (host is null) {
      return;
    }

    var splat = SceneHelpers.InstantiateNode<PaintSplat>();
    splat.Setup(group, width, dried: false, life: life, ends: ends);
    // Both before it is in the tree: physics interpolation is on for the whole project, and a node
    // given its transform after it is added draws its first frames sweeping in from its parent's
    // origin.
    splat.Position = host.ToLocal(new Vector2((from + to) / 2f, top));
    // The paint is a size in pixels wherever it lands, and some of the older platforms are sized by
    // scaling them.
    var scale = host.GlobalScale;
    splat.Scale = new Vector2(
      Mathf.IsZeroApprox(scale.X) ? 1f : 1f / scale.X,
      Mathf.IsZeroApprox(scale.Y) ? 1f : 1f / scale.Y
    );

    host.AddChild(splat);
    laid.Add(splat);
  }

  private static PhysicsRayQueryParameters2D _query() => new() {
    CollisionMask = PhysicsLayers.Platform.Mask,
    CollideWithAreas = false,
    CollideWithBodies = true,
  };
}
