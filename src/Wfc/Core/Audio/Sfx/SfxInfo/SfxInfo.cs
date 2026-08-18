namespace Wfc.Core.Audio;

using System.Collections.Generic;

public record SfxInfo(string Path, float Volume = 0, float? PitchScale = null, string Bus = "sfx");


public static class GameSfx {

  private const string BASE_PATH = "res://Assets/sfx/";

  // Every player built from this table fires once and is never stopped, so a path here has to
  // name a sound imported as a one-shot. The looping ambiences a scene owns - the gem shine, the
  // platform hum - keep their own files, and the cues that want their character use a burst cut
  // from them.

  public static readonly Dictionary<string, SfxInfo> Data = new() {
    ["brick"] = new SfxInfo(BASE_PATH + "brick.ogg", -4),
    ["bricksSlide"] = new SfxInfo(BASE_PATH + "bricks_slide_burst.ogg"),
    ["bucketFall"] = new SfxInfo(BASE_PATH + "bucket-fall.ogg", -4),
    ["bucketPush"] = new SfxInfo(BASE_PATH + "bucket-push.ogg", -8),
    ["dash"] = new SfxInfo(BASE_PATH + "dash.ogg", -6),
    ["paintPour"] = new SfxInfo(BASE_PATH + "paint-pour.ogg", -6),
    ["paintSplash"] = new SfxInfo(BASE_PATH + "paint-splash.ogg", -6),
    ["shooting"] = new SfxInfo(BASE_PATH + "shooting.ogg", -6),
    ["gunCooldown"] = new SfxInfo(BASE_PATH + "gun-cooldown.ogg"),
    // Placeholders: the door ceremony wants sounds of its own, and until they exist it borrows
    // two that are close enough to read. Only the paths here need replacing.
    ["doorCometFormed"] = new SfxInfo(BASE_PATH + "shine_burst.ogg", -2),
    ["doorGemFill"] = new SfxInfo(BASE_PATH + "pickup.ogg", -6),
    ["gemCollect"] = new SfxInfo(BASE_PATH + "gem.ogg", -15),
    ["jump"] = new SfxInfo(BASE_PATH + "jumping.ogg", -5),
    ["land"] = new SfxInfo(BASE_PATH + "stand.ogg", -8),
    ["menuFocus"] = new SfxInfo(BASE_PATH + "click2.ogg"),
    ["menuSelect"] = new SfxInfo(BASE_PATH + "menu_select.ogg"),
    ["menuValueChange"] = new SfxInfo(BASE_PATH + "click.ogg"),
    ["notification"] = new SfxInfo(BASE_PATH + "notification.ogg", -6),
    ["pageFlip"] = new SfxInfo(BASE_PATH + "piano/page-flip.ogg", 5),
    ["piano_0"] = new SfxInfo(BASE_PATH + "piano/do.ogg", -3),
    ["piano_1"] = new SfxInfo(BASE_PATH + "piano/re.ogg", -3),
    ["piano_2"] = new SfxInfo(BASE_PATH + "piano/mi.ogg", -3),
    ["piano_3"] = new SfxInfo(BASE_PATH + "piano/fa.ogg", -3),
    ["piano_4"] = new SfxInfo(BASE_PATH + "piano/sol.ogg", -3),
    ["piano_5"] = new SfxInfo(BASE_PATH + "piano/la.ogg", -3),
    ["piano_6"] = new SfxInfo(BASE_PATH + "piano/si.ogg", -3),
    ["pickup"] = new SfxInfo(BASE_PATH + "pickup.ogg", -4),
    ["playerExplode"] = new SfxInfo(BASE_PATH + "die.ogg", -10),
    ["playerFalling"] = new SfxInfo(BASE_PATH + "falling.ogg", -10),
    ["playerSquashed"] = new SfxInfo(BASE_PATH + "squash.ogg", -2),
    ["rotateLeft"] = new SfxInfo(BASE_PATH + "rotate-box.ogg", -20, 0.9f),
    ["rotateRight"] = new SfxInfo(BASE_PATH + "rotate-box.ogg", -20),
    ["success"] = new SfxInfo(BASE_PATH + "success.ogg", -1),
    ["tetrisLine"] = new SfxInfo(BASE_PATH + "tetris_line.ogg", -7),
    ["winMiniGame"] = new SfxInfo(BASE_PATH + "win_mini_game.ogg", 1),
    ["wrongAnswer"] = new SfxInfo(BASE_PATH + "piano/wrong-answer.ogg", 10)
  };
}
