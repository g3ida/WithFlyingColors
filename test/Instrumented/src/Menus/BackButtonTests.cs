namespace Wfc.test.instrumented.Menus;

using System;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Chickensoft.Sync.Primitives;
using Godot;
using Shouldly;
using Wfc.Core.Event;
using Wfc.Screens;
using Wfc.Screens.MenuManager;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// Each of these screens wires its back button to a handler named only in its .tscn, so nothing
// in C# refers to it and a rename or a signature change breaks the button silently. Pressing it
// has to announce GoBack - which is all the handler does; where that leads is GameMenu's to
// decide and is covered elsewhere.
public class BackButtonTests(Node testScene) : TestClass(testScene) {
  private const double SETTLE_TIMEOUT_SECONDS = 6.0;

  private FakeDependenciesProvider _provider = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await _idle();
  }

  [Cleanup]
  public void Cleanup() {
    TestScene.GetTree().Paused = false;
    _provider.QueueFree();
  }

  [Test]
  public async Task TheSettingsBackButtonAsksToGoBack() => await _pressingBackAnnouncesGoBack(GameMenus.SETTINGS_MENU);

  [Test]
  public async Task TheCreditsBackButtonAsksToGoBack() => await _pressingBackAnnouncesGoBack(GameMenus.CREDITS_MENU);

  [Test]
  public async Task TheSlotSelectBackButtonAsksToGoBack() => await _pressingBackAnnouncesGoBack(GameMenus.SELECT_SLOT);

  [Test]
  public async Task TheLevelSelectBackButtonAsksToGoBack() => await _pressingBackAnnouncesGoBack(GameMenus.LEVEL_SELECT_MENU);

  private async Task _pressingBackAnnouncesGoBack(GameMenus menu) {
    var screen = await _open(menu);
    var backButton = screen.FindDescendants<Button>()
      .FirstOrDefault(button => button.Name.ToString().StartsWith("BackButton"));
    backButton.ShouldNotBeNull($"{menu} has no back button");

    // A gamepad plugged into the machine running this announces GoBack on its own: its stick
    // noise reaches GameMenu._Input as UICancel. Standing that path down leaves the button's
    // own connection as the only thing that can speak here.
    screen.HandleBackEvent = false;

    var announced = 0;
    using var binding = GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.MenuActionPressed message) => {
        if (message.Action == MenuAction.GoBack) {
          announced++;
        }
      });

    backButton.EmitSignal(BaseButton.SignalName.Pressed);
    await _idle();

    announced.ShouldBe(1, $"{menu}'s back button announced nothing - its .tscn names a handler that is gone");
  }

  private async Task<GameMenu> _open(GameMenus menu) {
    _provider.MenuManager.GoToMenu(GameMenus.MAIN_MENU);
    await _idle();
    var mainMenu = _currentScreen();
    if (mainMenu != null) {
      await _waitUntil(() => !mainMenu.IsInTransitionState());
    }

    _provider.MenuManager.GoToMenu(menu);
    await _idle();
    var screen = _currentScreen();
    screen.ShouldNotBeNull($"{menu} produced no screen");
    (await _waitUntil(() => !screen.IsInTransitionState())).ShouldBeTrue($"{menu} never finished entering");
    return screen;
  }

  private GameMenu? _currentScreen() =>
    _provider.FindDescendants<GameMenu>().FirstOrDefault(screen => !screen.IsQueuedForDeletion());

  private async Task<bool> _waitUntil(Func<bool> condition) {
    var tree = TestScene.GetTree();
    var elapsed = 0.0;
    while (elapsed < SETTLE_TIMEOUT_SECONDS) {
      if (condition()) {
        return true;
      }
      await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
      elapsed += 1.0 / 60.0;
    }
    return condition();
  }

  private async Task _idle() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
  }
}
