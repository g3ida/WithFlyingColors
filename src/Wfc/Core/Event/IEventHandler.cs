namespace Wfc.Core.Event;

using System;
using Godot;

public interface IEventHandler {
  public void Connect(EventType eventType, Callable callable);
  public void ConnectOneShot(EventType eventType, Callable callable);
  public void Disconnect(EventType eventType, Callable callable);
  public void Emit(EventType eventType);
  public void Emit(EventType eventType, params Variant[] args);

  public bool Connect<T0>(EventType eventType, Node caller, Action<T0> action);
  public bool Connect<T0, T1>(EventType eventType, Node caller, Action<T0, T1> action);
  public bool Connect<T0, T1, T2>(EventType eventType, Node caller, Action<T0, T1, T2> action);
  public bool Connect<T0, T1, T2, T3>(EventType eventType, Node caller, Action<T0, T1, T2, T3> action);
  public bool Connect<T0, T1, T2, T3, T4>(EventType eventType, Node caller, Action<T0, T1, T2, T3, T4> action);
  public bool Connect<T0, T1, T2, T3, T4, T5>(EventType eventType, Node caller, Action<T0, T1, T2, T3, T4, T5> action);

  public bool Connect(EventType eventType, Node caller, Action action);

}
