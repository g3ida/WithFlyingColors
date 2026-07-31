namespace Wfc.Entities.Ui;

using Godot;
using Wfc.Core.Event;
using Wfc.Screens;
using Wfc.Screens.Levels;
using Wfc.Screens.MenuManager;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class SceneCard : Control {
  [Export]
  public string LevelName {
    get => _levelName;
    set => SetLevelName(value);
  }

  [Export]
  public LevelId LevelScene {
    get => _levelId;
    set => SetLevelScene(value);
  }

  // A locked card stands for a level the save has not earned yet: visible so the
  // player can see there is more, but never focusable or pressable.
  [Export]
  public bool Locked {
    get => _locked;
    set => SetLocked(value);
  }

  private string _levelName = "";
  private LevelId _levelId;
  private bool _locked;

  private static readonly Color LOCKED_MODULATE = new(0.55f, 0.55f, 0.6f, 0.6f);

  [NodePath("Description")]
  private Label _descriptionNode = null!;

  [NodePath("Button")]
  private Button _buttonNode = null!;

  public override void _Ready() {
    this.WireNodes();
    base._Ready();

    // Neither of these is OneShot any more. Hover-to-focus was, so a card could only
    // be focused with the mouse the first time it was pointed at; a second press is
    // harmless because NavigateToScreen ignores anything after the first.
    _buttonNode.Pressed += OnButtonPressed;
    _buttonNode.GrabFocusOnHover();
    _applyLockedState();
  }

  private void SetLevelName(string name) {
    _levelName = name;
    if (_descriptionNode != null) {
      _descriptionNode.Text = name;
    }
  }

  private string GetLevelName() {
    return _levelName;
  }

  private void SetLevelScene(LevelId levelId) {
    _levelId = levelId;
  }

  private LevelId GetLevelScene() {
    return _levelId;
  }

  private void SetLocked(bool locked) {
    _locked = locked;
    if (_buttonNode != null) {
      _applyLockedState();
    }
  }

  private void _applyLockedState() {
    _buttonNode.Disabled = _locked;
    // Out of the focus chain entirely, so controller navigation and hover-to-focus
    // both skip it rather than parking the cursor on a button that ignores presses.
    _buttonNode.FocusMode = _locked ? FocusModeEnum.None : FocusModeEnum.All;
    Modulate = _locked ? LOCKED_MODULATE : Colors.White;
  }

  private void OnButtonPressed() {
    EventHandler.Instance.EmitMenuActionPressed(MenuAction.GoToLevelSelect);
    GetParent().GetParent<GameMenu>().NavigateToLevelScreen(_levelId);
  }

  public new void GrabFocus() {
    _buttonNode.GrabFocus();
  }
}
