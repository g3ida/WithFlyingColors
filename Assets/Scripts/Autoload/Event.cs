using Godot;



public class Event : Node
{

    private static Node _gdInstance = null;
    private static Event _instance = null;

    public override void _Ready() {
      base._Ready();
      _gdInstance = GetTree().Root.GetNode<Node>("Event");
      _instance = GetTree().Root.GetNode<Event>("EventCS");
      SetProcess(false);
    }

    public static Event Instance() {
      return _instance;
    }

    public static Node GdInstance() {
      return _gdInstance;
    }

    [Signal]
    public delegate void player_landed(Node area, Vector2 position);

    [Signal]
    public delegate void player_diying(Node area, Vector2 position, Constants.EntityType entity_type);

    [Signal]
    public delegate void player_died();

    [Signal]
    public delegate void player_jumped();

    [Signal]
    public delegate void player_rotate(int dir);

    [Signal]
    public delegate void player_land();

    [Signal]
    public delegate void player_explode();

    [Signal]
    public delegate void player_fall();

    [Signal]
    public delegate void player_dash(Vector2 direction);

    [Signal]
    public delegate void player_slippering();

    [Signal]
    public delegate void gem_collected(string color, Vector2 position, int frames);

    [Signal]
    public delegate void slide_animation_ended(string animation_name);

    [Signal]
    public delegate void checkpoint_reached(Node checkpoint_object);

    [Signal]
    public delegate void checkpoint_loaded();

    [Signal]
    public delegate void menu_box_rotated();

    [Signal]
    public delegate void pause_menu_enter();

    [Signal]
    public delegate void pause_menu_exit();

    [Signal]
    public delegate void menu_button_pressed(string menu_button);

    [Signal]
    public delegate void fullscreen_toggled(bool value);

    [Signal]
    public delegate void vsync_toggled(bool value);

    [Signal]
    public delegate void screen_size_changed(int value);

    [Signal]
    public delegate void on_action_bound(string action, string key);

    [Signal]
    public delegate void tab_changed();

    [Signal]
    public delegate void focus_changed();

    [Signal]
    public delegate void keyboard_action_biding();

    [Signal]
    public delegate void sfx_volume_changed(float volume);

    [Signal]
    public delegate void music_volume_changed(float volume);

    [Signal]
    public delegate void tetris_lines_removed();

    [Signal]
    public delegate void brick_broken(string color, Vector2 position);

    [Signal]
    public delegate void bouncing_ball_removed(Node ball);

    [Signal]
    public delegate void picked_powerup();

    [Signal]
    public delegate void break_breaker_win();

    [Signal]
    public delegate void brick_breaker_start();

    [Signal]
    public delegate void piano_note_pressed(string note);

    [Signal]
    public delegate void piano_note_released(string note);

    [Signal]
    public delegate void page_flipped();

    [Signal]
    public delegate void wrong_piano_note_played();

    [Signal]
    public delegate void piano_puzzle_won();

    [Signal]
    public delegate void cutscene_request_start(int id);

    [Signal]
    public delegate void cutscene_request_end(int id);

    [Signal]
    public delegate void gem_temple_triggered();

    [Signal]
    public delegate void gem_engine_started();

    [Signal]
    public delegate void level_cleared();

    [Signal]
    public delegate void gem_put_in_temple();
        
    public void EmitPlayerLanded(Node area, Vector2 position) =>_gdInstance.EmitSignal(nameof(player_landed), area, position);

    public void EmitPlayerDiying(Node area, Vector2 position, Constants.EntityType entityType) =>_gdInstance.EmitSignal(nameof(player_diying), area, position, entityType);

    public void EmitPlayerDied() =>_gdInstance.EmitSignal(nameof(player_died));

    public void EmitPlayerSlippering() =>_gdInstance.EmitSignal(nameof(player_slippering));

    public void EmitPlayerJumped() =>_gdInstance.EmitSignal(nameof(player_jumped));

    public void EmitPlayerRotate(int dir) =>_gdInstance.EmitSignal(nameof(player_rotate), dir);

    public void EmitPlayerLand() =>_gdInstance.EmitSignal(nameof(player_land));

    public void EmitPlayerExplode() =>_gdInstance.EmitSignal(nameof(player_explode));

    public void EmitPlayerFall() =>_gdInstance.EmitSignal(nameof(player_fall));

