namespace Wfc.Entities.World.ButtonGame;

using Godot;
using Wfc.Core.Event;
using Wfc.Core.Localization;
using Wfc.Entities.World.Platforms;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;

// The room keeps two checkpoints and no more: the campfire beside the stand, which the player
// walks into on the way in, and the win itself. Everything between them - the rounds - is worth
// keeping across a death but is not progress the player has banked.
[ScenePath]
public partial class ButtonGameScene : Node2D {
  #region Exports
  // Which face the cube is put down on when it respawns past the door. The floor there is neutral,
  // so this is only about which way up the player comes back.
  [Export]
  public string WinColorGroup { get; set; } = ColorUtils.PURPLE;
  #endregion Exports

  #region Nodes
  [NodePath("ButtonGame")]
  private ButtonGame _buttonGameNode = default!;
  [NodePath("SlidingDoor/Slider")]
  private PlatformSlider _doorSliderNode = default!;
  [NodePath("WinCheckpoint")]
  private Marker2D _winCheckpointNode = default!;
  #endregion Nodes

  public override void _EnterTree() {
    base._EnterTree();
    this.WireNodes();
    _buttonGameNode.PuzzleWon += _onPuzzleWon;
  }

  public override void _ExitTree() {
    _buttonGameNode.PuzzleWon -= _onPuzzleWon;
    base._ExitTree();
  }

  private void _onPuzzleWon(bool immediate) {
    // A room that was already won is one the player has already opened, so the door is put where
    // its run leaves it rather than sent along the run again: replaying it shuts the door in the
    // player's face on every death and grinds it open once more, sound and all.
    if (immediate) {
      _doorSliderNode.SettleAtEnd();
      return;
    }
    _doorSliderNode.ResumeSlider();
    _recordWinCheckpoint();
  }

  // A checkpoint with no post to stand on: clearing the room is the progress, and the door opening
  // is where it is taken. It is reported the same way a campfire is, because to the player it is
  // the same promise - what they just did is theirs now.
  private void _recordWinCheckpoint() {
    GameEvents.Instance.OnCheckpointReached(_winCheckpointNode.GlobalPosition, WinColorGroup);
    GameEvents.Instance.OnNotificationRaised(TranslationKey.game_notification_checkpointReached);
  }
}
