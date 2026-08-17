namespace Wfc.Screens.Levels;

using System;
using Chickensoft.Sync.Primitives;
using Wfc.Entities.World.Player;

// The cube the level is being played with, which everything that can collide with it needs a
// way to recognise. Held as a value rather than a field on an autoload so that it is typed at
// the point of use and can be observed if anything ever needs to.
//
// A level replaces it as it opens, so a reader outside a level is reading the previous run's
// cube: every caller already checks IsInstanceValid before touching it, and still has to.
public interface IGameRepo : IDisposable {
  IAutoValue<Player?> Player { get; }

  void SetPlayer(Player? player);
}
