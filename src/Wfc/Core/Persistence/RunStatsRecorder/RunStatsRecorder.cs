namespace Wfc.Core.Persistence;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using EventHandler = Wfc.Core.Event.EventHandler;

// Turns what the player does into the run counters the hub's stats board reads.
//
// It listens rather than being called from the player states, because the same cube is
// driven from a dozen states and none of them should have to know a save exists. Lives
// beside the provider for the whole session, so a count is never missed between screens.
[Meta(typeof(IAutoNode))]
public partial class RunStatsRecorder : Node {
  #region Dependencies
  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public ISaveManager SaveManager => this.DependOn<ISaveManager>();
  #endregion Dependencies

  private bool _isSubscribed;
  private bool _isResolved;

  public void OnResolved() => _isResolved = true;

  public override void _EnterTree() {
    base._EnterTree();
    if (_isSubscribed) {
      return;
    }
    EventHandler.Instance.Events.PlayerJumped += _onPlayerJumped;
    EventHandler.Instance.Events.PlayerDash += _onPlayerDash;
    EventHandler.Instance.Events.PlayerRotate += _onPlayerRotate;
    EventHandler.Instance.Events.PlayerDied += _onPlayerDied;
    _isSubscribed = true;
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (!_isSubscribed) {
      return;
    }
    EventHandler.Instance.Events.PlayerJumped -= _onPlayerJumped;
    EventHandler.Instance.Events.PlayerDash -= _onPlayerDash;
    EventHandler.Instance.Events.PlayerRotate -= _onPlayerRotate;
    EventHandler.Instance.Events.PlayerDied -= _onPlayerDied;
    _isSubscribed = false;
  }

  private void _onPlayerJumped() => _record(RunStat.Jumps);

  private void _onPlayerDash(Vector2 direction) => _record(RunStat.Dashes);

  // The rotation states carry their direction as the sign they turn the cube by.
  private void _onPlayerRotate(int direction) =>
    _record(direction < 0 ? RunStat.RotationsLeft : RunStat.RotationsRight);

  private void _onPlayerDied() => _record(RunStat.Deaths);

  // Subscribed from _EnterTree, which runs before the dependency is up. Nothing the player
  // does can reach this before then, but a count taken early would throw out of a signal
  // callback rather than simply being lost.
  private void _record(RunStat stat) {
    if (_isResolved) {
      SaveManager.RecordRunStat(stat);
    }
  }
}
