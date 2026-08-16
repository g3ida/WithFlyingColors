namespace Wfc.Core.Logger;

using Godot;

public class GDLogger : ILogger {
  public Severity Severity { get; set; } = Severity.Info;

  // Each severity goes to the channel Godot sorts it into, rather than everything below an
  // error going to the console. A warning printed as text is a warning nobody sees: the editor
  // collects PushWarning into its own panel and counts it, and a headless run marks the line
  // as a warning instead of leaving it in the middle of ordinary output.
  public void Log(Severity severity, string message) {
    if (Severity > severity) {
      return;
    }

    switch (severity) {
      case Severity.Debug:
      case Severity.Info:
        GD.Print(message);
        break;
      case Severity.Warning:
        GD.PushWarning(message);
        break;
      case Severity.Error:
        GD.PushError(message);
        break;
      default:
        GD.PushError($"unknown severity {severity}: {message}");
        break;
    }
  }
}
