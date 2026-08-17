namespace Wfc.test.instrumented.Menus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.Ui;
using Wfc.Entities.Ui.Dialogs;
using Wfc.Entities.Ui.SettingsUI;
using Wfc.Entities.Ui.SettingsUI.UISelect;
using Wfc.Screens;
using Wfc.Screens.MenuManager;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// Resolution is the one setting that cannot be stepped through: every value on the
// way past would resize the window. The row opens a list instead, and applies only
// what the player takes out of it.
//
// What happens after they take one - the window resizing, the confirmation, the
// revert - is deliberately not covered here. Resizing the real window mid-suite
// stalls a frame or two, and the suites that measure movement against the clock
// start failing several classes later.
public class ResolutionDropdownTests(Node testScene) : TestClass(testScene) {
  private const int VIDEO_PANEL_INDEX = 1;
  private const double SETTLE_TIMEOUT_SECONDS = 6.0;

  private FakeDependenciesProvider _provider = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await _idle();
  }

  // A test that leaves its list open leaves the modal stack holding the tree paused,
  // and the provider carrying it is only freed deferred - so the screens of every
  // test after it transition on tweens that never run, and time out one by one.
  [Cleanup]
  public void Cleanup() {
    TestScene.GetTree().Paused = false;
    _provider.QueueFree();
  }

  [Test]
  public async Task TheListOffersEveryResolutionTheDriverHas() {
    var (screen, dropdown) = await _openVideoTab();

    dropdown.Open();
    await _idle();

    var list = screen.FindDescendants<UIDropdownList>().FirstOrDefault();
    list.ShouldNotBeNull("the row did not open a list");
    _captions(list).ShouldBe(dropdown.SelectDriver.Items, "the list and the row disagree on the options");
  }

  // Moving through the list is looking, not choosing. Nothing may be applied until
  // an entry is taken.
  [Test]
  public async Task ClosingTheListWithoutChoosingChangesNothing() {
    var (screen, dropdown) = await _openVideoTab();
    var sizeBefore = DisplayServer.WindowGetSize();

    dropdown.Open();
    await _idle();
    screen.FindDescendants<UIDropdownList>().First().Close();
    await _idle();

    DisplayServer.WindowGetSize().ShouldBe(sizeBefore, "backing out of the list still resized the window");
    _dialogOf(screen).Visible.ShouldBeFalse("backing out of the list still asked for a confirmation");
  }

  // The list holds the screen while it is up, which is what stands the settings and
  // their focus manager down behind it.
  [Test]
  public async Task TheOpenListHoldsTheScreen() {
    var (screen, dropdown) = await _openVideoTab();
    _provider.ModalStack.IsAnyOpen.ShouldBeFalse("something was already holding the screen");

    dropdown.Open();
    await _idle();
    _provider.ModalStack.IsAnyOpen.ShouldBeTrue("the open list left the settings behind it live");

    screen.FindDescendants<UIDropdownList>().First().Close();
    await _idle();
    _provider.ModalStack.IsAnyOpen.ShouldBeFalse("the closed list never let the screen go");
  }

  // Opening the list announces itself so it can be heard. It used to say so by
  // raising ShowDialog, which the settings screen answers by putting its own dialog
  // up - so opening the resolution row popped the invalid-bindings warning.
  [Test]
  public async Task OpeningTheListRaisesNoDialog() {
    var (screen, dropdown) = await _openVideoTab();

    dropdown.Open();
    await _idle();

    screen.FindDescendants<DialogContainer>()
        .ShouldAllBe(dialog => !dialog.Visible, "opening the list put a dialog on the screen");
  }

  // The options are written down the column the row's value is on. The list is
  // brought inside the screen after it is sized, so an indent measured against the
  // row rather than against the column carries that shift into the text and the two
  // stop lining up.
  //
  // Measured on the text itself, not on the box holding it: the value sits inside
  // room kept for the longest option, so a short one drifted off the column while
  // every box involved stayed exactly where it should be.
  [Test]
  public async Task TheOptionsLineUpWithTheValueTheRowShows() {
    var (screen, dropdown) = await _openVideoTab();
    // The shortest option, which is where the drift showed.
    await _showShortestOption(dropdown);
    var value = dropdown.FindDescendants<MarqueeLabel>().First().FindDescendants<Label>().First();

    dropdown.Open();
    await _idle();

    var item = screen.FindDescendants<UIDropdownList>().First().FindDescendants<Button>().First();
    var indent = ((StyleBoxFlat)item.GetThemeStylebox("normal")).ContentMarginLeft;
    (item.GlobalPosition.X + indent).ShouldBe(value.GlobalPosition.X, 0.5,
        "the options are not written down the column the row's value is on");
  }

  // Puts a short value on the row while a much longer one is still in the list, so
  // the room kept for the longest is far wider than the value standing in it. That
  // gap is what the value used to be centred in.
  private async Task _showShortestOption(UIDropdownButton dropdown) {
    var driver = dropdown.SelectDriver;
    var shown = driver.GetDefaultSelectedIndex();
    driver.Items[shown] = "8K";
    driver.Items[(shown + 1) % driver.Items.Count] = "A MUCH LONGER OPTION THAN THAT";
    driver.EmitSignal(UISelectDriver.SignalName.ItemListChanged);
    await _idle();
    await _idle();
  }

  // Closed, the row is one of the settings and is drawn like the rest of them. The
  // box is what joins it to the list hanging off its bottom edge, so it belongs to
  // the open state and to nothing else.
  [Test]
  public async Task TheValueIsBoxedOnlyWhileTheListIsOpen() {
    var (screen, dropdown) = await _openVideoTab();
    dropdown.GetThemeStylebox("normal").ShouldBeOfType<StyleBoxEmpty>(
        "a closed row was boxed as though its list were open");

    dropdown.Open();
    await _idle();
    dropdown.GetThemeStylebox("normal").ShouldBeOfType<StyleBoxFlat>(
        "the open list hung off a row with no box to join it to");

    screen.FindDescendants<UIDropdownList>().First().Close();
    await _idle();
    dropdown.GetThemeStylebox("normal").ShouldBeOfType<StyleBoxEmpty>(
        "the box outlived the list it belonged to");
  }

  // In fullscreen the driver has a single entry to give, and a list of one is only a
  // way of confirming what the row already says.
  [Test]
  public async Task TheListDoesNotOpenOnASingleOption() {
    var (screen, dropdown) = await _openVideoTab();
    await _leaveOneOption(dropdown);

    dropdown.Open();
    await _idle();

    screen.FindDescendants<UIDropdownList>().ShouldBeEmpty("a row with one option still opened a list");
  }

  // The arrow promises a list behind the value, so a row with nothing to open must
  // not wear one - the resolution row reads "Auto" in fullscreen and that is all it
  // will ever say. The gap the arrow was held clear by goes with it: a value left
  // indented against a row of checkboxes reads as misaligned.
  [Test]
  public async Task TheArrowAndItsGapGoWhenThereIsNothingToOpen() {
    var (screen, dropdown) = await _openVideoTab();
    var value = dropdown.FindDescendants<MarqueeLabel>().First();
    var checkbox = screen.FindDescendants<CheckBox>()
        .First(box => box.Name.ToString().StartsWith("Performance"));
    _arrowOf(dropdown).Visible.ShouldBeTrue("a row with a list to open wore no arrow");

    await _leaveOneOption(dropdown);

    _arrowOf(dropdown).Visible.ShouldBeFalse("a row with one option still promised a list");
    value.GlobalPosition.X.ShouldBe(checkbox.GlobalPosition.X, 0.5,
        "the value kept the arrow's indent and no longer lines up with the checkboxes");
  }

  private static Control _arrowOf(UIDropdownButton dropdown) =>
    dropdown.GetNode<Control>("HBoxContainer/Arrow");

  // A headless run reports no screen at all, so the driver falls back to offering the one
  // smallest size - which is exactly the state in which the row is supposed to refuse to open.
  // These tests are about what the row does with a choice, not about which sizes a screen
  // qualifies for, so the choice is put there rather than waited for.
  private async Task _ensureAChoice(UIDropdownButton dropdown) {
    var driver = dropdown.SelectDriver;
    while (driver.Items.Count < 3) {
      driver.Items.Add($"Test option {driver.Items.Count}");
      driver.ItemValues.Add(Variant.CreateFrom(new Vector2I(1280 - (driver.Items.Count * 16), 720)));
    }
    driver.EmitSignal(UISelectDriver.SignalName.ItemListChanged);
    await _idle();
  }

  private async Task _leaveOneOption(UIDropdownButton dropdown) {
    dropdown.SelectDriver.Items.Count.ShouldBeGreaterThan(1, "the screen offers no choice to begin with");
    while (dropdown.SelectDriver.Items.Count > 1) {
      dropdown.SelectDriver.Items.RemoveAt(1);
      dropdown.SelectDriver.ItemValues.RemoveAt(1);
    }
    dropdown.SelectDriver.EmitSignal(UISelectDriver.SignalName.ItemListChanged);
    await _idle();
  }

  private static DialogContainer _dialogOf(GameMenu screen) =>
    screen.FindDescendants<DialogContainer>().First(dialog => dialog.Name.ToString().StartsWith("Resolution"));

  private static List<string> _captions(UIDropdownList list) =>
    [.. list.FindDescendants<Button>().Select(button => button.Text)];

  private async Task<(GameMenu Screen, UIDropdownButton Dropdown)> _openVideoTab() {
    var screen = await _openSettings();
    screen.FindDescendants<SettingsTabManager>().First().SwitchToPanel(VIDEO_PANEL_INDEX);
    await _idle();
    var dropdown = screen.FindDescendants<UIDropdownButton>().FirstOrDefault();
    dropdown.ShouldNotBeNull("the display tab has no resolution row");
    await _ensureAChoice(dropdown);
    return (screen, dropdown);
  }

  private async Task<GameMenu> _openSettings() {
    _provider.MenuManager.GoToMenu(GameMenus.MAIN_MENU);
    await _idle();
    var mainMenu = _currentScreen();
    if (mainMenu != null) {
      await _waitUntil(() => !mainMenu.IsInTransitionState());
    }

    _provider.MenuManager.GoToMenu(GameMenus.SETTINGS_MENU);
    await _idle();
    var screen = _currentScreen();
    screen.ShouldNotBeNull("the settings menu produced no screen");
    (await _waitUntil(() => !screen.IsInTransitionState())).ShouldBeTrue("the settings menu never finished entering");
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
