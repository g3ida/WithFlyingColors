namespace Wfc.Core.Logger;

using System;

public interface ILogger {
  public Severity Severity { get; set; }
  void Log(Severity severity, string message);
  void LogDebug(string message) => Log(Severity.Debug, message);
  void LogInfo(string message) => Log(Severity.Info, message);
  void LogWarning(string message) => Log(Severity.Warning, message);
  void LogError(string message) => Log(Severity.Error, message);
  void LogException(Exception exception) => LogError(exception.ToString());
}
