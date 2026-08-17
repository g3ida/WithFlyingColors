namespace Wfc.Screens;

using Chickensoft.Sync.Primitives;
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
using Wfc.Utils;

public partial class GameMenu : Control {
  private AutoChannel.Binding? _menuBinding;

  // I faced resolution issues when deriving from dependency Injected classes
  // children needs to have [Meta(typeof(IAutoNode))] but still failed to resolve
  // dependencies in some cases So I used composition to get around it.
  [Meta(typeof(IAutoNode))]
  public partial class DependenciesWrapper : Node {
    public override void _Notification(int what) => this.Notify(what);

    [Dependency]
    public IMenuManager MenuManager => this.DependOn<IMenuManager>();
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

  // Takes stock of the transitions again after a widget rebuilt itself, so the
  // exit isn't left waiting on elements that no longer exist. The screen state and
  // the counts are left alone - this is not a second entrance, only a fresh list -
  // but a transition in flight is re-checked against it: the list may have shrunk
  // below what already reported in, and no further signal would ever finish it.
  public void RefreshTransitionElements() {
    _parseTransitionElements();
    _settleTransitionState();
  }

  public virtual void OnReady() {
    // Override this method in derived classes.
  }

  public override void _Process(double delta) {
    base._Process(delta);
    var focus_owner = GetViewport().GuiGetFocusOwner();
    if (focus_owner != null && focus_owner != _currentFocus) {
      GameEvents.Instance.OnFocusChanged();
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
      GameEvents.Instance.OnMenuActionPressed(MenuAction.GoBack);
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

  // Starting a new game wherever the player pointed: the main menu takes the first
  // slot when there is nothing to choose between, the slot picker takes the one that
  // was pressed. Both mean the same thing, so both come through here.
  protected void StartNewGameInSlot(int slotIndex) {
    // Wiping the slot clears the selection when it was the selected one, so the
    // reselect puts the new game exactly where the player pointed rather than
    // letting the first save land in slot 0.
    SaveManager.RemoveSaveSlot(slotIndex);
    SaveManager.SelectSlot(slotIndex);
    // A blank but real save: the meta file is what lets every later checkpoint write
    // into this slot.
    SaveManager.SaveGame(GetTree(), slotIndex);
    NavigateToLevelScreen(LevelDispatcher.LEVELS[0].Id);
  }

  private void _internalOnMenuButtonPressed(MenuAction menuButton) {
    if (_screenState != MenuScreenState.Entered) {
      return;
    }

    if (!OnMenuButtonPressed(menuButton) && menuButton == MenuAction.GoBack) {
      NavigateBack();
    }
  }

  // Back unwinds the navigation history. The target is resolved now rather than when
  // the exit transition lands, so a screen always leaves for the one that was under it
  // at the moment the player asked.
  public void NavigateBack() {
    if (MenuManager.PeekBack() is GameMenus target) {
      NavigateToScreen(target);
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
    _menuBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.MenuActionPressed m) => _internalOnMenuButtonPressed(m.Action));
  }

  private void _disconnectSignals() {
    _menuBinding?.Dispose();
    _menuBinding = null;
  }

  private void _parseTransitionElements() {
    // Disconnect first so a second call can't leave the previous set connected and
    // double-count every signal.
    _clearTransitionElements();
    foreach (var transition in this.FindDescendants<UITransition>()) {
      _transitionElements.Add(transition);
      transition.Connect(UITransition.SignalName.Entered, new Callable(this, nameof(_onTransitionElementEntered)));
      transition.Connect(UITransition.SignalName.Exited, new Callable(this, nameof(_onTransitionElementExited)));
    }
  }

  private void _clearTransitionElements() {
    foreach (var transition in _transitionElements) {
      // A widget that rebuilt itself takes its transitions with it, so some of
      // these may already be on their way out.
      if (!IsInstanceValid(transition)) {
        continue;
      }
      transition.Disconnect(UITransition.SignalName.Entered, new Callable(this, nameof(_onTransitionElementEntered)));
      transition.Disconnect(UITransition.SignalName.Exited, new Callable(this, nameof(_onTransitionElementExited)));
    }
    _transitionElements.Clear();
  }

  // Both walks prune elements that were freed since the last parse first: a freed
  // element can neither be told to move nor ever report back, and one left in the
  // list would leave the screen waiting on a signal that cannot come.
  private void _enterTransitionElements() {
    _transitionElements.RemoveAll(element => !IsInstanceValid(element));
    foreach (var element in _transitionElements) {
      element.Enter();
    }
  }

  private void _exitTransitionElements() {
    _transitionElements.RemoveAll(element => !IsInstanceValid(element));
    foreach (var element in _transitionElements) {
      element.Exit();
    }
    // Everything just told to exit may have been pruned away.
    _settleTransitionState();
  }

  public bool IsInTransitionState() {
    return _screenState != MenuScreenState.Entered;
  }

  private bool _hasNoTransitionElements() {
    return _transitionElements.Count == 0;
  }

  private void _onTransitionElementEntered() {
    _enteredTransitionElementsCount++;
    _settleTransitionState();
  }

  private void _onTransitionElementExited() {
    _exitedTransitionElementsCount++;
    _settleTransitionState();
  }

  // Completion is >= against the current list, never == : the list can shrink while
  // a transition is in flight (a language change rebuilds the title, and rebuilt
  // labels carry no transitions), and an exact match against the shrunken list may
  // already have been overshot - a screen stranded that way swallows every input.
  private void _settleTransitionState() {
    if (_screenState == MenuScreenState.Entering
        && _enteredTransitionElementsCount >= _transitionElements.Count) {
      _screenState = MenuScreenState.Entered;
    }
    else if (_screenState == MenuScreenState.Exiting
        && _exitedTransitionElementsCount >= _transitionElements.Count) {
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
