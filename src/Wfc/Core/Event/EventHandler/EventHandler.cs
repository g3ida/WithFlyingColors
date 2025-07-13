namespace Wfc.Core.Event;

using System;
using Godot;
using Wfc.Entities.World;
using Wfc.Screens.MenuManager;

public partial class EventHandler : Node, IEventHandler {
  public override void _EnterTree() {
    base._EnterTree();
    Instance = GetTree().Root.GetNode<EventHandler>("EventCS");
  }

  public override void _Ready() {
    base._Ready();
    SetProcess(false);
  }

  public static EventHandler Instance { get; private set; } = null!;

  [Signal]
  public delegate void PlayerLandedEventHandler(Node area, Vector2 position);

  [Signal]
  public delegate void PlayerDyingEventHandler(Node? area, Vector2 position, int entityType);

  [Signal]
  public delegate void PlayerDiedEventHandler();

  [Signal]
  public delegate void PlayerJumpedEventHandler();

  [Signal]
  public delegate void PlayerRotateEventHandler(int dir);

  [Signal]
  public delegate void PlayerLandEventHandler();

  [Signal]
  public delegate void PlayerExplodeEventHandler();

  [Signal]
  public delegate void PlayerFallEventHandler();

  [Signal]
  public delegate void PlayerDashEventHandler(Vector2 direction);

  [Signal]
  public delegate void PlayerSlipperingEventHandler();

  [Signal]
  public delegate void GemCollectedEventHandler(string color, Vector2 position, SpriteFrames frames);

  [Signal]
  public delegate void CheckpointReachedEventHandler(Node checkpointObject);

  [Signal]
  public delegate void CheckpointLoadedEventHandler();

  [Signal]
  public delegate void MenuBoxRotatedEventHandler();

  [Signal]
  public delegate void PauseMenuEnterEventHandler();

  [Signal]
  public delegate void PauseMenuExitEventHandler();

  [Signal]
  public delegate void MenuButtonPressedEventHandler(int menuButton);

  // Settings signals
  [Signal]
  public delegate void FullscreenToggledEventHandler(bool value);

  [Signal]
  public delegate void VsyncToggledEventHandler(bool value);

  [Signal]
  public delegate void ScreenSizeChangedEventHandler(Vector2 value);

  [Signal]
  public delegate void OnActionBoundEventHandler(string action, int key);

  [Signal]
  public delegate void TabChangedEventHandler();

  [Signal]
  public delegate void FocusChangedEventHandler();

  [Signal]
  public delegate void KeyboardActionBindingEventHandler();

  [Signal]
  public delegate void SfxVolumeChangedEventHandler(float volume);

  [Signal]
  public delegate void MusicVolumeChangedEventHandler(float volume);

  [Signal]
  public delegate void TetrisLinesRemovedEventHandler();

  [Signal]
  public delegate void BrickBrokenEventHandler(string color, Vector2 position);

  [Signal]
  public delegate void BouncingBallRemovedEventHandler(Node ball);

  [Signal]
  public delegate void PickedPowerUpEventHandler();

  [Signal]
  public delegate void BreakBreakerWinEventHandler();

  [Signal]
  public delegate void BrickBreakerStartEventHandler();

  [Signal]
  public delegate void PianoNotePressedEventHandler(string note);

  [Signal]
  public delegate void PianoNoteReleasedEventHandler(string note);

  [Signal]
  public delegate void PageFlippedEventHandler();

  [Signal]
  public delegate void WrongPianoNotePlayedEventHandler();

  [Signal]
  public delegate void PianoPuzzleWonEventHandler();

  [Signal]
  public delegate void CutSceneRequestStartEventHandler(string id);

  [Signal]
  public delegate void CutSceneRequestEndEventHandler(string id);

  [Signal]
  public delegate void GemTempleTriggeredEventHandler();

  [Signal]
  public delegate void GemEngineStartedEventHandler();

  [Signal]
  public delegate void LevelClearedEventHandler();

  [Signal]
  public delegate void GemPutInTempleEventHandler();

  public void Connect(EventType eventType, Callable callable) {
    Connect(eventType.ToString(), callable);
  }

  public bool Connect(EventType eventType, Node caller, Action action) {
    // "boxing" the action inside one other action to avoid having the same hashcode for different instances
    var callable = Callable.From(() => action());
    return Connect(eventType, caller, callable);
  }

