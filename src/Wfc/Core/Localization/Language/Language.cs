namespace Wfc.Core.Localization;

using System.Collections.Generic;

public enum Language {
  English,
  French,
  German,
  Spanish,
  Italian,
  Portuguese,
  Dutch
}

public static class LanguageExtensions {
  public static string GetLanguageCode(this Language language) => language switch {
    Language.English => "en",
    Language.French => "fr",
    Language.German => "de",
    Language.Spanish => "es",
    Language.Italian => "it",
    Language.Portuguese => "pt",
    Language.Dutch => "nl",
    _ => "en"
  };

  public static Language LangaugeCodeToLanguage(this string code) => code switch {
    "en" => Language.English,
    "fr" => Language.French,
    "de" => Language.German,
    "es" => Language.Spanish,
    "it" => Language.Italian,
    "pt" => Language.Portuguese,
    "nl" => Language.Dutch,
    _ => Language.English
  };

  public static string GetLanguageNativeName(this Language language) => language switch {
    Language.English => "English",
    Language.French => "Français",
    Language.German => "Deutsch",
    Language.Spanish => "Español",
    Language.Italian => "Italiano",
    Language.Portuguese => "Português",
    Language.Dutch => "Nederlands",
    _ => "English"
  };

  public static List<Language> Languages => new List<Language> {
    Language.English,
    Language.French,
    Language.German,
    Language.Spanish,
    Language.Italian,
    Language.Portuguese,
    Language.Dutch
  };
}
