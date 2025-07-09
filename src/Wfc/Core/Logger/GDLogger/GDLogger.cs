namespace Wfc.Core.Logger;
using Godot;

public class GDLogger : ILogger {
  public Severity Severity { get; set; } = Severity.Info;

  public void Log(Severity severity, string message) {
    if (Severity > severity) {
      return;
    }

    switch (severity) {
      case Severity.Debug:
        GD.Print(message);
        break;
      case Severity.Info:
        GD.Print(message);
        break;
      case Severity.Warning:
        GD.Print(message);
        break;
      case Severity.Error:
        GD.PushError(message);
        break;
      default:
        GD.PushError("unknown severity: " + severity);
        break;
    }
  }
}
