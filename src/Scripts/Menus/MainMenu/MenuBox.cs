using Godot;
using System;

public partial class MenuBox : Control
{
  private const float SUB_MENU_POPUP_DURATION = 0.2f;

  private PlayerRotationAction boxRotation;
  private MenuBoxButton[] buttons;
  private int activeIndex = 0;
  private Vector2 playSubMenuPos;

  private enum States { MENU, SUB_MENU_ENTER, SUB_MENU, SUB_MENU_EXIT, EXIT }
  private States currentState = States.MENU;

  private MenuBoxButton playButton;
  private MenuBoxButton settingsButton;
  private MenuBoxButton statsButton;
  private MenuBoxButton quitButton;

  private CharacterBody2D MenuBoxNode;
  private Control PlaySubMenuNode = null;
  private Sprite2D SpriteNode;
  private float SpriteHeight;

  private Tween subMenuTweener;

  public void SetPreviousMenu()
  {
    currentState = States.MENU;
    if (MenuManager.Instance().PreviousMenu == MenuManager.Menus.STATS_MENU)
    {
      MenuBoxNode.Rotate(-Mathf.Pi);
      activeIndex = 2;
    }
    else if (MenuManager.Instance().PreviousMenu == MenuManager.Menus.SETTINGS_MENU)
    {
      MenuBoxNode.Rotate(-Mathf.Pi / 2);
      activeIndex = 1;
    }
  }

  public override void _Ready()
  {
    MenuBoxNode = GetNode<CharacterBody2D>("MenuBox");
    boxRotation = new PlayerRotationAction();
    boxRotation.Set(MenuBoxNode);

    playButton = GetNode<MenuBoxButton>("MenuBox/Spr/PlayBoxButton");
    settingsButton = GetNode<MenuBoxButton>("MenuBox/Spr/SettingsBoxButton");
    statsButton = GetNode<MenuBoxButton>("MenuBox/Spr/StatsBoxButton");
    quitButton = GetNode<MenuBoxButton>("MenuBox/Spr/QuitBoxButton");

    SpriteNode = GetNode<Sprite2D>("MenuBox/Spr");
    SpriteHeight = SpriteNode.Texture.GetHeight();

    buttons = new MenuBoxButton[] { playButton, settingsButton, statsButton, quitButton };


    SetPreviousMenu();
    SetButtonsEnabled(false);
    buttons[activeIndex].disabled = false;
  }

  private void SetButtonsEnabled(bool enabled)
  {
    foreach (var b in buttons)
    {
      b.disabled = !enabled;
    }
  }

  public override void _PhysicsProcess(double delta)
  {
    boxRotation.Step((float)delta);

    if (PlaySubMenuNode != null)
    {
      PlaySubMenuNode.SetPosition(playSubMenuPos);
    }

    if (Input.IsActionJustPressed("rotate_left") || Input.IsActionJustPressed("ui_left"))
    {
      _on_LeftButton_pressed();
    }
    else if (Input.IsActionJustPressed("rotate_right") || Input.IsActionJustPressed("ui_right"))
    {
      _on_RightButton_pressed();
    }
    else if (Input.IsActionJustPressed("ui_accept"))
    {
      ClickOnActiveButton();
    }
  }

  public void _HideSubMenuIfNeeded()
  {
    if (currentState == States.SUB_MENU || currentState == States.SUB_MENU_ENTER)
    {
      _DisplayOrHidePlaySubMenu(false);
    }
  }

  public void _on_RightButton_pressed()
  {
    _HideSubMenuIfNeeded();
    if (boxRotation.Execute(1, Constants.PI2, 0.1f, false, true, true))
    {
      buttons[activeIndex].disabled = true;
      activeIndex = (activeIndex - 1 + buttons.Length) % buttons.Length;
      buttons[activeIndex].disabled = false;
      Event.Instance.EmitMenuBoxRotated();
    }
  }

  public void _on_LeftButton_pressed()
  {
    _HideSubMenuIfNeeded();
    if (boxRotation.Execute(-1, Constants.PI2, 0.1f, false, true, true))
    {
      buttons[activeIndex].disabled = true;
      activeIndex = (activeIndex + 1) % buttons.Length;
      buttons[activeIndex].disabled = false;
      Event.Instance.EmitMenuBoxRotated();
    }
  }

