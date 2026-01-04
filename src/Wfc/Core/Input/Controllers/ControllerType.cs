namespace Wfc.Core.Input.Controllers;

using System.Collections.Generic;
using Wfc.Core.Localization;

public enum ControllerType {
    Keyboard = 0,
    Gamepad = 1,
}

public static class ControllerTypeExtensions {

    public static string GetLocalizedName(this ControllerType controllerType, ILocalizationService localizationService) => controllerType switch {
        ControllerType.Keyboard => localizationService.GetLocalizedString(TranslationKey.controller_type_keyboard),
        ControllerType.Gamepad => localizationService.GetLocalizedString(TranslationKey.controller_type_gamepad),
        _ => "Unknown",
    };

    public static List<ControllerType> ControllerTypes => new List<ControllerType> {
    ControllerType.Keyboard,
    ControllerType.Gamepad,
  };
}
