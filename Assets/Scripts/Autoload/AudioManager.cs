using Godot;
using System;
using System.Collections.Generic;

public class AudioManager : Node
{
    [Signal]
    public delegate void PlaySfx(string sfxName);

    private const string BasePath = "res://Assets/sfx/";

    private Dictionary<string, Dictionary<string, object>> sfxData = new Dictionary<string, Dictionary<string, object>>()
    {
        {"brick", new Dictionary<string, object> {{"path", BasePath + "brick.ogg"}, {"volume", -4}}},
        {"bricksSlide", new Dictionary<string, object> {{"path", BasePath + "bricks_slide.ogg"}, {"volume", 0}}},
        {"gemCollect", new Dictionary<string, object> {{"path", BasePath + "gem.ogg"}, {"volume", -15}}},
        {"GemEngine", new Dictionary<string, object> {{"path", BasePath + "gems/gem-engine.ogg"}, {"volume", 8}}},
        {"GemPut", new Dictionary<string, object> {{"path", BasePath + "gems/temple_put_gem.ogg"}, {"volume", -5}}},
        {"jump", new Dictionary<string, object> {{"path", BasePath + "jumping.ogg"}, {"volume", -5}}},
        {"land", new Dictionary<string, object> {{"path", BasePath + "stand.ogg"}, {"volume", -8}}},
        {"menuFocus", new Dictionary<string, object> {{"path", BasePath + "click2.ogg"}}},
        {"menuMove", new Dictionary<string, object> {{"path", BasePath + "menu_move.ogg"}, {"volume", 0}}},
        {"menuSelect", new Dictionary<string, object> {{"path", BasePath + "menu_select.ogg"}, {"volume", 0}}},
        {"menuValueChange", new Dictionary<string, object> {{"path", BasePath + "click.ogg"}, {"volume", 0}}},
        {"pageFlip", new Dictionary<string, object> {{"path", BasePath + "piano/page-flip.ogg"}, {"volume", 5}}},
        {"piano_do", new Dictionary<string, object> {{"path", BasePath + "piano/do.ogg"}, {"volume", -3}}},
        {"piano_re", new Dictionary<string, object> {{"path", BasePath + "piano/re.ogg"}, {"volume", -3}}},
        {"piano_mi", new Dictionary<string, object> {{"path", BasePath + "piano/mi.ogg"}, {"volume", -3}}},
        {"piano_fa", new Dictionary<string, object> {{"path", BasePath + "piano/fa.ogg"}, {"volume", -3}}},
        {"piano_sol", new Dictionary<string, object> {{"path", BasePath + "piano/sol.ogg"}, {"volume", -3}}},
        {"piano_la", new Dictionary<string, object> {{"path", BasePath + "piano/la.ogg"}, {"volume", -3}}},
        {"piano_si", new Dictionary<string, object> {{"path", BasePath + "piano/si.ogg"}, {"volume", -3}}},
        {"pickup", new Dictionary<string, object> {{"path", BasePath + "pickup.ogg"}, {"volume", -4}}},
        {"playerExplode", new Dictionary<string, object> {{"path", BasePath + "die.ogg"}, {"volume", -10}}},
        {"playerFalling", new Dictionary<string, object> {{"path", BasePath + "falling.ogg"}, {"volume", -10}}},
        {"rotateLeft", new Dictionary<string, object> {{"path", BasePath + "rotatex.ogg"}, {"volume", -20}, {"pitch_scale", 0.9}}},
        {"rotateRight", new Dictionary<string, object> {{"path", BasePath + "rotatex.ogg"}, {"volume", -20}}},
        {"shine", new Dictionary<string, object> {{"path", BasePath + "shine.ogg"}, {"volume", -5}}},
        {"success", new Dictionary<string, object> {{"path", BasePath + "success.ogg"}, {"volume", -1}}},
        {"tetrisLine", new Dictionary<string, object> {{"path", BasePath + "tetris_line.ogg"}, {"volume", -7}}},
        {"winMiniGame", new Dictionary<string, object> {{"path", BasePath + "win_mini_game.ogg"}, {"volume", 1}}},
        {"wrongAnswer", new Dictionary<string, object> {{"path", BasePath + "piano/wrong-answer.ogg"}, {"volume", 10}}}
    };

