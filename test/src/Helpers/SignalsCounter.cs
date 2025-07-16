namespace Wfc.test;

using System.Collections.Generic;
using Godot;
using Godot.NativeInterop;
using Shouldly;

public partial class SignalsCounter : GodotObject {

  private Dictionary<string, SignalCounter> _signalCounters = new();

  public void Connect(string key, GodotObject source, string signalName) {
    if (_signalCounters.TryGetValue(key, out var counter)) {
      counter.Disconnect();
    }
    _signalCounters[key] = new SignalCounter(source, signalName);
  }

  public void Clear() {
    foreach (var (k, v) in _signalCounters) {
      v.Disconnect();
    }
    _signalCounters.Clear();
  }

  public int getCallCount(string key) {
    if (_signalCounters.TryGetValue(key, out var counter)) {
      return counter.CallCount;
    }
    return 0;
  }
}
