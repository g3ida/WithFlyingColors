namespace Wfc.Entities.Ui;

using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Localization;
using Wfc.Screens;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[Tool]
[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class MenuTitle : Control {
  #region Exports
  // Preview text for the editor
  [Export]
  public string DummyContent { get; set; } = "";
  [Export(hint: PropertyHint.Enum, hintString: "Key for the translatable string to display")]
  public TranslationKey Content { get; set; }

  // The room this title has beside whatever else the screen puts next to it. A title
  // whose longest word won't fit is scaled down as a whole - text, shadow and
  // underline together - rather than being left to run under the list beside it.
  // Zero, the default, means the title has the screen to itself.
  [Export]
  public float MaxLineWidth { get; set; }
  #endregion Exports

  #region Dependencies
  [Dependency]
  public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();
  #endregion Dependencies

  #region Fields
  private List<TitleLabel> _labelNodes = new List<TitleLabel>();
  #endregion Fields

  #region Constants
  // padding between
  private const float TITLES_PADDING_TOP = 50f;
  private const float TITLE_LINE_SPACING = 200f;
  private const float TITLES_PADDING_LEFT = 60f;
  private const float TRANSITION_DELAY = 0.25f;
  private const float TRANSITION_DURATION = 0.3f;
  private const float HIDE_OFFSET = 40f;
  private static readonly SkinColor[] UNDERLINE_COLOR_BAG = new SkinColor[] {
    SkinColor.TopFace,
    SkinColor.RightFace,
    SkinColor.LeftFace,
    SkinColor.BottomFace
  };
  #endregion Constants

  public override void _EnterTree() {
    base._EnterTree();
    this.WireNodes();
  }

  public void OnResolved() {
    if (!Engine.IsEditorHint()) {
      _configure(LocalizationService.GetLocalizedString(Content), withTransitions: true);
    }
    else {
      _configure(DummyContent, withTransitions: false);
    }
  }

  // A title is one label per word, and languages don't agree on how many words
  // that is, so following a language change means building the titles again
  // rather than rewriting them. Deferred because a node may not touch its own
  // children while the engine is walking the tree to deliver this notification.
  public override void _Notification(int what) {
    this.Notify(what);
    if (what == NotificationTranslationChanged && !Engine.IsEditorHint() && _labelNodes.Count > 0) {
      Callable.From(_rebuild).CallDeferred();
    }
  }

  private void _rebuild() {
    foreach (var label in _labelNodes) {
      RemoveChild(label);
      label.QueueFree();
    }
    _labelNodes.Clear();

    // The rebuilt titles get no transition of their own: the screen slid in long
    // before the player reached the language setting, and there is no way to hand
    // a late arrival an entrance that has already played.
    _configure(LocalizationService.GetLocalizedString(Content), withTransitions: false);

    // The screen drives the transitions it found when it was built on the way out,
    // and the ones that went with the old titles are no longer among them.
    (Owner as GameMenu)?.RefreshTransitionElements();
  }

  private void _configure(String content, bool withTransitions) {
    var labels = content.Split(" ");
    var built = new List<TitleLabel>();
    foreach (var label in labels) {
      var titleLabel = SceneHelpers.InstantiateNode<TitleLabel>();
      titleLabel.content = label;
      built.Add(titleLabel);
    }

    // Every line takes the same scale, so a title still reads as one block, and the
    // gap between lines comes down with it.
    var scale = _fitScale(built);

    var i = 0;
    foreach (var titleLabel in built) {
      var label = titleLabel.content;
      titleLabel.Scale = new Vector2(scale, scale);
      titleLabel.UnderlineSkinColor = UNDERLINE_COLOR_BAG[i % UNDERLINE_COLOR_BAG.Length];
      titleLabel.Position = new Vector2(TITLES_PADDING_LEFT, TITLES_PADDING_TOP + (i * TITLE_LINE_SPACING * scale));
      titleLabel.Name = $"{label} #{i}";
      // https://github.com/godotengine/godot/issues/85459
      if (withTransitions) {
        // Add UITransition to title
        var transition = SceneHelpers.InstantiateNode<UITransition>();
        transition.Delay = (i + 1) * TRANSITION_DELAY;
        transition.Duration = TRANSITION_DURATION;
        titleLabel.TreeEntered += () => {
          transition.HiddenRelativePosition =
            new Vector2(-titleLabel.Position.X - titleLabel.getEstimatedWidth() - HIDE_OFFSET, 0f);
        };
        titleLabel.AddChild(transition);
        transition.Owner = titleLabel;
      }
      // Add titleLabel
      AddChild(titleLabel);
      _labelNodes.Add(titleLabel);
      titleLabel.Owner = this;

      i++;
    }
  }

  // How far the whole title has to come down for its longest word to fit the room it
  // was given. Never enlarges: a title that already fits is drawn at the size the
  // design asks for, which is every title on a screen that set no limit.
  private float _fitScale(List<TitleLabel> labels) {
    if (MaxLineWidth <= 0f) {
      return 1f;
    }

    var widest = 0f;
    foreach (var label in labels) {
      widest = Mathf.Max(widest, label.MeasureContentWidth());
    }
    return widest <= MaxLineWidth ? 1f : MaxLineWidth / widest;
  }
}
