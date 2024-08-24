using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Wfc.Core.Event;

public partial class SaveGame : Node2D {
  private const string SAVE_FILE_PATH = "user://save_slot_{0}.save";
  private const string SAVE_IMAGE_PATH = "user://save_slot_img_{0}.png";
  private const string SAVE_INFO_PATH = "user://save_info.save";
  private const int NUM_SLOTS = 3;

  private static readonly string[] SAVE_SLOTS = {
        string.Format(SAVE_FILE_PATH, 1),
        string.Format(SAVE_FILE_PATH, 2),
        string.Format(SAVE_FILE_PATH, 3)
    };

  private static readonly string[] SAVE_IMAGE_SLOTS = {
        string.Format(SAVE_IMAGE_PATH, 1),
        string.Format(SAVE_IMAGE_PATH, 2),
        string.Format(SAVE_IMAGE_PATH, 3)
    };

  private List<bool> isSlotFilledArray = new List<bool>();
  private List<Dictionary<string, object>> slotMetaData = new List<Dictionary<string, object>>();
  private List<ulong?> slotLastLoadDate = new List<ulong?>();
  private bool hasFilledSlots = false;
  public int currentSlotIndex = 0;

  private readonly JsonSerializerOptions _serializationOptions = new JsonSerializerOptions();


  private const string NODE_PATH_VAR = "_node_path_";
  private const string SAVE_DATE_VAR = "_save_date_";

  private static SaveGame _instance = null;

  public static SaveGame Instance() {
    return _instance;
  }

  public override void _Ready() {
    base._Ready();
    _serializationOptions.Converters.Add(new DictionaryStringObjectJsonConverter());
    _instance = GetTree().Root.GetNode<SaveGame>("SaveGameCS");

    SetProcess(false);
    ConnectSignals(); // FIXME: move this to _EnterTree after preload scripts full migration to c#
    Init();
  }

  public override void _EnterTree() {
    //Event.Instance.Connect("checkpoint_reached", this, nameof(OnCheckpoint));
    //ConnectSignals();
  }

  public void ConnectSignals() {
    Event.Instance.Connect(EventType.CheckpointReached, new Callable(this, nameof(OnCheckpoint)));
  }

  public void DisconnectSignals() {
    Event.Instance.Disconnect(EventType.CheckpointReached, new Callable(this, nameof(OnCheckpoint)));
  }

  public override void _ExitTree() {
    DisconnectSignals();
  }

  public void Init(bool createSlotIfEmpty = true) {
    Refresh();
    currentSlotIndex = GetMostRecentlyLoadedSlotIndex();

    // Uncomment to reset the game then comment it back
    // RemoveAllSaveSlots();

    if (createSlotIfEmpty && !hasFilledSlots) {
      currentSlotIndex = 0;
      Save(currentSlotIndex, true);
      Init();
    }
  }

  public void Refresh() {
    InitCheckFilledSlots();
    InitSaveSlotMetaData();
    InitSlotLastLoadDate();
  }

  private void InitCheckFilledSlots() {
    isSlotFilledArray.Clear();
    hasFilledSlots = false;

    foreach (var slotPath in SAVE_SLOTS) {
      bool exists = FileAccess.FileExists(slotPath);
      hasFilledSlots = exists || hasFilledSlots;
      isSlotFilledArray.Add(exists);
    }
  }

  private void InitSaveSlotMetaData() {
    slotMetaData.Clear();

    for (int slotIdx = 0; slotIdx < NUM_SLOTS; slotIdx++) {
      if (IsSlotFilled(slotIdx)) {
        var saveFile = FileAccess.Open(GetSaveSlotFilePath(slotIdx), FileAccess.ModeFlags.Read);
        var metaData = JsonSerializer.Deserialize<Dictionary<string, object>>(saveFile.GetLine(), _serializationOptions);
        slotMetaData.Add(metaData);
        saveFile.Close();
      }
      else {
        slotMetaData.Add(null);
      }
    }
  }

  private void InitSlotLastLoadDate() {
    if (!_LoadGameInfo()) {
      slotLastLoadDate.Clear();
      for (int i = 0; i < NUM_SLOTS; i++) {
        slotLastLoadDate.Add(null);
      }
    }
  }

