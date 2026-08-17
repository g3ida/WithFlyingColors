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
  IProvide<ILogger>,
  IProvide<IMenuManager>,
  IProvide<ISaveManager>,
  IProvide<ILocalizationService>,
  IProvide<ISfxManager>,
  IProvide<IMusicTrackManager>,
  IProvide<IInputManager>,
  IProvide<IModalStack>,
  IProvide<IPauseOwnership> {
  public override void _Notification(int what) => this.Notify(what);

  public FakeInputManager Input { get; } = new();
  public FakeSaveManager Save { get; set; } = new();
  public IMenuManager MenuManager => _menuManager.Value;
  public IModalStack ModalStack => _modalStack.Value;

  private readonly Lazy<IMenuManager> _menuManager;
  private readonly Lazy<IModalStack> _modalStack;
  private readonly ILocalizationService _localizationService = new LocalizationService();
  private readonly Lazy<IPauseOwnership> _pauseOwnership;

  ILogger IProvide<ILogger>.Value() => Log.Logger;
  IMenuManager IProvide<IMenuManager>.Value() => _menuManager.Value;
  ISaveManager IProvide<ISaveManager>.Value() => Save;
  // Held rather than built per resolve, like every other service here. It is stateless, so
  // the copies were harmless - but seventeen types depend on it and each got its own.
  ILocalizationService IProvide<ILocalizationService>.Value() => _localizationService;
  ISfxManager IProvide<ISfxManager>.Value() => Wfc.Autoload.AutoloadManager.Instance.SfxManager;
  IMusicTrackManager IProvide<IMusicTrackManager>.Value() => Wfc.Autoload.AutoloadManager.Instance.MusicTrackManager;
  IInputManager IProvide<IInputManager>.Value() => Input;
  IModalStack IProvide<IModalStack>.Value() => _modalStack.Value;
  IPauseOwnership IProvide<IPauseOwnership>.Value() => _pauseOwnership.Value;

  public FakeDependenciesProvider() : base() {
    // Anything a test drives into saving (closing a settings view does) writes through
    // this path. The suite is already pointed here before the first test runs; this is
    // belt and braces for a fake built outside it, and shares the same constant so the
    // two cannot drift apart.
    GameSettings.ConfigFilePath = WithFlyingColors.Main.TEST_CONFIG_PATH;
    _menuManager = new Lazy<IMenuManager>(() => new MenuManager(this));
    _pauseOwnership = new Lazy<IPauseOwnership>(() => new PauseOwnership(GetTree()));
    _modalStack = new Lazy<IModalStack>(() => new ModalStack(_pauseOwnership.Value));
  }

  public void OnReady() => this.Provide();

  public void OnEnterTree() { }

  public void OnProvided() { }
}
