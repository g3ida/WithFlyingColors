namespace Wfc.Screens;

using System.Collections.Generic;
using System.Linq;
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

  public void OnResolved() { }

  public override void _Ready() {
    base._Ready();
    PopulateWithCards();
  }

  private void PopulateWithCards() {
    var metaData = SaveManager.GetSlotMetaData();
    var chain = LevelDispatcher.LEVELS.Select(level => level.Id).ToList();
    var clearedLevels = metaData?.ClearedLevels ?? [];

    foreach (var level in LevelDispatcher.LEVELS) {
      var sceneCard = AddSceneCard(level);
      sceneCard.Locked = !LevelUnlockPolicy.IsUnlocked(level.Id, chain, clearedLevels, metaData?.LevelId);
      _sceneCards.Add(sceneCard);
    }

    // Focus follows the save: the level the player would resume, or failing that the
    // furthest one the slot has earned.
    var resumeLevelId = metaData?.LevelId ?? chain[0];
    var focusCard = _sceneCards.FirstOrDefault(card => card.LevelScene == resumeLevelId && !card.Locked)
      ?? _sceneCards.LastOrDefault(card => !card.Locked);
    focusCard?.GrabFocus();
  }

  private SceneCard AddSceneCard(LevelDispatcher.LevelInfo level) {
    var sceneNode = SceneHelpers.InstantiateNode<SceneCard>();
    var levelsContainer = GetNode<Control>("LevelsContainer");
    levelsContainer.AddChild(sceneNode);
    sceneNode.Owner = levelsContainer;
    sceneNode.LevelScene = level.Id;
    var number = LevelDispatcher.PlayOrderNumberOf(level.Id);
    sceneNode.LevelName = $"{number}.{LocalizationService.GetLocalizedString(level.TranslationKey)}";
    return sceneNode;
  }

  // No transition guard here: back buttons only report the intent, and GameMenu drops
  // it unless the screen has finished entering.
  private void OnBackButtonPressed() => EventHandler.EmitMenuActionPressed(MenuAction.GoBack);
}
