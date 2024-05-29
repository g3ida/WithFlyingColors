using Godot;
using System;
using System.Collections.Generic;

public class GameMenu : Control
{
    public enum MenuScreenState
    {
        ENTERING,
        ENTERED,
        EXITING,
        EXITED
    }

    private const int LEVEL_SCREEN_IDX = -1;

    protected MenuScreenState screenState;
    private MenuManager.Menus destination_screen;
    private string destination_scene_path;
    private Node current_focus = null;
    private bool handle_back_event = true;

    private List<UITransition> transition_elements = new List<UITransition>();
    private int entered_transition_elements_count = 0;

    public override void _EnterTree()
    {
        ConnectSignals();
        ParseTransitionElements();
        screenState = HasNoTransitionElements() ? MenuScreenState.ENTERED : MenuScreenState.ENTERING;
        OnEnter();
    }

    public override void _ExitTree()
    {
        DisconnectSignals();
        OnExit();
    }

    public override void _Ready()
    {
        EnterTransitionElements();
        Ready();
    }

    public virtual void Ready()
    {
        // Override this method in derived classes.
    }

    public override void _Process(float delta)
    {
        var focus_owner = GetFocusOwner();
        if (focus_owner != null && focus_owner != current_focus)
        {
            Event.Instance().EmitSignal("focus_changed");
        }
        current_focus = focus_owner;
        Process(delta);
    }

    public virtual void Process(float delta)
    {
        // Override this method in derived classes.
    }

    public override void _Input(InputEvent @event)
    {
        if (screenState == (int)MenuScreenState.ENTERING || screenState == MenuScreenState.EXITING)
        {
            GetTree().SetInputAsHandled();
        }

        if (handle_back_event && (Input.IsActionJustPressed("ui_cancel") || Input.IsActionJustPressed("ui_home")))
        {
            Event.Instance().EmitMenuButtonPressed(MenuButtons.BACK);
        }
    }

    public void NavigateToScreen(MenuManager.Menus menu_screen)
    {
        if (screenState == MenuScreenState.ENTERING || screenState == MenuScreenState.ENTERED)
        {
            destination_screen = menu_screen;
            if (HasNoTransitionElements())
            {
                screenState = MenuScreenState.EXITED;
                MenuManager.Instance().GoToMenu(destination_screen);
            }
            else
            {
                screenState = MenuScreenState.EXITING;
                StopProcessInput();
                ExitTransitionElements();
            }
            OnExit();
        }
    }

    public void NavigateToLevelScreen(string level_screen_path)
    {
        if (string.IsNullOrEmpty(level_screen_path)) return;
        MenuManager.Instance().SetCurrentLevel(level_screen_path);
        NavigateToScreen(MenuManager.Menus.GAME);
    }

    private void _OnMenuButtonPressed(MenuButtons menu_button)
    {
        if (screenState != MenuScreenState.ENTERED) return;

        if (!OnMenuButtonPressed(menu_button) && menu_button == MenuButtons.BACK)
        {
            NavigateToScreen(MenuManager.Instance().PreviousMenu);
        }
    }

    public virtual bool OnMenuButtonPressed(MenuButtons menu_button)
    {
        return false;
    }

    public virtual void OnEnter()
    {
        // Override this method in derived classes.
    }

    public virtual void OnExit()
    {
        // Override this method in derived classes.
    }

    private void ConnectSignals()
    {
        Event.GdInstance().Connect("menu_button_pressed", this, nameof(_OnMenuButtonPressed));
    }

    private void DisconnectSignals()
    {
        Event.GdInstance().Disconnect("menu_button_pressed", this, nameof(_OnMenuButtonPressed));
    }

    private void ParseTransitionElements()
    {
        transition_elements.Clear();
        foreach (Node ch in GetChildren())
        {
            foreach (Node chch in ch.GetChildren())
            {
                if (chch is UITransition transition)
                {
                    transition_elements.Add(transition);
                    transition.Connect("entered", this, nameof(OnTransitionElementEntered));
                    transition.Connect("exited", this, nameof(OnTransitionElementExited));
                    break;
                }
            }
        }
    }

    private void ClearTransitionElements()
    {
        foreach (var transition in transition_elements)
        {
            transition.Disconnect("entered", this, nameof(OnTransitionElementEntered));
            transition.Disconnect("exited", this, nameof(OnTransitionElementExited));
        }
    }

    private void EnterTransitionElements()
    {
        foreach (var element in transition_elements)
        {
            element.Enter();
        }
    }

    private void ExitTransitionElements()
    {
        foreach (var element in transition_elements)
        {
            element.Exit();
        }
    }

    private bool IsInTransitionState()
    {
        return screenState != MenuScreenState.ENTERED;
    }

    private bool HasNoTransitionElements()
    {
        return transition_elements.Count == 0;
    }

    private void OnTransitionElementEntered()
    {
        entered_transition_elements_count++;
        if (entered_transition_elements_count == transition_elements.Count)
        {
            screenState = MenuScreenState.ENTERED;
        }
    }

    private void OnTransitionElementExited()
    {
        entered_transition_elements_count--;
        if (entered_transition_elements_count == 0)
        {
            screenState = MenuScreenState.EXITED;
            // if (destination_screen == LEVEL_SCREEN_IDX)
            // {
            //     MenuManager.Instance().GoToScreen(destination_scene_path);
            // }
            // else
            // {
                MenuManager.Instance().GoToMenu(destination_screen);
            // }
        }
    }

    private void StopProcessInput(Node node = null)
    {
        node = node ?? this;
        foreach (Node ch in node.GetChildren())
        {
            if (ch is Control control)
            {
                control.SetProcessInput(false);
            }
            StopProcessInput(ch);
        }
    }
}
