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

  private string _levelName = "";
  private LevelId _levelId;


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

  private void OnButtonPressed() {
    EventHandler.Instance.EmitMenuActionPressed(MenuAction.GoToLevelSelect);
    GetParent().GetParent<GameMenu>().NavigateToLevelScreen(_levelId);
  }

  public new void GrabFocus() {
    _buttonNode.GrabFocus();
  }
}
