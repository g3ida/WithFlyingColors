namespace Wfc.Entities.Ui.SettingsUI;

using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Chickensoft.Sync.Primitives;
using Godot;
using Wfc.Core.Localization;
using Wfc.Core.Logger;
using Wfc.Core.Settings;
using Wfc.Entities.Ui.SettingsUI.Grid;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class SettingsTabManager : Control {

  #region Nodes
  [NodePath("PanelManager/VBoxContainer/GeneralSettingsPanel")]
  private PanelContainer _generalSettingsPanel = default!;
  [NodePath("PanelManager/VBoxContainer/VideoSettingsPanel")]
  private PanelContainer _videoSettingsPanel = default!;
  [NodePath("PanelManager/VBoxContainer/ControllerSettingsPanel")]
  private PanelContainer _controllerSettingsPanel = default!;
  [NodePath("PanelManager/VBoxContainer/AudioSettingsPanel")]
  private PanelContainer _audioSettingsPanel = default!;
  private List<PanelContainer> _panels = new List<PanelContainer>();

  [NodePath("PanelManager/VBoxContainer/HBoxContainer/GeneralSettingsButton")]
  private Button _generalSettingsButton = default!;
  [NodePath("PanelManager/VBoxContainer/HBoxContainer/VideoSettingsButton")]
  private Button _videoSettingsButton = default!;
  [NodePath("PanelManager/VBoxContainer/HBoxContainer/ControllerSettingsButton")]
  private Button _controllerSettingsButton = default!;
  [NodePath("PanelManager/VBoxContainer/HBoxContainer/AudioSettingsButton")]
  private Button _audioSettingsButton = default!;
  private List<Button> _buttons = new List<Button>();

  [NodePath("PanelManager/VBoxContainer/GeneralSettingsPanel/MarginContainer/GridContainer/SkinGridRow")]
  private UIGridRow _skinGridRow = default!;
  #endregion Nodes

  #region Dependencies
  // The tab captions hold strings that were already translated when the screen was
  // built, so they have to be written again to follow a language change.
  public override void _Notification(int what) {
    this.Notify(what);
    if (what == NotificationTranslationChanged && _areButtonsLocalized) {
      _applyLocalizedText();
    }
  }

  [Dependency]
  public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();

  [Dependency]
  public ILogger Logger => this.DependOn<ILogger>();

  #endregion Dependencies

  #region Signals
  // Emitted when the active panel changes, providing the focusable rows for that panel
  [Signal]
  public delegate void PanelChangedEventHandler(Button currentPanelButton, Godot.Collections.Array<UIGridRow> rows);
  #endregion Signals

  #region Exports
  // Set by a host that draws the tabs over the level (the pause overlay) instead
  // of on the light menu backdrop: the dark theme replaces the light one, and
  // every widget that keeps its own light-surface colours is told to swap them.
  [Export]
  public bool OnDarkBackground { get; set; }
  [Export]
  public Theme? DarkTheme { get; set; }

  // Set by a host that is up while a level is (the pause overlay). The palette is
  // baked into every sprite as the level builds itself, so changing it under a level
  // already on screen changes nothing the player can see. The row is taken away there
  // rather than left sitting there apparently doing nothing.
  [Export]
  public bool HideSettingsNeedingAFreshLevel { get; set; }
  #endregion Exports

  public const int CONTROLLER_PANEL_INDEX = 2;

  private GameSkin _skin = SkinManager.Instance.CurrentSkin;
  private AutoChannel.Binding? _skinBinding;
  private int _currentPanelIndex = 0;
  // The buttons are wired in _Ready and captioned once dependencies resolve, so
  // nothing may write to them before both have happened.
  private bool _areButtonsLocalized;

  // Consulted with the panel about to be left before any switch. Returning false
  // keeps the player where they are; whoever set the guard says why.
  public Func<int, bool>? CanLeavePanel { get; set; }

  // Gets the current panel index (0=General, 1=Video, 2=Controller, 3=Audio)
  public int CurrentPanelIndex => _currentPanelIndex;

  // Gets the total number of panels/tabs
  public int PanelCount => _panels.Count;

  public override void _EnterTree() {
    base._EnterTree();
    _skinBinding ??= SettingsRepo.Instance.Channel.Bind()
      .On((in ISettingsRepo.SkinChanged _) => _onSkinChanged());
  }

  public override void _ExitTree() {
    base._ExitTree();
    _skinBinding?.Dispose();
    _skinBinding = null;
  }

  // The tab washes are cut from the palette when the screen is built, so picking
  // another palette on the general tab has to cut them again - otherwise the one
  // screen still showing the old colours is the screen they were changed on.
  private void _onSkinChanged() {
    _skin = SkinManager.Instance.CurrentSkin;
    _setButtonStyles();
  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _panels.Add(_generalSettingsPanel);
    _panels.Add(_videoSettingsPanel);
    _panels.Add(_controllerSettingsPanel);
    _panels.Add(_audioSettingsPanel);

    _buttons.Add(_generalSettingsButton);
    _buttons.Add(_videoSettingsButton);
    _buttons.Add(_controllerSettingsButton);
    _buttons.Add(_audioSettingsButton);

    if (OnDarkBackground) {
      _applyDarkBackground();
    }
    // Before the first panel is opened, so the row is already gone when the focus
    // order is worked out and nothing can land on it.
    _skinGridRow.Visible = !HideSettingsNeedingAFreshLevel;
    _setButtonStyles();

    _generalSettingsButton.Pressed += () => SwitchToPanel(0);
    _videoSettingsButton.Pressed += () => SwitchToPanel(1);
    _controllerSettingsButton.Pressed += () => SwitchToPanel(2);
    _audioSettingsButton.Pressed += () => SwitchToPanel(3);

    // default panel
    SwitchToPanel(0);
  }

  public void OnResolved() {
    _applyLocalizedText();
    _areButtonsLocalized = true;
  }

  private void _applyLocalizedText() {
    _generalSettingsButton.Text = LocalizationService.GetLocalizedString(TranslationKey.game_settings_category_general);
    _videoSettingsButton.Text = LocalizationService.GetLocalizedString(TranslationKey.game_settings_category_display);
    _controllerSettingsButton.Text = LocalizationService.GetLocalizedString(TranslationKey.game_settings_category_controller);
    _audioSettingsButton.Text = LocalizationService.GetLocalizedString(TranslationKey.game_settings_category_audio);
  }

  // Only the rows are told: each one hands the surface on to the label and the
  // widgets it holds, and hands it to them again every time it takes focus.
  private void _applyDarkBackground() {
    if (DarkTheme != null) {
      Theme = DarkTheme;
    }
    foreach (var row in this.FindDescendants<UIGridRow>()) {
      row.OnDarkBackground = true;
    }
    foreach (var titleRow in this.FindDescendants<UIGridTitleRow>()) {
      titleRow.OnDarkBackground = true;
    }
  }

  private void _setButtonStyles() {
    List<SkinColor> skinColors = [SkinColor.TopFace, SkinColor.LeftFace, SkinColor.RightFace, SkinColor.BottomFace];
    _setPressedButtonStyles(skinColors);
    _setFocusAndHoverButtonStyles(skinColors);
  }

  // The washes behind the tab captions lighten the pressed tab on a dark surface
  // and darken it on a light one. The pressed wash matches the panel fill, so the
  // selected tab reads as part of the panel it opens rather than a button on it.
  private Color _tabWashColor(string lightWash, string darkWash) =>
      Color.FromHtml(OnDarkBackground ? darkWash : lightWash);

  private void _setPressedButtonStyles(List<SkinColor> skinColors) {
    for (int i = 0; i < _buttons.Count; i++) {
      var style = new StyleBoxFlat();
      style.BorderWidthTop = 7;
      style.ExpandMarginBottom = 4;
      style.BgColor = _tabWashColor("#00000019", "#FFFFFF26");
      style.BorderColor = _skin.GetColor(skinColors[i % skinColors.Count], SkinColorIntensity.Basic);
      _buttons[i].AddThemeStyleboxOverride("pressed", style);
    }
  }

  private void _setFocusAndHoverButtonStyles(List<SkinColor> skinColors) {
    for (int i = 0; i < _buttons.Count; i++) {
      var style = new StyleBoxFlat();
      style.BorderWidthTop = 7;
      style.ExpandMarginBottom = 4;
      style.BgColor = _tabWashColor("#00000040", "#FFFFFF40");
      style.BorderColor = _skin.GetColor(skinColors[i % skinColors.Count], SkinColorIntensity.Basic);
      _buttons[i].AddThemeStyleboxOverride("hover", style);
      _buttons[i].AddThemeStyleboxOverride("focus", style);
    }
  }

  // Switches to the panel at the given index and emits the PanelChanged signal with the focusable rows.
  public void SwitchToPanel(int index) {
    if (index < 0 || index >= _panels.Count)
      return;

    if (index != _currentPanelIndex && CanLeavePanel?.Invoke(_currentPanelIndex) == false) {
      // Clicking another tab already toggled it; put the pressed look back on
      // the tab that stays open.
      _syncPressedTabButton();
      return;
    }

    _currentPanelIndex = index;
    _syncPressedTabButton();

    // Show the panel
    _showPanel(_panels[index]);

    // Get focusable rows and emit signal
    var rows = GetFocusableRowsForPanel(_panels[index]);
    var currentPanelButton = _buttons[index];
    EmitSignal(SignalName.PanelChanged, currentPanelButton, rows);
  }

  private void _syncPressedTabButton() {
    for (int i = 0; i < _buttons.Count; i++) {
      _buttons[i].ButtonPressed = i == _currentPanelIndex;
    }
  }

  // Navigates to the next or previous tab. Direction: 1=next, -1=previous
  public void NavigateTab(int direction) {
    int newIndex = _currentPanelIndex + direction;

    // Clamp to valid range (no wrapping for tabs)
    newIndex = Mathf.Clamp(newIndex, 0, _panels.Count - 1);
    Logger.LogInfo($"[SettingsTabManager] Navigating tab to index: {newIndex} from {_currentPanelIndex}");
    if (newIndex != _currentPanelIndex) {
      SwitchToPanel(newIndex);
    }
  }

  private void _showPanel(PanelContainer PanelToShow) {
    foreach (var panel in _panels) {
      panel.Hide();
    }
    PanelToShow.Show();
  }

  // Every row in a panel that has something focusable to offer.
  private static Godot.Collections.Array<UIGridRow> GetFocusableRowsForPanel(PanelContainer panel) {
    var rows = new Godot.Collections.Array<UIGridRow>();
    foreach (var row in panel.FindDescendants<UIGridRow>()) {
      if (row.GetFocusableControl() != null) {
        rows.Add(row);
      }
    }
    return rows;
  }
}
