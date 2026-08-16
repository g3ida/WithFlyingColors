namespace WithFlyingColors;

using Godot;
#if DEBUG
using System.Reflection;
using Chickensoft.GoDotTest;
using Wfc.Core.Persistence;
using Wfc.Core.Settings;
#endif

// This entry-point file is responsible for determining if we should run tests.
//
// If you want to edit your game's main entry-point, please change RunScene() method.

public partial class Main : Node2D {
#if DEBUG
  // Where a test run's settings live instead of the real file beside the project.
  public const string TEST_CONFIG_PATH = "user://test-settings.ini";

  // And where its save slots live instead of the player's own. Anything that writes or deletes
  // a slot goes through SavePaths, so redirecting the root is what keeps a suite that exercises
  // the real SaveManager from destroying a real game.
  public const string TEST_SLOTS_ROOT = "user://test-slots";

  public TestEnvironment Environment = default!;
#endif

  public override void _Ready() {
#if DEBUG
    // If this is a debug build, use GoDotTest to examine the
    // command line arguments and determine if we should run tests.
    Environment = TestEnvironment.From(OS.GetCmdlineArgs());
    if (Environment.ShouldRunTests) {
      CallDeferred(Main.MethodName.RunTests);
      return;
    }
#endif

    // If we don't need to run tests, we can just switch to the game scene.
    CallDeferred(Main.MethodName.RunScene);
  }

#if DEBUG
  private void RunTests() {
    // The whole suite shares one process and the settings path is a static, so this is
    // the only place that can promise no test ever writes the developer's real
    // settings.ini. A suite that saves used to reach it whenever a test restored the
    // path it found rather than the one it wanted.
    GameSettings.ConfigFilePath = TEST_CONFIG_PATH;
    SavePaths.Root = TEST_SLOTS_ROOT;
    _ = GoTest.RunTests(Assembly.GetExecutingAssembly(), this, Environment);
  }
#endif

  private void RunScene() =>
    GetTree().ChangeSceneToFile("res://src/Wfc/Base/RootNode/RootNode.tscn");
}
