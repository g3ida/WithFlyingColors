namespace Wfc.Entities.Ui.Menubox;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.Core.Persistence;
using Wfc.Entities.World.Player;
using Wfc.Screens;
using Wfc.Screens.MenuManager;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[Meta(typeof(IAutoNode))]
public partial class Menubox : Control {
  #region Dependencies
  public override void _Notification(int what) => this.Notify(what);
  [Dependency]
  public IMenuManager MenuManager => this.DependOn<IMenuManager>();
  [Dependency]
  public IInputManager InputManager => this.DependOn<IInputManager>();
  [Dependency]
  public ISaveManager SaveManager => this.DependOn<ISaveManager>();
  #endregion Dependencies

  #region Types

  private enum States { MENU, SUB_MENU_ENTER, SUB_MENU, SUB_MENU_EXIT, EXIT }
  #endregion Types

  #region Constants
  private const float SUB_MENU_POPUP_DURATION = 0.2f;
  private const float ROTATION_DURATION = 0.1f;
  private const int TURN_CLOCKWISE = 1;
  private const int TURN_COUNTER_CLOCKWISE = -1;
  #endregion Constants

  #region Fields
  // The same quarter-turn the player's cube uses, stepped from _PhysicsProcess below.
  private readonly PlayerRotationAction _boxRotation = new();
  private Tween _subMenuTweener = null!;
  private States _currentState = States.MENU;
  private int _activeIndex = 0;
  public int ActiveIndex {
    get => _activeIndex;
    set => _setActiveButton(value);
  }
  private Vector2 _playSubMenuPos;
  #endregion Fields

  #region Nodes
  [NodePath("MenuBox/Spr/PlayBoxButton")]
  private MenuBoxButton _playButtonNode = null!;
  [NodePath("MenuBox/Spr/SettingsBoxButton")]
  private MenuBoxButton _settingsButtonNode = null!;
  [NodePath("MenuBox/Spr/CreditsBoxButton")]
  private MenuBoxButton _creditsButtonNode = null!;
  [NodePath("MenuBox/Spr/QuitBoxButton")]
  private MenuBoxButton _quitButtonNode = null!;
  private MenuBoxButton[] _buttons = [];
  [NodePath("MenuBox")]
  private CharacterBody2D _menuBoxNode = null!;
  private Control? _playSubMenuNode;

  [NodePath("MenuBox/Spr")]
  private Sprite2D _spriteNode = null!;
  #endregion Nodes

  // Comes back facing the button the player went out through, so returning from the
  // settings doesn't spin the box back round to Play.
  // FIXME: make high level. The menu should update the box.
  public void FaceLastVisitedMenu() {
    _currentState = States.MENU;
    switch (MenuManager.GetLastVisitedMenu()) {
      case GameMenus.CREDITS_MENU:
        _menuBoxNode.Rotate(-Mathf.Pi);
        ActiveIndex = 2;
        break;
      case GameMenus.SETTINGS_MENU:
        _menuBoxNode.Rotate(-Mathf.Pi / 2);
        ActiveIndex = 1;
        break;
      default:
        ActiveIndex = 0;
        break;
    }
    // Turning the box here is a teleport rather than a rotation, so the angle the next
    // press measures from has to be told about it.
    _boxRotation.Reset(_menuBoxNode.Rotation);
  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _boxRotation.SetBody(_menuBoxNode);

    _spriteNode.Texture = MenuboxTextureGenerator.GenerateTexture();
    _buttons = [_playButtonNode, _settingsButtonNode, _creditsButtonNode, _quitButtonNode];
  }

  public void OnResolved() {
    FaceLastVisitedMenu();
  }

  private void _setActiveButton(int index) {
    _activeIndex = index;
    _setButtonsEnabled(false);
    _buttons[_activeIndex].disabled = false;
  }

  private void _setButtonsEnabled(bool enabled) {
    foreach (var b in _buttons) {
      b.disabled = !enabled;
    }
  }

  // UICancel is deliberately absent: the screen owns back, and MainMenu answers it by
  // closing this sub-menu. Handling it here as well meant one key press got two
  // answers.
  public override void _PhysicsProcess(double delta) {
    _boxRotation.Step((float)delta);
    _playSubMenuNode?.SetPosition(_playSubMenuPos);

    if (InputManager.IsJustPressed(IInputManager.Action.RotateLeft)
      || InputManager.IsJustPressed(IInputManager.Action.UILeft)
      || InputManager.IsJustPressed(IInputManager.Action.UITabPrevious)) {
      OnLeftButtonPressed();
    }
    else if (InputManager.IsJustPressed(IInputManager.Action.RotateRight)
      || InputManager.IsJustPressed(IInputManager.Action.UIRight)
      || InputManager.IsJustPressed(IInputManager.Action.UITabNext)) {
      OnRightButtonPressed();
    }
    else if (InputManager.IsJustPressed(IInputManager.Action.UIConfirm)) {
      _clickOnActiveButton();
    }
  }

