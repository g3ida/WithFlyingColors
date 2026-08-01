namespace Wfc.Entities.Ui.Slots;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Persistence;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class SlotsContainer : Control {

  public override void _Notification(int what) => this.Notify(what);
  [Signal]
  public delegate void SlotPressedEventHandler(int id);

  [Export]
  public bool centered_on_screen_v = false;
  [Export]
  public bool centered_on_screen_h = false;

  #region Nodes
  [NodePath("SlotsBox")]
  private Control _boxContainerNode = default!;
  [NodePath("SlotsBox/SaveSlot1")]
  private SaveSlotPanel _saveSlot1Node = default!;
  [NodePath("SlotsBox/SaveSlot2")]
  private SaveSlotPanel _saveSlot2Node = default!;
  [NodePath("SlotsBox/SaveSlot3")]
  private SaveSlotPanel _saveSlot3Node = default!;
  private SaveSlotPanel[] _saveSlots = default!;
  #endregion Nodes

  [Dependency]
  public ISaveManager SaveManager => this.DependOn<ISaveManager>();

  private bool _allowSelectingEmptySlots = true;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _saveSlots = new SaveSlotPanel[] { _saveSlot1Node, _saveSlot2Node, _saveSlot3Node };

    SetProcess(false);
    Size = _boxContainerNode.Size;
  }

  // Filling the panels in belongs here rather than in _Ready. _Ready runs before
  // AutoInject has resolved anything, so reading SaveManager there threw
  // ProviderNotFoundException on the first slot and abandoned the rest of the method:
  // no metadata, no focus and no centering. Godot logs the exception and carries on,
  // which is why the screen looked merely empty rather than broken.
  public void OnResolved() {
    for (int i = 0; i < _saveSlots.Length; i++) {
      _saveSlots[i].SlotIndexLabel = i;
    }
    _refresh();

    if (centered_on_screen_h) {
      Position = new Vector2((GetViewportRect().Size.X - Size.X) * 0.5f, Position.Y);
    }
    if (centered_on_screen_v) {
      Position = new Vector2(Position.X, (GetViewportRect().Size.Y - Size.Y) * 0.5f);
    }

    // Focus opens on the slot Continue would resume; with nothing played yet, on
    // the first slot a press can act on.
    var lastPlayed = SaveManager.MostRecentlyPlayedSlotIndex();
    if (lastPlayed is { } index && !_saveSlots[index].IsDisabled) {
      _saveSlots[index].SetHasFocus(true);
    }
    else {
      _focusFirstEnabledSlot();
    }
  }

  private void _onSaveSlot1Pressed() => EmitSignal(SignalName.SlotPressed, 0);

  private void _onSaveSlot2Pressed() => EmitSignal(SignalName.SlotPressed, 1);

  private void _onSaveSlot3Pressed() => EmitSignal(SignalName.SlotPressed, 2);

  // Re-applies everything derived from save data: card contents, which slots a
  // press can act on, and where the last-played badge sits. The badge is purely
  // informative - the border and side bar belong to focus, not to this.
  private void _refresh() {
    for (int i = 0; i < _saveSlots.Length; i++) {
      _saveSlots[i].UpdateMetaData();
      _saveSlots[i].SetIsDisabled(!_allowSelectingEmptySlots && !SaveManager.IsSLotFilled(i));
    }
    _setLastPlayedSlot(SaveManager.MostRecentlyPlayedSlotIndex());
  }

  // Purely visual: the badge and border mark the slot Continue would resume, not a
  // selection - selecting a slot stays the caller's decision.
  private void _setLastPlayedSlot(int? index) {
    for (int i = 0; i < _saveSlots.Length; i++) {
      _saveSlots[i].SetLastPlayed(index == i);
    }
  }

  public void SetAllowSelectingEmptySlots(bool allow) {
    _allowSelectingEmptySlots = allow;
    // _Ready may not have run yet: the screen configures its mode before the
    // container fills itself in, and the refresh in OnResolved applies the flag.
    if (_saveSlots == null) {
      return;
    }
    for (int i = 0; i < _saveSlots.Length; i++) {
      _saveSlots[i].SetIsDisabled(!allow && !SaveManager.IsSLotFilled(i));
    }
    // Disabling a slot releases its focus; the screen applies the mode after this
    // container already focused a slot, so land focus back on a usable one.
    if (FocusedSlotIndex == null) {
      _focusFirstEnabledSlot();
    }
  }

  private int? FocusedSlotIndex {
    get {
      for (int i = 0; i < _saveSlots.Length; i++) {
        if (_saveSlots[i].GetHasFocus()) {
          return i;
        }
      }
      return null;
    }
  }

  private void _focusFirstEnabledSlot() {
    foreach (var slot in _saveSlots) {
      if (!slot.IsDisabled) {
        slot.SetHasFocus(true);
        return;
      }
    }
  }
}
