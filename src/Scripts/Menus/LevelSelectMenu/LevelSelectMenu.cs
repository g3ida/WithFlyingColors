using Godot;
using System.Collections.Generic;
using Wfc.Core.Event;
using Wfc.Screens;

public partial class LevelSelectMenu : GameMenu {
  private const int X_POS = 1000;
  private const int Y_POS = 200;
  private const int Y_STEP = 300;

  private Control LevelsContainer;
  private List<SceneCard> sceneCards = new List<SceneCard>();
  private PackedScene SceneCardScene = ResourceLoader.Load<PackedScene>("res://Assets/Scenes/LevelSelectMenu/SceneCard.tscn");

  public override void _Ready() {
    base._Ready();
    LevelsContainer = GetNode<Control>("LevelsContainer");
    PopulateWithCards();
  }

  private void PopulateWithCards() {
    foreach (var level in Levels.LEVELS) {
      var sceneCard = AddSceneCard(level);
      sceneCards.Add(sceneCard);
    }
    if (sceneCards.Count > 0) {
      sceneCards[sceneCards.Count - 1].GrabFocus();
    }
  }

  private SceneCard AddSceneCard(Levels.Level level) {
    var sceneNode = SceneCardScene.Instantiate<SceneCard>();
    LevelsContainer.AddChild(sceneNode);
    var id = level.Id;
    sceneNode.Owner = LevelsContainer;
    sceneNode.LevelScene = level.Scene;
    sceneNode.LevelName = $"{id}.{level.Name}";
    return sceneNode;
  }

  private void OnBackButtonPressed() {
    if (!IsInTransitionState()) {
      EventHandler.Emit(EventType.MenuButtonPressed, (int)MenuButtons.BACK);
    }
  }
}