  private bool _isSubMenuDisplayed() => _currentState is States.SUB_MENU or States.SUB_MENU_ENTER;

  public void HideSubMenuIfNeeded() {
    if (_isSubMenuDisplayed()) {
      _displayOrHidePlaySubMenu(false);
    }
  }

  public void OnRightButtonPressed() {
    if (!CanRespondToInput()) {
      return;
    }
    HideSubMenuIfNeeded();
    if (_boxRotation.Execute(TURN_CLOCKWISE, MathUtils.PI2, ROTATION_DURATION, shouldForce: false)) {
      _setActiveButton((_activeIndex - 1 + _buttons.Length) % _buttons.Length);
      GameEvents.Instance.OnMenuBoxRotated();
    }
  }

  public void OnLeftButtonPressed() {
    if (!CanRespondToInput()) {
      return;
    }
    HideSubMenuIfNeeded();
    if (_boxRotation.Execute(TURN_COUNTER_CLOCKWISE, MathUtils.PI2, ROTATION_DURATION, shouldForce: false)) {
      _setActiveButton((_activeIndex + 1) % _buttons.Length);
      GameEvents.Instance.OnMenuBoxRotated();
    }
  }

  public void OnPlayButtonPressed() {
    if (!CanRespondToInput()) {
      return;
    }
    if (_currentState is States.MENU or States.SUB_MENU_EXIT) {
      // With every slot empty there is nothing to continue or load, so Play can only
      // mean one thing and the sub-menu would be a detour: the screen starts the
      // game directly off the emitted action.
      if (SaveManager.HasNoSaves()) {
        _currentState = States.EXIT;
      }
      else {
        _displayOrHidePlaySubMenu(true);
      }
      GameEvents.Instance.OnMenuActionPressed(MenuAction.Play);
    }
  }

  public void OnQuitButtonPressed() => _processButtonPress(MenuAction.Quit);

  public void OnSettingsButtonPressed() => _processButtonPress(MenuAction.GoToSettings);

  public void OnCreditsButtonPressed() => _processButtonPress(MenuAction.GoToCredits);

  private void _processButtonPress(MenuAction menuAction) {
    if (!CanRespondToInput()) {
      return;
    }
    _currentState = States.EXIT;
    GameEvents.Instance.OnMenuActionPressed(menuAction);
  }

  private void _clickOnActiveButton() {
    // Only from the resting state. During the sub-menu's exit tween one of its items
    // still holds focus, so ui_accept would press both that item and the box button
    // behind it.
    if (_currentState != States.MENU) {
      return;
    }
    if (_buttons[ActiveIndex] == _playButtonNode) {
      OnPlayButtonPressed();
    }
    else if (_buttons[ActiveIndex] == _quitButtonNode) {
      OnQuitButtonPressed();
    }
    else if (_buttons[ActiveIndex] == _settingsButtonNode) {
      OnSettingsButtonPressed();
    }
    else if (_buttons[ActiveIndex] == _creditsButtonNode) {
      OnCreditsButtonPressed();
    }
  }

  private void _displayOrHidePlaySubMenu(bool shouldShow = true) {
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
        _interpolateSubMenu(source, destination);
      }
    }
    else if (!shouldShow) {
      var sz = _playSubMenuNode.CustomMinimumSize;
      var destination = new Vector2(-sz.X * 0.5f, -sz.Y);
      var source = _playSubMenuPos;
      _currentState = States.SUB_MENU_EXIT;
      _interpolateSubMenu(source, destination);
    }
  }

  private void _interpolateSubMenu(Vector2 source, Vector2 destination) {
    _subMenuTweener?.Kill();
    _subMenuTweener = CreateTween();
    _subMenuTweener.Connect(
      Tween.SignalName.Finished,
      new Callable(this, nameof(_submenuTweenCompleted)),
      (uint)ConnectFlags.OneShot
    );

    _playSubMenuPos = source;
    _subMenuTweener.TweenProperty(this, nameof(_playSubMenuPos), destination, SUB_MENU_POPUP_DURATION)
        .From(_playSubMenuPos)
        .SetTrans(Tween.TransitionType.Linear)
        .SetEase(Tween.EaseType.InOut);
  }

  private void _submenuTweenCompleted() {
    if (_currentState == States.SUB_MENU_ENTER) {
      _currentState = States.SUB_MENU;
    }
    else if (_currentState == States.SUB_MENU_EXIT) {
      _currentState = States.MENU;
      _playSubMenuNode?.QueueFree();
      _playSubMenuNode = null;
    }
  }

  private void _onOutsideButtonPressed() => HideSubMenuIfNeeded();

  // Null parent screen when the box is previewed outside a menu, in which case there
  // is no transition to wait on.
  private bool CanRespondToInput() {
    var screen = GetParent() as GameMenu;
    return _currentState != States.EXIT && screen?.IsInTransitionState() != true;
  }
}