  public bool Connect<T0>(EventType eventType, Node caller, Action<T0> action) {
    // "boxing" the action inside one other action to avoid having the same hashcode for different instances
    var callable = Callable.From((T0 v0) => action(v0));
    return Connect(eventType, caller, callable);
  }

  public bool Connect<T0, T1>(EventType eventType, Node caller, Action<T0, T1> action) {
    // "boxing" the action inside one other action to avoid having the same hashcode for different instances
    var callable = Callable.From((T0 v0, T1 v1) => action(v0, v1));
    return Connect(eventType, caller, callable);
  }

  public bool Connect<T0, T1, T2>(EventType eventType, Node caller, Action<T0, T1, T2> action) {
    // "boxing" the action inside one other action to avoid having the same hashcode for different instances
    var callable = Callable.From((T0 v0, T1 v1, T2 v2) => action(v0, v1, v2));
    return Connect(eventType, caller, callable);
  }

  public bool Connect<T0, T1, T2, T3>(EventType eventType, Node caller, Action<T0, T1, T2, T3> action) {
    // "boxing" the action inside one other action to avoid having the same hashcode for different instances
    var callable = Callable.From((T0 v0, T1 v1, T2 v2, T3 v3) => action(v0, v1, v2, v3));
    return Connect(eventType, caller, callable);
  }

  public bool Connect<T0, T1, T2, T3, T4>(EventType eventType, Node caller, Action<T0, T1, T2, T3, T4> action) {
    // "boxing" the action inside one other action to avoid having the same hashcode for different instances
    var callable = Callable.From((T0 v0, T1 v1, T2 v2, T3 v3, T4 v4) => action(v0, v1, v2, v3, v4));
    return Connect(eventType, caller, callable);
  }

  public bool Connect<T0, T1, T2, T3, T4, T5>(EventType eventType, Node caller, Action<T0, T1, T2, T3, T4, T5> action) {
    // "boxing" the action inside one other action to avoid having the same hashcode for different instances
    var callable = Callable.From((T0 v0, T1 v1, T2 v2, T3 v3, T4 v4, T5 v5) => action(v0, v1, v2, v3, v4, v5));
    return Connect(eventType, caller, callable);
  }

  private bool Connect(EventType eventType, Node caller, Callable callable) {
    var eventName = eventType.ToString();
    if (IsConnected(eventName, callable)) {
    }
    else {
      var error = Connect(eventName, callable);
      if (error == Error.Ok) {
        caller.TreeExiting += () => Disconnect(eventName, callable);
        return true;
      }
      else {
        GD.PushError("error: " + error);
      }
    }
    return false;
  }

  public void ConnectOneShot(EventType eventType, Callable callable) {
    Connect(eventType.ToString(), callable, flags: (uint)ConnectFlags.OneShot);
  }

  public void Disconnect(EventType eventType, Callable callable) {
    Disconnect(eventType.ToString(), callable);
  }

  public void Emit(EventType eventType) {
    EmitSignal(eventType.ToString());
  }

  public void Emit(EventType eventType, params Variant[] args)
    => EmitSignal(eventType.ToString(), args);

  public void EmitPlayerLanded(Node area, Vector2 position) => Instance.EmitSignal(nameof(PlayerLanded), area, position);
  public void EmitPlayerDying(Node area, Vector2 position, EntityType entityType) => Instance.EmitSignal(nameof(PlayerDying), area, position, (int)entityType);
  public void EmitPlayerDying(Vector2 position, EntityType entityType) => Instance.EmitSignal(nameof(PlayerDying), default(Variant), position, (int)entityType);
  public void EmitPlayerDied() => Instance.EmitSignal(nameof(PlayerDied));
  public void EmitPlayerSlippering() => Instance.EmitSignal(nameof(PlayerSlippering));
  public void EmitPlayerJumped() => Instance.EmitSignal(nameof(PlayerJumped));
  public void EmitPlayerRotate(int dir) => Instance.EmitSignal(nameof(PlayerRotate), dir);
  public void EmitPlayerLand() => Instance.EmitSignal(nameof(PlayerLand));
  public void EmitPlayerExplode() => Instance.EmitSignal(nameof(PlayerExplode));
  public void EmitPlayerFall() => Instance.EmitSignal(nameof(PlayerFall));
  public void EmitPlayerDash(Vector2 dir) => Instance.EmitSignal(nameof(PlayerDash), dir);
  public void EmitGemCollected(string color, Vector2 position, SpriteFrames frames) => Instance.EmitSignal(nameof(GemCollected), color, position, frames);
  public void EmitCheckpointReached(Node checkpoint) => Instance.EmitSignal(nameof(CheckpointReached), checkpoint);
  public void EmitCheckpointLoaded() => Instance.EmitSignal(nameof(CheckpointLoaded));

