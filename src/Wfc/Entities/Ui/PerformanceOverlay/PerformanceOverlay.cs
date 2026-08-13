namespace Wfc.Entities.Ui;

using System.Globalization;
using Godot;
using Wfc.Core.Settings;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

// What the frame costs, over the game itself. It belongs to the game screen rather than the
// root: the numbers are about what a level costs to run, and a menu has nothing to say.
[ScenePath]
public partial class PerformanceOverlay : CanvasLayer {
  #region Constants
  // The counters are worth far less often than they change: read every frame, the digits
  // move too fast to be read at all.
  private const double REFRESH_INTERVAL = 0.25;
  private const double SECONDS_TO_MS = 1000.0;
  #endregion Constants

  #region Nodes
  [NodePath("Panel/Stats")]
  private Label _statsNode = default!;
  #endregion Nodes

  private double _sinceRefresh;
  private bool _isSubscribed;

  public override void _EnterTree() {
    base._EnterTree();
    if (!_isSubscribed) {
      EventHandler.Instance.Events.PerformanceOverlayToggled += _onPerformanceOverlayToggled;
      _isSubscribed = true;
    }
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (_isSubscribed) {
      EventHandler.Instance.Events.PerformanceOverlayToggled -= _onPerformanceOverlayToggled;
      _isSubscribed = false;
    }
    _setMeasuringRenderTime(false);
  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _setEnabled(GameSettings.PerformanceOverlay);
  }

  private void _onPerformanceOverlayToggled(bool enabled) => _setEnabled(enabled);

  // Timing the render asks the driver for a pair of timestamps every frame, so it is only
  // measured while somebody is reading it.
  private void _setEnabled(bool enabled) {
    Visible = enabled;
    SetProcess(enabled);
    _setMeasuringRenderTime(enabled);
    if (enabled) {
      _sinceRefresh = REFRESH_INTERVAL;
    }
  }

  private void _setMeasuringRenderTime(bool measuring) {
    if (GetViewport() is { } viewport) {
      RenderingServer.ViewportSetMeasureRenderTime(viewport.GetViewportRid(), measuring);
    }
  }

  public override void _Process(double delta) {
    _sinceRefresh += delta;
    if (_sinceRefresh < REFRESH_INTERVAL) {
      return;
    }
    _sinceRefresh = 0.0;
    _statsNode.Text = _buildStats();
  }

  private string _buildStats() {
    var fps = Performance.GetMonitor(Performance.Monitor.TimeFps);
    var frameMs = fps > 0.0 ? SECONDS_TO_MS / fps : 0.0;
    // The engine splits its own work into the idle step and the physics step; what the
    // frame cost on the processor is the two of them together.
    var cpuMs = (Performance.GetMonitor(Performance.Monitor.TimeProcess)
      + Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess)) * SECONDS_TO_MS;
    var gpuMs = RenderingServer.ViewportGetMeasuredRenderTimeGpu(GetViewport().GetViewportRid());

    return _row("FPS", _count(fps), "FRAME", _ms(frameMs))
      + "\n" + _row("CPU", _ms(cpuMs), "GPU", _ms(gpuMs));
  }

  // The font is monospaced, so padding the two pairs to a fixed width is what keeps the
  // labels and their numbers in columns as the digits come and go.
  private static string _row(string leftLabel, string leftValue, string rightLabel, string rightValue) =>
    $"{leftLabel,-6}{leftValue,9}   {rightLabel,-6}{rightValue,9}";

  private static string _ms(double milliseconds) =>
    milliseconds.ToString("0.00", CultureInfo.InvariantCulture) + " ms";

  private static string _count(double value) => value.ToString("0", CultureInfo.InvariantCulture);
}
