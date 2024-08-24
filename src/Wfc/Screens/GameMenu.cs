namespace Wfc.Screens;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using System.Collections.Generic;
using Wfc.Core.Event;
using Wfc.Screens.MenuManager;

public partial class GameMenu : Control {
  // I faced resolution issues when deriving from dependency Injected classes
  // So I used composition to get around it.
  [Meta(typeof(IAutoNode))]
  public partial class MenuManagerWrapper : Node {
    public override void _Notification(int what) => this.Notify(what);

    [Dependency]
    public IMenuManager MenuManager => this.DependOn<IMenuManager>();

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
  private Node _currentFocus = null!;
  public bool HandleBackEvent = true;

  private MenuManagerWrapper _menuManagerWrapper = null!;

  private readonly List<UITransition> _transitionElements = [];
  private int _enteredTransitionElementsCount;

  public override void _EnterTree() {
    base._EnterTree();
    ConnectSignals();
    SetupMenuManager();
    ParseTransitionElements();
    _screenState = HasNoTransitionElements() ? MenuScreenState.ENTERED : MenuScreenState.ENTERING;
    OnEnter();
  }

  private void SetupMenuManager() {
    _menuManagerWrapper = new MenuManagerWrapper();
    AddChild(_menuManagerWrapper);
    _menuManagerWrapper.Owner = this;
  }

  public override void _ExitTree() {
    base._ExitTree();
    DisconnectSignals();
    OnExit();
  }

  public override void _Ready() {
    base._Ready();
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
      Event.Instance.EmitFocusChanged();
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
      Event.Instance.EmitMenuButtonPressed(MenuButtons.BACK);
    }
  }

  public void NavigateToScreen(GameMenus menu_screen) {
    if (_screenState is MenuScreenState.ENTERING or MenuScreenState.ENTERED) {
      _destinationScreen = menu_screen;
      if (HasNoTransitionElements()) {
        _screenState = MenuScreenState.EXITED;
        _menuManagerWrapper.MenuManager.GoToMenu(_destinationScreen);
      }
      else {
        _screenState = MenuScreenState.EXITING;
        StopProcessInput();
        ExitTransitionElements();
      }
      OnExit();
    }
  }

  public void NavigateToLevelScreen(string level_screen_path) {
    if (string.IsNullOrEmpty(level_screen_path)) {
      return;
    }

    _menuManagerWrapper.MenuManager.SetCurrentLevel(level_screen_path);
    NavigateToScreen(GameMenus.GAME);
  }

  private void _OnMenuButtonPressed(MenuButtons menu_button) {
    if (_screenState != MenuScreenState.ENTERED) {
      return;
    }

    if (!OnMenuButtonPressed(menu_button) && menu_button == MenuButtons.BACK) {
      NavigateToScreen(_menuManagerWrapper.MenuManager.GetPreviousMenu());
    }
  }

  public virtual bool OnMenuButtonPressed(MenuButtons menu_button) {
    return false;
  }

  public virtual void OnEnter() {
    // Override this method in derived classes.
  }

  public virtual void OnExit() {
    // Override this method in derived classes.
  }

  private void ConnectSignals() {
    Event.Instance.Connect(EventType.MenuButtonPressed, new Callable(this, nameof(_OnMenuButtonPressed)));
  }

  private void DisconnectSignals() {
    Event.Instance.Disconnect(EventType.MenuButtonPressed, new Callable(this, nameof(_OnMenuButtonPressed)));
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
      _menuManagerWrapper.MenuManager.GoToMenu(_destinationScreen);
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
