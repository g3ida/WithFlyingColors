namespace Wfc.Core.Localization;

using Godot;

public class LocalizationService : ILocalizationService {
  public string GetLocalizedString(TranslationKey key) {
    var keyName = key.ToTranslationKeyString();
    return TranslationServer.Translate(keyName);
  }
}
