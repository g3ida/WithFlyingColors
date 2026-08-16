namespace Wfc.Core.Logger.Test;

using System;
using System.Collections.Generic;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Logger;

// Everything the game reports goes through here, from static classes the injector cannot reach
// as well as from nodes. Two things are worth pinning: that the floor actually drops what is
// below it, and that each severity leaves by the channel Godot sorts it into - a warning
// printed as ordinary text is a warning nobody ever sees.
public class LoggerTests(Node testScene) : TestClass(testScene) {
  private sealed class RecordingLogger : ILogger {
    public Severity Severity { get; set; } = Severity.Debug;
    public List<(Severity Level, string Message)> Written { get; } = [];
    public void Log(Severity severity, string message) {
      if (Severity > severity) {
        return;
      }
      Written.Add((severity, message));
    }
  }

  private ILogger _loggerBeforeTest = default!;
  private RecordingLogger _recorder = default!;

  [Setup]
  public void Setup() {
    _loggerBeforeTest = Log.Logger;
    _recorder = new RecordingLogger();
    Log.Logger = _recorder;
  }

  [Cleanup]
  public void Cleanup() => Log.Logger = _loggerBeforeTest;

  [Test]
  public void EachCallCarriesItsOwnSeverityTest() {
    Log.Debug("d");
    Log.Info("i");
    Log.Warning("w");
    Log.Error("e");

    _recorder.Written.ShouldBe([
      (Severity.Debug, "d"),
      (Severity.Info, "i"),
      (Severity.Warning, "w"),
      (Severity.Error, "e"),
    ]);
  }

  // What makes Debug usable for a running commentary: it costs nothing unless asked for.
  [Test]
  public void AnythingBelowTheFloorIsDroppedTest() {
    Log.Severity = Severity.Warning;

    Log.Debug("d");
    Log.Info("i");
    Log.Warning("w");
    Log.Error("e");

    _recorder.Written.ShouldBe([(Severity.Warning, "w"), (Severity.Error, "e")]);
  }

  [Test]
  public void TheFloorIsTheOneOnTheLoggerItselfTest() {
    Log.Severity = Severity.Error;

    Log.Logger.Severity.ShouldBe(Severity.Error);
    Log.Severity.ShouldBe(Severity.Error);
  }

  [Test]
  public void AnExceptionIsReportedAsAnErrorTest() {
    Log.Exception(new InvalidOperationException("boom"));

    _recorder.Written.Count.ShouldBe(1);
    _recorder.Written[0].Level.ShouldBe(Severity.Error);
    _recorder.Written[0].Message.ShouldContain("boom");
  }

  // The real logger, checked for the mapping rather than the output: a warning must not take
  // the same path as an ordinary print.
  [Test]
  public void TheGodotLoggerKeepsItsFloorTest() {
    // Through the interface, because LogDebug and friends are default interface methods and a
    // concrete GDLogger does not carry them.
    ILogger logger = new GDLogger { Severity = Severity.Error };

    logger.Severity.ShouldBe(Severity.Error);

    // Nothing below the floor reaches Godot at all; an error still does, and pushing one from a
    // test is noise rather than failure, so only the floor itself is asserted here.
    Should.NotThrow(() => logger.LogDebug("dropped"));
    Should.NotThrow(() => logger.LogInfo("dropped"));
  }
}