    private readonly Dictionary<string, AudioStreamPlayer> sfxPool = new Dictionary<string, AudioStreamPlayer>();
    public MusicTrackManager MusicTrackManager;

    private static AudioManager _instance = null;

    public static AudioManager Instance() {
      return _instance;
    }

    public override void _Ready()
    {
        _instance = GetTree().Root.GetNode<AudioManager>("AudioManagerCS");
        PauseMode = PauseModeEnum.Process;
        SetProcess(false);
        /// FIXME: make this an extension function and use all over any node CreateChild<NodeType>()
        MusicTrackManager = new MusicTrackManager();
        AddChild(MusicTrackManager);
        MusicTrackManager.Owner = this;
        ///
        ConnectSignals();
    }

    private void FillSfxPool()
    {
        foreach (string key in sfxData.Keys)
        {
            var stream = GD.Load<AudioStream>(sfxData[key]["path"].ToString());
            var audioPlayer = new AudioStreamPlayer();
            Helpers.SetLooping(stream, false);
            audioPlayer.Stream = stream;

            if (sfxData[key].ContainsKey("volume"))
            {
                audioPlayer.VolumeDb = Convert.ToSingle(sfxData[key]["volume"]);
            }

            if (sfxData[key].ContainsKey("pitch_scale"))
            {
                audioPlayer.PitchScale = Convert.ToSingle(sfxData[key]["pitch_scale"]);
            }

            var bus = "sfx";
            if (sfxData[key].ContainsKey("bus"))
            {
                bus = sfxData[key]["bus"].ToString();
            }

            audioPlayer.Bus = bus;
            sfxPool[key] = audioPlayer;
            AddChild(audioPlayer);
            audioPlayer.Owner = this;
        }
    }

    private void ConnectSignals()
    {
        Connect(nameof(PlaySfx), this, nameof(_OnPlaySfx));
        Event.GdInstance().Connect("player_jumped", this, nameof(_OnPlayerJumped));
        Event.GdInstance().Connect("player_rotate", this, nameof(_OnPlayerRotate));
        Event.GdInstance().Connect("player_land", this, nameof(_OnPlayerLand));
        Event.GdInstance().Connect("gem_collected", this, nameof(_OnGemCollected));
        Event.GdInstance().Connect("Fullscreen_toggled", this, nameof(_OnButtonToggle));
        Event.GdInstance().Connect("Vsync_toggled", this, nameof(_OnButtonToggle));
        Event.GdInstance().Connect("Screen_size_changed", this, nameof(_OnButtonToggle));
        Event.GdInstance().Connect("on_action_bound", this, nameof(_OnKeyBound));
        Event.GdInstance().Connect("tab_changed", this, nameof(_OnTabChanged));
        Event.GdInstance().Connect("focus_changed", this, nameof(_OnFocusChanged));
        Event.GdInstance().Connect("menu_box_rotated", this, nameof(_OnMenuBoxRotated));
        Event.GdInstance().Connect("keyboard_action_biding", this, nameof(_OnKeyboardActionBinding));
        Event.GdInstance().Connect("player_explode", this, nameof(_OnPlayerExplode));
        Event.GdInstance().Connect("pause_menu_enter", this, nameof(_OnPauseMenuEnter));
        Event.GdInstance().Connect("pause_menu_exit", this, nameof(_OnPauseMenuExit));
        Event.GdInstance().Connect("player_fall", this, nameof(_OnPlayerFalling));
        Event.GdInstance().Connect("tetris_lines_removed", this, nameof(_OnTetrisLinesRemoved));
        Event.GdInstance().Connect("picked_powerup", this, nameof(_OnPickedPowerup));
        Event.GdInstance().Connect("brick_broken", this, nameof(_OnBrickBroken));
        Event.GdInstance().Connect("break_breaker_win", this, nameof(_OnWinMiniGame));
        Event.GdInstance().Connect("brick_breaker_start", this, nameof(_OnBrickBreakerStart));
        Event.GdInstance().Connect("menu_button_pressed", this, nameof(_OnMenuButtonPressed));
        Event.GdInstance().Connect("sfx_volume_changed", this, nameof(_OnButtonToggle));
        Event.GdInstance().Connect("music_volume_changed", this, nameof(_OnButtonToggle));
        Event.GdInstance().Connect("piano_note_pressed", this, nameof(_OnPianoNotePressed));
        Event.GdInstance().Connect("piano_note_released", this, nameof(_OnPianoNoteReleased));
        Event.GdInstance().Connect("page_flipped", this, nameof(_OnPageFlipped));
        Event.GdInstance().Connect("wrong_piano_note_played", this, nameof(_OnWrongPianoNotePlayed));
        Event.GdInstance().Connect("piano_puzzle_won", this, nameof(_OnPianoPuzzleWon));
        Event.GdInstance().Connect("gem_temple_triggered", this, nameof(_OnGemTempleTriggered));
        Event.GdInstance().Connect("gem_engine_started", this, nameof(_OnGemEngineStarted));
        Event.GdInstance().Connect("gem_put_in_temple", this, nameof(_OnGemPutInTemple));
    }

