namespace Wfc.test.instrumented.BrickBreaker;

using System.Collections.Generic;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.BrickBreaker.Powerups;
using Wfc.Utils.Colors;

// The power-up pool is dealt without replacement, so a scene that forgot its exports
// comes up once every six pickups. Nothing about that is visible until it is picked up:
// the exports simply stay null and the pickup handler dereferences one of them.
//
// This walks the directory rather than a hardcoded list, so a power-up added later is
// covered the day it is written.
public class PowerUpSceneContractTests(Node testScene) : TestClass(testScene) {
  private const string POWERUPS_DIR = "res://src/Wfc/Entities/World/BrickBreaker/Powerups";

  // The scene every other one instances. It is the blank the others fill in, so it is the
  // one place these exports are legitimately unset.
  private const string BASE_POWERUP_SCENE = POWERUPS_DIR + "/PowerUp/PowerUp.tscn";

  [Test]
  public void EveryPowerUpSceneIsFullyAuthored() {
    var scenePaths = _powerUpScenePaths();
    scenePaths.ShouldNotBeEmpty("no power-up scenes were found - has the folder moved?");

    foreach (var path in scenePaths) {
      var powerUp = GD.Load<PackedScene>(path).Instantiate();
      if (powerUp is not PowerUp authored || path == BASE_POWERUP_SCENE) {
        powerUp.QueueFree();
        continue;
      }

      authored.Texture.ShouldNotBeNull($"{path} sets no Texture, so it renders as nothing");
      authored.OnHitScript.ShouldNotBeNull($"{path} sets no OnHitScript, so picking it up throws");
      ColorUtils.COLOR_GROUPS.ShouldContain(
        authored.ColorGroup,
        $"{path} is in group '{authored.ColorGroup}', which is not a color group"
      );
      authored.QueueFree();
    }
  }

  private static List<string> _powerUpScenePaths() {
    var paths = new List<string>();
    foreach (var directory in DirAccess.GetDirectoriesAt(POWERUPS_DIR)) {
      var subDir = $"{POWERUPS_DIR}/{directory}";
      foreach (var file in DirAccess.GetFilesAt(subDir)) {
        if (file.EndsWith(".tscn")) {
          paths.Add($"{subDir}/{file}");
        }
      }
    }
    return paths;
  }
}
