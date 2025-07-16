namespace Wfc.Core.Audio;

using System.Collections.Generic;

public record SfxInfo(string Path, float Volume = 0, float? PitchScale = null, string Bus = "sfx");


public static class GameSfx {

  private const string BASE_PATH = "res://Assets/sfx/";

  public static readonly Dictionary<string, SfxInfo> Data = new() {
    ["brick"] = new SfxInfo(BASE_PATH + "brick.ogg", -4),
    ["bricksSlide"] = new SfxInfo(BASE_PATH + "bricks_slide.ogg"),
    ["gemCollect"] = new SfxInfo(BASE_PATH + "gem.ogg", -15),
    ["GemEngine"] = new SfxInfo(BASE_PATH + "gems/gem-engine.ogg", 8),
    ["GemPut"] = new SfxInfo(BASE_PATH + "gems/temple_put_gem.ogg", -5),
    ["jump"] = new SfxInfo(BASE_PATH + "jumping.ogg", -5),
    ["land"] = new SfxInfo(BASE_PATH + "stand.ogg", -8),
    ["menuFocus"] = new SfxInfo(BASE_PATH + "click2.ogg"),
    ["menuMove"] = new SfxInfo(BASE_PATH + "menu_move.ogg"),
    ["menuSelect"] = new SfxInfo(BASE_PATH + "menu_select.ogg"),
    ["menuValueChange"] = new SfxInfo(BASE_PATH + "click.ogg"),
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
    ["rotateLeft"] = new SfxInfo(BASE_PATH + "rotate-box.ogg", -20, 0.9f),
    ["rotateRight"] = new SfxInfo(BASE_PATH + "rotate-box.ogg", -20),
    ["shine"] = new SfxInfo(BASE_PATH + "shine.ogg", -5),
    ["success"] = new SfxInfo(BASE_PATH + "success.ogg", -1),
    ["tetrisLine"] = new SfxInfo(BASE_PATH + "tetris_line.ogg", -7),
    ["winMiniGame"] = new SfxInfo(BASE_PATH + "win_mini_game.ogg", 1),
    ["wrongAnswer"] = new SfxInfo(BASE_PATH + "piano/wrong-answer.ogg", 10)
  };
}
