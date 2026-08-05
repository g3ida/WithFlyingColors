namespace Wfc.Entities.Ui.Slots;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Localization;
using Wfc.Core.Persistence;
using Wfc.Screens.Levels;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class SaveSlotPanel : PanelContainer {

  // The panel holds strings that were already translated when the slot list was
  // built, so the engine's own auto-translation has nothing left to redo once the
  // player picks another language.
  public override void _Notification(int what) {
    this.Notify(what);
    if (what == NotificationTranslationChanged) {
      _refreshLocalizedText();
    }
  }

  [Dependency]
  public ISaveManager SaveManager => this.DependOn<ISaveManager>();
  [Dependency]
  public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();

  [Signal]
  public delegate void PressedEventHandler();

  private const int MIN_WIDTH = 1160;
  private const int FOCUS_BORDER_WIDTH = 6;
  private const float SIDE_BAR_WIDTH = 24f;
  private const float SIDE_BAR_FOCUSED_WIDTH = 60f;
  private const float SIDE_BAR_TWEEN_DURATION = 0.15f;
  private static readonly Color DISABLED_TINT = new(1, 1, 1, 0.5f);
  private static readonly Color DISABLED_SIDE_BAR_COLOR = new(0.5f, 0.5f, 0.5f);
  // The focused card inverts: light text on a near-black panel.
  private static readonly Color FOCUSED_BACKGROUND_COLOR = Color.FromHtml("2d2d2d");
  private static readonly Color FOCUSED_FONT_COLOR = Colors.White;
  private static readonly Color FONT_COLOR = Colors.Black;

  // Which face of the game's four-color skin each slot wears, in slot order.
  private static readonly SkinColor[] ACCENT_COLOR_BAG = [
    SkinColor.TopFace,
    SkinColor.RightFace,
    SkinColor.LeftFace,
  ];

  private int _createdTimestamp = -1;
  private int _lastPlayedTimestamp = -1;
  private string _description = "";
  private bool _isDisabled = false;
  private int _id = 0;

  #region Nodes
  [NodePath("HBoxContainer/VBoxContainer/Description")]
  private Label _descriptionNode = default!;
  [NodePath("HBoxContainer/VBoxContainer/Created")]
  private Label _createdNode = default!;
  [NodePath("HBoxContainer/VBoxContainer/LastPlayed")]
  private Label _lastPlayedNode = default!;
  [NodePath("HBoxContainer")]
  private HBoxContainer _containerNode = default!;
  [NodePath("HBoxContainer/SideBar")]
  private ColorRect _sideBarNode = default!;
  [NodePath("HBoxContainer/VBoxContainer/SlotIndex")]
  private Label _slotIndexNode = default!;
  [NodePath("HBoxContainer/VBoxContainer/LevelName")]
  private Label _levelNameNode = default!;
  [NodePath("Button")]
  private Button _buttonNode = default!;
  #endregion Nodes

  private Tween? _sideBarTweener;

  // Set once dependencies are up, which is also the guard for reading them: _Ready
  // runs first and writes the slot index before LocalizationService is available.
  private bool _isResolved;

  public void OnResolved() {
    _isResolved = true;
    _refreshLocalizedText();
  }

  private void _refreshLocalizedText() {
    if (!_isResolved) {
      return;
    }
    _slotIndexNode.Text = string.Format(
        LocalizationService.GetLocalizedString(TranslationKey.menu_label_slotIndex), _id + 1);
    UpdateMetaData();
  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();

    SetDescription(_description);
    SetSlotIndexLabel(_id);
    CustomMinimumSize = new Vector2(MIN_WIDTH, CustomMinimumSize.Y);
    _buttonNode.GrabFocusOnHover();
    _refreshSideBarColor();
  }

  public string Description {
    get => _description;
    set => SetDescription(value);
  }

  public void SetDescription(string value) {
    _description = value;
    _descriptionNode.Text = _description;
  }

  public int SlotIndexLabel {
    get => _id;
    set => SetSlotIndexLabel(value);
  }

  public void SetSlotIndexLabel(int value) {
    _id = value;
    if (_isResolved) {
      _slotIndexNode.Text = string.Format(
          LocalizationService.GetLocalizedString(TranslationKey.menu_label_slotIndex), _id + 1);
    }
    _refreshSideBarColor();
  }

  // The dates always need their localized "Created"/"Last played" prefixes, so the
  // labels are only written once LocalizationService is up; _refreshLocalizedText
  // re-runs this through UpdateMetaData after a language change.
  private void _setTimestamps(int created, int lastPlayed) {
    _createdTimestamp = created;
    _lastPlayedTimestamp = lastPlayed;
    if (!_isResolved) {
      return;
    }
    _createdNode.Text = string.Format(
        LocalizationService.GetLocalizedString(TranslationKey.menu_label_slotCreated),
        _formatTimestamp(_createdTimestamp));
    _lastPlayedNode.Text = string.Format(
        LocalizationService.GetLocalizedString(TranslationKey.menu_label_slotLastPlayed),
        _formatTimestamp(_lastPlayedTimestamp));
  }

  private static string _formatTimestamp(int value) {
    if (value == -1) {
      return "----/--/-- --:--";
    }
    // Read out as ints first: the dictionary hands back Variants, and a numeric format
    // string applied to one is ignored, so midnight printed as "0:0".
    var time = Time.GetDatetimeDictFromUnixTime(value);
    return $"{time["year"].AsInt32()}/{time["month"].AsInt32():00}/{time["day"].AsInt32():00}"
        + $" {time["hour"].AsInt32():00}:{time["minute"].AsInt32():00}";
  }

  // Single press, single meaning: the screen decides what selecting this slot does
  // in its current mode.
  private void _onButtonPressed() => EmitSignal(SignalName.Pressed);

  // The focus feedback is threefold: the card inverts its colors, its side bar
  // widens and a border in the slot's own color surrounds it, so the focused card
  // reads as focused from any distance.
  private void _onButtonFocusEntered() => _setFocusedLook(true);

  private void _onButtonFocusExited() => _setFocusedLook(false);

  private void _setFocusedLook(bool focused) {
    _animateSideBarWidth(focused ? SIDE_BAR_FOCUSED_WIDTH : SIDE_BAR_WIDTH);
    _setFontColor(focused ? FOCUSED_FONT_COLOR : FONT_COLOR);
    if (focused) {
      var style = (StyleBoxFlat)GetThemeStylebox("panel").Duplicate();
      style.BorderWidthLeft = FOCUS_BORDER_WIDTH;
      style.BorderWidthTop = FOCUS_BORDER_WIDTH;
      style.BorderWidthRight = FOCUS_BORDER_WIDTH;
      style.BorderWidthBottom = FOCUS_BORDER_WIDTH;
      style.BorderColor = _accentColor();
      style.BgColor = FOCUSED_BACKGROUND_COLOR;
      AddThemeStyleboxOverride("panel", style);
    }
    else {
      RemoveThemeStyleboxOverride("panel");
    }
  }

  private void _setFontColor(Color color) {
    foreach (var label in new[] { _slotIndexNode, _levelNameNode, _createdNode, _lastPlayedNode, _descriptionNode }) {
      label.AddThemeColorOverride("font_color", color);
    }
  }

  private void _animateSideBarWidth(float width) {
    _sideBarTweener?.Kill();
    _sideBarTweener = CreateTween();
    _sideBarTweener.TweenProperty(_sideBarNode, "custom_minimum_size:x", width, SIDE_BAR_TWEEN_DURATION)
        .SetTrans(Tween.TransitionType.Quad)
        .SetEase(Tween.EaseType.Out);
  }

  private Color _accentColor() =>
      SkinManager.Instance.CurrentSkin.GetColor(
          ACCENT_COLOR_BAG[_id % ACCENT_COLOR_BAG.Length], SkinColorIntensity.Basic);

  private void _refreshSideBarColor() =>
      _sideBarNode.Color = _isDisabled ? DISABLED_SIDE_BAR_COLOR : _accentColor();

  public new bool HasFocus {
    get => GetHasFocus();
    set => SetHasFocus(value);
  }

  public void SetHasFocus(bool value) {
    if (value) {
      _buttonNode.GrabFocus();
    }
  }

  public bool GetHasFocus() => _buttonNode.HasFocus();

  public bool IsDisabled {
    get => _isDisabled;
    set => SetIsDisabled(value);
  }

  public void SetIsDisabled(bool value) {
    _isDisabled = value;
    _buttonNode.Disabled = value;
    // Focus skips disabled slots entirely, so a controller can never land on a
    // card that has nothing to answer a press with.
    _buttonNode.FocusMode = value ? Control.FocusModeEnum.None : Control.FocusModeEnum.All;
    _containerNode.Modulate = value ? DISABLED_TINT : Colors.White;
    _refreshSideBarColor();
  }

  public void UpdateMetaData() {
    var metaData = SaveManager.GetSlotMetaData(_id);
    if (metaData != null) {
      // LastLoadDate is written once, when the slot first materializes, and never
      // again - so it is the slot's creation date in all but name. SaveTimestamp
      // moves on every load and checkpoint, which makes it "last played".
      _setTimestamps((int)metaData.LastLoadDate, (int)metaData.SaveTimestamp);
      var titleKey = LevelDispatcher.TitleKeyOf(metaData.LevelId);
      _levelNameNode.Text = titleKey == null
          ? ""
          : LocalizationService.GetLocalizedString(titleKey.Value);
      _levelNameNode.Visible = titleKey != null;
      // Whole-game completion, not the in-level checkpoint Progress: two slots are
      // only comparable on the card when the number means the same thing for both.
      SetDescription(string.Format(
          LocalizationService.GetLocalizedString(TranslationKey.menu_label_slotCompletion),
          metaData.CompletionPercent(LevelDispatcher.LEVELS.Count)));
    }
    else {
      _setTimestamps(-1, -1);
      _levelNameNode.Text = "";
      _levelNameNode.Visible = false;
      SetDescription(LocalizationService.GetLocalizedString(TranslationKey.menu_label_emptySlot));
    }
  }
}
