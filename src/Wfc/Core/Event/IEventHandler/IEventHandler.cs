namespace Wfc.Core.Event;

using System;
using Godot;
using Wfc.Core.Input.Controllers;
using Wfc.Core.Localization;
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

  public void EmitCheckpointReached(Vector2 position, string colorGroup);
  public void EmitCheckpointLoaded();

  public void EmitMenuActionPressed(MenuAction menuAction);
  public void EmitMenuBoxRotated();
  public void EmitPauseMenuEnter();
  public void EmitPauseMenuExit();
  public void EmitOnActionBound(string action, int key);
  public void EmitFocusChanged();
  public void EmitKeyboardActionBinding();
  public void EmitOnGamepadActionBound(string action, int buttonOrAxis, bool isAxis, float axisDirection);
  public void EmitLastUsedControllerChanged(ControllerType controllerType);
  public void EmitControllerSelectionChanged(ControllerType controllerType);
  public void EmitGamepadConnected(int deviceId, string deviceName);
  public void EmitGamepadDisconnected(int deviceId);
  public void EmitCutsceneRequestStart(string id);
  public void EmitCutsceneRequestEnd(string id);
  public void EmitLevelCleared();
  public void EmitDoorGemFilled();
  public void EmitDoorCometFormed();
  public void EmitDoorEntered(int levelId);
  public void EmitLevelRestartRequested();
  public void EmitSaveSlotUpdated();
  public void EmitNotificationRaised(TranslationKey key);

}
