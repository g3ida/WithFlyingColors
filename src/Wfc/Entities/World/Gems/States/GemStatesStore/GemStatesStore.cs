namespace Wfc.Entities.World.Gems;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.State;

public partial class GemStatesStore : IStatesStore<Gem> {

  private readonly Dictionary<Type, IState<Gem>> _states;

  public GemStatesStore(Gem gem) {
    _states = new Dictionary<Type, IState<Gem>> {
            { typeof(GemNotCollectedState), new GemNotCollectedState(this, gem) },
            { typeof(GemCollectingState), new GemCollectingState(this) },
            { typeof(GemCollectedState), new GemCollectedState() }
        };
  }

  public TState? GetState<TState>() where TState : class, IState<Gem> {
    if (_states.TryGetValue(typeof(TState), out var state))
      return state as TState;
    return null;
  }
}
