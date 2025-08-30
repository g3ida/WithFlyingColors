namespace Wfc.Entities.Ui;

using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Localization;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[Tool]
[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class MenuTitle : Control {
  public override void _Notification(int what) => this.Notify(what);

  #region Exports
  // Preview text for the editor
  [Export]
  public string DummyContent { get; set; } = "";
  [Export(hint: PropertyHint.Enum, hintString: "Key for the translatable string to display")]
  public TranslationKey Content { get; set; }
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
      _configure(LocalizationService.GetLocalizedString(Content));
    }
    else {
      _configure(DummyContent);
    }
  }

  private void _configure(String content) {
    var labels = content.Split(" ");
    var i = 0;
    foreach (var label in labels) {
      // Add title
      var titleLabel = SceneHelpers.InstantiateNode<TitleLabel>();
      titleLabel.content = label;
      titleLabel.UnderlineSkinColor = UNDERLINE_COLOR_BAG[i % UNDERLINE_COLOR_BAG.Length];
      titleLabel.Position = new Vector2(TITLES_PADDING_LEFT, TITLES_PADDING_TOP + i * TITLE_LINE_SPACING);
      titleLabel.Name = $"{label} #{i}";
      // https://github.com/godotengine/godot/issues/85459
      if (!Engine.IsEditorHint()) {
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
}
