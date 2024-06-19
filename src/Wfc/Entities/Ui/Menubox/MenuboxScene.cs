using Godot;
using Wfc.Entities.World.Player;
using Wfc.Utils.Attributes;

namespace Wfc.Entities.Ui.Menubox
{
  [ScenePath("res://Assets/Scenes/MainMenu/MenuBox.tscn")]
  public partial class MenuboxScene : Control
  {
    private enum States { MENU, SUB_MENU_ENTER, SUB_MENU, SUB_MENU_EXIT, EXIT }

    private const float SUB_MENU_POPUP_DURATION = 0.2f;

    private States _currentState = States.MENU;

    private int _activeIndex = 0;
    private Vector2 _playSubMenuPos;

    [NodePath("MenuBox/Spr/PlayBoxButton")]
    private MenuBoxButton _playButtonNode;
    [NodePath("MenuBox/Spr/SettingsBoxButton")]
    private MenuBoxButton _settingsButtonNode;
    [NodePath("MenuBox/Spr/StatsBoxButton")]
    private MenuBoxButton _statsButtonNode;
    [NodePath("MenuBox/Spr/QuitBoxButton")]
    private MenuBoxButton _quitButtonNode;
    private MenuBoxButton[] _buttons;

    [NodePath("MenuBox")]
    private CharacterBody2D _menuBoxNode;
    private PlayerRotation _boxRotationNode;

    private Control _playSubMenuNode = null;

    [NodePath("MenuBox/Spr")]
    private Sprite2D _spriteNode;

    private Tween _subMenuTweener;

    // FIXME: make high level. The menu should update the box.
    public void SetPreviousMenu()
    {
      _currentState = States.MENU;
      if (MenuManager.Instance().PreviousMenu == MenuManager.Menus.STATS_MENU)
      {
        _menuBoxNode.Rotate(-Mathf.Pi);
        _activeIndex = 2;
      }
      else if (MenuManager.Instance().PreviousMenu == MenuManager.Menus.SETTINGS_MENU)
      {
        _menuBoxNode.Rotate(-Mathf.Pi / 2);
        _activeIndex = 1;
      }
    }

    public override void _Ready()
    {
      this.WireNodes();
      _boxRotationNode = new PlayerRotation(parent: _menuBoxNode);

      _spriteNode.Texture = MenuboxTextureGenerator.GenerateTexture();
      _buttons = [_playButtonNode, _settingsButtonNode, _statsButtonNode, _quitButtonNode];
      SetPreviousMenu();
      SetButtonsEnabled(false);
      _buttons[_activeIndex].disabled = false;
    }

    private void SetButtonsEnabled(bool enabled)
    {
      foreach (var b in _buttons)
      {
        b.disabled = !enabled;
      }
    }

