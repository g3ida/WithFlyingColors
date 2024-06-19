using Godot;
using System;
using System.Collections.Generic;

public partial class GameMenu : Control {
  public enum MenuScreenState {
    ENTERING,
    ENTERED,
    EXITING,
    EXITED
  }

  protected MenuScreenState screenState;
  private MenuManager.Menus destination_screen;
  private Node current_focus = null;
  public bool HandleBackEvent = true;

  private List<UITransition> transition_elements = new List<UITransition>();
  private int entered_transition_elements_count = 0;

  public override void _EnterTree() {
    ConnectSignals();
    ParseTransitionElements();
    screenState = HasNoTransitionElements() ? MenuScreenState.ENTERED : MenuScreenState.ENTERING;
    OnEnter();
  }

  public override void _ExitTree() {
    DisconnectSignals();
    OnExit();
  }

  public override void _Ready() {
    EnterTransitionElements();
    OnReady();
  }

  public virtual void OnReady() {
    // Override this method in derived classes.
  }

  public override void _Process(double delta) {
    var focus_owner = GetViewport().GuiGetFocusOwner();
    if (focus_owner != null && focus_owner != current_focus) {
      Event.Instance.EmitSignal("focus_changed");
    }
    current_focus = focus_owner;
    OnProcess(delta);
  }

  public virtual void OnProcess(double delta) {
    // Override this method in derived classes.
  }

  public override void _Input(InputEvent ev) {
    if (screenState == (int)MenuScreenState.ENTERING || screenState == MenuScreenState.EXITING) {
      GetViewport().SetInputAsHandled();
    }

    if (HandleBackEvent && (Input.IsActionJustPressed("ui_cancel") || Input.IsActionJustPressed("ui_home"))) {
      Event.Instance.EmitMenuButtonPressed(MenuButtons.BACK);
    }
  }

  public void NavigateToScreen(MenuManager.Menus menu_screen) {
    if (screenState == MenuScreenState.ENTERING || screenState == MenuScreenState.ENTERED) {
      destination_screen = menu_screen;
      if (HasNoTransitionElements()) {
        screenState = MenuScreenState.EXITED;
        MenuManager.Instance().GoToMenu(destination_screen);
      }
      else {
        screenState = MenuScreenState.EXITING;
        StopProcessInput();
        ExitTransitionElements();
      }
      OnExit();
    }
  }

  public void NavigateToLevelScreen(string level_screen_path) {
    if (string.IsNullOrEmpty(level_screen_path))
      return;
    MenuManager.Instance().SetCurrentLevel(level_screen_path);
    NavigateToScreen(MenuManager.Menus.GAME);
  }

  private void _OnMenuButtonPressed(MenuButtons menu_button) {
    if (screenState != MenuScreenState.ENTERED)
      return;

    if (!on_menu_button_pressed(menu_button) && menu_button == MenuButtons.BACK) {
      NavigateToScreen(MenuManager.Instance().PreviousMenu);
    }
  }

  public virtual bool on_menu_button_pressed(MenuButtons menu_button) {
    return false;
  }

  public virtual void OnEnter() {
    // Override this method in derived classes.
  }

  public virtual void OnExit() {
    // Override this method in derived classes.
  }

  private void ConnectSignals() {
    Event.Instance.Connect("menu_button_pressed", new Callable(this, nameof(_OnMenuButtonPressed)));
  }

  private void DisconnectSignals() {
    Event.Instance.Disconnect("menu_button_pressed", new Callable(this, nameof(_OnMenuButtonPressed)));
  }

  private void ParseTransitionElements() {
    transition_elements.Clear();
    foreach (Node ch in GetChildren()) {
      foreach (Node grandchild in ch.GetChildren()) {
        if (grandchild is UITransition transition) {
          transition_elements.Add(transition);
          transition.Connect("entered", new Callable(this, nameof(OnTransitionElementEntered)));
          transition.Connect("exited", new Callable(this, nameof(OnTransitionElementExited)));
          break;
        }
      }
    }
  }

  private void ClearTransitionElements() {
    foreach (var transition in transition_elements) {
      transition.Disconnect("entered", new Callable(this, nameof(OnTransitionElementEntered)));
      transition.Disconnect("exited", new Callable(this, nameof(OnTransitionElementExited)));
    }
  }

  private void EnterTransitionElements() {
    foreach (var element in transition_elements) {
      element.Enter();
    }
  }

  private void ExitTransitionElements() {
    foreach (var element in transition_elements) {
      element.Exit();
    }
  }

  public bool IsInTransitionState() {
    return screenState != MenuScreenState.ENTERED;
  }

  private bool HasNoTransitionElements() {
    return transition_elements.Count == 0;
  }

  private void OnTransitionElementEntered() {
    entered_transition_elements_count++;
    if (entered_transition_elements_count == transition_elements.Count) {
      screenState = MenuScreenState.ENTERED;
    }
  }

  private void OnTransitionElementExited() {
    entered_transition_elements_count--;
    if (entered_transition_elements_count == 0) {
      screenState = MenuScreenState.EXITED;
      MenuManager.Instance().GoToMenu(destination_screen);
    }
  }

  private void StopProcessInput(Node node = null) {
    node = node ?? this;
    foreach (Node ch in node.GetChildren()) {
      if (ch is Control control) {
        control.SetProcessInput(false);
      }
      StopProcessInput(ch);
    }
  }
}
