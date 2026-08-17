namespace Wfc.test.instrumented.Levels;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Screens.Levels;
using Wfc.test.instrumented.Helpers.Fakes;

// Everything that can collide with the cube finds it through the repo, and most of those are
// entities with no way to take a dependency - they read the shared instance. So the one thing
// that has to hold is that the instance they read and the value the level provides are the same
// object: if those ever came apart, half the game would be testing against the previous run's
// cube and nothing would say so.
public class GameRepoPlayerTests(Node testScene) : TestClass(testScene) {
  private FakeDependenciesProvider _provider = default!;
  private GameLevel? _level;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await _frames(1);
  }

  [Cleanup]
  public void Cleanup() {
    if (_level != null && GodotObject.IsInstanceValid(_level)) {
      _level.QueueFree();
    }
    _provider.QueueFree();
  }

  [Test]
  public async Task TheLevelPutsItsPlayerWhereEveryEntityLooksTest() {
    var level = await _load(LevelId.FourColors);

    GameRepo.Instance.Player.Value.ShouldBeSameAs(level.PlayerNode);
  }

  [Test]
  public async Task WhatTheLevelProvidesIsWhatTheEntitiesReadTest() {
    var level = await _load(LevelId.FourColors);

    level.GameRepo.ShouldBeSameAs(GameRepo.Instance);
  }

  private async Task<GameLevel> _load(LevelId levelId) {
    _level = LevelDispatcher.InstantiateLevel(levelId)!;
    _provider.AddChild(_level);
    await _frames(2);
    return _level;
  }

  private async Task _frames(int count) {
    var tree = TestScene.GetTree();
    for (var i = 0; i < count; i++) {
      await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
  }
}
