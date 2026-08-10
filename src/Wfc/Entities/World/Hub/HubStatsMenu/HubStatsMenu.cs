namespace Wfc.Entities.World.Hub;

using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Input;
using Wfc.Core.Localization;
using Wfc.Core.Persistence;
using Wfc.Core.Ui;
using Wfc.Entities.Ui.InputHint;
using Wfc.Screens.Levels;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;

// What the run has done so far, read off the board in the hub.
//
// An overlay rather than a screen: the hub stays where it is behind the same darken and
// blur the pause menu uses, and the modal stack holds the room still while it is up -
// which is why this processes Always.
[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class HubStatsMenu : CanvasLayer {
  #region Signals
  // The board puts its own prompt back when this closes: it has no other way of knowing
  // the player is standing in front of it again.
  [Signal]
  public delegate void ClosedEventHandler();
  #endregion Signals

  #region Constants
  private const int LABEL_FONT_SIZE = 34;
  private const int NAME_COLUMN_WIDTH = 560;
  private const int ROW_HEIGHT = 52;
  private const int ROW_PADDING = 24;

  // The panel the facts are read off, and the colour the tab standing over it takes so the
  // two read as one surface.
  private static readonly Color PANEL_COLOR = new(0.223529f, 0.223529f, 0.243137f, 0.964706f);
  private static readonly Color UNSELECTED_TAB_COLOR = new(0.113725f, 0.113725f, 0.129412f, 0.941176f);

  // Each tab wears a band of one of the cube's own faces, in face order, so the row belongs
  // to whichever palette the player picked rather than to a colour chosen here.
  private static readonly SkinColor[] TAB_COLOR_BAG = [SkinColor.TopFace, SkinColor.RightFace];
  private const int TAB_BAND_HEIGHT = 6;
  private const int TAB_MARGIN_SIDE = 10;
  private const int TAB_MARGIN_TOP = 20;
  private const int TAB_MARGIN_BOTTOM = 15;
  // The selected tab is drawn a touch past its own bottom edge, so no seam shows where it
  // meets the panel it belongs to.
  private const float TAB_PANEL_OVERLAP = 4f;

  private static readonly Color NAME_COLOR = new(0.78f, 0.78f, 0.86f);
  private static readonly Color VALUE_COLOR = Colors.White;
  // Every other row is lifted off the panel, the way a long table is easier to follow
  // when the eye has something to run along.
  private static readonly Color STRIPE_COLOR = new(1f, 1f, 1f, 0.05f);
  #endregion Constants

  #region Dependencies
  public override void _Notification(int what) {
    this.Notify(what);
    if (what == NotificationTranslationChanged && _isOpen) {
      _rebuild();
    }
  }

  [Dependency]
  public ISaveManager SaveManager => this.DependOn<ISaveManager>();
  [Dependency]
  public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();
  [Dependency]
  public IInputManager InputManager => this.DependOn<IInputManager>();
  [Dependency]
  public IModalStack ModalStack => this.DependOn<IModalStack>();
  #endregion Dependencies

  #region Nodes
  [NodePath("ScreenShaders")]
  private Core.ScreenShaders _screenShadersNode = default!;
  [NodePath("Panel/Center/Column/Tabs/GameFactsButton")]
  private Button _gameFactsButtonNode = default!;
  [NodePath("Panel/Center/Column/Tabs/AchievementsButton")]
  private Button _achievementsButtonNode = default!;
  [NodePath("Panel/Center/Column/GameFactsPanel")]
  private PanelContainer _gameFactsPanelNode = default!;
  [NodePath("Panel/Center/Column/AchievementsPanel")]
  private PanelContainer _achievementsPanelNode = default!;
  [NodePath("Panel/Center/Column/GameFactsPanel/Margin/Rows")]
  private VBoxContainer _gameFactsRowsNode = default!;
  [NodePath("InputHintBar")]
  private InputHintBar _inputHintBarNode = default!;
  #endregion Nodes

  private bool _isOpen;
  private bool _isResolved;

  public bool IsOpen => _isOpen;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    // The board is the only way in and out; a mouse on the tabs would leave the row
    // showing one panel with the other's caption lit.
    _gameFactsButtonNode.FocusMode = Control.FocusModeEnum.None;
    _achievementsButtonNode.FocusMode = Control.FocusModeEnum.None;
    _gameFactsButtonNode.Pressed += () => _showTab(gameFacts: true);
    _achievementsButtonNode.Pressed += () => _showTab(gameFacts: false);
    // Only the two actions this overlay answers to: there is nothing here to navigate
    // between and nothing to select.
    _inputHintBarNode.RemoveCard("NavigateCard");
    _inputHintBarNode.RemoveCard("SelectCard");
    _gameFactsPanelNode.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = PANEL_COLOR });
    _achievementsPanelNode.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = PANEL_COLOR });
  }

  public void OnResolved() {
    _isResolved = true;
    _applyTabCaptions();
  }

  public void Open() {
    if (_isOpen || !_isResolved) {
      return;
    }
    _isOpen = true;
    // Restyled on the way in rather than once at build time: the palette can be changed from
    // the pause menu while this same hub is loaded.
    _applyTabStyles();
    _rebuild();
    _showTab(gameFacts: true);
    Show();
    _screenShadersNode.ActivatePauseShader();
    _inputHintBarNode.Enter();
    // Pushed last: the stack pauses the tree, and everything above wants the frame it
    // was set up in to still be running.
    ModalStack.Push(this);
  }

  public void Close() {
    if (!_isOpen) {
      return;
    }
    _isOpen = false;
    ModalStack.Pop(this);
    _screenShadersNode.DisablePauseShader();
    _inputHintBarNode.Exit();
    Hide();
    EmitSignal(SignalName.Closed);
  }

  public override void _Input(InputEvent @event) {
    if (!_isOpen || ModalStack.IsBlockedFor(this)) {
      return;
    }

    if (InputManager.IsEventActionJustPressed(IInputManager.Action.UICancel, @event)) {
      Close();
      GetViewport().SetInputAsHandled();
      return;
    }

    // Left/right as well as the shoulders: the tab actions are only bound on a gamepad, so
    // on a keyboard they would leave the second tab with no way in at all.
    if (InputManager.IsEventActionJustPressed(IInputManager.Action.UITabNext, @event)
        || InputManager.IsEventActionJustPressed(IInputManager.Action.UITabPrevious, @event)
        || InputManager.IsEventActionJustPressed(IInputManager.Action.UILeft, @event)
        || InputManager.IsEventActionJustPressed(IInputManager.Action.UIRight, @event)) {
      _showTab(gameFacts: !_gameFactsPanelNode.Visible);
      GetViewport().SetInputAsHandled();
    }
  }

  private void _applyTabStyles() {
    var skin = SkinManager.Instance.CurrentSkin;
    var buttons = new[] { _gameFactsButtonNode, _achievementsButtonNode };
    for (var i = 0; i < buttons.Length; i++) {
      var band = skin.GetColor(TAB_COLOR_BAG[i % TAB_COLOR_BAG.Length], SkinColorIntensity.Light);
      buttons[i].AddThemeStyleboxOverride("normal", _tabStyle(band, UNSELECTED_TAB_COLOR, selected: false));
      buttons[i].AddThemeStyleboxOverride("hover", _tabStyle(band, UNSELECTED_TAB_COLOR, selected: false));
      foreach (var selectedState in new[] { "pressed", "hover_pressed", "focus" }) {
        buttons[i].AddThemeStyleboxOverride(selectedState, _tabStyle(band, PANEL_COLOR, selected: true));
      }
    }
  }

  private static StyleBoxFlat _tabStyle(Color band, Color background, bool selected) => new() {
    BgColor = background,
    BorderWidthTop = TAB_BAND_HEIGHT,
    BorderColor = band,
    ContentMarginLeft = TAB_MARGIN_SIDE,
    ContentMarginRight = TAB_MARGIN_SIDE,
    ContentMarginTop = TAB_MARGIN_TOP,
    ContentMarginBottom = TAB_MARGIN_BOTTOM,
    ExpandMarginBottom = selected ? TAB_PANEL_OVERLAP : 0f,
  };

  private void _showTab(bool gameFacts) {
    _gameFactsPanelNode.Visible = gameFacts;
    _achievementsPanelNode.Visible = !gameFacts;
    _gameFactsButtonNode.ButtonPressed = gameFacts;
    _achievementsButtonNode.ButtonPressed = !gameFacts;
  }

  private void _applyTabCaptions() {
    _gameFactsButtonNode.Text = LocalizationService.GetLocalizedString(TranslationKey.menu_tab_gameFacts);
    _achievementsButtonNode.Text = LocalizationService.GetLocalizedString(TranslationKey.menu_tab_achievements);
  }

  private void _rebuild() {
    _applyTabCaptions();
    foreach (var row in _gameFactsRowsNode.GetChildren()) {
      _gameFactsRowsNode.RemoveChild(row);
      row.QueueFree();
    }

    var index = 0;
    foreach (var (key, value) in _facts()) {
      _addRow(LocalizationService.GetLocalizedString(key), value, striped: index % 2 == 1);
      index++;
    }
  }

  // The run standing in this hub, not the game as a whole: every other thing in the room -
  // the doors, the gems on them - belongs to the slot the player came in on, and a board
  // beside them reading off another one would be describing somebody else's game.
  private IEnumerable<(TranslationKey Key, string Value)> _facts() {
    var metaData = SaveManager.GetSlotMetaData();
    var chain = LevelDispatcher.LEVELS.Select(level => level.Id).ToList();
    var clearedLevels = metaData?.ClearedLevels ?? [];
    var unlocked = chain.Count(levelId =>
        LevelUnlockPolicy.IsUnlocked(levelId, chain, clearedLevels, metaData?.LevelId));

    yield return (TranslationKey.stats_label_totalTimePlayed, PlayTimeFormat.Format(metaData?.PlayTimeSeconds ?? 0));
    yield return (TranslationKey.stats_label_levelsUnlocked, $"{unlocked}/{chain.Count}");
    yield return (TranslationKey.stats_label_totalJumps, _count(metaData, RunStat.Jumps));
    yield return (TranslationKey.stats_label_totalDashes, _count(metaData, RunStat.Dashes));
    yield return (TranslationKey.stats_label_totalRotateLeft, _count(metaData, RunStat.RotationsLeft));
    yield return (TranslationKey.stats_label_totalRotateRight, _count(metaData, RunStat.RotationsRight));
    yield return (TranslationKey.stats_label_totalDeaths, _count(metaData, RunStat.Deaths));
    yield return (TranslationKey.stats_label_totalGemsCollected, $"{metaData?.TotalGemsCollected() ?? 0}");
  }

  private static string _count(SlotMetaData? metaData, RunStat stat) => $"{metaData?.CounterOf(stat) ?? 0}";

  private void _addRow(string name, string value, bool striped) {
    var row = new PanelContainer { CustomMinimumSize = new Vector2(0, ROW_HEIGHT) };
    if (striped) {
      row.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
        BgColor = STRIPE_COLOR,
        ContentMarginLeft = ROW_PADDING,
        ContentMarginRight = ROW_PADDING,
      });
    }
    else {
      row.AddThemeStyleboxOverride("panel", new StyleBoxEmpty {
        ContentMarginLeft = ROW_PADDING,
        ContentMarginRight = ROW_PADDING,
      });
    }

    var line = new HBoxContainer();
    line.AddThemeConstantOverride("separation", ROW_PADDING);
    line.AddChild(_label(name, NAME_COLOR, HorizontalAlignment.Right, NAME_COLUMN_WIDTH));
    line.AddChild(_label(value, VALUE_COLOR, HorizontalAlignment.Left, 0));
    row.AddChild(line);

    _gameFactsRowsNode.AddChild(row);
    row.Owner = _gameFactsRowsNode;
  }

  private static Label _label(string text, Color color, HorizontalAlignment alignment, int minimumWidth) {
    var label = new Label {
      Text = text,
      HorizontalAlignment = alignment,
      VerticalAlignment = VerticalAlignment.Center,
      CustomMinimumSize = new Vector2(minimumWidth, 0),
    };
    label.AddThemeFontSizeOverride("font_size", LABEL_FONT_SIZE);
    label.AddThemeColorOverride("font_color", color);
    return label;
  }
}
