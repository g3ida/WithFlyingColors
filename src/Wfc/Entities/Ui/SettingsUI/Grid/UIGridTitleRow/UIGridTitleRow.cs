namespace Wfc.Entities.Ui.SettingsUI.Grid;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Localization;
using Wfc.Core.Settings;
using Wfc.Entities.Ui;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class UIGridTitleRow : MarginContainer {
  // The title holds a string that was already translated when the row was built,
  // so it has to be written again for the row to follow a language change.
  public override void _Notification(int what) {
    this.Notify(what);
    if (what == NotificationTranslationChanged) {
      _refreshLabel();
    }
  }

  [Dependency]
  public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();

  [Export]
  public TranslationKey TranslationKey { get; set; } = TranslationKey.menu_header_mainMenu;

  // The rules above and below the title are drawn black for a light panel; over
  // the level (the pause overlay) they swap to white along with the text, which
  // follows the host's theme on its own.
  public bool OnDarkBackground {
    get => _onDarkBackground;
    set {
      _onDarkBackground = value;
      _setPanelStyle();
    }
  }

  [NodePath("PanelContainer")]
  public PanelContainer _panelContainerNode = default!;
  [NodePath("PanelContainer/Content")]
  public CenterContainer _contentNode = default!;

  private bool _onDarkBackground;
  private Label? _labelNode = null;

  public void _setPanelStyle() {
    _panelContainerNode.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    var style = new StyleBoxFlat();
    style.BgColor = Colors.Transparent;
    style.ContentMarginTop = 5;
    style.ContentMarginBottom = 5;
    // Fixme: this hack is to fix line spacing between items in the parent grid.
    style.ExpandMarginTop = 4;
    style.BorderColor = _onDarkBackground ? Colors.White : Colors.Black;
    style.BorderWidthTop = 6;
    style.BorderWidthBottom = 6;
    _panelContainerNode.AddThemeStyleboxOverride("panel", style);
  }

  private void _addContent() {
    // Make this row stretch across the parent
    _contentNode.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    _contentNode.SizeFlagsVertical = SizeFlags.ShrinkCenter;

    // Create label
    _labelNode = new Label {
      Text = LocalizationService.GetLocalizedString(TranslationKey),
      HorizontalAlignment = HorizontalAlignment.Right,
      SizeFlagsHorizontal = SizeFlags.ExpandFill,
      SizeFlagsVertical = SizeFlags.ShrinkCenter
    };
    _contentNode.AddChild(_labelNode);
  }

  // Null until the row has been built, which only happens once its dependencies
  // have resolved, so this doubles as the guard for reading LocalizationService.
  private void _refreshLabel() {
    if (_labelNode != null) {
      _labelNode.Text = LocalizationService.GetLocalizedString(TranslationKey);
    }
  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _setPanelStyle();
  }

  public void OnResolved() {
    _addContent();
  }
}
