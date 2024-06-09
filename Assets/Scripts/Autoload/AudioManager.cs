using Godot;
using System;
using System.Collections.Generic;

public partial class AudioManager : Node
{
  [Signal]
  public delegate void PlaySfxEventHandler(string sfxName);

  private readonly PackedScene MusicTrackManagerScene = ResourceLoader.Load<PackedScene>("res://Assets/Scenes/Audio/MusicTrackManager.tscn");

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

  public static AudioManager Instance()
  {
    return _instance;
  }

  public override void _EnterTree()
  {
    base._EnterTree();
    _instance = GetTree().Root.GetNode<AudioManager>("AudioManagerCS");
    MusicTrackManager = MusicTrackManagerScene.Instantiate<MusicTrackManager>();
    ConnectSignals();
  }

  public override void _Ready()
  {
    ProcessMode = ProcessModeEnum.Always;
    SetProcess(false);
    /// FIXME: make this an extension function and use all over any node CreateChild<NodeType>()
    AddChild(MusicTrackManager);
    MusicTrackManager.Owner = this;
    ///
    FillSfxPool();
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
    PlaySfx += _OnPlaySfx;
    Event.Instance.Connect(nameof(Event.player_jumped), new Callable(this, nameof(_OnPlayerJumped)));
    Event.Instance.Connect("player_rotate", new Callable(this, nameof(_OnPlayerRotate)));
    Event.Instance.Connect("player_land", new Callable(this, nameof(_OnPlayerLand)));
    Event.Instance.Connect("gem_collected", new Callable(this, nameof(_OnGemCollected)));
    Event.Instance.Connect("Fullscreen_toggled", new Callable(this, nameof(_OnButtonToggle)));
    Event.Instance.Connect("Vsync_toggled", new Callable(this, nameof(_OnButtonToggle)));
    Event.Instance.Connect("Screen_size_changed", new Callable(this, nameof(_OnButtonToggle)));
    Event.Instance.Connect("on_action_bound", new Callable(this, nameof(_OnKeyBound)));
    Event.Instance.Connect("tab_changed", new Callable(this, nameof(_OnTabChanged)));
    Event.Instance.Connect("focus_changed", new Callable(this, nameof(_OnFocusChanged)));
    Event.Instance.Connect("menu_box_rotated", new Callable(this, nameof(_OnMenuBoxRotated)));
    Event.Instance.Connect("keyboard_action_biding", new Callable(this, nameof(_OnKeyboardActionBinding)));
    Event.Instance.Connect("player_explode", new Callable(this, nameof(_OnPlayerExplode)));
    Event.Instance.Connect("pause_menu_enter", new Callable(this, nameof(_OnPauseMenuEnter)));
    Event.Instance.Connect("pause_menu_exit", new Callable(this, nameof(_OnPauseMenuExit)));
    Event.Instance.Connect("player_fall", new Callable(this, nameof(_OnPlayerFalling)));
    Event.Instance.Connect("tetris_lines_removed", new Callable(this, nameof(_OnTetrisLinesRemoved)));
    Event.Instance.Connect("picked_powerup", new Callable(this, nameof(_OnPickedPowerup)));
    Event.Instance.Connect("brick_broken", new Callable(this, nameof(_OnBrickBroken)));
    Event.Instance.Connect("break_breaker_win", new Callable(this, nameof(_OnWinMiniGame)));
    Event.Instance.Connect("brick_breaker_start", new Callable(this, nameof(_OnBrickBreakerStart)));
    Event.Instance.Connect("menu_button_pressed", new Callable(this, nameof(_OnMenuButtonPressed)));
    Event.Instance.Connect("sfx_volume_changed", new Callable(this, nameof(_OnButtonToggle)));
    Event.Instance.Connect("music_volume_changed", new Callable(this, nameof(_OnButtonToggle)));
    Event.Instance.Connect("piano_note_pressed", new Callable(this, nameof(_OnPianoNotePressed)));
    Event.Instance.Connect("piano_note_released", new Callable(this, nameof(_OnPianoNoteReleased)));
    Event.Instance.Connect("page_flipped", new Callable(this, nameof(_OnPageFlipped)));
    Event.Instance.Connect("wrong_piano_note_played", new Callable(this, nameof(_OnWrongPianoNotePlayed)));
    Event.Instance.Connect("piano_puzzle_won", new Callable(this, nameof(_OnPianoPuzzleWon)));
    Event.Instance.Connect("gem_temple_triggered", new Callable(this, nameof(_OnGemTempleTriggered)));
    Event.Instance.Connect("gem_engine_started", new Callable(this, nameof(_OnGemEngineStarted)));
    Event.Instance.Connect("gem_put_in_temple", new Callable(this, nameof(_OnGemPutInTemple)));
  }

