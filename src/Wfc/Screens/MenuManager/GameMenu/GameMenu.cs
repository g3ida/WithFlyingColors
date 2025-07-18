namespace Wfc.Screens;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Localization;
using Wfc.Core.Persistence;
using Wfc.Entities.Ui;
using Wfc.Screens.Levels;
using Wfc.Screens.MenuManager;
using EventHandler = Core.Event.EventHandler;

public partial class GameMenu : Control {
  // I faced resolution issues when deriving from dependency Injected classes
  // So I used composition to get around it.
  [Meta(typeof(IAutoNode))]
  public partial class DependenciesWrapper : Node {
    public override void _Notification(int what) => this.Notify(what);

    [Dependency]
    public IMenuManager MenuManager => this.DependOn<IMenuManager>();
    [Dependency]
    public IEventHandler EventHandler => this.DependOn<IEventHandler>();
    [Dependency]
    public ISaveManager SaveManager => this.DependOn<ISaveManager>();
    [Dependency]
    public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();

    public void OnResolved() { }
  }

  public enum MenuScreenState {
    ENTERING,
    ENTERED,
    EXITING,
    EXITED
  }

  protected MenuScreenState _screenState;
  private GameMenus _destinationScreen;
  private Node? _currentFocus;
  public bool HandleBackEvent = true;

  private readonly List<UITransition> _transitionElements = [];
  private int _enteredTransitionElementsCount;

  // Dependencies
  private DependenciesWrapper _dependenciesWrapper = null!;
  protected IMenuManager MenuManager => _dependenciesWrapper.MenuManager;
  protected IEventHandler EventHandler => _dependenciesWrapper.EventHandler;
  protected ISaveManager SaveManager => _dependenciesWrapper.SaveManager;
  protected ILocalizationService LocalizationService => _dependenciesWrapper.LocalizationService;

  public override void _EnterTree() {
    base._EnterTree();
    SetupDependencies();
    _parseTransitionElements();
    _screenState = _hasNoTransitionElements() ? MenuScreenState.ENTERED : MenuScreenState.ENTERING;
    OnEnter();
  }

  private void SetupDependencies() {
    _dependenciesWrapper = new DependenciesWrapper();
    AddChild(_dependenciesWrapper);
    _dependenciesWrapper.Owner = this;
  }

  public override void _ExitTree() {
    base._ExitTree();
    OnExit();
    _disconnectSignals();
  }

  public override void _Ready() {
    base._Ready();
    _enterTransitionElements();
    _connectSignals();
    OnReady();
  }

  public virtual void OnReady() {
    // Override this method in derived classes.
  }

  public override void _Process(double delta) {
    base._Process(delta);
    var focus_owner = GetViewport().GuiGetFocusOwner();
    if (focus_owner != null && focus_owner != _currentFocus) {
      EventHandler.EmitFocusChanged();
    }
    _currentFocus = focus_owner;
    OnProcess(delta);
  }

  public virtual void OnProcess(double delta) {
    // Override this method in derived classes.
  }

  public override void _Input(InputEvent @event) {
    base._Input(@event);
    if (_screenState is ((int)MenuScreenState.ENTERING) or MenuScreenState.EXITING) {
      GetViewport().SetInputAsHandled();
    }

    if (HandleBackEvent && (Input.IsActionJustPressed("ui_cancel") || Input.IsActionJustPressed("ui_home"))) {
      // Event.Instance.EmitMenuButtonPressed(MenuButtons.BACK);
      EventHandler.EmitMenuButtonPressed(MenuButtons.BACK);
    }
  }

  public void NavigateToScreen(GameMenus menuScreen) {
    if (_screenState is MenuScreenState.ENTERING or MenuScreenState.ENTERED) {
      _destinationScreen = menuScreen;
      if (_hasNoTransitionElements()) {
        _screenState = MenuScreenState.EXITED;
        MenuManager.GoToMenu(_destinationScreen);
      }
      else {
        _screenState = MenuScreenState.EXITING;
        StopProcessInput();
        _exitTransitionElements();
      }
      OnExit();
    }
  }

  public void NavigateToLevelScreen(LevelId levelId) {

    MenuManager.SetCurrentLevel(levelId);
    NavigateToScreen(GameMenus.GAME);
  }

  private void _internalOnMenuButtonPressed(int menuButtonValue) {
    var menuButton = (MenuButtons)menuButtonValue;
    if (_screenState != MenuScreenState.ENTERED) {
      return;
    }

    if (!OnMenuButtonPressed(menuButton) && menuButton == MenuButtons.BACK) {
      NavigateToScreen(MenuManager.GetPreviousMenu());
    }
  }

  public virtual bool OnMenuButtonPressed(MenuButtons menuButton) {
    return false;
  }

  public virtual void OnEnter() {
    // Override this method in derived classes.
  }

  public virtual void OnExit() {
    // Override this method in derived classes.
  }

  private void _connectSignals() {
    EventHandler.Events.MenuButtonPressed += _internalOnMenuButtonPressed;
  }

  private void _disconnectSignals() {
    EventHandler.Events.MenuButtonPressed -= _internalOnMenuButtonPressed;
  }

  private void _parseTransitionElements() {
    _transitionElements.Clear();
    foreach (var ch in GetChildren()) {
      foreach (var grandchild in ch.GetChildren()) {
        if (grandchild is UITransition transition) {
          _transitionElements.Add(transition);
          transition.Connect(UITransition.SignalName.Entered, new Callable(this, nameof(_onTransitionElementEntered)));
          transition.Connect(UITransition.SignalName.Exited, new Callable(this, nameof(_onTransitionElementExited)));
          break;
        }
      }
    }
  }

  private void _clearTransitionElements() {
    foreach (var transition in _transitionElements) {
      transition.Disconnect(UITransition.SignalName.Entered, new Callable(this, nameof(_onTransitionElementEntered)));
      transition.Disconnect(UITransition.SignalName.Exited, new Callable(this, nameof(_onTransitionElementExited)));
    }
  }

  private void _enterTransitionElements() {
    foreach (var element in _transitionElements) {
      element.Enter();
    }
  }

  private void _exitTransitionElements() {
    foreach (var element in _transitionElements) {
      element.Exit();
    }
  }

  public bool IsInTransitionState() {
    return _screenState != MenuScreenState.ENTERED;
  }

  private bool _hasNoTransitionElements() {
    return _transitionElements.Count == 0;
  }

  private void _onTransitionElementEntered() {
    _enteredTransitionElementsCount++;
    if (_enteredTransitionElementsCount == _transitionElements.Count) {
      _screenState = MenuScreenState.ENTERED;
    }
  }

  private void _onTransitionElementExited() {
    _enteredTransitionElementsCount--;
    if (_enteredTransitionElementsCount == 0) {
      _screenState = MenuScreenState.EXITED;
      MenuManager.GoToMenu(_destinationScreen);
    }
  }

  private void StopProcessInput(Node? node = null) {
    node ??= this;
    foreach (var ch in node.GetChildren()) {
      if (ch is Control control) {
        control.SetProcessInput(false);
      }
      StopProcessInput(ch);
    }
  }
}
