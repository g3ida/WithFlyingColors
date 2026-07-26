namespace Wfc.test.Helpers.Fakes;

using Wfc.Core.Localization;

// Hands back the key itself rather than a translation, so a test can assert which
// entry a screen asked for without standing up the translation table.
public class FakeLocalizationService : ILocalizationService {
  public string GetLocalizedString(TranslationKey key) => key.ToTranslationKeyString();
}
