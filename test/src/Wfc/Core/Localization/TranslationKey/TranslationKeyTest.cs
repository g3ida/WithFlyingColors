namespace Wfc.Core.Localization.Test;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using GodotTestDriver;
using LightMock.Generator;
using LightMoq;
using Shouldly;
using Wfc.Entities.World.Player;

public class LocalizationKeyTests(Node testScene) : TestClass(testScene) {
  private List<string> _csvLines = default!;


  [SetupAll]
  public void Setup() {
    _csvLines = new List<string>();
    using var file = Godot.FileAccess.Open("res://Assets/Locale/translations.csv", Godot.FileAccess.ModeFlags.Read);
    while (!file.EofReached()) {
      _csvLines.Add(file.GetLine());
    }
  }

  [Test]
  public void ToTranslationKeyString_ReturnsExpectedKeys() {
    TranslationKey.menu_button_play.ToTranslationKeyString().ShouldBe("menu.button.play");
    TranslationKey.menu_button_newGame.ToTranslationKeyString().ShouldBe("menu.button.new_game");
    TranslationKey.menu_button_selectedSlot.ToTranslationKeyString().ShouldBe("menu.button.selected_slot");
    TranslationKey.game_settings_screenResolution.ToTranslationKeyString().ShouldBe("game.settings.screen_resolution");
  }

  [Test]
  public void AllTranslationKeysExistInCsv() {
    var csvKeys = _csvLines
        .Skip(1)
        .Select(line => line.Split(',')[0].Trim())
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    foreach (TranslationKey key in Enum.GetValues<TranslationKey>()) {
      var trKey = key.ToTranslationKeyString();
      csvKeys.Contains(trKey).ShouldBeTrue($"Missing translation key in CSV: {trKey}");
    }
  }

  [Test]
  public void AllTranslationsArePresent() {
    var headers = _csvLines.First().Split(',');
    var dataLines = _csvLines.Skip(1);

    foreach (var line in dataLines) {
      // skip trailing empty lines
      if (line.Length <= 1) {
        continue;
      }
      var columns = line.Split(',');
      columns.Length.ShouldBe(headers.Length, $"Column count mismatch in line for key '{columns[0]}'");

      for (int i = 1; i < headers.Length; i++) {
        string.IsNullOrWhiteSpace(columns[i]).ShouldBeFalse($"Missing translation for key '{columns[0]}' in language '{headers[i]}'");
      }
    }
  }

}