    private void DisconnectSignals()
    {
        Disconnect(nameof(PlaySfx), this, nameof(_OnPlaySfx));
        Event.GdInstance().Disconnect("player_jumped", this, nameof(_OnPlayerJumped));
        Event.GdInstance().Disconnect("player_rotate", this, nameof(_OnPlayerRotate));
        Event.GdInstance().Disconnect("player_land", this, nameof(_OnPlayerLand));
        Event.GdInstance().Disconnect("gem_collected", this, nameof(_OnGemCollected));
        Event.GdInstance().Disconnect("Fullscreen_toggled", this, nameof(_OnButtonToggle));
        Event.GdInstance().Disconnect("Vsync_toggled", this, nameof(_OnButtonToggle));
        Event.GdInstance().Disconnect("Screen_size_changed", this, nameof(_OnButtonToggle));
        Event.GdInstance().Disconnect("on_action_bound", this, nameof(_OnKeyBound));
        Event.GdInstance().Disconnect("tab_changed", this, nameof(_OnTabChanged));
        Event.GdInstance().Disconnect("focus_changed", this, nameof(_OnFocusChanged));
        Event.GdInstance().Disconnect("menu_box_rotated", this, nameof(_OnMenuBoxRotated));
        Event.GdInstance().Disconnect("keyboard_action_biding", this, nameof(_OnKeyboardActionBinding));
        Event.GdInstance().Disconnect("player_explode", this, nameof(_OnPlayerExplode));
        Event.GdInstance().Disconnect("pause_menu_enter", this, nameof(_OnPauseMenuEnter));
        Event.GdInstance().Disconnect("pause_menu_exit", this, nameof(_OnPauseMenuExit));
        Event.GdInstance().Disconnect("player_fall", this, nameof(_OnPlayerFalling));
        Event.GdInstance().Disconnect("tetris_lines_removed", this, nameof(_OnTetrisLinesRemoved));
        Event.GdInstance().Disconnect("picked_powerup", this, nameof(_OnPickedPowerup));
        Event.GdInstance().Disconnect("brick_broken", this, nameof(_OnBrickBroken));
        Event.GdInstance().Disconnect("break_breaker_win", this, nameof(_OnWinMiniGame));
        Event.GdInstance().Disconnect("brick_breaker_start", this, nameof(_OnBrickBreakerStart));
        Event.GdInstance().Disconnect("menu_button_pressed", this, nameof(_OnMenuButtonPressed));
        Event.GdInstance().Disconnect("sfx_volume_changed", this, nameof(_OnButtonToggle));
        Event.GdInstance().Disconnect("music_volume_changed", this, nameof(_OnButtonToggle));
        Event.GdInstance().Disconnect("piano_note_pressed", this, nameof(_OnPianoNotePressed));
        Event.GdInstance().Disconnect("piano_note_released", this, nameof(_OnPianoNoteReleased));
        Event.GdInstance().Disconnect("page_flipped", this, nameof(_OnPageFlipped));
        Event.GdInstance().Disconnect("wrong_piano_note_played", this, nameof(_OnWrongPianoNotePlayed));
        Event.GdInstance().Disconnect("piano_puzzle_won", this, nameof(_OnPianoPuzzleWon));
        Event.GdInstance().Disconnect("gem_temple_triggered", this, nameof(_OnGemTempleTriggered));
        Event.GdInstance().Disconnect("gem_engine_started", this, nameof(_OnGemEngineStarted));
        Event.GdInstance().Disconnect("gem_put_in_temple", this, nameof(_OnGemPutInTemple));
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        //ConnectSignals();
    }

