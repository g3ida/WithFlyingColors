namespace Wfc.Screens;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Localization;
using Wfc.Entities.Ui;
using Wfc.Screens.Levels;
using Wfc.Screens.MenuManager;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class LevelSelectMenu : GameMenu {

  public override void _Notification(int what) => this.Notify(what);

  private const int X_POS = 1000;
  private const int Y_POS = 200;
  private const int Y_STEP = 300;

  private readonly List<SceneCard> _sceneCards = [];

  [Dependency]
  public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();

  public void OnResolved() { }

  public override void _Ready() {
    base._Ready();
    PopulateWithCards();
  }

  private void PopulateWithCards() {
    foreach (var level in LevelDispatcher.LEVELS) {
      var sceneCard = AddSceneCard(level);
      _sceneCards.Add(sceneCard);
    }
    if (_sceneCards.Count > 0) {
      _sceneCards[^1].GrabFocus();
    }
  }

  private SceneCard AddSceneCard(LevelDispatcher.LevelInfo level) {
    var sceneNode = SceneHelpers.InstantiateNode<SceneCard>();
    var levelsContainer = GetNode<Control>("LevelsContainer");
    levelsContainer.AddChild(sceneNode);
    var id = level.Id;
    sceneNode.Owner = levelsContainer;
    sceneNode.LevelScene = level.Id;
    sceneNode.LevelName = $"{id}.{level.Name}";
    return sceneNode;
  }

  private void OnBackButtonPressed() {
    if (!IsInTransitionState()) {
      EventHandler.EmitMenuActionPressed(MenuAction.GoBack);
    }
  }
}
