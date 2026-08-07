namespace Wfc.Base;

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
using Wfc.Core.Ui;
using Wfc.Screens.MenuManager;

[Meta(typeof(IAutoNode))]
public partial class DependenciesProvider :
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

  private readonly Lazy<IMenuManager> _menuManager;
  private readonly Lazy<IInputManager> _inputManager;
  private readonly Lazy<IModalStack> _modalStack;
  private readonly ILogger _logger = new GDLogger();
  private readonly SaveManager _saveManager = new SaveManager();
  IMenuManager IProvide<IMenuManager>.Value() => _menuManager.Value;
  IModalStack IProvide<IModalStack>.Value() => _modalStack.Value;
  ISaveManager IProvide<ISaveManager>.Value() => _saveManager;
  ILocalizationService IProvide<ILocalizationService>.Value() => new LocalizationService();
  ILogger IProvide<ILogger>.Value() => _logger;
  IEventHandler IProvide<IEventHandler>.Value() => AutoloadManager.Instance.EventHandler;
  ISfxManager IProvide<ISfxManager>.Value() => AutoloadManager.Instance.SfxManager;
  IMusicTrackManager IProvide<IMusicTrackManager>.Value() => AutoloadManager.Instance.MusicTrackManager;
  IInputManager IProvide<IInputManager>.Value() => _inputManager.Value;

  public DependenciesProvider() : base() {
    _menuManager = new Lazy<IMenuManager>(() => new MenuManager(this));
    _inputManager = new Lazy<IInputManager>(() => new InputManager());
    // Lazy so GetTree() is only reached once this node is in the tree.
    _modalStack = new Lazy<IModalStack>(() => new ModalStack(GetTree()));
  }

  public void OnReady() {
    GameSettings.Load();
    this.Provide();
  }

  public void OnEnterTree() { }

  public void OnProvided() {
    // You can optionally implement this method. It gets called once you call
    // this.Provide() to inform AutoInject that the provided values are now
    // available.
    // Before the first screen is built, not after: the main menu reads the selected
    // slot in its _Ready, so loading the save data second left it showing slot 1 on
    // every cold start whatever the player had actually selected.
    _saveManager.Init();

    // The first navigation, which is also what seeds the history the back button
    // unwinds. It only succeeds from an empty history, so nothing else may navigate
    // before this runs.
    //
    // Anything the settings file does not answer is asked before the main menu, in the
    // order the screens chain: the language every later screen is written in, then the
    // palette the game is played in. A file that answers both goes straight through.
    var firstScreen =
      !GameSettings.HasStoredLanguage ? GameMenus.LANGUAGE_SELECT
      : !GameSettings.HasStoredSkin ? GameMenus.SKIN_SELECT
      : GameMenus.MAIN_MENU;
    if (!_menuManager.Value.GoToMenu(firstScreen)) {
      throw new GameExceptions.InvalidArgumentException($"{firstScreen} scene could not be shown");
    }
  }
}
