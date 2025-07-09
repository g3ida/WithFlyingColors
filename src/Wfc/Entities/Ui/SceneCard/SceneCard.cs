namespace Wfc.Entities.Ui;

using Godot;
using Wfc.Core.Event;
using Wfc.Screens;
using Wfc.Screens.Levels;
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

    _buttonNode.Connect(
      Button.SignalName.Pressed,
      new Callable(this, nameof(OnButtonPressed)),
      (uint)ConnectFlags.OneShot
    );
    _buttonNode.Connect(
      Button.SignalName.MouseEntered,
      new Callable(this, nameof(OnButtonMouseEntered)),
      (uint)ConnectFlags.OneShot);
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
    EventHandler.Instance.EmitMenuButtonPressed(MenuButtons.SELECT_LEVEL);
    GetParent().GetParent<GameMenu>().NavigateToLevelScreen(_levelId);
  }

  private void OnButtonMouseEntered() {
    _buttonNode.GrabFocus();
  }

  public new void GrabFocus() {
    _buttonNode.GrabFocus();
  }
}
