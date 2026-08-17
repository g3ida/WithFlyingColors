namespace Wfc.Utils.Attributes.Test;

using System;
using System.IO;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Chickensoft.GodotTestDriver;
using Godot;
using LightMock.Generator;
using LightMoq;
using Shouldly;
using Wfc.Entities.World.Player;

public class LevelPathAttributeTests(Node testScene) : TestClass(testScene) {
  // The real LevelId.cs sits here. The paths below are only ever the compile-time location
  // of that file on whichever machine built it.
  private const string LEVEL_LIST_DIR = "src/Wfc/Screens/Levels/LevelList";
  private const string FakeCallerFilePath = @"C:\Projects\WithFlyingColors\src\Wfc\Screens\Levels\LevelList\LevelId.cs";

  private Fixture _fixture = default!;

  [SetupAll]
  public void Setup() {
    _fixture = new Fixture(TestScene.GetTree());
  }

  [CleanupAll]
  public void Cleanup() => _fixture.Cleanup();

  [Test]
  public void AbsolutePath_IsRecognized() {
    var attr = new LevelPathAttribute("res://src/Main.tscn", FakeCallerFilePath);
    attr.ResolvePath("Level1").ShouldBe("res://src/Main.tscn");
  }

  [Test]
  public void DirectoryPath_IsRecognized() {
    var attr = new LevelPathAttribute("res://src", FakeCallerFilePath);
    attr.ResolvePath("World1").ShouldBe("res://src/World1.tscn");
  }

  [Test]
  public void RelativeFile_IsCombinedWithCallerDir() {
    var attr = new LevelPathAttribute("tutorialX", FakeCallerFilePath);
    attr.ResolvePath("Tutorial").ShouldBe($"res://{LEVEL_LIST_DIR}/tutorialX.tscn");
  }

  [Test]
  public void EmptyPath_UsesEnumNameAndCallerDir() {
    var attr = new LevelPathAttribute("", FakeCallerFilePath);
    attr.ResolvePath("Level1").ShouldBe($"res://{LEVEL_LIST_DIR}/Level1.tscn");
  }

  // GitHub Actions checks the repository out into a directory of the same name, so the
  // project name appears twice in the caller path. Splitting on the first occurrence
  // used to leave "WithFlyingColors/" in front of the res:// path, and every level in
  // the game failed to load on CI while working on every developer's machine.
  [Test]
  public void CallerFilePath_WithRepeatedProjectName_UsesTheLastOne() {
    var attr = new LevelPathAttribute("", "/home/runner/work/WithFlyingColors/WithFlyingColors/src/Wfc/Screens/Levels/LevelList/LevelId.cs");
    attr.ResolvePath("Level1").ShouldBe("res://src/Wfc/Screens/Levels/LevelList/Level1.tscn");
  }

  // This used to assert the fallback it produced when the checkout was not called
  // WithFlyingColors - "res://Level1.tscn", a level that does not exist - which is the bug
  // rather than the behavior. Nothing about the directory the repository sits in is the
  // project's to depend on.
  [Test]
  public void CallerFilePath_InACheckoutOfAnyName_StillResolves() {
    var attr = new LevelPathAttribute("", @"C:\OtherProject\src\Wfc\Screens\Levels\LevelList\LevelId.cs");
    attr.ResolvePath("Level1").ShouldBe($"res://{LEVEL_LIST_DIR}/Level1.tscn");
  }
}
