using Godot;



public partial class Event : Node
{
    private static Event _instance = null;

    public override void _EnterTree()
    {
        base._EnterTree();
        _instance = GetTree().Root.GetNode<Event>("EventCS");
    } 

    public override void _Ready() {
      base._Ready();
      SetProcess(false);
    }

    public static Event Instance() {
      return _instance;
    }

    [Signal]
    public delegate void player_landedEventHandler(Node area, Vector2 position);

    [Signal]
    public delegate void player_diyingEventHandler(Node area, Vector2 position, int entity_type);

    [Signal]
    public delegate void player_diedEventHandler();

    [Signal]
    public delegate void player_jumpedEventHandler();

    [Signal]
    public delegate void player_rotateEventHandler(int dir);

    [Signal]
    public delegate void player_landEventHandler();

    [Signal]
    public delegate void player_explodeEventHandler();

    [Signal]
    public delegate void player_fallEventHandler();

    [Signal]
    public delegate void player_dashEventHandler(Vector2 direction);

    [Signal]
    public delegate void player_slipperingEventHandler();

    [Signal]
    public delegate void gem_collectedEventHandler(string color, Vector2 position, SpriteFrames frames);

    [Signal]
    public delegate void slide_animation_endedEventHandler(string animation_name);

    [Signal]
    public delegate void checkpoint_reachedEventHandler(Node checkpoint_object);

    [Signal]
    public delegate void checkpoint_loadedEventHandler();

    [Signal]
    public delegate void menu_box_rotatedEventHandler();

    [Signal]
    public delegate void pause_menu_enterEventHandler();

    [Signal]
    public delegate void pause_menu_exitEventHandler();

    [Signal]
    public delegate void menu_button_pressedEventHandler(int menu_button);


    // Settings signals
    [Signal]
    public delegate void Fullscreen_toggledEventHandler(bool value);

    [Signal]
    public delegate void Vsync_toggledEventHandler(bool value);

    [Signal]
    public delegate void Screen_size_changedEventHandler(Vector2 value);

    [Signal]
    public delegate void on_action_boundEventHandler(string action, int key);

    [Signal]
    public delegate void tab_changedEventHandler();

    [Signal]
    public delegate void focus_changedEventHandler();

    [Signal]
    public delegate void keyboard_action_bidingEventHandler();

    [Signal]
    public delegate void sfx_volume_changedEventHandler(float volume);

    [Signal]
    public delegate void music_volume_changedEventHandler(float volume);

    [Signal]
    public delegate void tetris_lines_removedEventHandler();

    [Signal]
    public delegate void brick_brokenEventHandler(string color, Vector2 position);

    [Signal]
    public delegate void bouncing_ball_removedEventHandler(Node ball);

    [Signal]
    public delegate void picked_powerupEventHandler();

    [Signal]
    public delegate void break_breaker_winEventHandler();

    [Signal]
    public delegate void brick_breaker_startEventHandler();

    [Signal]
    public delegate void piano_note_pressedEventHandler(string note);

    [Signal]
    public delegate void piano_note_releasedEventHandler(string note);

    [Signal]
    public delegate void page_flippedEventHandler();

    [Signal]
    public delegate void wrong_piano_note_playedEventHandler();

    [Signal]
    public delegate void piano_puzzle_wonEventHandler();

    [Signal]
    public delegate void cutscene_request_startEventHandler(string id);

    [Signal]
    public delegate void cutscene_request_endEventHandler(string id);

    [Signal]
    public delegate void gem_temple_triggeredEventHandler();

    [Signal]
    public delegate void gem_engine_startedEventHandler();

    [Signal]
    public delegate void level_clearedEventHandler();

    [Signal]
    public delegate void gem_put_in_templeEventHandler();
        
    public void EmitPlayerLanded(Node area, Vector2 position) =>_instance.EmitSignal(nameof(player_landed), area, position);

    public void EmitPlayerDiying(Node area, Vector2 position, Constants.EntityType entityType) =>_instance.EmitSignal(nameof(player_diying), area, position, (int)entityType);

    public void EmitPlayerDied() =>_instance.EmitSignal(nameof(player_died));

    public void EmitPlayerSlippering() =>_instance.EmitSignal(nameof(player_slippering));

    public void EmitPlayerJumped() =>_instance.EmitSignal(nameof(player_jumped));

    public void EmitPlayerRotate(int dir) =>_instance.EmitSignal(nameof(player_rotate), dir);