  private ulong GetUnixTimestamp() {
    return (ulong)Time.GetUnixTimeFromSystem();
  }

  private Dictionary<string, object> GenerateSaveMetaData(int saveSlotIndex) {
    return new Dictionary<string, object>
        {
            { "image_path", GetSaveSlotImagePath(saveSlotIndex) },
            { "save_time", GetUnixTimestamp() },
            // fixme: this creates issues of scene paths ? use enums instead ?
            { "scene_path", GetTree().CurrentScene.GetChild(0).SceneFilePath },
            { "progress", 0 } // TODO: calculate progress
        };
  }

  public void Save(int saveSlotIndex, bool newEmptySlot = false) {
    var filePath = GetSaveSlotFilePath(saveSlotIndex);
    var saveFile = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);

    var saveMetaData = GenerateSaveMetaData(saveSlotIndex);
    SaveScreenshot(saveMetaData["image_path"].ToString());
    saveFile.StoreLine(JsonSerializer.Serialize(saveMetaData));

    if (!newEmptySlot) {
      var saveNodes = GetTree().GetNodesInGroup("persist");
      foreach (Node node in saveNodes) {
        if (node.SceneFilePath.Length == 0) {
          GD.PushError($"persistent node '{node.Name}' is not an instanced scene, skipped");
          continue;
        }
        var persistent = node as IPersistent;
        if (persistent == null) {
          GD.PushError($"persistent node '{node.Name}' does not implement IPersistent, skipped");
          continue;
        }

        var nodeData = persistent.save();
        nodeData[NODE_PATH_VAR] = node.GetPath().ToString();
        saveFile.StoreLine(JsonSerializer.Serialize(nodeData));
      }
    }

