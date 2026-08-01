namespace Wfc.test.instrumented.Levels;

using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Camera;
using Wfc.Entities.World.Platforms;
using Wfc.Screens.Levels;
using Wfc.Utils.Colors;
using Wfc.Utils.Layers;

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
  // so an entry with no scene is a crash the moment the card is pressed. The hub is the
  // one deliberate absentee: its doors offer the levels, it is never on offer itself.
  [Test]
  public void EveryOfferedLevelIsAKnownLevelListedOnce() {
    var offered = LevelDispatcher.LEVELS.Select(level => level.Id).ToList();

    offered.Distinct().Count().ShouldBe(offered.Count, "a level is offered more than once");
    offered.ShouldNotContain(LevelId.Hub, "the hub is the level select, not a level on offer");
    foreach (var levelId in Enum.GetValues<LevelId>()) {
      if (levelId == LevelId.Hub) {
        continue;
      }
      offered.ShouldContain(levelId, $"{levelId} exists but no card offers it");
    }
  }

  [Test]
  public void EveryHostedLevelInstantiates() {
    foreach (var levelId in _hostedLevelIds()) {
      var level = LevelDispatcher.InstantiateLevel(levelId);

      level.ShouldNotBeNull($"level {levelId} could not be instantiated");
      level.LevelId.ShouldBe(levelId);
      level.QueueFree();
    }
  }

  // A platform instance whose script override was deleted keeps its collision shape but
  // never joins a color group, and BoxFace kills the player on contact with any face.
  // The node type is the tell: without the script it is a bare AnimatableBody2D.
  [Test]
  public void EveryPlatformInEveryLevelKeepsItsScriptAndDeclaresOneColorGroup() {
    foreach (var levelId in _hostedLevelIds()) {
      var level = LevelDispatcher.InstantiateLevel(levelId);
      level.ShouldNotBeNull();

      foreach (var node in _descendantsOf(level)) {
        var group = node.SceneFilePath switch {
          PLATFORM_SCENE_PATH => _groupOf<Platform>(node, levelId, platform => platform.Group),
          SIMPLE_PLATFORM_SCENE_PATH => _groupOf<SimplePlatform>(node, levelId, platform => platform.Group),
          _ => null,
        };

        // An empty group is the deliberate neutral platform - SimplePlatform joins
        // every color group and stays white. Only a non-empty group has to name a
        // real color, or it is a typo that kills on contact.
        if (!string.IsNullOrEmpty(group)) {
          ColorUtils.COLOR_GROUPS.ShouldContain(
            group,
            $"{levelId} / {node.Name} is in group '{group}', which is not a color group"
          );
        }
      }

      level.QueueFree();
    }
  }

  // A level can override the cube's collision mask per instance, and Level1's did: a mask
  // written for Godot 3, where a tetromino's own mask was enough to make the pair collide.
  // Godot 4 asks only the mover, so a cube whose mask has lost a body layer walks straight
  // into those bodies and is killed by whatever face ends up buried - which read as "the
  // tetrominos kill every color". The body layers the Player scene masks are the contract.
  [Test]
  public void EveryLevelsCubeKeepsCollidingWithEveryBodyLayerTheSceneMasks() {
    var bodyLayers = PhysicsLayers.Default.Mask | PhysicsLayers.Platform.Mask | PhysicsLayers.Tetris.Mask;
    foreach (var levelId in _hostedLevelIds()) {
      var level = LevelDispatcher.InstantiateLevel(levelId);
      level.ShouldNotBeNull();

      if (level.GetNodeOrNull("Player") is CharacterBody2D player) {
        (player.CollisionMask & bodyLayers).ShouldBe(
          bodyLayers,
          $"{levelId} overrides the cube's collision mask and drops a body layer with it"
        );
      }

      level.QueueFree();
    }
  }

  // A localizer that freezes the camera behind a viewport-sized drag box takes following
  // out of the picture, so its limits must decide the whole frame: full limits with both
  // axes collapsed to the view size, one legal framing. The pool shipped with vertical
  // slack in its band, and the framing rested wherever history parked the camera - a
  // squash death parked it somewhere else for good.
  [Test]
  public void EveryCameraFreezingLocalizerFullyDeterminesItsFraming() {
    foreach (var levelId in _hostedLevelIds()) {
      var level = LevelDispatcher.InstantiateLevel(levelId);
      level.ShouldNotBeNull();

      foreach (var node in _descendantsOf(level)) {
        if (node is CameraLocalizer localizer && localizer.FullViewportDragMargin) {
          localizer.PositionClippingMode.ShouldBe(
            CameraLimit.FullLimit,
            $"{levelId} / {node.Name} freezes the camera without full limits to decide the frame"
          );
          localizer.LimitXAxisToViewSize.ShouldBeTrue(
            $"{levelId} / {node.Name} freezes the camera but leaves the horizontal framing to history"
          );
          localizer.LimitYAxisToViewSize.ShouldBeTrue(
            $"{levelId} / {node.Name} freezes the camera but leaves the vertical framing to history"
          );
        }
      }

      level.QueueFree();
    }
  }

  // Every scene the orchestrator can host: the offered chain plus the hub it walks
  // back to between levels.
  private static IEnumerable<LevelId> _hostedLevelIds() =>
    LevelDispatcher.LEVELS.Select(info => info.Id).Append(LevelId.Hub);

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
