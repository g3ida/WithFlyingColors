using Godot;
using Wfc.Core.Event;
using Wfc.Screens;

public partial class SceneCard : Control {
  [Export]
  public string LevelName {
    get => levelName;
    set => SetLevelName(value);
  }

  [Export]
  public string LevelScene {
    get => levelScene;
    set => SetLevelScene(value);
  }

  private string levelName;
  private string levelScene;

  private Label DescriptionNode;
  private Button ButtonNode;

  public override void _Ready() {
    base._Ready();
    DescriptionNode = GetNode<Label>("Description");
    ButtonNode = GetNode<Button>("Button");

    // ButtonNode.Connect("pressed", this, nameof(_on_Button_pressed));
    // ButtonNode.Connect("mouse_entered", this, nameof(_on_Button_mouse_entered));
  }

  private void SetLevelName(string name) {
    levelName = name;
    if (DescriptionNode != null) {
      DescriptionNode.Text = name;
    }
  }

  private string GetLevelName() {
    return levelName;
  }

  private void SetLevelScene(string scene) {
    levelScene = scene;
  }

  private string GetLevelScene() {
    return levelScene;
  }

  private void _on_Button_pressed() {
    Event.Instance.EmitMenuButtonPressed(MenuButtons.SELECT_LEVEL);
    GetParent().GetParent<GameMenu>().NavigateToLevelScreen(levelScene);
  }

  private void _on_Button_mouse_entered() {
    ButtonNode.GrabFocus();
  }

  public new void GrabFocus() {
    ButtonNode.GrabFocus();
  }
}
