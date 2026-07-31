namespace Wfc.Entities.Ui;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Input;
using Wfc.Core.Localization;
using Wfc.Utils;
using Wfc.Utils.Attributes;

// The presentation around a level start: an opaque cover that hides the moment two
// levels are swapped, and a title that fades in and out over the intro cutscene
// playing underneath. The cover runs while the tree is paused, so both layers
// process Always.
[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class LevelTitleCard : CanvasLayer {
  #region Dependencies
  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();
  [Dependency]
  public IInputManager InputManager => this.DependOn<IInputManager>();
  #endregion Dependencies

  #region Signals
  // The cover is fully opaque: whatever happens underneath now is invisible.
  [Signal]
  public delegate void CoveredEventHandler();
  // The title has faded back out: the intro presentation is over.
  [Signal]
  public delegate void TitleFinishedEventHandler();
  #endregion Signals

  #region Constants
  private const float COVER_FADE_DURATION = 0.4f;
  private const float TITLE_FADE_DURATION = 0.4f;
  private const float TITLE_HOLD_DURATION = 1.6f;
  #endregion Constants

  #region Nodes
  [NodePath("Cover")]
  private ColorRect _coverNode = default!;
  [NodePath("Title")]
  private Control _titleNode = default!;
  [NodePath("Title/TitleText")]
  private Label _titleTextNode = default!;
  [NodePath("HoldTimer")]
  private Timer _holdTimerNode = default!;
  #endregion Nodes

  private enum TitleState { Inactive, FadingIn, Holding, FadingOut }
  private TitleState _titleState = TitleState.Inactive;
  private bool _coverShown;
  private Tween? _coverTween;
  private Tween? _titleTween;

  public void OnResolved() { }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _holdTimerNode.Timeout += _onHoldTimerTimeout;
  }

  // Fades to opaque so the level swap happens out of sight. The cover stays up until
  // PresentTitle lifts it.
  public void CoverForSwap() {
    if (_coverShown) {
      return;
    }
    _coverShown = true;
    _coverNode.Modulate = new Color(1, 1, 1, 0);
    _coverNode.Visible = true;
    _coverTween = _fade(_coverTween, _coverNode, 1f, COVER_FADE_DURATION, () => EmitSignal(SignalName.Covered));
  }

  // Runs the title over whatever is playing underneath - lifting the cover first if
  // one is up - and reports TitleFinished once it has faded back out.
  //
  // A restart rather than a guard: a swap can begin while the previous level's title
  // is still up, and the new level's entrance must not be swallowed by it.
  public void PresentTitle(TranslationKey titleKey) {
    _holdTimerNode.Stop();
    if (_coverShown) {
      _coverShown = false;
      _coverTween = _fade(_coverTween, _coverNode, 0f, COVER_FADE_DURATION, () => _coverNode.Visible = false);
    }

    _titleTextNode.Text = LocalizationService.GetLocalizedString(titleKey);
    _titleNode.Modulate = new Color(1, 1, 1, 0);
    _titleNode.Visible = true;
    _titleState = TitleState.FadingIn;
    _titleTween = _fade(_titleTween, _titleNode, 1f, TITLE_FADE_DURATION, _onTitleFadedIn);
  }

  public override void _Input(InputEvent @event) {
    if (_titleState == TitleState.Inactive && !_coverShown) {
      return;
    }
    // The pause menu also processes while the tree is paused; letting it open under
    // the cover would fight the orchestrator over GetTree().Paused, so this node
    // owns that key while the presentation runs.
    if (InputManager.IsEventActionJustPressed(IInputManager.Action.Pause, @event)) {
      GetViewport().SetInputAsHandled();
      return;
    }
    if (_titleState == TitleState.Holding &&
        (InputManager.IsEventActionJustPressed(IInputManager.Action.UIConfirm, @event) ||
         InputManager.IsEventActionJustPressed(IInputManager.Action.Jump, @event))) {
      GetViewport().SetInputAsHandled();
      _holdTimerNode.Stop();
      _startTitleFadeOut();
    }
  }

  private void _onTitleFadedIn() {
    _titleState = TitleState.Holding;
    _holdTimerNode.Start(TITLE_HOLD_DURATION);
  }

  private void _onHoldTimerTimeout() => _startTitleFadeOut();

  private void _startTitleFadeOut() {
    _titleState = TitleState.FadingOut;
    _titleTween = _fade(_titleTween, _titleNode, 0f, TITLE_FADE_DURATION, _onTitleFadedOut);
  }

  private void _onTitleFadedOut() {
    _titleState = TitleState.Inactive;
    _titleNode.Visible = false;
    EmitSignal(SignalName.TitleFinished);
  }

  private Tween _fade(Tween? previous, CanvasItem target, float alpha, float duration, Action onCompleted) {
    previous?.Kill();
    var tween = CreateTween();
    tween.TweenProperty(target, "modulate:a", alpha, duration);
    tween.Finished += onCompleted;
    return tween;
  }
}
