namespace Wfc.Entities.Ui.SettingsUI;

using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Localization;
using Wfc.Entities.Ui.SettingsUI.Grid;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;

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
  #endregion Nodes

  #region Dependencies
  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();
  #endregion Dependencies

  #region Signals
  /// <summary>Emitted when the active panel changes, providing the focusable rows for that panel</summary>
  [Signal]
  public delegate void PanelChangedEventHandler(Button currentPanelButton, Godot.Collections.Array<UIGridRow> rows);
  #endregion Signals

  private GameSkin _skin = SkinManager.Instance.CurrentSkin;
  private int _currentPanelIndex = 0;

  /// <summary>Gets the current panel index (0=General, 1=Video, 2=Controller, 3=Audio)</summary>
  public int CurrentPanelIndex => _currentPanelIndex;

  /// <summary>Gets the total number of panels/tabs</summary>
  public int PanelCount => _panels.Count;

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

    _setButtonStyles();

    _generalSettingsButton.Pressed += () => SwitchToPanel(0);
    _videoSettingsButton.Pressed += () => SwitchToPanel(1);
    _controllerSettingsButton.Pressed += () => SwitchToPanel(2);
    _audioSettingsButton.Pressed += () => SwitchToPanel(3);

    // default panel
    SwitchToPanel(0);
  }

  public void OnResolved() {
    _generalSettingsButton.Text = LocalizationService.GetLocalizedString(TranslationKey.game_settings_category_general);
    _videoSettingsButton.Text = LocalizationService.GetLocalizedString(TranslationKey.game_settings_category_display);
    _controllerSettingsButton.Text = LocalizationService.GetLocalizedString(TranslationKey.game_settings_category_controller);
    _audioSettingsButton.Text = LocalizationService.GetLocalizedString(TranslationKey.game_settings_category_audio);
  }

  private void _setButtonStyles() {
    List<SkinColor> skinColors = [SkinColor.TopFace, SkinColor.LeftFace, SkinColor.RightFace, SkinColor.BottomFace];
    _setPressedButtonStlyes(skinColors);
    _setFocusAndHoverButtonStyles(skinColors);
  }

  private void _setPressedButtonStlyes(List<SkinColor> skinColors) {
    for (int i = 0; i < _buttons.Count; i++) {
      var style = new StyleBoxFlat();
      style.BorderWidthTop = 7;
      style.ExpandMarginBottom = 4;
      style.BgColor = Color.FromHtml("#00000019");
      style.BorderColor = _skin.GetColor(skinColors[i % skinColors.Count], SkinColorIntensity.Basic);
      _buttons[i].AddThemeStyleboxOverride("pressed", style);
    }
  }

  private void _setFocusAndHoverButtonStyles(List<SkinColor> skinColors) {
    for (int i = 0; i < _buttons.Count; i++) {
      var style = new StyleBoxFlat();
      style.BorderWidthTop = 7;
      style.ExpandMarginBottom = 4;
      style.BgColor = Color.FromHtml("#00000040");
      style.BorderColor = _skin.GetColor(skinColors[i % skinColors.Count], SkinColorIntensity.Basic);
      _buttons[i].AddThemeStyleboxOverride("hover", style);
      _buttons[i].AddThemeStyleboxOverride("focus", style);
    }
  }

  // Switches to the panel at the given index and emits the PanelChanged signal with the focusable rows.
  public void SwitchToPanel(int index) {
    if (index < 0 || index >= _panels.Count)
      return;

    _currentPanelIndex = index;

    // Update button pressed state
    for (int i = 0; i < _buttons.Count; i++) {
      _buttons[i].ButtonPressed = i == index;
    }

    // Show the panel
    _showPanel(_panels[index]);

    // Get focusable rows and emit signal
    var rows = GetFocusableRowsForPanel(_panels[index]);
    var currentPanelButton = _buttons[index];
    EmitSignal(SignalName.PanelChanged, currentPanelButton, rows);
  }

  /// <summary>
  /// Navigates to the next or previous tab.
  /// </summary>
  /// <param name="direction">-1 for previous tab, 1 for next tab</param>
  public void NavigateTab(int direction) {
    int newIndex = _currentPanelIndex + direction;

    // Clamp to valid range (no wrapping for tabs)
    newIndex = Mathf.Clamp(newIndex, 0, _panels.Count - 1);
    GD.Print($"[SettingsTabManager] Navigating tab to index: {newIndex} from {_currentPanelIndex}");

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

  /// <summary>
  /// Recursively finds all UIGridRow nodes within a panel.
  /// </summary>
  private static Godot.Collections.Array<UIGridRow> GetFocusableRowsForPanel(PanelContainer panel) {
    var rows = new Godot.Collections.Array<UIGridRow>();
    FindUIGridRowsRecursive(panel, rows);
    return rows;
  }

  private static void FindUIGridRowsRecursive(Node node, Godot.Collections.Array<UIGridRow> rows) {
    foreach (var child in node.GetChildren()) {
      if (child is UIGridRow row) {
        // Only add rows that have a focusable control
        if (row.GetFocusableControl() != null) {
          rows.Add(row);
        }
      }
      // Continue searching in children
      FindUIGridRowsRecursive(child, rows);
    }
  }
}
