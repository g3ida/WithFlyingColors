namespace Wfc.Screens.Levels;

using System;
using Chickensoft.Sync.Primitives;
using Wfc.Entities.World.Player;

public class GameRepo : IGameRepo {
  // Reached both ways, as SettingsRepo is: the level provides it so its own nodes can take it
  // as a dependency, while the entities that cannot - the powerups, the enemies, a gem state
  // that is not a node at all - still have to find the cube.
  private static GameRepo? _instance;
  public static GameRepo Instance => _instance ??= new GameRepo();

  private readonly AutoValue<Player?> _player = new(null);
  public IAutoValue<Player?> Player => _player;

  private bool _disposed;

  public void SetPlayer(Player? player) => _player.Value = player;

  protected virtual void Dispose(bool disposing) {
    if (_disposed) {
      return;
    }
    if (disposing) {
      _player.Dispose();
    }
    _disposed = true;
  }

  public void Dispose() {
    Dispose(disposing: true);
    GC.SuppressFinalize(this);
  }
}
