namespace Wfc.Base;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Autoload;
using Wfc.Core.Audio;
using Wfc.Core.Event;
using Wfc.Core.Exceptions;
using Wfc.Core.Localization;
using Wfc.Core.Logger;
using Wfc.Core.Persistence;
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
  IProvide<IMusicTrackManager> {
  public override void _Notification(int what) => this.Notify(what);

  private readonly Lazy<IMenuManager> _menuManager;
  private readonly ILogger _logger = new GDLogger();
  private readonly ISaveManager _saveManager = new SaveManager();
  IMenuManager IProvide<IMenuManager>.Value() => _menuManager.Value;
  ISaveManager IProvide<ISaveManager>.Value() => _saveManager;
  ILocalizationService IProvide<ILocalizationService>.Value() => new LocalizationService();
  ILogger IProvide<ILogger>.Value() => _logger;
  IEventHandler IProvide<IEventHandler>.Value() => AutoloadManager.Instance.EventHandler;
  ISfxManager IProvide<ISfxManager>.Value() => AutoloadManager.Instance.SfxManager;
  IMusicTrackManager IProvide<IMusicTrackManager>.Value() => AutoloadManager.Instance.MusicTrackManager;

  public DependenciesProvider() : base() {
    _menuManager = new Lazy<IMenuManager>(() => new MenuManager(this));
  }

  public void OnReady() {
    this.Provide();
  }

  public void OnEnterTree() {
  }

  public void OnProvided() {
    // You can optionally implement this method. It gets called once you call
    // this.Provide() to inform AutoInject that the provided values are now
    // available.
    var mainMenuScenePath = _menuManager.Value.GetMenuScenePath(GameMenus.MAIN_MENU);
    if (mainMenuScenePath != null) {
      _menuManager.Value.SwitchScene(mainMenuScenePath);
    }
    else {
      throw new GameExceptions.InvalidArgumentException("Main menu scene not found");
    }
    _saveManager.Init();
  }
}