  // fixme [deprecated] in favor of EmitMenuActionPressed
  public void EmitMenuButtonPressed(MenuButtons menuButton) => Instance.EmitSignal(nameof(MenuButtonPressed), (int)menuButton);
  public void EmitMenuActionPressed(MenuAction menuAction) => Instance.EmitSignal(nameof(MenuButtonPressed), (int)menuAction);
  public void EmitMenuBoxRotated() => Instance.EmitSignal(nameof(MenuBoxRotated));
  public void EmitPauseMenuEnter() => Instance.EmitSignal(nameof(PauseMenuEnter));
  public void EmitPauseMenuExit() => Instance.EmitSignal(nameof(PauseMenuExit));
  public void EmitFullscreenToggled(bool fullscreen) => Instance.EmitSignal(nameof(FullscreenToggled), fullscreen);
  public void EmitVsyncToggled(bool vsync) => Instance.EmitSignal(nameof(VsyncToggled), vsync);
  public void EmitScreenSizeChanged(Vector2 size) => Instance.EmitSignal(nameof(ScreenSizeChanged), size);
  public void EmitSfxVolumeChanged(float volume) => Instance.EmitSignal(nameof(SfxVolumeChanged), volume);
  public void EmitMusicVolumeChanged(float volume) => Instance.EmitSignal(nameof(MusicVolumeChanged), volume);
  public void EmitOnActionBound(string action, int key) => Instance.EmitSignal(nameof(OnActionBound), action, key);
  public void EmitTabChanged() => Instance.EmitSignal(nameof(TabChanged));
  public void EmitFocusChanged() => Instance.EmitSignal(nameof(FocusChanged));
  public void EmitKeyboardActionBiding() => Instance.EmitSignal(nameof(KeyboardActionBinding));
  public void EmitTetrisLinesRemoved() => Instance.EmitSignal(nameof(TetrisLinesRemoved));
  public void EmitBrickBroken(string color, Vector2 position) => Instance.EmitSignal(nameof(BrickBroken), color, position);
  public void EmitBouncingBallRemoved(Node ball) => Instance.EmitSignal(nameof(BouncingBallRemoved), ball);
  public void EmitPickedPowerup() => Instance.EmitSignal(nameof(PickedPowerUp));
  public void EmitBreakBreakerWin() => Instance.EmitSignal(nameof(BreakBreakerWin));
  public void EmitBrickBreakerStart() => Instance.EmitSignal(nameof(BrickBreakerStart));
  public void EmitPianoNotePressed(string note) => Instance.EmitSignal(nameof(PianoNotePressed), note);
  public void EmitPianoNoteReleased(string note) => Instance.EmitSignal(nameof(PianoNoteReleased), note);
  public void EmitPageFlipped() => Instance.EmitSignal(nameof(PageFlipped));
  public void EmitWrongPianoNotePlayed() => Instance.EmitSignal(nameof(WrongPianoNotePlayed));
  public void EmitPianoPuzzleWon() => Instance.EmitSignal(nameof(PianoPuzzleWon));
  public void EmitCutsceneRequestStart(string id) => Instance.EmitSignal(nameof(CutSceneRequestStart), id);
  public void EmitCutsceneRequestEnd(string id) => Instance.EmitSignal(nameof(CutSceneRequestEnd), id);
  public void EmitGemTempleTriggered() => Instance.EmitSignal(nameof(GemTempleTriggered));
  public void EmitGemEngineStarted() => Instance.EmitSignal(nameof(GemEngineStarted));
  public void EmitLevelCleared() => Instance.EmitSignal(nameof(LevelCleared));
  public void EmitGemPutInTemple() => Instance.EmitSignal(nameof(GemPutInTemple));
}

