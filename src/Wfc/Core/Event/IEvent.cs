namespace Wfc.Core.Event;

using Godot;

public interface IEvent {
  public void Connect(EventType eventType, Callable callable);
  public void ConnectOneShot(EventType eventType, Callable callable);
  public void Disconnect(EventType eventType, Callable callable);
  public void Emit(EventType eventType);
  public void Emit(EventType eventType, params Variant[] args);
}
