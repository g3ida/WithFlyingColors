namespace Wfc.test.instrumented.Levels;

using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Platforms;
using Wfc.Screens.Levels;
using Wfc.Utils.Colors;

// Scene-vs-script contracts fail silently: a level card whose scene does not exist, or a
// platform whose script override was deleted, loads without a word and breaks in a
// playtest. These assert the contracts that only the editor was enforcing.
//
// The levels are instantiated but never added to the tree, so no _Ready runs and nothing
// needs a dependency provider — the assertions are about what the .tscn stores.
public class LevelSceneContractTests(Node testScene) : TestClass(testScene) {
  // Platform has no [ScenePath], so the path it is instanced from is spelled out here.
  private const string PLATFORM_SCENE_PATH = "res://src/Wfc/Entities/World/Platforms/Platform/Platform.tscn";
  private const string SIMPLE_PLATFORM_SCENE_PATH = "res://src/Wfc/Entities/World/Platforms/SimplePlatform/SimplePlatform.tscn";

  [Test]
  public void EveryLevelIdResolvesToASceneThatExists() {
    foreach (var levelId in Enum.GetValues<LevelId>()) {
      var path = levelId.GetLevelPath();
      path.ShouldNotBeNullOrEmpty($"{levelId} carries no usable [LevelPath]");
      ResourceLoader.Exists(path).ShouldBeTrue($"{levelId} points at '{path}', which does not exist");
    }
  }

  // LevelSelectMenu builds one card per entry and the orchestrator boots into the first,
  // so an entry with no scene is a crash the moment the card is pressed.
  [Test]
  public void EveryOfferedLevelIsAKnownLevelListedOnce() {
    var offered = LevelDispatcher.LEVELS.Select(level => level.Id).ToList();

    offered.Distinct().Count().ShouldBe(offered.Count, "a level is offered more than once");
    foreach (var levelId in Enum.GetValues<LevelId>()) {
      offered.ShouldContain(levelId, $"{levelId} exists but no card offers it");
    }
  }

  [Test]
  public void EveryOfferedLevelInstantiates() {
    foreach (var info in LevelDispatcher.LEVELS) {
      var level = LevelDispatcher.InstantiateLevel(info.Id);

      level.ShouldNotBeNull($"level {info.Id} could not be instantiated");
      level.LevelId.ShouldBe(info.Id);
      level.QueueFree();
    }
  }

  // A platform instance whose script override was deleted keeps its collision shape but
  // never joins a color group, and BoxFace kills the player on contact with any face.
  // The node type is the tell: without the script it is a bare AnimatableBody2D.
  [Test]
  public void EveryPlatformInEveryLevelKeepsItsScriptAndDeclaresOneColorGroup() {
    foreach (var info in LevelDispatcher.LEVELS) {
      var level = LevelDispatcher.InstantiateLevel(info.Id);
      level.ShouldNotBeNull();

      foreach (var node in _descendantsOf(level)) {
        var group = node.SceneFilePath switch {
          PLATFORM_SCENE_PATH => _groupOf<Platform>(node, info.Id, platform => platform.Group),
          SIMPLE_PLATFORM_SCENE_PATH => _groupOf<SimplePlatform>(node, info.Id, platform => platform.Group),
          _ => null,
        };

        if (group != null) {
          ColorUtils.COLOR_GROUPS.ShouldContain(
            group,
            $"{info.Id} / {node.Name} is in group '{group}', which is not a color group"
          );
        }
      }

      level.QueueFree();
    }
  }

  private static string _groupOf<T>(Node node, LevelId levelId, Func<T, string> group) where T : Node {
    node.ShouldBeAssignableTo<T>(
      $"{levelId} / {node.Name} is instanced from {typeof(T).Name}.tscn but has no {typeof(T).Name} script"
    );
    return group((T)node);
  }

  private static IEnumerable<Node> _descendantsOf(Node root) {
    foreach (var child in root.GetChildren()) {
      yield return child;
      foreach (var descendant in _descendantsOf(child)) {
        yield return descendant;
      }
    }
  }
}
