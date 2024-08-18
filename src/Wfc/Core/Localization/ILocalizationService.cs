namespace Wfc.Core.Localization;

public interface ILocalizationService {
  abstract string GetLocalizedString(TranslationKey key);
}
