namespace Wfc.Entities.World.Gems;

using Godot;
using Wfc.State;

public partial class GemStatesStore : BaseStatesStore<Gem, GemState> {
  public readonly GemBaseState NotCollected;
  public readonly GemBaseState Collecting;
  public readonly GemBaseState Collected;

  public GemStatesStore() {
    NotCollected = new GemNotCollectedState();
    Collecting = new GemCollectingState();
    Collected = new GemCollectedState();
  }

  public void Init(Gem gem) {
    NotCollected.Init(gem);
    Collecting.Init(gem);
    Collected.Init(gem);
  }

  public override BaseState<Gem>? GetState(GemState state) {
    if (state == GemState.NotCollected) {
      return NotCollected;
    }
    if (state == GemState.Collecting) {
      return Collecting;
    }
    if (state == GemState.Collected) {
      return Collected;
    }
    return null;
  }

  public GemState GetStateEnum(GemBaseState state) {
    if (state == NotCollected) {
      return GemState.NotCollected;
    }
    if (state == Collecting) {
      return GemState.Collecting;
    }
    return GemState.Collected;
  }
}
