namespace Wfc.Entities.World.Door;

using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.Core.Localization;
using Wfc.Core.Persistence;
using Wfc.Screens.Levels;
using Wfc.Utils;
using Wfc.Utils.Attributes;
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
  #endregion Nodes

  private Player.Player? _playerInside;
  private bool _entering;
  private bool _isResolved;
  private bool _isSubscribed;
  private Tween? _lockShakeTween;

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
    var index = LevelDispatcher.LEVELS.FindIndex(level => level.Id == TargetLevel);
    var titleKey = LevelDispatcher.TitleKeyOf(TargetLevel);
    var title = titleKey == null
        ? TargetLevel.ToString()
        : LocalizationService.GetLocalizedString(titleKey.Value);
    var upperTitle = title.ToUpperInvariant();
    _titleLabelNode.Text = index < 0 ? upperTitle : $"{index}. {upperTitle}";
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

    var collectedGems = metaData?.GemsCollectedIn(TargetLevel) ?? new HashSet<string>();
    _doorGemNode.SetCollectedGems(collectedGems);
    foreach (var archGem in _archGemsNode.GetChildren().OfType<DoorArchGem>()) {
      archGem.SetCollected(collectedGems.Contains(archGem.ColorGroup));
    }
    _refreshPrompt();
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
