namespace Wfc.test;

using Godot;
using Godot.NativeInterop;
using Shouldly;

public partial class SignalCounter : GodotObject {

  public int CallCount { get; private set; } = 0;
  private GodotObject _source;
  private string _signalName;
  private Callable _callable;

  public SignalCounter(GodotObject source, string signalName) {
    _source = source;
    _signalName = signalName;
    _callable = new Callable(this, nameof(_onSignalFired));
    _source.Connect(signalName, _callable);
  }

  private void _onSignalFired() {
    CallCount++;
  }


  public void Disconnect() {
    _source.Disconnect(_signalName, _callable);
  }
}
