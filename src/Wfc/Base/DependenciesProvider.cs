namespace Wfc.Base;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Localization;
using Wfc.Screens.MenuManager;
using System;
using Wfc.Core.Logger;

[Meta(typeof(IAutoNode))]
public partial class DependenciesProvider : Node,
  IProvide<ILogger>, IProvide<IMenuManager>, IProvide<ILocalizationService> {
  public override void _Notification(int what) => this.Notify(what);

  private readonly Lazy<IMenuManager> _menuManager;
  private readonly ILogger _logger = new GDLogger();

  IMenuManager IProvide<IMenuManager>.Value() => _menuManager.Value;
  ILocalizationService IProvide<ILocalizationService>.Value() => new LocalizationService();
  ILogger IProvide<ILogger>.Value() => _logger;


  public DependenciesProvider() : base() {
    _menuManager = new Lazy<IMenuManager>(() => new MenuManager(this));
  }

  public void OnReady() => this.Provide();

  public void OnProvided() {
    // You can optionally implement this method. It gets called once you call
    // this.Provide() to inform AutoInject that the provided values are now
    // available.
    _menuManager.Value.SwitchScene(MenuScenes.MAIN_MENU_SCENE);
  }
}
