namespace Wfc.test;

using System.Threading.Tasks;
using Godot;
using Shouldly;
using Wfc.Utils;

public static class TestHelpers {
  public static void ShouldBeCloseTo(this float number, float value, float epsilon = MathUtils.EPSILON) {
    Mathf.Abs(value - number).ShouldBeLessThan(epsilon);
  }

  public static async Task SleepFor(this SceneTree tree, double secs) {
    var timer = tree.CreateTimer(secs);
    await tree.ToSignal(timer, Timer.SignalName.Timeout);
  }

  // Awaits a signal, but gives up instead of waiting forever. A bare ToSignal turns a
  // regression into a hung job rather than a failed one: the await never resumes, the
  // test never returns, and CI burns until GitHub's six-hour default kills it without
  // saying which test was stuck. Returns false on timeout so the caller can assert.
  //
  // The timeout is counted in process frames, the same clock the awaited work runs on, so
  // a slow CI runner stretches both together rather than only the deadline.
  public static async Task<bool> ExpectSignal(
    this SceneTree tree,
    GodotObject source,
    StringName signal,
    double timeoutSeconds = 5.0
  ) {
    var fired = _completion(tree.ToSignal(source, signal));
    var timedOut = _completion(tree.ToSignal(tree.CreateTimer(timeoutSeconds), SceneTreeTimer.SignalName.Timeout));

    return await Task.WhenAny(fired, timedOut) == fired;
  }

  // SignalAwaiter is awaitable but is not a Task, and WhenAny needs Tasks to race.
  private static async Task<Variant[]> _completion(SignalAwaiter awaiter) => await awaiter;
}
