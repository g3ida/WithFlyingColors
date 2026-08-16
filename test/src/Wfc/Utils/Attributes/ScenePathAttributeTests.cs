namespace Wfc.Utils.Attributes.Test;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Utils;
using Wfc.Utils.Attributes;

// Where a scene-backed type finds its scene. The attribute is handed the compile-time path of
// the file it is written in, and has to turn that into a res:// path - which means deciding
// where the project root is inside somebody's absolute path. It used to decide by looking for
// the repository's own name, so the game only worked out of a directory called
// WithFlyingColors.
public class ScenePathAttributeTests(Node testScene) : TestClass(testScene) {
  [Test]
  public void ItNamesTheSceneBesideTheSourceFileTest() {
    var attribute = new ScenePathAttribute("/home/dev/WithFlyingColors/src/Wfc/Entities/Ui/Thing/Thing.cs");

    attribute.Path.ShouldBe("res://src/Wfc/Entities/Ui/Thing/Thing.tscn");
  }

  // The regression this file exists for: nothing about the checkout directory's name is the
  // project's to depend on. This used to yield "res:///home/dev/MyGame/src/...", an absolute
  // path on the build machine that resolves to nothing anywhere else.
  [Test]
  public void ACheckoutDirectoryOfAnyNameStillResolvesTest() {
    var attribute = new ScenePathAttribute("/home/dev/MyGame/src/Wfc/Entities/Ui/Thing/Thing.cs");

    attribute.Path.ShouldBe("res://src/Wfc/Entities/Ui/Thing/Thing.tscn");
  }

  // What CI actually does: one directory of the repository's name inside another.
  [Test]
  public void ANestedCheckoutOfTheSameNameResolvesTest() {
    var attribute = new ScenePathAttribute(
      "/home/runner/work/WithFlyingColors/WithFlyingColors/src/Wfc/Entities/Ui/Thing/Thing.cs");

    attribute.Path.ShouldBe("res://src/Wfc/Entities/Ui/Thing/Thing.tscn");
  }

  // res:// only ever speaks in forward slashes, whatever the machine that compiled it used.
  [Test]
  public void AWindowsPathResolvesTest() {
    var attribute = new ScenePathAttribute(@"D:\build\Whatever\src\Wfc\Entities\Ui\Thing\Thing.cs");

    attribute.Path.ShouldBe("res://src/Wfc/Entities/Ui/Thing/Thing.tscn");
  }

  [Test]
  public void APathGivenOutrightIsTakenAsItStandsTest() {
    var attribute = new ScenePathAttribute("res://src/Wfc/Somewhere/Else.tscn");

    attribute.Path.ShouldBe("res://src/Wfc/Somewhere/Else.tscn");
  }

  // Every scene-backed type in the game, resolved against the scenes actually on disk. This is
  // what would have caught the original bug on any machine, without renaming anything.
  [Test]
  public void EverySceneBackedTypeFindsItsSceneTest() {
    var missing = new System.Collections.Generic.List<string>();
    foreach (var type in typeof(SceneHelpers).Assembly.GetTypes()) {
      var attribute = System.Reflection.CustomAttributeExtensions
        .GetCustomAttribute<ScenePathAttribute>(type);
      if (attribute is null) {
        continue;
      }
      if (!ResourceLoader.Exists(attribute.Path)) {
        missing.Add($"{type.Name} -> '{attribute.Path}'");
      }
    }

    missing.ShouldBeEmpty();
  }
}
