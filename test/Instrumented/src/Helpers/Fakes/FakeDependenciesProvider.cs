namespace Wfc.test.instrumented.Helpers.Fakes;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Autoload;
using Wfc.Core.Audio;
using Wfc.Core.Event;
using Wfc.Core.Exceptions;
using Wfc.Core.Input;
using Wfc.Core.Localization;
using Wfc.Core.Logger;
using Wfc.Core.Persistence;
using Wfc.Core.Settings;
using Wfc.Screens.MenuManager;

[Meta(typeof(IAutoNode))]
public partial class FakeDependenciesProvider :
  Node,
  IProvide<IInputManager> {
  public override void _Notification(int what) => this.Notify(what);

  private readonly Lazy<IInputManager> _inputManager;
  IInputManager IProvide<IInputManager>.Value() => _inputManager.Value;

  public FakeDependenciesProvider() : base() {

    _inputManager = new Lazy<IInputManager>(() => new FakeInputManager());
  }

  public void OnReady() {
    GameSettings.Load();
    this.Provide();
  }

  public void OnEnterTree() { }

  public void OnProvided() { }
}
