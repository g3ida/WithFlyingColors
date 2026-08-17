namespace Wfc.Screens;

using Godot;
using Chickensoft.Sync.Primitives;
using Wfc.Core.Event;
using Wfc.Core.Localization;
using Wfc.Entities.World.Player;
using Wfc.Screens.MenuManager;
using Wfc.Utils;
using Wfc.Utils.Attributes;

// The second and last thing the game asks on a first launch: which of its palettes the
// player can tell apart most easily. Worth asking because colour is the mechanic here
// and not decoration - the four skin colours are the player's four faces and the
// platforms they have to be matched to - so a player who cannot separate them cannot
// play at all, and would never reach the settings menu to say so.
//
// The question is put as a preference and never mentions colour vision: the four
// colours are shown as they will be played, and whoever is looking answers by eye.
// That is answerable by anyone without knowing anything about their own sight, and it
// catches a washed-out screen or a bright room just as well.
[ScenePath]
public partial class SkinSelectMenu : FirstRunMenu {
  #region Nodes
  [NodePath("Picker/VBox/Caption")]
  private Label _captionNode = default!;
  [NodePath("Picker/VBox/Player")]
  private TextureRect _playerNode = default!;
  #endregion Nodes

  private AutoChannel.Binding? _skinBinding;

  protected override GameMenus NextScreen => GameMenus.MAIN_MENU;

  // Subscribed from _Ready rather than _EnterTree, which is where the rest of this
  // screen's signals are wired too: the event handler is a dependency, and _EnterTree
  // is where it is asked for, not where the answer has arrived.
  protected override void OnFirstRunReady() {
    this.WireNodes();
    _skinBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.SkinChanged _) => _showPalette());
    _captionNode.Text = LocalizationService.GetLocalizedString(TranslationKey.menu_header_pickColors);
    _showPalette();
  }

  public override void _ExitTree() {
    base._ExitTree();
    _skinBinding?.Dispose();
    _skinBinding = null;
  }

  // The swatches keep themselves in step with the palette; only the box the player
  // will actually be moving has to be cut again, which is what asking for it does.
  private void _showPalette() => _playerNode.Texture = PlayerSpriteGenerator.GetTexture();
}