    public void EmitPlayerLand() =>_instance.EmitSignal(nameof(player_land));

    public void EmitPlayerExplode() =>_instance.EmitSignal(nameof(player_explode));

    public void EmitPlayerFall() =>_instance.EmitSignal(nameof(player_fall));

    public void EmitPlayerDash(Vector2 dir) =>_instance.EmitSignal(nameof(player_dash), dir);

    public void EmitGemCollected(string color, Vector2 position, SpriteFrames frames) =>_instance.EmitSignal(nameof(gem_collected), color, position, frames);

    public void EmitSlideAnimationEnded(string animationName) =>_instance.EmitSignal(nameof(slide_animation_ended), animationName);

    public void EmitCheckpointReached(Node checkpoint) =>_instance.EmitSignal(nameof(checkpoint_reached), checkpoint);

    public void EmitCheckpointLoaded() =>_instance.EmitSignal(nameof(checkpoint_loaded));

    public void EmitMenuButtonPressed(MenuButtons menuButton) =>_instance.EmitSignal(nameof(menu_button_pressed), (int)menuButton);

    public void EmitMenuBoxRotated() =>_instance.EmitSignal(nameof(menu_box_rotated));

    public void EmitPauseMenuEnter() =>_instance.EmitSignal(nameof(pause_menu_enter));

    public void EmitPauseMenuExit() =>_instance.EmitSignal(nameof(pause_menu_exit));

    public void EmitFullscreenToggled(bool fullscreen) =>_instance.EmitSignal(nameof(Fullscreen_toggled), fullscreen);

    public void EmitVsyncToggled(bool vsync) =>_instance.EmitSignal(nameof(Vsync_toggled), vsync);

    public void EmitScreenSizeChanged(Vector2 size) =>_instance.EmitSignal(nameof(Screen_size_changed), size);

    public void EmitSfxVolumeChanged(float volume) =>_instance.EmitSignal(nameof(sfx_volume_changed), volume);

    public void EmitMusicVolumeChanged(float volume) =>_instance.EmitSignal(nameof(music_volume_changed), volume);

    public void EmitOnActionBound(string action, int key) =>_instance.EmitSignal(nameof(on_action_bound), action, key);

    public void EmitTabChanged() =>_instance.EmitSignal(nameof(tab_changed));

    public void EmitFocusChanged() =>_instance.EmitSignal(nameof(focus_changed));

    public void EmitKeyboardActionBiding() =>_instance.EmitSignal(nameof(keyboard_action_biding));

    public void EmitTetrisLinesRemoved() =>_instance.EmitSignal(nameof(tetris_lines_removed));

    public void EmitBrickBroken(string color, Vector2 position) =>_instance.EmitSignal(nameof(brick_broken), color, position);

    public void EmitBouncingBallRemoved(Node ball) =>_instance.EmitSignal(nameof(bouncing_ball_removed), ball);

    public void EmitPickedPowerup() =>_instance.EmitSignal(nameof(picked_powerup));

    public void EmitBreakBreakerWin() =>_instance.EmitSignal(nameof(break_breaker_win));

    public void EmitBrickBreakerStart() =>_instance.EmitSignal(nameof(brick_breaker_start));

    public void EmitPianoNotePressed(string note) =>_instance.EmitSignal(nameof(piano_note_pressed), note);

    public void EmitPianoNoteReleased(string note) =>_instance.EmitSignal(nameof(piano_note_released), note);

    public void EmitPageFlipped() =>_instance.EmitSignal(nameof(page_flipped));

    public void EmitWrongPianoNotePlayed() =>_instance.EmitSignal(nameof(wrong_piano_note_played));

    public void EmitPianoPuzzleWon() =>_instance.EmitSignal(nameof(piano_puzzle_won));

    public void EmitCutsceneRequestStart(string id) =>_instance.EmitSignal(nameof(cutscene_request_start), id);

    public void EmitCutsceneRequestEnd(string id) =>_instance.EmitSignal(nameof(cutscene_request_end), id);

    public void EmitGemTempleTriggered() =>_instance.EmitSignal(nameof(gem_temple_triggered));

    public void EmitGemEngineStarted() => _instance.EmitSignal(nameof(gem_engine_started));

    public void EmitLevelCleared() => _instance.EmitSignal(nameof(level_cleared));

    public void EmitGemPutInTemple() => _instance.EmitSignal(nameof(gem_put_in_temple));
}