    public override void _ExitTree()
    {
        DisconnectSignals();
        base._ExitTree();
    }

    private void _OnPlaySfx(string sfx)
    {
        if (sfxPool.ContainsKey(sfx))
        {
            sfxPool[sfx].Play();
        }
    }

    public void StopAllSfx()
    {
        foreach (var sfx in sfxPool.Values)
        {
            sfx.Stop();
        }
    }

    public void StopAllExcept(string[] sfxList)
    {
        foreach (var sfx in sfxPool)
        {
            if (!Array.Exists(sfxList, element => element == sfx.Key))
            {
                sfx.Value.Stop();
            }
        }
    }

    public void EmitPlaySfx(string sfxName)
    {
        EmitSignal(nameof(PlaySfx), sfxName);
    }

    public void PauseAllSfx()
    {
        foreach (var sfx in sfxPool.Values)
        {
            if (sfx.Playing)
            {
                sfx.StreamPaused = true;
            }
        }
    }

    public void ResumeAllSfx()
    {
        foreach (var sfx in sfxPool.Values)
        {
            if (sfx.Playing)
            {
                sfx.StreamPaused = false;
            }
        }
    }

    private void _OnPlayerJumped()
    {
        _OnPlaySfx("jump");
    }

    private void _OnPlayerRotate(int dir)
    {
        _OnPlaySfx(dir == -1 ? "rotateLeft" : "rotateRight");
    }

    private void _OnPlayerLand()
    {
        _OnPlaySfx("land");
    }

    private void _OnMenuButtonPressed(string menuButton)
    {
        _OnPlaySfx("menuSelect");
    }

    private void _OnGemCollected(string color, Vector2 position, int x)
    {
        _OnPlaySfx("gemCollect");
    }

    private void _OnButtonToggle(bool value)
    {
        _OnPlaySfx("menuValueChange");
    }

    private void _OnKeyBound(bool value, bool value2)
    {
        _OnPlaySfx("menuValueChange");
    }

    private void _OnTabChanged()
    {
        _OnPlaySfx("menuFocus");
    }

    private void _OnFocusChanged()
    {
        _OnPlaySfx("menuFocus");
    }

    private void _OnMenuBoxRotated()
    {
        _OnPlaySfx("rotateRight");
    }

    private void _OnKeyboardActionBinding()
    {
        _OnPlaySfx("menuValueChange");
    }

    private void _OnPlayerExplode()
    {
        _OnPlaySfx("playerExplode");
    }

    private void _OnPlayerFalling()
    {
        _OnPlaySfx("playerFalling");
    }

    private void _OnTetrisLinesRemoved()
    {
        _OnPlaySfx("tetrisLine");
    }

    private void _OnPickedPowerup()
    {
        _OnPlaySfx("pickup");
    }

    private void _OnBrickBroken(string color, Vector2 position)
    {
        _OnPlaySfx("brick");
    }

    private void _OnWinMiniGame()
    {
        _OnPlaySfx("winMiniGame");
    }

    private void _OnBrickBreakerStart()
    {
        _OnPlaySfx("bricksSlide");
    }

    private void _OnPianoNotePressed(string note)
    {
        _OnPlaySfx("piano_" + note);
    }

    private void _OnPianoNoteReleased(string note)
    {
        // do nothing
    }

    private void _OnPageFlipped()
    {
        _OnPlaySfx("pageFlip");
    }

    private void _OnWrongPianoNotePlayed()
    {
        _OnPlaySfx("wrongAnswer");
    }

    private void _OnPianoPuzzleWon()
    {
        _OnPlaySfx("success");
    }

    private void _OnGemTempleTriggered()
    {
        _OnPlaySfx("shine");
    }

    private void _OnGemEngineStarted()
    {
        _OnPlaySfx("GemEngine");
    }

    private void _OnGemPutInTemple()
    {
        _OnPlaySfx("GemPut");
    }

    private void _OnPauseMenuEnter()
    {
        _OnPlaySfx("menuSelect");
    }

    private void _OnPauseMenuExit()
    {
        _OnPlaySfx("menuSelect");
    }
}