  public void _on_PlayButton_Pressed()
  {
    if (!CanRespondToInput()) return;
    if (currentState == States.MENU || currentState == States.SUB_MENU_EXIT)
    {
      _DisplayOrHidePlaySubMenu(true);
      Event.Instance.EmitMenuButtonPressed(MenuButtons.PLAY);
    }
  }

  public void _on_QuitButton_pressed()
  {
    _ProcessButtonPress(MenuButtons.QUIT);
  }

  public void _on_SettingsButton_pressed()
  {
    _ProcessButtonPress(MenuButtons.SETTINGS);
  }

  public void _on_StatsButton_pressed()
  {
    _ProcessButtonPress(MenuButtons.STATS);
  }

  private void _ProcessButtonPress(MenuButtons menuButton)
  {
    if (!CanRespondToInput()) return;
    currentState = States.EXIT;
    Event.Instance.EmitMenuButtonPressed(menuButton);
  }

  private void ClickOnActiveButton()
  {
    if (currentState == States.SUB_MENU || currentState == States.SUB_MENU_ENTER)
    {
      return;
    }
    if (buttons[activeIndex] == playButton)
    {
      _on_PlayButton_Pressed();
    }
    else if (buttons[activeIndex] == quitButton)
    {
      _on_QuitButton_pressed();
    }
    else if (buttons[activeIndex] == settingsButton)
    {
      _on_SettingsButton_pressed();
    }
    else if (buttons[activeIndex] == statsButton)
    {
      _on_StatsButton_pressed();
    }
  }

  private void _DisplayOrHidePlaySubMenu(bool shouldShow = true)
  {
    if (PlaySubMenuNode == null)
    {
      PackedScene PlaySubMenuScene = GD.Load<PackedScene>("res://Assets/Scenes/MainMenu/PlaySubMenu.tscn");
      PlaySubMenuNode = (Control)PlaySubMenuScene.Instantiate();
      MenuBoxNode.AddChild(PlaySubMenuNode);
      PlaySubMenuNode.Owner = MenuBoxNode;
      Vector2 sz = ((Control)PlaySubMenuNode).CustomMinimumSize;
      Vector2 source = new Vector2(-sz.X * 0.5f, -sz.Y);
      Vector2 destination = source + Vector2.Up * SpriteHeight * 0.5f;
      if (shouldShow)
      {
        currentState = States.SUB_MENU_ENTER;
        InterpolateSubMenu(source, destination);
      }
    }
    else if (!shouldShow)
    {
      Vector2 sz = ((Control)PlaySubMenuNode).CustomMinimumSize;
      Vector2 destination = new Vector2(-sz.X * 0.5f, -sz.Y);
      Vector2 source = playSubMenuPos;
      currentState = States.SUB_MENU_EXIT;
      InterpolateSubMenu(source, destination);
    }
  }

  private void InterpolateSubMenu(Vector2 source, Vector2 destination)
  {
    if (subMenuTweener != null)
    {
      subMenuTweener.Kill();
    }
    subMenuTweener = CreateTween();
    subMenuTweener.Connect("finished", new Callable(this, nameof(_SubmenuTweenCompleted)), (uint)ConnectFlags.OneShot);

    playSubMenuPos = source;
    subMenuTweener.TweenProperty(this, nameof(playSubMenuPos), destination, SUB_MENU_POPUP_DURATION)
        .From(playSubMenuPos)
        .SetTrans(Tween.TransitionType.Linear)
        .SetEase(Tween.EaseType.InOut);
  }

  private void _SubmenuTweenCompleted()
  {
    if (currentState == States.SUB_MENU_ENTER)
    {
      currentState = States.SUB_MENU;
    }
    else if (currentState == States.SUB_MENU_EXIT)
    {
      currentState = States.MENU;
      PlaySubMenuNode.QueueFree();
      PlaySubMenuNode = null;
    }
  }

  private void _on_OutsideButton_pressed()
  {
    _HideSubMenuIfNeeded();
  }

  private bool CanRespondToInput()
  {
    // FIXME make a better check after c# migration
    var isInTransition = GetParent<GameMenu>().IsInTransitionState();
    return currentState != States.EXIT && !isInTransition;
  }
}