    public override void _PhysicsProcess(double delta)
    {
      if (_playSubMenuNode != null)
      {
        _playSubMenuNode.SetPosition(_playSubMenuPos);
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
      if (_currentState == States.SUB_MENU || _currentState == States.SUB_MENU_ENTER)
      {
        _DisplayOrHidePlaySubMenu(false);
      }
    }

    public void _on_RightButton_pressed()
    {
      _HideSubMenuIfNeeded();
      if (_boxRotationNode.Fire(Constants.PI2, 0.1f, forceRotationIfBusy: false))
      {
        _buttons[_activeIndex].disabled = true;
        _activeIndex = (_activeIndex - 1 + _buttons.Length) % _buttons.Length;
        _buttons[_activeIndex].disabled = false;
        Event.Instance.EmitMenuBoxRotated();
      }
    }

    public void _on_LeftButton_pressed()
    {
      _HideSubMenuIfNeeded();
      if (_boxRotationNode.Fire(-Constants.PI2, 0.1f, forceRotationIfBusy: false))
      {
        _buttons[_activeIndex].disabled = true;
        _activeIndex = (_activeIndex + 1) % _buttons.Length;
        _buttons[_activeIndex].disabled = false;
        Event.Instance.EmitMenuBoxRotated();
      }
    }

    public void _on_PlayButton_Pressed()
    {
      if (!CanRespondToInput()) return;
      if (_currentState == States.MENU || _currentState == States.SUB_MENU_EXIT)
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
      _currentState = States.EXIT;
      Event.Instance.EmitMenuButtonPressed(menuButton);
    }

    private void ClickOnActiveButton()
    {
      if (_currentState == States.SUB_MENU || _currentState == States.SUB_MENU_ENTER)
      {
        return;
      }
      if (_buttons[_activeIndex] == _playButtonNode)
      {
        _on_PlayButton_Pressed();
      }
      else if (_buttons[_activeIndex] == _quitButtonNode)
      {
        _on_QuitButton_pressed();
      }
      else if (_buttons[_activeIndex] == _settingsButtonNode)
      {
        _on_SettingsButton_pressed();
      }
      else if (_buttons[_activeIndex] == _statsButtonNode)
      {
        _on_StatsButton_pressed();
      }
    }

    private void _DisplayOrHidePlaySubMenu(bool shouldShow = true)
    {
      if (_playSubMenuNode == null)
      {
        // PackedScene PlaySubMenuScene = GD.Load<PackedScene>("res://Assets/Scenes/MainMenu/PlaySubMenu.tscn");
        // _playSubMenuNode = PlaySubMenuScene.Instantiate<Control>();
        // _menuBoxNode.AddChild(_playSubMenuNode);
        // _playSubMenuNode.Owner = _menuBoxNode;

        _playSubMenuNode = new PlaySubMenu();
        _menuBoxNode.AddChild(_playSubMenuNode);
        _playSubMenuNode.Owner = _menuBoxNode;
        _currentState = States.SUB_MENU_ENTER;
        Vector2 sz = _playSubMenuNode.CustomMinimumSize;
        _playSubMenuNode.Position = sz * new Vector2(-0.5f, -1);

        var spriteHeight = _spriteNode.Texture.GetHeight();
        Vector2 source = _playSubMenuNode.Position;
        Vector2 destination = source + Vector2.Up * spriteHeight * 0.5f;
        if (shouldShow)
        {
          InterpolateSubMenu(source, destination);
        }
      }
      else if (!shouldShow)
      {
        Vector2 sz = _playSubMenuNode.CustomMinimumSize;
        Vector2 destination = new Vector2(-sz.X * 0.5f, -sz.Y);
        Vector2 source = _playSubMenuPos;
        _currentState = States.SUB_MENU_EXIT;
        InterpolateSubMenu(source, destination);
      }
    }

    private void InterpolateSubMenu(Vector2 source, Vector2 destination)
    {
      if (_subMenuTweener != null)
      {
        _subMenuTweener.Kill();
      }
      _subMenuTweener = CreateTween();
      _subMenuTweener.Connect("finished", new Callable(this, nameof(_SubmenuTweenCompleted)), (uint)ConnectFlags.OneShot);

      _playSubMenuPos = source;
      _subMenuTweener.TweenProperty(this, nameof(_playSubMenuPos), destination, SUB_MENU_POPUP_DURATION)
          .From(_playSubMenuPos)
          .SetTrans(Tween.TransitionType.Linear)
          .SetEase(Tween.EaseType.InOut);
    }

    private void _SubmenuTweenCompleted()
    {
      if (_currentState == States.SUB_MENU_ENTER)
      {
        _currentState = States.SUB_MENU;
      }
      else if (_currentState == States.SUB_MENU_EXIT)
      {
        _currentState = States.MENU;
        _playSubMenuNode.QueueFree();
        _playSubMenuNode = null;
      }
    }

    private void _on_OutsideButton_pressed()
    {
      _HideSubMenuIfNeeded();
    }

    private bool CanRespondToInput()
    {
      var isInTransition = GetParent<GameMenu>().IsInTransitionState();
      return _currentState != States.EXIT && !isInTransition;
    }
  }
}