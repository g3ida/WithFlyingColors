namespace Wfc.Screens;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Localization;
using Wfc.Core.Persistence;
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
    ParseTransitionElements();
    _screenState = HasNoTransitionElements() ? MenuScreenState.ENTERED : MenuScreenState.ENTERING;
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
  }

  public override void _Ready() {
    base._Ready();
    ConnectSignals();
    EnterTransitionElements();
    OnReady();
  }

  public virtual void OnReady() {
    // Override this method in derived classes.
  }

  public override void _Process(double delta) {
    base._Process(delta);
    var focus_owner = GetViewport().GuiGetFocusOwner();
    if (focus_owner != null && focus_owner != _currentFocus) {
      EventHandler.Emit(EventType.FocusChanged);
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
      EventHandler.Emit(EventType.MenuButtonPressed, (int)MenuButtons.BACK);
    }
  }

  public void NavigateToScreen(GameMenus menuScreen) {
    if (_screenState is MenuScreenState.ENTERING or MenuScreenState.ENTERED) {
      _destinationScreen = menuScreen;
      if (HasNoTransitionElements()) {
        _screenState = MenuScreenState.EXITED;
        MenuManager.GoToMenu(_destinationScreen);
      }
      else {
        _screenState = MenuScreenState.EXITING;
        StopProcessInput();
        ExitTransitionElements();
      }
      OnExit();
    }
  }

  public void NavigateToLevelScreen(LevelId levelId) {

    MenuManager.SetCurrentLevel(levelId);
    NavigateToScreen(GameMenus.GAME);
  }

  private void InternalOnMenuButtonPressed(MenuButtons menuButton) {
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

  private void ConnectSignals() {
    EventHandler.Connect(EventType.MenuButtonPressed, this, (MenuButtons mb) => InternalOnMenuButtonPressed(mb));
  }

  private void ParseTransitionElements() {
    _transitionElements.Clear();
    foreach (var ch in GetChildren()) {
      foreach (var grandchild in ch.GetChildren()) {
        if (grandchild is UITransition transition) {
          _transitionElements.Add(transition);
          transition.Connect("entered", new Callable(this, nameof(OnTransitionElementEntered)));
          transition.Connect("exited", new Callable(this, nameof(OnTransitionElementExited)));
          break;
        }
      }
    }
  }

  private void ClearTransitionElements() {
    foreach (var transition in _transitionElements) {
      transition.Disconnect("entered", new Callable(this, nameof(OnTransitionElementEntered)));
      transition.Disconnect("exited", new Callable(this, nameof(OnTransitionElementExited)));
    }
  }

  private void EnterTransitionElements() {
    foreach (var element in _transitionElements) {
      element.Enter();
    }
  }

  private void ExitTransitionElements() {
    foreach (var element in _transitionElements) {
      element.Exit();
    }
  }

  public bool IsInTransitionState() {
    return _screenState != MenuScreenState.ENTERED;
  }

  private bool HasNoTransitionElements() {
    return _transitionElements.Count == 0;
  }

  private void OnTransitionElementEntered() {
    _enteredTransitionElementsCount++;
    if (_enteredTransitionElementsCount == _transitionElements.Count) {
      _screenState = MenuScreenState.ENTERED;
    }
  }

  private void OnTransitionElementExited() {
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