  private void DisconnectSignals()
  {
    Disconnect(nameof(PlaySfx), new Callable(this, nameof(_OnPlaySfx)));
    Event.Instance.Disconnect("player_jumped", new Callable(this, nameof(_OnPlayerJumped)));
    Event.Instance.Disconnect("player_rotate", new Callable(this, nameof(_OnPlayerRotate)));
    Event.Instance.Disconnect("player_land", new Callable(this, nameof(_OnPlayerLand)));
    Event.Instance.Disconnect("gem_collected", new Callable(this, nameof(_OnGemCollected)));
    Event.Instance.Disconnect("Fullscreen_toggled", new Callable(this, nameof(_OnButtonToggle)));
    Event.Instance.Disconnect("Vsync_toggled", new Callable(this, nameof(_OnButtonToggle)));
    Event.Instance.Disconnect("Screen_size_changed", new Callable(this, nameof(_OnButtonToggle)));
    Event.Instance.Disconnect("on_action_bound", new Callable(this, nameof(_OnKeyBound)));
    Event.Instance.Disconnect("tab_changed", new Callable(this, nameof(_OnTabChanged)));
    Event.Instance.Disconnect("focus_changed", new Callable(this, nameof(_OnFocusChanged)));
    Event.Instance.Disconnect("menu_box_rotated", new Callable(this, nameof(_OnMenuBoxRotated)));
    Event.Instance.Disconnect("keyboard_action_biding", new Callable(this, nameof(_OnKeyboardActionBinding)));
    Event.Instance.Disconnect("player_explode", new Callable(this, nameof(_OnPlayerExplode)));
    Event.Instance.Disconnect("pause_menu_enter", new Callable(this, nameof(_OnPauseMenuEnter)));
    Event.Instance.Disconnect("pause_menu_exit", new Callable(this, nameof(_OnPauseMenuExit)));
    Event.Instance.Disconnect("player_fall", new Callable(this, nameof(_OnPlayerFalling)));
    Event.Instance.Disconnect("tetris_lines_removed", new Callable(this, nameof(_OnTetrisLinesRemoved)));
    Event.Instance.Disconnect("picked_powerup", new Callable(this, nameof(_OnPickedPowerup)));
    Event.Instance.Disconnect("brick_broken", new Callable(this, nameof(_OnBrickBroken)));
    Event.Instance.Disconnect("break_breaker_win", new Callable(this, nameof(_OnWinMiniGame)));
    Event.Instance.Disconnect("brick_breaker_start", new Callable(this, nameof(_OnBrickBreakerStart)));
    Event.Instance.Disconnect("menu_button_pressed", new Callable(this, nameof(_OnMenuButtonPressed)));
    Event.Instance.Disconnect("sfx_volume_changed", new Callable(this, nameof(_OnButtonToggle)));
    Event.Instance.Disconnect("music_volume_changed", new Callable(this, nameof(_OnButtonToggle)));
    Event.Instance.Disconnect("piano_note_pressed", new Callable(this, nameof(_OnPianoNotePressed)));
    Event.Instance.Disconnect("piano_note_released", new Callable(this, nameof(_OnPianoNoteReleased)));
    Event.Instance.Disconnect("page_flipped", new Callable(this, nameof(_OnPageFlipped)));
    Event.Instance.Disconnect("wrong_piano_note_played", new Callable(this, nameof(_OnWrongPianoNotePlayed)));
    Event.Instance.Disconnect("piano_puzzle_won", new Callable(this, nameof(_OnPianoPuzzleWon)));
    Event.Instance.Disconnect("gem_temple_triggered", new Callable(this, nameof(_OnGemTempleTriggered)));
    Event.Instance.Disconnect("gem_engine_started", new Callable(this, nameof(_OnGemEngineStarted)));
    Event.Instance.Disconnect("gem_put_in_temple", new Callable(this, nameof(_OnGemPutInTemple)));
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

  private void _OnMenuButtonPressed(int menuButton)
  {
    _OnPlaySfx("menuSelect");
  }

  private void _OnGemCollected(string color, Vector2 position, SpriteFrames x)
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
