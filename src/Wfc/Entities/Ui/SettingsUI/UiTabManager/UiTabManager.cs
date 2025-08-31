namespace Wfc.Entities.Ui.SettingsUI;

using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Localization;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class UiTabManager : Control {

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

  private GameSkin _skin = SkinManager.Instance.CurrentSkin;


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

    _generalSettingsButton.Pressed += () => _showPanel(_generalSettingsPanel);
    _videoSettingsButton.Pressed += () => _showPanel(_videoSettingsPanel);
    _controllerSettingsButton.Pressed += () => _showPanel(_controllerSettingsPanel);
    _audioSettingsButton.Pressed += () => _showPanel(_audioSettingsPanel);

    // default panel
    _showPanel(_generalSettingsPanel);
    _generalSettingsButton.GrabFocus();
    _generalSettingsButton.ButtonPressed = true;
  }

  public void OnResolved() {
    _generalSettingsButton.Text = LocalizationService.GetLocalizedString(TranslationKey.game_settings_category_general);
    _videoSettingsButton.Text = LocalizationService.GetLocalizedString(TranslationKey.game_settings_category_display);
    _controllerSettingsButton.Text = LocalizationService.GetLocalizedString(TranslationKey.game_settings_category_controller);
    _audioSettingsButton.Text = LocalizationService.GetLocalizedString(TranslationKey.game_settings_category_audio);
  }

  private void _setButtonStyles() {
    List<SkinColor> skinColors = [SkinColor.TopFace, SkinColor.LeftFace, SkinColor.RightFace, SkinColor.BottomFace];
    for (int i = 0; i < _buttons.Count; i++) {
      var style = new StyleBoxFlat();
      style.BorderWidthTop = 7;
      style.ExpandMarginBottom = 4;
      style.BgColor = Color.FromHtml("#00000019");
      style.BorderColor = _skin.GetColor(skinColors[i % skinColors.Count], SkinColorIntensity.Basic);
      _buttons[i].AddThemeStyleboxOverride("pressed", style);
    }

  }

  private void _showPanel(PanelContainer PanelToShow) {
    foreach (var panel in _panels) {
      panel.Hide();
    }
    PanelToShow.Show();
  }
}
