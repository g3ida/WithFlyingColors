namespace Wfc.Core.Logger;

using System;

// The one way the game says anything to the console.
//
// A static way in rather than an injected ILogger everywhere, because most of what has
// something to report is not a node and AutoInject cannot reach it: the save files, the
// settings, the path attributes, the serializers. A node that would rather take ILogger as a
// dependency still can - DependenciesProvider hands out this same instance, so the severity
// floor means the same thing whichever way a caller arrived.
public static class Log {
  // Swappable so a test can read back what was reported instead of watching the console, and
  // so the floor below can be moved without any call site knowing.
  public static ILogger Logger { get; set; } = new GDLogger();

  // What is worth hearing about. Anything below this is dropped, which is what makes Debug
  // usable for a running commentary that is off unless somebody asks for it.
  public static Severity Severity {
    get => Logger.Severity;
    set => Logger.Severity = value;
  }

  public static void Debug(string message) => Logger.LogDebug(message);

  public static void Info(string message) => Logger.LogInfo(message);

  public static void Warning(string message) => Logger.LogWarning(message);

  public static void Error(string message) => Logger.LogError(message);

  public static void Exception(Exception exception) => Logger.LogException(exception);
}
