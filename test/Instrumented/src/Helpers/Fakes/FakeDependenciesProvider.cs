namespace Wfc.test.instrumented.Helpers.Fakes;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Audio;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.Core.Localization;
using Wfc.Core.Logger;
using Wfc.Core.Persistence;
using Wfc.Core.Settings;
using Wfc.Core.Ui;
using Wfc.Screens.MenuManager;
using Wfc.test.Helpers.Fakes;
using EventHandler = Wfc.Core.Event.EventHandler;

// Stands in for the RootNode that provides the game's services, which tests never get
// because Main runs the suite instead of the game scene.
//
// Add one of these to the tree and any menu screen parented under it resolves exactly
// as it would in the real game. Save data and input are fakes a test can drive; the
// rest are the real implementations, since the point is to exercise them.
//
// GameSettings.Load() is deliberately not called: it rebinds the InputMap process
// wide, and a test has no business doing that to the ones that run after it.
[Meta(typeof(IAutoNode))]
public partial class FakeDependenciesProvider :
  Node,
  IProvide<IEventHandler>,
  IProvide<ILogger>,
  IProvide<IMenuManager>,
  IProvide<ISaveManager>,
  IProvide<ILocalizationService>,
  IProvide<ISfxManager>,
  IProvide<IMusicTrackManager>,
  IProvide<IInputManager>,
  IProvide<IModalStack> {
  public override void _Notification(int what) => this.Notify(what);

  public FakeInputManager Input { get; } = new();
  public FakeSaveManager Save { get; set; } = new();
  public IMenuManager MenuManager => _menuManager.Value;
  public IModalStack ModalStack => _modalStack.Value;

  private readonly Lazy<IMenuManager> _menuManager;
  private readonly Lazy<IModalStack> _modalStack;
  private readonly ILogger _logger = new GDLogger();

  IEventHandler IProvide<IEventHandler>.Value() => EventHandler.Instance;
  ILogger IProvide<ILogger>.Value() => _logger;
  IMenuManager IProvide<IMenuManager>.Value() => _menuManager.Value;
  ISaveManager IProvide<ISaveManager>.Value() => Save;
  ILocalizationService IProvide<ILocalizationService>.Value() => new LocalizationService();
  ISfxManager IProvide<ISfxManager>.Value() => Wfc.Autoload.AutoloadManager.Instance.SfxManager;
  IMusicTrackManager IProvide<IMusicTrackManager>.Value() => Wfc.Autoload.AutoloadManager.Instance.MusicTrackManager;
  IInputManager IProvide<IInputManager>.Value() => Input;
  IModalStack IProvide<IModalStack>.Value() => _modalStack.Value;

  public FakeDependenciesProvider() : base() {
    // Anything a test drives into saving (closing a settings view does) writes
    // through this path. Left on the default it overwrites the developer's real
    // settings.ini - a headless run wrote the window's zero size into it once.
    GameSettings.ConfigFilePath = "user://test-settings.ini";
    _menuManager = new Lazy<IMenuManager>(() => new MenuManager(this));
    _modalStack = new Lazy<IModalStack>(() => new ModalStack(GetTree()));
  }

  public void OnReady() => this.Provide();

  public void OnEnterTree() { }

  public void OnProvided() { }
}
