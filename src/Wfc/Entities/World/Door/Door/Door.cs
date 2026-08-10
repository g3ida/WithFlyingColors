namespace Wfc.Entities.World.Door;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.Core.Localization;
using Wfc.Core.Persistence;
using Wfc.Screens.Levels;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;
using EventHandler = Wfc.Core.Event.EventHandler;

// A level entrance standing in the hub: the level select card made walkable. The
// keystone pentagon and the gems set into the arch show what the level behind it has
// already given up, a chained padlock says it is still locked, and dashing in the
// doorway walks in.
[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class Door : Node2D {
  #region Dependencies
  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public ISaveManager SaveManager => this.DependOn<ISaveManager>();
  [Dependency]
  public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();
  [Dependency]
  public IInputManager InputManager => this.DependOn<IInputManager>();
  #endregion Dependencies

  // Holds the player still while the swap cover closes; the level's Cutscene node
  // releases the lock when it leaves the tree with the rest of the hub.
  private const string DOOR_CUTSCENE_ID = "DoorEnter";

  #region Constants
  // The ceremony, beat by beat: the gems drop into the arch one after another, their light
  // takes a moment to reach the meeting point over the door, and the comet gathers there
  // before it is carried down into the keystone.
  private const float GEM_LANDING_STAGGER = 0.4f;
  private const float PHOTON_TRAVEL = 0.9f;
  private const float MERGE_FLASH = 0.5f;
  private const float COMET_FORM = 0.55f;
  private const float COMET_TRAVEL = 0.7f;
  #endregion Constants

  // What a doorway answers to. Jump is what the player is holding the cube up with
  // while they stand in one, so walking in has its own button; DoorPrompt reads this
  // to name the same one on screen.
  public const IInputManager.Action ENTER_ACTION = IInputManager.Action.Dash;

  [Export]
  public LevelId TargetLevel { get; set; } = LevelId.Tutorial;

  #region Nodes
  [NodePath("DoorGem")]
  private DoorGem _doorGemNode = default!;
  [NodePath("ArchGems")]
  private Node2D _archGemsNode = default!;
  [NodePath("LockSprite")]
  private Sprite2D _lockSpriteNode = default!;
  [NodePath("TitleLabel")]
  private Label _titleLabelNode = default!;
  [NodePath("EnterArea")]
  private Area2D _enterAreaNode = default!;
  [NodePath("DoorPrompt")]
  private DoorPrompt _promptNode = default!;
  [NodePath("DoorCeremony")]
  private DoorCeremony _ceremonyNode = default!;
  [NodePath("CometFormingPoint")]
  private Marker2D _formingPointNode = default!;
  #endregion Nodes

  private Player.Player? _playerInside;
  private bool _entering;
  private bool _isResolved;
  private bool _isSubscribed;
  private Tween? _lockShakeTween;

  // What the door is showing, which lags the slot while a ceremony is owed: gems the player
  // has just carried back out of a level are worth watching arrive, and the slot is written
  // while the screen is still covered.
  private readonly HashSet<string> _shownGems = [];
  private readonly HashSet<string> _heldGems = [];
  private bool _isCeremonyExpected;
  private bool _isCelebrating;

  public bool IsLocked { get; private set; } = true;

  public override void _EnterTree() {
    base._EnterTree();
    if (_isSubscribed) {
      return;
    }
    EventHandler.Instance.Events.SaveSlotUpdated += _onSaveSlotUpdated;
    _isSubscribed = true;
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (!_isSubscribed) {
      return;
    }
    EventHandler.Instance.Events.SaveSlotUpdated -= _onSaveSlotUpdated;
    _isSubscribed = false;
  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _enterAreaNode.BodyEntered += _onEnterAreaBodyEntered;
    _enterAreaNode.BodyExited += _onEnterAreaBodyExited;
  }

  public void OnResolved() {
    _isResolved = true;
    _applyTitle();
    _applySaveState();
  }

  public override void _Input(InputEvent @event) {
    base._Input(@event);
    if (_playerInside is not { } player || _entering) {
      return;
    }
    if (!InputManager.IsEventActionJustPressed(ENTER_ACTION, @event)) {
      return;
    }
    // A dash in mid air stays a dash; only a grounded press in the doorway is a
    // request to walk in.
    if (!player.IsOnFloor()) {
      return;
    }
    if (IsLocked) {
      _shakeLock();
      return;
    }
    _enterDoor();
  }

  // Doors number themselves from play order, not from LevelId ordinals, so the
  // labels always match the order the doors are cleared in.
  private void _applyTitle() {
    var number = LevelDispatcher.PlayOrderNumberOf(TargetLevel);
    var titleKey = LevelDispatcher.TitleKeyOf(TargetLevel);
    var title = titleKey == null
        ? TargetLevel.ToString()
        : LocalizationService.GetLocalizedString(titleKey.Value);
    var upperTitle = title.ToUpperInvariant();
    _titleLabelNode.Text = number == null ? upperTitle : $"{number}. {upperTitle}";
  }

  // The hub is built before the clear that sent the player back to it has been
  // banked, so this runs again on every slot write rather than only on the way in -
  // otherwise the door of the level just finished still shows it chained, with a gray
  // keystone, the moment the player walks out of it.
  private void _onSaveSlotUpdated() {
    if (_isResolved) {
      _applySaveState();
    }
  }

  private void _applySaveState() {
    var metaData = SaveManager.GetSlotMetaData();
    IReadOnlySet<LevelId> clearedLevels = metaData?.ClearedLevels ?? new HashSet<LevelId>();
    var chain = LevelDispatcher.LEVELS.Select(level => level.Id).ToList();
    IsLocked = !LevelUnlockPolicy.IsUnlocked(TargetLevel, chain, clearedLevels, metaData?.LevelId);
    _lockSpriteNode.Visible = IsLocked;

    // A ceremony in flight is already showing the slot, one gem at a time; re-reading it
    // underneath would put the rest of them on the arch before their turn.
    if (!_isCelebrating) {
      _takeGems(metaData?.GemsCollectedIn(TargetLevel) ?? new HashSet<string>());
      _showGems();
    }
    _refreshPrompt();
  }

  // A door about to celebrate holds back the gems it has not shown yet. Every other door -
  // and every gem it was already showing - takes the slot as it stands.
  private void _takeGems(IReadOnlySet<string> collectedGems) {
    _heldGems.Clear();
    if (!_isCeremonyExpected) {
      _shownGems.Clear();
      _shownGems.UnionWith(collectedGems);
      return;
    }
    foreach (var colorGroup in collectedGems) {
      if (!_shownGems.Contains(colorGroup)) {
        _heldGems.Add(colorGroup);
      }
    }
  }

  private void _showGems() {
    _ceremonyNode.Clear();
    _doorGemNode.SnapToRest();
    _doorGemNode.SetCollectedGems(_shownGems);
    foreach (var archGem in _archGemsNode.GetChildren().OfType<DoorArchGem>()) {
      archGem.SetCollected(_shownGems.Contains(archGem.ColorGroup));
    }
  }

  // Told before the clear it is about to show reaches the slot. Without this the gems land
  // on the arch while the swap cover is still down and the player walks out to a door that
  // has already finished celebrating for them.
  public void ExpectCelebration() => _isCeremonyExpected = true;

  // The gems the level gave up arriving one by one, and - once the fourth is home - the four
  // of them going up as light to be made into the comet over the door.
  public async void Celebrate() {
    _isCeremonyExpected = false;
    if (_isCelebrating || !_isResolved) {
      return;
    }
    var arriving = ColorUtils.COLOR_GROUPS.Where(_heldGems.Contains).ToList();
    _heldGems.Clear();
    if (arriving.Count == 0) {
      return;
    }

    _isCelebrating = true;
    foreach (var colorGroup in arriving) {
      _shownGems.Add(colorGroup);
      var archGem = _archGemFor(colorGroup);
      archGem?.SetCollected(true);
      archGem?.PlayLanding();
      EventHandler.Instance.EmitDoorGemFilled();
      if (!await _wait(GEM_LANDING_STAGGER)) {
        return;
      }
    }

    if (ColorUtils.COLOR_GROUPS.All(_shownGems.Contains)) {
      await _formComet();
    }
    _isCelebrating = false;
  }

  private async Task _formComet() {
    var meetingPoint = _formingPointNode.Position;
    var photons = ColorUtils.COLOR_GROUPS
      .Select(colorGroup => (_archGemFor(colorGroup)?.Position ?? Vector2.Zero, _gemColor(colorGroup)))
      .ToList();

    await _ceremonyNode.RunPhotons(photons, meetingPoint, PHOTON_TRAVEL);
    if (!IsInsideTree()) {
      return;
    }

    EventHandler.Instance.EmitDoorCometFormed();
    _doorGemNode.SetCollectedGems(_shownGems);
    _doorGemNode.FormAt(meetingPoint, COMET_FORM, COMET_TRAVEL);
    await _ceremonyNode.Flash(meetingPoint, Colors.White, MERGE_FLASH);
  }

  private DoorArchGem? _archGemFor(string colorGroup) =>
    _archGemsNode.GetChildren().OfType<DoorArchGem>().FirstOrDefault(gem => gem.ColorGroup == colorGroup);

  private static Color _gemColor(string colorGroup) =>
    SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(colorGroup),
      SkinColorIntensity.Basic
    );

  // False once the hub has been torn down under the ceremony, which is the caller's cue to
  // stop: the door it was decorating is on its way out.
  private async Task<bool> _wait(float seconds) {
    try {
      await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    }
    catch (ObjectDisposedException) {
      return false;
    }
    return IsInsideTree();
  }

  // Only an unlocked door has anything to press: on a chained one the prompt would
  // be advertising a level the player cannot reach.
  private void _refreshPrompt() => _promptNode.Visible = _playerInside != null && !IsLocked && !_entering;

  private void _enterDoor() {
    _entering = true;
    _refreshPrompt();
    GetViewport().SetInputAsHandled();
    EventHandler.Instance.EmitCutsceneRequestStart(DOOR_CUTSCENE_ID);
    EventHandler.Instance.EmitDoorEntered((int)TargetLevel);
  }

  // The press stays a dash, and the chain answers it: the cube lurches, the padlock
  // rattles, and the door has said "not yet" without a dialog.
  private void _shakeLock() {
    _lockShakeTween?.Kill();
    _lockShakeTween = CreateTween();
    var basePosition = _lockSpriteNode.Position;
    _lockShakeTween.TweenProperty(_lockSpriteNode, "position:x", basePosition.X + 6f, 0.05f);
    _lockShakeTween.TweenProperty(_lockSpriteNode, "position:x", basePosition.X - 6f, 0.1f);
    _lockShakeTween.TweenProperty(_lockSpriteNode, "position:x", basePosition.X + 3f, 0.08f);
    _lockShakeTween.TweenProperty(_lockSpriteNode, "position:x", basePosition.X, 0.06f);
  }

  private void _onEnterAreaBodyEntered(Node2D body) {
    if (body is Player.Player player) {
      _playerInside = player;
      _refreshPrompt();
    }
  }

  private void _onEnterAreaBodyExited(Node2D body) {
    if (body == _playerInside) {
      _playerInside = null;
      _refreshPrompt();
    }
  }
}
