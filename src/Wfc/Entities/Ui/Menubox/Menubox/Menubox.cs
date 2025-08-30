namespace Wfc.Entities.Ui.Menubox;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Input;
using Wfc.Entities.World.Player;
using Wfc.Screens;
using Wfc.Screens.MenuManager;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Core.Event.EventHandler;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class Menubox : Control {
  #region Dependencies
  public override void _Notification(int what) => this.Notify(what);
  [Dependency]
  public IMenuManager MenuManager => this.DependOn<IMenuManager>();
  [Dependency]
  public IInputManager InputManager => this.DependOn<IInputManager>();
  #endregion Dependencies

  #region Types

  private enum States { MENU, SUB_MENU_ENTER, SUB_MENU, SUB_MENU_EXIT, EXIT }
  #endregion Types

  #region Constants
  private const float SUB_MENU_POPUP_DURATION = 0.2f;
  #endregion Constants

  #region Fields
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
  #endregion Nodes

  // FIXME: make high level. The menu should update the box.
  public void SetPreviousMenu() {
    _currentState = States.MENU;
    if (MenuManager.GetPreviousMenu() == GameMenus.STATS_MENU) {
      _menuBoxNode.Rotate(-Mathf.Pi);
      ActiveIndex = 2;
    }
    else if (MenuManager.GetPreviousMenu() == GameMenus.SETTINGS_MENU) {
      _menuBoxNode.Rotate(-Mathf.Pi / 2);
      ActiveIndex = 1;
    }
    else {
      ActiveIndex = 0;
    }
  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _boxRotationNode = new PlayerRotation(parent: _menuBoxNode);

    _spriteNode.Texture = MenuboxTextureGenerator.GenerateTexture();
    _buttons = [_playButtonNode, _settingsButtonNode, _statsButtonNode, _quitButtonNode];
  }

  public void OnResolved() {
    SetPreviousMenu();
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

  public override void _PhysicsProcess(double delta) {
    _playSubMenuNode?.SetPosition(_playSubMenuPos);

    if (InputManager.IsJustPressed(IInputManager.Action.UICancel)) {
      HideSubMenuIfNeeded();
    }
    else if (InputManager.IsJustPressed(IInputManager.Action.RotateLeft) || InputManager.IsJustPressed(IInputManager.Action.UILeft)) {
      OnLeftButtonPressed();
    }
    else if (InputManager.IsJustPressed(IInputManager.Action.RotateRight) || InputManager.IsJustPressed(IInputManager.Action.UIRight)) {
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
    HideSubMenuIfNeeded();
    if (_boxRotationNode.Fire(MathUtils.PI2, 0.1f, forceRotationIfBusy: false)) {
      _setActiveButton((_activeIndex - 1 + _buttons.Length) % _buttons.Length);
      EventHandler.Instance.EmitMenuBoxRotated();
    }
  }

  public void OnLeftButtonPressed() {
    HideSubMenuIfNeeded();
    if (_boxRotationNode.Fire(-MathUtils.PI2, 0.1f, forceRotationIfBusy: false)) {
      _setActiveButton((_activeIndex + 1) % _buttons.Length);
      EventHandler.Instance.EmitMenuBoxRotated();
    }
  }

  public void OnPlayButtonPressed() {
    if (!CanRespondToInput()) {
      return;
    }
    if (_currentState is States.MENU or States.SUB_MENU_EXIT) {
      _displayOrHidePlaySubMenu(true);
      EventHandler.Instance.EmitMenuActionPressed(MenuAction.Play);
    }
  }

  public void OnQuitButtonPressed() => _processButtonPress(MenuAction.Quit);

  public void OnSettingsButtonPressed() => _processButtonPress(MenuAction.GoToSettings);

  public void OnStatsButtonPressed() => _processButtonPress(MenuAction.GoToStats);

  private void _processButtonPress(MenuAction menuAction) {
    if (!CanRespondToInput()) {
      return;
    }
    _currentState = States.EXIT;
    EventHandler.Instance.EmitMenuActionPressed(menuAction);
  }

  private void _clickOnActiveButton() {
    if (_isSubMenuDisplayed()) {
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
    else if (_buttons[ActiveIndex] == _statsButtonNode) {
      OnStatsButtonPressed();
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

  private bool CanRespondToInput() {
    var isInTransition = GetParent<GameMenu>().IsInTransitionState();
    return _currentState != States.EXIT && !isInTransition;
  }
}
