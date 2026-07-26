namespace Wfc.Screens;

using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.Core.Localization;
using Wfc.Core.Persistence;
using Wfc.Core.Ui;
using Wfc.Entities.Ui;
using Wfc.Screens.Levels;
using Wfc.Screens.MenuManager;
using EventHandler = Core.Event.EventHandler;

public partial class GameMenu : Control {
  // I faced resolution issues when deriving from dependency Injected classes
  // children needs to have [Meta(typeof(IAutoNode))] but still failed to resolve
  // dependencies in some cases So I used composition to get around it.
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
    [Dependency]
    public IInputManager InputManager => this.DependOn<IInputManager>();
    [Dependency]
    public IModalStack ModalStack => this.DependOn<IModalStack>();
    public void OnResolved() {
      // This is called on resolving dependencies to make sure localized text is configured.
      (Owner as GameMenu)?.ConfigureTransitionElements();
    }
  }

  public enum MenuScreenState {
    Entering,
    Entered,
    Exiting,
    Exited
  }

  protected MenuScreenState _screenState = MenuScreenState.Entering;
  private GameMenus _destinationScreen;
  private Node? _currentFocus;
  public bool HandleBackEvent = true;

  private readonly List<UITransition> _transitionElements = [];
  // Counted separately rather than as one balance: exiting while the screen is still
  // entering leaves some elements that will never report Entered, and a single
  // counter would go negative and never come back to zero, stranding the screen.
  private int _enteredTransitionElementsCount;
  private int _exitedTransitionElementsCount;

  // Dependencies
  private DependenciesWrapper _dependenciesWrapper = null!;
  protected IMenuManager MenuManager => _dependenciesWrapper.MenuManager;
  protected IEventHandler EventHandler => _dependenciesWrapper.EventHandler;
  protected ISaveManager SaveManager => _dependenciesWrapper.SaveManager;
  protected ILocalizationService LocalizationService => _dependenciesWrapper.LocalizationService;
  protected IInputManager InputManager => _dependenciesWrapper.InputManager;
  protected IModalStack ModalStack => _dependenciesWrapper.ModalStack;

  public override void _EnterTree() {
    base._EnterTree();
    SetupDependencies();
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
    _connectSignals();
    OnReady();
  }

  public void ConfigureTransitionElements() {
    _parseTransitionElements();
    _enteredTransitionElementsCount = 0;
    _exitedTransitionElementsCount = 0;
    _screenState = _hasNoTransitionElements() ? MenuScreenState.Entered : MenuScreenState.Entering;
    _enterTransitionElements();
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

  // The screen is the single owner of back for everything it contains: its widgets
  // and the settings focus manager leave UICancel alone and let it land here.
  public override void _Input(InputEvent @event) {
    base._Input(@event);
    // Nothing on a screen mid-transition may be touched, children included, so the
    // event is marked handled rather than merely ignored.
    if (IsInTransitionState()) {
      GetViewport().SetInputAsHandled();
      return;
    }

    // An overlay speaks for the screen while it is up.
    if (!HandleBackEvent || ModalStack.IsAnyOpen) {
      return;
    }

    // Tested against the event rather than polled: _Input runs once per event, and
    // IsJustPressed stays true for the whole frame, so polling here fired once per
    // event delivered in that frame - several times over with a gamepad connected.
    if (InputManager.IsEventActionJustPressed(IInputManager.Action.UICancel, @event) ||
        InputManager.IsEventActionJustPressed(IInputManager.Action.UIHome, @event)) {
      EventHandler.EmitMenuActionPressed(MenuAction.GoBack);
      GetViewport().SetInputAsHandled();
    }
  }

  public void NavigateToScreen(GameMenus menuScreen) {
    if (_screenState is MenuScreenState.Entering or MenuScreenState.Entered) {
      _destinationScreen = menuScreen;
      if (_hasNoTransitionElements()) {
        _screenState = MenuScreenState.Exited;
        MenuManager.GoToMenu(_destinationScreen);
      }
      else {
        _screenState = MenuScreenState.Exiting;
        StopProcessInput();
        _exitTransitionElements();
      }
    }
  }

  public void NavigateToLevelScreen(LevelId levelId) {

    MenuManager.SetCurrentLevel(levelId);
    NavigateToScreen(GameMenus.GAME);
  }

  private void _internalOnMenuButtonPressed(int menuButtonValue) {
    var menuButton = (MenuAction)menuButtonValue;
    if (_screenState != MenuScreenState.Entered) {
      return;
    }

    if (!OnMenuButtonPressed(menuButton) && menuButton == MenuAction.GoBack) {
      NavigateToScreen(MenuManager.GetPreviousMenu());
    }
  }

  public virtual bool OnMenuButtonPressed(MenuAction menuAction) {
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
    // Disconnect first so a second call can't leave the previous set connected and
    // double-count every signal.
    _clearTransitionElements();
    foreach (var child in GetChildren()) {
      // only look 3 levels deep for performance
      _collectTransitionsRecursive(child, 3);
    }
  }

  // Helper: recursively collects transitions from the entire subtree.
  private void _collectTransitionsRecursive(Node node, int remainingDepth) {
    if (remainingDepth == 0) {
      return;
    }
    if (node is UITransition transition) {
      _transitionElements.Add(transition);
      transition.Connect(UITransition.SignalName.Entered, new Callable(this, nameof(_onTransitionElementEntered)));
      transition.Connect(UITransition.SignalName.Exited, new Callable(this, nameof(_onTransitionElementExited)));
      // No need to descend further; transitions shouldn't have child transitions, but remove this return if that's possible.
      return;
    }
    foreach (var child in node.GetChildren()) {
      _collectTransitionsRecursive(child, remainingDepth - 1);
    }
  }

  private void _clearTransitionElements() {
    foreach (var transition in _transitionElements) {
      transition.Disconnect(UITransition.SignalName.Entered, new Callable(this, nameof(_onTransitionElementEntered)));
      transition.Disconnect(UITransition.SignalName.Exited, new Callable(this, nameof(_onTransitionElementExited)));
    }
    _transitionElements.Clear();
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
    return _screenState != MenuScreenState.Entered;
  }

  private bool _hasNoTransitionElements() {
    return _transitionElements.Count == 0;
  }

  private void _onTransitionElementEntered() {
    _enteredTransitionElementsCount++;
    if (_enteredTransitionElementsCount == _transitionElements.Count) {
      _screenState = MenuScreenState.Entered;
    }
  }

  private void _onTransitionElementExited() {
    _exitedTransitionElementsCount++;
    if (_exitedTransitionElementsCount == _transitionElements.Count) {
      _screenState = MenuScreenState.Exited;
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
