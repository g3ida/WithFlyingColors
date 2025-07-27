namespace Wfc.Core.Event;

using System;
using Godot;
using Wfc.Entities.World;
using Wfc.Screens.MenuManager;

public interface IEventHandler {

  public Events Events { get; }
  public void Connect(string eventType, Callable callable);
  public void ConnectOneShot(string eventType, Callable callable);
  public void Disconnect(string eventType, Callable callable);
  public void Emit(string eventType);
  public void Emit(string eventType, params Variant[] args);

  public bool Connect<T0>(string eventType, Node caller, Action<T0> action);
  public bool Connect<T0, T1>(string eventType, Node caller, Action<T0, T1> action);
  public bool Connect<T0, T1, T2>(string eventType, Node caller, Action<T0, T1, T2> action);
  public bool Connect<T0, T1, T2, T3>(string eventType, Node caller, Action<T0, T1, T2, T3> action);
  public bool Connect<T0, T1, T2, T3, T4>(string eventType, Node caller, Action<T0, T1, T2, T3, T4> action);
  public bool Connect<T0, T1, T2, T3, T4, T5>(string eventType, Node caller, Action<T0, T1, T2, T3, T4, T5> action);

  public bool Connect(string eventType, Node caller, Action action);

  public void EmitPlayerLanded(Node area, Vector2 position);
  public void EmitPlayerDying(Node area, Vector2 position, EntityType entityType);
  public void EmitPlayerDying(Vector2 position, EntityType entityType);
  public void EmitPlayerDied();
  public void EmitPlayerSlippering();
  public void EmitPlayerJumped();
  public void EmitPlayerRotate(int dir);
  public void EmitPlayerLand();
  public void EmitPlayerExplode();
  public void EmitPlayerFall();
  public void EmitPlayerDash(Vector2 dir);
  public void EmitGemCollected(string color, Vector2 position, SpriteFrames frames);
  public void EmitCheckpointReached(Node checkpoint);
  public void EmitCheckpointLoaded();

  public void EmitMenuActionPressed(MenuAction menuAction);
  public void EmitMenuBoxRotated();
  public void EmitPauseMenuEnter();
  public void EmitPauseMenuExit();
  public void EmitFullscreenToggled(bool fullscreen);
  public void EmitVsyncToggled(bool vsync);
  public void EmitScreenSizeChanged(Vector2 size);
  public void EmitSfxVolumeChanged(float volume);
  public void EmitMusicVolumeChanged(float volume);
  public void EmitOnActionBound(string action, int key);
  public void EmitTabChanged();
  public void EmitFocusChanged();
  public void EmitKeyboardActionBiding();
  public void EmitTetrisLinesRemoved();
  public void EmitBrickBroken(string color, Vector2 position);
  public void EmitBouncingBallRemoved(Node ball);
  public void EmitPickedPowerup();
  public void EmitBreakBreakerWin();
  public void EmitBrickBreakerStart();
  public void EmitPianoNotePressed(int noteIndex);
  public void EmitPianoNoteReleased(int noteIndex);
  public void EmitPageFlipped();
  public void EmitWrongPianoNotePlayed();
  public void EmitPianoPuzzleWon();
  public void EmitCutsceneRequestStart(string id);
  public void EmitCutsceneRequestEnd(string id);
  public void EmitGemTempleTriggered();
  public void EmitGemEngineStarted();
  public void EmitLevelCleared();
  public void EmitGemPutInTemple();

}