    currentSlotIndex = saveSlotIndex;
    saveFile.Close();
    UpdateSlotLoadDate(saveSlotIndex);
  }

  private void LoadLevel(int saveSlotIndex) {
    var filePath = GetSaveSlotFilePath(saveSlotIndex);

    if (!IsSlotFilled(saveSlotIndex)) {
      GD.PushError("FILE NOT FOUND"); // need to recover
      return; // Error! We don't have a save to load.
    }

    var saveGame = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
    var metaData = JsonSerializer.Deserialize<Dictionary<string, object>>(saveGame.GetLine()); // the metadata is ignored

    while (saveGame.GetPosition() < saveGame.GetLength()) {
      var nodeData = JsonSerializer.Deserialize<Dictionary<string, object>>(saveGame.GetLine(), _serializationOptions);
      var nodePath = nodeData[NODE_PATH_VAR].ToString();
      var obj = GetNode(nodePath) as Node;
      if (obj is IPersistent persistent) {
        persistent.load(nodeData);
      }
      else {
        GD.PushError($"Node '{nodePath}' not found, skipping");
        continue;
      }
    }

    currentSlotIndex = saveSlotIndex;
    saveGame.Close();
    UpdateSlotLoadDate(saveSlotIndex); // update last load date needed for the continue button

    // Update camera position to the player position avoiding smoothing
    // which would make you see the camera move quickly to the checkpoint position
    // when we load a level. We put it here instead of the reset method because
    // I like the smoothing effect when the player loses
    Global.Instance().Camera.update_position(Global.Instance().Player.GlobalPosition);
  }

  private void OnCheckpoint(Node checkpoint) {
    CallDeferred(nameof(SaveToCurrentSlot));
  }

  private string GetSaveSlotFilePath(int saveSlotIndex) {
    return SAVE_SLOTS[saveSlotIndex];
  }

  private string GetSaveSlotImagePath(int saveSlotIndex) {
    return SAVE_IMAGE_SLOTS[saveSlotIndex];
  }

  public bool IsSlotFilled(int saveSlotIndex) {
    return isSlotFilledArray[saveSlotIndex];
  }

  public bool DoesSlotHaveProgress(int saveSlotIndex) {
    return IsSlotFilled(saveSlotIndex) && Convert.ToSingle(slotMetaData[saveSlotIndex]["progress"]) > Constants.EPSILON2;
  }

  public Dictionary<string, object> GetSlotMetaData(int saveSlotIndex) {
    return slotMetaData[saveSlotIndex];
  }

  public Dictionary<string, object> GetCurrentSlotMetaData() {
    return GetSlotMetaData(currentSlotIndex);
  }

  public void SaveToCurrentSlot() {
    Save(currentSlotIndex);
  }

  public bool LoadIfNeeded() {
    if (IsSlotFilled(currentSlotIndex)) {
      LoadLevel(currentSlotIndex);
      return true;
    }

    return false;
  }

  public void RemoveSaveSlot(int saveSlotIndex) {
    if (IsSlotFilled(saveSlotIndex)) {
      DirAccess.RemoveAbsolute(GetSaveSlotFilePath(saveSlotIndex));
      DirAccess.RemoveAbsolute(GetSaveSlotImagePath(saveSlotIndex));

      // reset currently selected slot
      if (currentSlotIndex == saveSlotIndex) {
        currentSlotIndex = -1;
      }
    }
  }

  private void RemoveAllSaveSlots() {
    for (int slotIdx = 0; slotIdx < isSlotFilledArray.Count; slotIdx++) {
      RemoveSaveSlot(slotIdx);
    }
  }

  private void SaveScreenshot(string filePath) {
    var image = GetViewport().GetTexture().GetImage();
    image.FlipY();
    image.ShrinkX2(); // TODO: resize image to fixed size
    image.SavePng(filePath);
  }

  private ImageTexture LoadImage(string filePath) {
    if (FileAccess.FileExists(filePath)) {
      var image = new Image();
      image.Load(filePath);
      var texture = ImageTexture.CreateFromImage(image);
      return texture;
    }

    return null;
  }

  public ImageTexture LoadSlotImage(int saveSlotIndex) {
    if (IsSlotFilled(saveSlotIndex)) {
      return LoadImage(GetSaveSlotImagePath(saveSlotIndex));
    }

    return null;
  }

  private int GetMostRecentlyLoadedSlotIndex() {
    int maxSlotIndex = 0;
    ulong maxSlotTime = 0;

    for (int slotIdx = 0; slotIdx < NUM_SLOTS; slotIdx++) {
      if (IsSlotFilled(slotIdx)) {
        if (slotLastLoadDate[slotIdx] > maxSlotTime) {
          maxSlotTime = slotLastLoadDate[slotIdx].Value;
          maxSlotIndex = slotIdx;
        }
      }
    }

    return maxSlotIndex;
  }

  private int GetMostRecentSavedSlotIndex() {
    int maxSlotIndex = 0;
    long maxSlotTime = -1;

    for (int slotIdx = 0; slotIdx < NUM_SLOTS; slotIdx++) {
      if (IsSlotFilled(slotIdx)) {
        var saveTime = Convert.ToInt64(slotMetaData[slotIdx]["save_time"]);
        if (saveTime > maxSlotTime) {
          maxSlotTime = saveTime;
          maxSlotIndex = slotIdx;
        }
      }
    }

    return maxSlotIndex;
  }

  private void _SaveGameInfo() {
    var data = new Dictionary<string, object>
        {
            { "slot_last_load_date", slotLastLoadDate }
        };

    var saveFile = FileAccess.Open(SAVE_INFO_PATH, FileAccess.ModeFlags.Write);
    saveFile.StoreLine(JsonSerializer.Serialize(data));
    saveFile.Close();
  }

  private bool _LoadGameInfo() {
    if (FileAccess.FileExists(SAVE_INFO_PATH)) {
      var loadFile = FileAccess.Open(SAVE_INFO_PATH, FileAccess.ModeFlags.Read);
      var data = JsonSerializer.Deserialize<Dictionary<string, object>>(loadFile.GetLine(), _serializationOptions);
      var list = (List<object>)data["slot_last_load_date"];
      slotLastLoadDate = list.Select<object, ulong?>(el => (el == null) ? null : Convert.ToUInt64(el)).ToList();
      loadFile.Close();
      return true;
    }

    return false;
  }

  public void UpdateSlotLoadDate(int slotIndex) {
    slotLastLoadDate[slotIndex] = GetUnixTimestamp();
    _SaveGameInfo();
  }
}
