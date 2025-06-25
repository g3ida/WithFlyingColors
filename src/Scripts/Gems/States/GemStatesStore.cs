using Godot;
using Wfc.State;

public partial class GemStatesStore : BaseStatesStore<Gem, GemStatesEnum> {
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

  public override BaseState<Gem>? GetState(GemStatesEnum state) {
    if (state == GemStatesEnum.NOT_COLLECTED) {
      return NotCollected;
    }
    if (state == GemStatesEnum.COLLECTING) {
      return Collecting;
    }
    if (state == GemStatesEnum.COLLECTED) {
      return Collected;
    }
    return null;
  }

  public GemStatesEnum GetStateEnum(GemBaseState state) {
    if (state == NotCollected) {
      return GemStatesEnum.NOT_COLLECTED;
    }
    if (state == Collecting) {
      return GemStatesEnum.COLLECTING;
    }
    return GemStatesEnum.COLLECTED;
  }
}
