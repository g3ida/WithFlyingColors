namespace Wfc.Entities.Ui.Menubox;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Entities.World.Player;
using Wfc.Screens;
using Wfc.Screens.MenuManager;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Core.Event.EventHandler;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class Menubox : Control {
  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public IMenuManager MenuManager => this.DependOn<IMenuManager>();

  private enum States { MENU, SUB_MENU_ENTER, SUB_MENU, SUB_MENU_EXIT, EXIT }

  private const float SUB_MENU_POPUP_DURATION = 0.2f;

  private States _currentState = States.MENU;

  private int _activeIndex;
  private Vector2 _playSubMenuPos;

  [NodePath("MenuBox/Spr/PlayBoxButton")]
  private MenuBoxButton _playButtonNode = null!;
  [NodePath("MenuBox/Spr/SettingsBoxButton")]
  private MenuBoxButton _settingsButtonNode = null!;
  [NodePath("MenuBox/Spr/StatsBoxButton")]
  private MenuBoxButton _statsButtonNode = null!;
  [NodePath("MenuBox/Spr/QuitBoxButton")]
  private MenuBoxButton _quitButtonNode = null!;
  private MenuBoxButton[] _buttons = [];

  [NodePath("MenuBox")]
  private CharacterBody2D _menuBoxNode = null!;
  private PlayerRotation _boxRotationNode = null!;

  private Control? _playSubMenuNode;

  [NodePath("MenuBox/Spr")]
  private Sprite2D _spriteNode = null!;

  private Tween _subMenuTweener = null!;

  private Menubox() { }

  // FIXME: make high level. The menu should update the box.
  public void SetPreviousMenu() {
    _currentState = States.MENU;
    if (MenuManager.GetPreviousMenu() == GameMenus.STATS_MENU) {
      _menuBoxNode.Rotate(-Mathf.Pi);
      _activeIndex = 2;
    }
    else if (MenuManager.GetPreviousMenu() == GameMenus.SETTINGS_MENU) {
      _menuBoxNode.Rotate(-Mathf.Pi / 2);
      _activeIndex = 1;
    }
  }

  public override void _Ready() {
    this.WireNodes();
    _boxRotationNode = new PlayerRotation(parent: _menuBoxNode);

    _spriteNode.Texture = MenuboxTextureGenerator.GenerateTexture();
    _buttons = [_playButtonNode, _settingsButtonNode, _statsButtonNode, _quitButtonNode];
    SetButtonsEnabled(false);
    _buttons[_activeIndex].disabled = false;
  }

  public void OnResolved() {
    SetPreviousMenu();
  }

  private void SetButtonsEnabled(bool enabled) {
    foreach (var b in _buttons) {
      b.disabled = !enabled;
    }
  }

  public override void _PhysicsProcess(double delta) {
    _playSubMenuNode?.SetPosition(_playSubMenuPos);

    if (Input.IsActionJustPressed("rotate_left") || Input.IsActionJustPressed("ui_left")) {
      OnLeftButtonPressed();
    }
    else if (Input.IsActionJustPressed("rotate_right") || Input.IsActionJustPressed("ui_right")) {
      OnRightButtonPressed();
    }
    else if (Input.IsActionJustPressed("ui_accept")) {
      ClickOnActiveButton();
    }
  }

  public void HideSubMenuIfNeeded() {
    if (_currentState is States.SUB_MENU or States.SUB_MENU_ENTER) {
      DisplayOrHidePlaySubMenu(false);
    }
  }

  public void OnRightButtonPressed() {
    HideSubMenuIfNeeded();
    if (_boxRotationNode.Fire(Constants.PI2, 0.1f, forceRotationIfBusy: false)) {
      _buttons[_activeIndex].disabled = true;
      _activeIndex = (_activeIndex - 1 + _buttons.Length) % _buttons.Length;
      _buttons[_activeIndex].disabled = false;
      EventHandler.Instance.EmitMenuBoxRotated();
    }
  }

  public void OnLeftButtonPressed() {
    HideSubMenuIfNeeded();
    if (_boxRotationNode.Fire(-Constants.PI2, 0.1f, forceRotationIfBusy: false)) {
      _buttons[_activeIndex].disabled = true;
      _activeIndex = (_activeIndex + 1) % _buttons.Length;
      _buttons[_activeIndex].disabled = false;
      EventHandler.Instance.EmitMenuBoxRotated();
    }
  }

  public void OnPlayButtonPressed() {
    if (!CanRespondToInput()) {
      return;
    }

    if (_currentState is States.MENU or States.SUB_MENU_EXIT) {
      DisplayOrHidePlaySubMenu(true);
      EventHandler.Instance.EmitMenuButtonPressed(MenuButtons.PLAY);
    }
  }

  public void OnQuitButtonPressed() => ProcessButtonPress(MenuButtons.QUIT);

  public void OnSettingsButtonPressed() => ProcessButtonPress(MenuButtons.SETTINGS);

  public void OnStatsButtonPressed() => ProcessButtonPress(MenuButtons.STATS);

  private void ProcessButtonPress(MenuButtons menuButton) {
    if (!CanRespondToInput()) {
      return;
    }
    _currentState = States.EXIT;
    EventHandler.Instance.EmitMenuButtonPressed(menuButton);
  }

  private void ClickOnActiveButton() {
    if (_currentState is States.SUB_MENU or States.SUB_MENU_ENTER) {
      return;
    }
    if (_buttons[_activeIndex] == _playButtonNode) {
      OnPlayButtonPressed();
    }
    else if (_buttons[_activeIndex] == _quitButtonNode) {
      OnQuitButtonPressed();
    }
    else if (_buttons[_activeIndex] == _settingsButtonNode) {
      OnSettingsButtonPressed();
    }
    else if (_buttons[_activeIndex] == _statsButtonNode) {
      OnStatsButtonPressed();
    }
  }

  private void DisplayOrHidePlaySubMenu(bool shouldShow = true) {
    if (_playSubMenuNode == null) {
      _playSubMenuNode = new PlaySubMenu();
      _menuBoxNode.AddChild(_playSubMenuNode);
      _playSubMenuNode.Owner = _menuBoxNode;
      _currentState = States.SUB_MENU_ENTER;
      var sz = _playSubMenuNode.CustomMinimumSize;
      _playSubMenuNode.Position = sz * new Vector2(-0.5f, -1);

      var spriteHeight = _spriteNode.Texture.GetHeight();
      var source = _playSubMenuNode.Position;
      var destination = source + (Vector2.Up * spriteHeight * 0.5f);
      if (shouldShow) {
        InterpolateSubMenu(source, destination);
      }
    }
    else if (!shouldShow) {
      var sz = _playSubMenuNode.CustomMinimumSize;
      var destination = new Vector2(-sz.X * 0.5f, -sz.Y);
      var source = _playSubMenuPos;
      _currentState = States.SUB_MENU_EXIT;
      InterpolateSubMenu(source, destination);
    }
  }

  private void InterpolateSubMenu(Vector2 source, Vector2 destination) {
    _subMenuTweener?.Kill();
    _subMenuTweener = CreateTween();
    _subMenuTweener.Connect("finished", new Callable(this, nameof(SubmenuTweenCompleted)), (uint)ConnectFlags.OneShot);

    _playSubMenuPos = source;
    _subMenuTweener.TweenProperty(this, nameof(_playSubMenuPos), destination, SUB_MENU_POPUP_DURATION)
        .From(_playSubMenuPos)
        .SetTrans(Tween.TransitionType.Linear)
        .SetEase(Tween.EaseType.InOut);
  }

  private void SubmenuTweenCompleted() {
    if (_currentState == States.SUB_MENU_ENTER) {
      _currentState = States.SUB_MENU;
    }
    else if (_currentState == States.SUB_MENU_EXIT) {
      _currentState = States.MENU;
      _playSubMenuNode?.QueueFree();
      _playSubMenuNode = null;
    }
  }

  private void OnOutsideButtonPressed() => HideSubMenuIfNeeded();

  private bool CanRespondToInput() {
    var isInTransition = GetParent<GameMenu>().IsInTransitionState();
    return _currentState != States.EXIT && !isInTransition;
  }
}
