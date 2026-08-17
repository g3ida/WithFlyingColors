namespace Wfc.test.instrumented.Ui;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Event;
using Wfc.Core.Localization;
using Wfc.Entities.Ui;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// Anything the game wants to say in passing goes in as a translation key and comes out as a bar in
// the corner. What has to hold is that the line raised is the line shown, that the corner empties
// itself again, and that a run of them never walls off the screen.
public class NotificationCenterTests(Node testScene) : TestClass(testScene) {
  private const int MAX_STACKED = 3;
  // A card's whole run - slide in, hold, slide out - with slack for a slow machine.
  private const double RUN_TIMEOUT_SECONDS = 8.0;
  // Long enough for a card sent away early to finish leaving, far shorter than a full hold, so
  // settling inside it proves the stack was actually cut back rather than just running its course.
  private const double SETTLE_TIMEOUT_SECONDS = 2.0;

  private FakeDependenciesProvider _provider = default!;
  private NotificationCenter _center = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    _center = SceneHelpers.InstantiateNode<NotificationCenter>();
    _provider.AddChild(_center);
    await _idle();
  }

  [Cleanup]
  public void Cleanup() => _provider.QueueFree();

  [Test]
  public async Task ShowsTheLineItWasRaisedWith() {
    GameEvents.Instance.OnNotificationRaised(TranslationKey.game_notification_checkpointReached);
    await _idle();

    var expected = new LocalizationService()
      .GetLocalizedString(TranslationKey.game_notification_checkpointReached)
      .ToUpperInvariant();
    _lines().ShouldBe([expected]);
  }

  [Test]
  public async Task TakesItselfAwayAgain() {
    GameEvents.Instance.OnNotificationRaised(TranslationKey.game_notification_checkpointReached);
    (await _waitUntil(() => _cards().Count == 1)).ShouldBeTrue("nothing was ever shown");

    (await _waitUntil(() => _cards().Count == 0, RUN_TIMEOUT_SECONDS))
      .ShouldBeTrue("the notification never left the corner");
  }

  [Test]
  public async Task ARunOfThemNeverWallsOffTheScreen() {
    for (var i = 0; i < MAX_STACKED + 3; i++) {
      GameEvents.Instance.OnNotificationRaised(TranslationKey.game_notification_checkpointReached);
    }

    (await _waitUntil(() => _cards().Count <= MAX_STACKED, SETTLE_TIMEOUT_SECONDS))
      .ShouldBeTrue($"more than {MAX_STACKED} bars were left standing");
    _cards().Count.ShouldBe(MAX_STACKED);
  }

  private List<NotificationCard> _cards() => [.. _center.FindDescendants<NotificationCard>()];

  private List<string> _lines() =>
    [.. _cards().SelectMany(card => card.FindDescendants<Label>()).Select(label => label.Text)];

  private async Task<bool> _waitUntil(Func<bool> condition, double timeoutSeconds = SETTLE_TIMEOUT_SECONDS) {
    var tree = TestScene.GetTree();
    var deadline = Time.GetTicksMsec() + (ulong)(timeoutSeconds * 1000);
    while (Time.GetTicksMsec() < deadline) {
      if (condition()) {
        return true;
      }
      await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
    return condition();
  }

  private async Task _idle() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
  }
}
