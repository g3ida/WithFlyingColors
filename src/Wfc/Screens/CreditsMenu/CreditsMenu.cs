namespace Wfc.Screens;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Audio;
using Wfc.Core.Event;
using Wfc.Core.Localization;
using Wfc.Screens.MenuManager;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class CreditsMenu : GameMenu {
  #region Constants
  private const string AUTHOR = "Mohamed Alaa Eddine Gaida";
  private const string SOUND_EFFECTS_SOURCE = "pixabay.com";

  private const int SECTION_FONT_SIZE = 40;
  private const int ENTRY_FONT_SIZE = 31;
  private const int NOTE_FONT_SIZE = 22;
  private const float SECTION_SPACING = 26f;

  // The menu background is pale, so the credits are read the way the titles are: dark
  // on light, with the licenses stepped back rather than lit up.
  private static readonly Color SECTION_COLOR = new(0.176471f, 0.176471f, 0.176471f);
  private static readonly Color ENTRY_COLOR = new(0.27f, 0.27f, 0.29f);
  private static readonly Color NOTE_COLOR = new(0.44f, 0.44f, 0.47f);
  #endregion Constants

  #region Nodes
  [NodePath("CreditsContainer")]
  private VBoxContainer _creditsContainerNode = default!;
  #endregion Nodes

  #region Fields
  private readonly List<Control> _creditNodes = [];
  #endregion Fields

  // The container also holds the screen's UITransition, so the credits are rebuilt
  // from what this screen put there rather than from the container's children.
  public override void _Notification(int what) {
    this.Notify(what);
    if (what == NotificationTranslationChanged && _creditNodes.Count > 0) {
      Callable.From(_populate).CallDeferred();
    }
  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
  }

  public void OnResolved() => _populate();

  private void _populate() {
    foreach (var node in _creditNodes) {
      _creditsContainerNode.RemoveChild(node);
      node.QueueFree();
    }
    _creditNodes.Clear();

    _addSection(TranslationKey.credits_section_createdBy);
    _addEntry(AUTHOR);
    _addNote(LocalizationService.GetLocalizedString(TranslationKey.credits_role_designCodeArt));

    _addSpacer();
    _addSection(TranslationKey.credits_section_music);
    foreach (var track in GameMusicTracks.Data.Values) {
      _addEntry($"{track.Title} — {track.Artist}");
      _addNote(track.License);
    }

    // The bank is part homemade and part downloaded, so both are named rather than the
    // whole of it being handed to whichever source is easier to point at.
    _addSpacer();
    _addSection(TranslationKey.credits_section_soundEffects);
    _addEntry(AUTHOR);
    _addEntry(SOUND_EFFECTS_SOURCE);

    _addSpacer();
    _addSection(TranslationKey.credits_section_aiAssistance);
    _addEntry(LocalizationService.GetLocalizedString(TranslationKey.credits_note_aiAssistance));
  }

  private void _addSection(TranslationKey key) =>
    _addLabel(LocalizationService.GetLocalizedString(key).ToUpperInvariant(), SECTION_FONT_SIZE, SECTION_COLOR);

  private void _addEntry(string text) => _addLabel(text, ENTRY_FONT_SIZE, ENTRY_COLOR);

  private void _addNote(string text) => _addLabel(text, NOTE_FONT_SIZE, NOTE_COLOR);

  private void _addSpacer() =>
    _addChild(new Control { CustomMinimumSize = new Vector2(0f, SECTION_SPACING) });

  private void _addLabel(string text, int fontSize, Color color) {
    var label = new Label {
      Text = text,
      AutowrapMode = TextServer.AutowrapMode.WordSmart
    };
    label.AddThemeFontSizeOverride("font_size", fontSize);
    label.AddThemeColorOverride("font_color", color);
    _addChild(label);
  }

  private void _addChild(Control node) {
    _creditsContainerNode.AddChild(node);
    node.Owner = _creditsContainerNode;
    _creditNodes.Add(node);
  }

  // No transition guard here: back buttons only report the intent, and GameMenu drops
  // it unless the screen has finished entering.
  private void OnBackButtonPressed() => EventHandler.EmitMenuActionPressed(MenuAction.GoBack);
}
