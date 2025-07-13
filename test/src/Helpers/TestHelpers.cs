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
    await tree.ToSignal(timer, "timeout");
  }
}
