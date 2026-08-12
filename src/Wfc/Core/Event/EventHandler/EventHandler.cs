namespace Wfc.Core.Event;

using System;
using Godot;
using Wfc.Core.Input.Controllers;
using Wfc.Core.Localization;
using Wfc.Entities.World;
using Wfc.Entities.World.Piano;
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

  private Events _event = new Events();
  public static EventHandler Instance { get; private set; } = null!;
  public Events Events => _event;

  public void Connect(string eventType, Callable callable) {
    Events.Connect(eventType.ToString(), callable);
  }

  public bool Connect(string eventType, Node caller, Action action) {
    // "boxing" the action inside one other action to avoid having the same hashcode for different instances
    var callable = Callable.From(() => action());
    return Connect(eventType, caller, callable);
  }

  public bool Connect<T0>(string eventType, Node caller, Action<T0> action) {
    // "boxing" the action inside one other action to avoid having the same hashcode for different instances
    var callable = Callable.From((T0 v0) => action(v0));
    return Connect(eventType, caller, callable);
  }

  public bool Connect<T0, T1>(string eventType, Node caller, Action<T0, T1> action) {
    // "boxing" the action inside one other action to avoid having the same hashcode for different instances
    var callable = Callable.From((T0 v0, T1 v1) => action(v0, v1));
    return Connect(eventType, caller, callable);
  }

  public bool Connect<T0, T1, T2>(string eventType, Node caller, Action<T0, T1, T2> action) {
    // "boxing" the action inside one other action to avoid having the same hashcode for different instances
    var callable = Callable.From((T0 v0, T1 v1, T2 v2) => action(v0, v1, v2));
    return Connect(eventType, caller, callable);
  }

  public bool Connect<T0, T1, T2, T3>(string eventType, Node caller, Action<T0, T1, T2, T3> action) {
    // "boxing" the action inside one other action to avoid having the same hashcode for different instances
    var callable = Callable.From((T0 v0, T1 v1, T2 v2, T3 v3) => action(v0, v1, v2, v3));
    return Connect(eventType, caller, callable);
  }

  public bool Connect<T0, T1, T2, T3, T4>(string eventType, Node caller, Action<T0, T1, T2, T3, T4> action) {
    // "boxing" the action inside one other action to avoid having the same hashcode for different instances
    var callable = Callable.From((T0 v0, T1 v1, T2 v2, T3 v3, T4 v4) => action(v0, v1, v2, v3, v4));
    return Connect(eventType, caller, callable);
  }

  public bool Connect<T0, T1, T2, T3, T4, T5>(string eventType, Node caller, Action<T0, T1, T2, T3, T4, T5> action) {
    // "boxing" the action inside one other action to avoid having the same hashcode for different instances
    var callable = Callable.From((T0 v0, T1 v1, T2 v2, T3 v3, T4 v4, T5 v5) => action(v0, v1, v2, v3, v4, v5));
    return Connect(eventType, caller, callable);
  }

  private bool Connect(string eventType, Node caller, Callable callable) {
    var eventName = eventType.ToString();
    if (!Events.IsConnected(eventName, callable)) {
      var error = Events.Connect(eventName, callable);
      if (error == Error.Ok) {
        caller.TreeExiting += () => {
          if (Events.IsConnected(eventName, callable)) {
            Events.Disconnect(eventName, callable);
          }
        };
        return true;
      }
      else {
        GD.PushError("error: " + error);
      }
    }
    return false;
  }

  public void ConnectOneShot(string eventType, Callable callable) {
    Events.Connect(eventType.ToString(), callable, flags: (uint)ConnectFlags.OneShot);
  }

  public void Disconnect(string eventType, Callable callable) {
    Events.Disconnect(eventType.ToString(), callable);
  }

  public void Emit(string eventType) {
    Events.EmitSignal(eventType.ToString());
  }

  public void Emit(string eventType, params Variant[] args)
    => Events.EmitSignal(eventType.ToString(), args);

  public void EmitPlayerLanded(Node area, Vector2 position) => Events.EmitSignal(Events.SignalName.PlayerLanded, area, position);
  public void EmitPlayerDying(Node area, Vector2 position, EntityType entityType) => Events.EmitSignal(Events.SignalName.PlayerDying, area, position, (int)entityType);
  public void EmitPlayerDying(Vector2 position, EntityType entityType) => Events.EmitSignal(Events.SignalName.PlayerDying, default(Variant), position, (int)entityType);
  public void EmitPlayerDied() => Events.EmitSignal(Events.SignalName.PlayerDied);
  public void EmitPlayerSlippering() => Events.EmitSignal(Events.SignalName.PlayerSlippering);
  public void EmitPlayerJumped() => Events.EmitSignal(Events.SignalName.PlayerJumped);
  public void EmitPlayerRotate(int dir) => Events.EmitSignal(Events.SignalName.PlayerRotate, dir);
  public void EmitPlayerLand() => Events.EmitSignal(Events.SignalName.PlayerLand);
  public void EmitPlayerExplode() => Events.EmitSignal(Events.SignalName.PlayerExplode);
  public void EmitPlayerFall() => Events.EmitSignal(Events.SignalName.PlayerFall);
  public void EmitPlayerSquashed() => Events.EmitSignal(Events.SignalName.PlayerSquashed);
  public void EmitPlayerDash(Vector2 dir) => Events.EmitSignal(Events.SignalName.PlayerDash, dir);
  public void EmitGemCollected(string color, Vector2 position, SpriteFrames frames) => Events.EmitSignal(Events.SignalName.GemCollected, color, position, frames);
  public void EmitCheckpointReached(Vector2 position, string colorGroup) => Events.EmitSignal(Events.SignalName.CheckpointReached, position, colorGroup);
  public void EmitCheckpointLoaded() => Events.EmitSignal(Events.SignalName.CheckpointLoaded);
  public void EmitMenuActionPressed(MenuAction menuAction) => Events.EmitSignal(Events.SignalName.MenuButtonPressed, (int)menuAction);
  public void EmitMenuBoxRotated() => Events.EmitSignal(Events.SignalName.MenuBoxRotated);
  public void EmitPauseMenuEnter() => Events.EmitSignal(Events.SignalName.PauseMenuEnter);
  public void EmitPauseMenuExit() => Events.EmitSignal(Events.SignalName.PauseMenuExit);
  public void EmitFullscreenToggled(bool fullscreen) => Events.EmitSignal(Events.SignalName.FullscreenToggled, fullscreen);
  public void EmitVsyncToggled(bool vsync) => Events.EmitSignal(Events.SignalName.VsyncToggled, vsync);
  public void EmitScreenSizeChanged(Vector2 size) => Events.EmitSignal(Events.SignalName.ScreenSizeChanged, size);
  public void EmitLanguageChanged(Language language) => Events.EmitSignal(Events.SignalName.LanguageChanged, (int)language);
  public void EmitSkinChanged(string skin) => Events.EmitSignal(Events.SignalName.SkinChanged, skin);
  public void EmitSfxVolumeChanged(float volume) => Events.EmitSignal(Events.SignalName.SfxVolumeChanged, volume);
  public void EmitMusicVolumeChanged(float volume) => Events.EmitSignal(Events.SignalName.MusicVolumeChanged, volume);
  public void EmitOnActionBound(string action, int key) => Events.EmitSignal(Events.SignalName.OnActionBound, action, key);
  public void EmitFocusChanged() => Events.EmitSignal(Events.SignalName.FocusChanged);
  public void EmitKeyboardActionBiding() => Events.EmitSignal(Events.SignalName.KeyboardActionBinding);
  public void EmitGamepadActionBinding() => Events.EmitSignal(Events.SignalName.GamepadActionBinding);
  public void EmitOnGamepadActionBound(string action, int buttonOrAxis, bool isAxis, float axisDirection) =>
    Events.EmitSignal(Events.SignalName.OnGamepadActionBound, action, buttonOrAxis, isAxis, axisDirection);
  public void EmitLastUsedControllerChanged(ControllerType controllerType) =>
    Events.EmitSignal(Events.SignalName.LastUsedControllerChanged, (int)controllerType);
  public void EmitControllerSelectionChanged(ControllerType controllerType) =>
    Events.EmitSignal(Events.SignalName.ControllerSelectionChanged, (int)controllerType);
  public void EmitGamepadConnected(int deviceId, string deviceName) =>
    Events.EmitSignal(Events.SignalName.GamepadConnected, deviceId, deviceName);
  public void EmitGamepadDisconnected(int deviceId) =>
    Events.EmitSignal(Events.SignalName.GamepadDisconnected, deviceId);
  public void EmitTetrisLinesRemoved() => Events.EmitSignal(Events.SignalName.TetrisLinesRemoved);
  public void EmitBrickBroken(string color, Vector2 position) => Events.EmitSignal(Events.SignalName.BrickBroken, color, position);
  public void EmitBouncingBallRemoved(Node ball) => Events.EmitSignal(Events.SignalName.BouncingBallRemoved, ball);
  public void EmitPickedPowerup() => Events.EmitSignal(Events.SignalName.PickedPowerUp);
  public void EmitBreakBreakerWin() => Events.EmitSignal(Events.SignalName.BreakBreakerWin);
  public void EmitBrickBreakerStart() => Events.EmitSignal(Events.SignalName.BrickBreakerStart);
  public void EmitPianoNotePressed(int noteIndex) => Events.EmitSignal(Events.SignalName.PianoNotePressed, noteIndex);
  public void EmitPianoNoteReleased(int noteIndex) => Events.EmitSignal(Events.SignalName.PianoNoteReleased, noteIndex);
  public void EmitPageFlipped() => Events.EmitSignal(Events.SignalName.PageFlipped);
  public void EmitWrongPianoNotePlayed() => Events.EmitSignal(Events.SignalName.WrongPianoNotePlayed);
  public void EmitPianoPuzzleWon() => Events.EmitSignal(Events.SignalName.PianoPuzzleWon);
  public void EmitButtonGameNotePlayed(int noteIndex) => Events.EmitSignal(Events.SignalName.ButtonGameNotePlayed, noteIndex);
  public void EmitButtonGameWrongNotePlayed() => Events.EmitSignal(Events.SignalName.ButtonGameWrongNotePlayed);
  public void EmitButtonGameWon() => Events.EmitSignal(Events.SignalName.ButtonGameWon);
  public void EmitCutsceneRequestStart(string id) => Events.EmitSignal(Events.SignalName.CutSceneRequestStart, id);
  public void EmitCutsceneRequestEnd(string id) => Events.EmitSignal(Events.SignalName.CutSceneRequestEnd, id);
  public void EmitLevelCleared() => Events.EmitSignal(Events.SignalName.LevelCleared);
  public void EmitDoorGemFilled() => Events.EmitSignal(Events.SignalName.DoorGemFilled);
  public void EmitDoorCometFormed() => Events.EmitSignal(Events.SignalName.DoorCometFormed);
  public void EmitDoorEntered(int levelId) => Events.EmitSignal(Events.SignalName.DoorEntered, levelId);
  public void EmitLevelRestartRequested() => Events.EmitSignal(Events.SignalName.LevelRestartRequested);
  public void EmitSaveSlotUpdated() => Events.EmitSignal(Events.SignalName.SaveSlotUpdated);
  public void EmitNotificationRaised(TranslationKey key) => Events.EmitSignal(Events.SignalName.NotificationRaised, (int)key);
  public void EmitPaintSpilled(Vector2 position) => Events.EmitSignal(Events.SignalName.PaintSpilled, position);
  public void EmitBucketShoved(Vector2 position) => Events.EmitSignal(Events.SignalName.BucketShoved, position);
  public void EmitPaintPouring(Vector2 position) => Events.EmitSignal(Events.SignalName.PaintPouring, position);
  public void EmitPaintSplashed(Vector2 position) => Events.EmitSignal(Events.SignalName.PaintSplashed, position);
  public void EmitPaintGunCooling(Vector2 position) => Events.EmitSignal(Events.SignalName.PaintGunCooling, position);
  public void EmitPaintGunFired(Vector2 position) => Events.EmitSignal(Events.SignalName.PaintGunFired, position);

  public void EmitCameraShakeRequest(float amplitude) => Events.EmitSignal(Events.SignalName.CameraShakeRequest, amplitude);
  public void EmitCameraZoomPunchRequest(float strength) => Events.EmitSignal(Events.SignalName.CameraZoomPunchRequest, strength);
}