    public void EmitPlayerDash(Vector2 dir) =>_gdInstance.EmitSignal(nameof(player_dash), dir);

    public void EmitGemCollected(string color, Vector2 position, int frames) =>_gdInstance.EmitSignal(nameof(gem_collected), color, position, frames);

    public void EmitSlideAnimationEnded(string animationName) =>_gdInstance.EmitSignal(nameof(slide_animation_ended), animationName);

    public void EmitCheckpointReached(Node checkpoint) =>_gdInstance.EmitSignal(nameof(checkpoint_reached), checkpoint);

    public void EmitCheckpointLoaded() =>_gdInstance.EmitSignal(nameof(checkpoint_loaded));

    public void EmitMenuButtonPressed(MenuButtons menuButton) =>_gdInstance.EmitSignal(nameof(menu_button_pressed), menuButton);

    public void EmitMenuBoxRotated() =>_gdInstance.EmitSignal(nameof(menu_box_rotated));

    public void EmitPauseMenuEnter() =>_gdInstance.EmitSignal(nameof(pause_menu_enter));

    public void EmitPauseMenuExit() =>_gdInstance.EmitSignal(nameof(pause_menu_exit));

    public void EmitFullscreenToggled(bool fullscreen) =>_gdInstance.EmitSignal(nameof(fullscreen_toggled), fullscreen);

    public void EmitVsyncToggled(bool vsync) =>_gdInstance.EmitSignal(nameof(vsync_toggled), vsync);

    public void EmitScreenSizeChanged(int size) =>_gdInstance.EmitSignal(nameof(screen_size_changed), size);

    public void EmitSfxVolumeChanged(float volume) =>_gdInstance.EmitSignal(nameof(sfx_volume_changed), volume);

    public void EmitMusicVolumeChanged(float volume) =>_gdInstance.EmitSignal(nameof(music_volume_changed), volume);

    public void EmitOnActionBound(string action, string key) =>_gdInstance.EmitSignal(nameof(on_action_bound), action, key);

    public void EmitTabChanged() =>_gdInstance.EmitSignal(nameof(tab_changed));

    public void EmitFocusChanged() =>_gdInstance.EmitSignal(nameof(focus_changed));

    public void EmitKeyboardActionBiding() =>_gdInstance.EmitSignal(nameof(keyboard_action_biding));

    public void EmitTetrisLinesRemoved() =>_gdInstance.EmitSignal(nameof(tetris_lines_removed));

    public void EmitBrickBroken(string color, Vector2 position) =>_gdInstance.EmitSignal(nameof(brick_broken), color, position);

    public void EmitBouncingBallRemoved(Node ball) =>_gdInstance.EmitSignal(nameof(bouncing_ball_removed), ball);

    public void EmitPickedPowerup() =>_gdInstance.EmitSignal(nameof(picked_powerup));

    public void EmitBreakBreakerWin() =>_gdInstance.EmitSignal(nameof(break_breaker_win));

    public void EmitBrickBreakerStart() =>_gdInstance.EmitSignal(nameof(brick_breaker_start));

    public void EmitPianoNotePressed(string note) =>_gdInstance.EmitSignal(nameof(piano_note_pressed), note);

    public void EmitPianoNoteReleased(string note) =>_gdInstance.EmitSignal(nameof(piano_note_released), note);

    public void EmitPageFlipped() =>_gdInstance.EmitSignal(nameof(page_flipped));

    public void EmitWrongPianoNotePlayed() =>_gdInstance.EmitSignal(nameof(wrong_piano_note_played));

    public void EmitPianoPuzzleWon() =>_gdInstance.EmitSignal(nameof(piano_puzzle_won));

    public void EmitCutsceneRequestStart(int id) =>_gdInstance.EmitSignal(nameof(cutscene_request_start), id);

    public void EmitCutsceneRequestEnd(int id) =>_gdInstance.EmitSignal(nameof(cutscene_request_end), id);

    public void EmitGemTempleTriggered() =>_gdInstance.EmitSignal(nameof(gem_temple_triggered));

    public void EmitGemEngineStarted() => _gdInstance.EmitSignal(nameof(gem_engine_started));

    public void EmitLevelCleared() => _gdInstance.EmitSignal(nameof(level_cleared));

    public void EmitGemPutInTemple() => _gdInstance.EmitSignal(nameof(gem_put_in_temple));
}
