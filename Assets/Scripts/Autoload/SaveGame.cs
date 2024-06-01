using Godot;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class SaveGame : Node2D
{
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
    private List<long?> slotLastLoadDate = new List<long?>();
    private bool hasFilledSlots = false;
    private int currentSlotIndex = 0;

    private const string NODE_PATH_VAR = "_node_path_";
    private const string SAVE_DATE_VAR = "_save_date_";

    private static SaveGame _instance = null;

    public static SaveGame Instance() {
      return _instance;
    }

    public override void _Ready()
    {
        base._Ready();
        _instance = GetTree().Root.GetNode<SaveGame>("SaveGameCS");
        SetProcess(false);
        ConnectSignals(); // FIXME: move this to _EnterTree after preload scripts full migation to c#
        Init();
    }

    public override void _EnterTree()
    {
        //Event.GdInstance().Connect("checkpoint_reached", this, nameof(OnCheckpoint));
        //ConnectSignals();
    }

    public void ConnectSignals() {
        Event.GdInstance().Connect("checkpoint_reached", this, nameof(OnCheckpoint));
    }

    public void DisconnectSignals() {
        Event.GdInstance().Disconnect("checkpoint_reached", this, nameof(OnCheckpoint));
    }

    public override void _ExitTree()
    {
        DisconnectSignals();
    }

    private void Init(bool createSlotIfEmpty = true)
    {
        Refresh();
        currentSlotIndex = GetMostRecentlyLoadedSlotIndex();

        // Uncomment to reset the game then comment it back
        // RemoveAllSaveSlots();

        if (createSlotIfEmpty && !hasFilledSlots)
        {
            currentSlotIndex = 0;
            Save(currentSlotIndex, true);
            Init();
        }
    }

    private void Refresh()
    {
        InitCheckFilledSlots();
        InitSaveSlotMetaData();
        InitSlotLastLoadDate();
    }

    private void InitCheckFilledSlots()
    {
        isSlotFilledArray.Clear();
        var file = new File();
        hasFilledSlots = false;

        foreach (var slotPath in SAVE_SLOTS)
        {
            bool exists = file.FileExists(slotPath);
            hasFilledSlots = exists || hasFilledSlots;
            isSlotFilledArray.Add(exists);
        }
    }

    private void InitSaveSlotMetaData()
    {
        slotMetaData.Clear();

        for (int slotIdx = 0; slotIdx < NUM_SLOTS; slotIdx++)
        {
            if (IsSlotFilled(slotIdx))
            {
                var saveFile = new File();
                saveFile.Open(GetSaveSlotFilePath(slotIdx), File.ModeFlags.Read);
                var metaData = JsonConvert.DeserializeObject<Dictionary<string, object>>(saveFile.GetLine());
                slotMetaData.Add(metaData);
                saveFile.Close();
            }
            else
            {
                slotMetaData.Add(null);
            }
        }
    }

    private void InitSlotLastLoadDate()
    {
        if (!_LoadGameInfo())
        {
            slotLastLoadDate.Clear();
            for (int i = 0; i < NUM_SLOTS; i++)
            {
                slotLastLoadDate.Add(null);
            }
        }
    }

    private ulong GetUnixTimestamp()
    {
        return OS.GetUnixTime();
    }

    private Dictionary<string, object> GenerateSaveMetaData(int saveSlotIndex)
    {
        return new Dictionary<string, object>
        {
            { "image_path", GetSaveSlotImagePath(saveSlotIndex) },
            { "save_time", GetUnixTimestamp() },
            { "scene_path", GetTree().CurrentScene.GetChild(0).Filename },
            { "progress", 0 } // TODO: calculate progress
        };
    }

    private void Save(int saveSlotIndex, bool newEmptySlot = false)
    {
        var filePath = GetSaveSlotFilePath(saveSlotIndex);
        var saveFile = new File();
        saveFile.Open(filePath, File.ModeFlags.Write);

        var saveMetaData = GenerateSaveMetaData(saveSlotIndex);
        SaveScreenshot(saveMetaData["image_path"].ToString());
        saveFile.StoreLine(JsonConvert.SerializeObject(saveMetaData));

        if (!newEmptySlot)
        {
            var saveNodes = GetTree().GetNodesInGroup("persist");
            foreach (Node node in saveNodes)
            {
                if (node.Filename.Empty())
                {
                    GD.PushError($"persistent node '{node.Name}' is not an instanced scene, skipped");
                    continue;
                }
                var persistant = node as IPersistant;
                if (persistant == null)
                {
                    GD.PushError($"persistent node '{node.Name}' does not implement IPersistant, skipped");
                    continue;
                }

                var nodeData = persistant.save();
                GD.Print($"Saving {node.GetPath()}");
                nodeData[NODE_PATH_VAR] = node.GetPath().ToString();
                saveFile.StoreLine(JsonConvert.SerializeObject(nodeData));
            }
        }

        currentSlotIndex = saveSlotIndex;
        saveFile.Close();
        UpdateSlotLoadDate(saveSlotIndex);
    }

    private void LoadLevel(int saveSlotIndex)
    {
        var filePath = GetSaveSlotFilePath(saveSlotIndex);
        var saveGame = new File();

        if (!IsSlotFilled(saveSlotIndex))
        {
            GD.PushError("FILE NOT FOUND"); // need to recover
            return; // Error! We don't have a save to load.
        }

        saveGame.Open(filePath, File.ModeFlags.Read);
        var metaData = JsonConvert.DeserializeObject<Dictionary<string, object>>(saveGame.GetLine()); // the metadata is ignored

        while (saveGame.GetPosition() < saveGame.GetLen())
        {
            var nodeData = JsonConvert.DeserializeObject<Dictionary<string, object>>(saveGame.GetLine());
            var nodePath = nodeData[NODE_PATH_VAR].ToString();
            var obj = GetNode(nodePath) as Node;
            if (obj is IPersistant persistant)
            {
                persistant.load(nodeData);
            } else
            {
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

    private void OnCheckpoint(object checkpoint)
    {
        CallDeferred(nameof(SaveToCurrentSlot));
    }

    private string GetSaveSlotFilePath(int saveSlotIndex)
    {
        return SAVE_SLOTS[saveSlotIndex];
    }

    private string GetSaveSlotImagePath(int saveSlotIndex)
    {
        return SAVE_IMAGE_SLOTS[saveSlotIndex];
    }

    private bool IsSlotFilled(int saveSlotIndex)
    {
        return isSlotFilledArray[saveSlotIndex];
    }

    private bool DoesSlotHaveProgress(int saveSlotIndex)
    {
        return IsSlotFilled(saveSlotIndex) && Convert.ToSingle(slotMetaData[saveSlotIndex]["progress"]) > Constants.EPSILON2;
    }

    public Dictionary<string, object> GetSlotMetaData(int saveSlotIndex)
    {
        return slotMetaData[saveSlotIndex];
    }

    public Dictionary<string, object> GetCurrentSlotMetaData()
    {
        return GetSlotMetaData(currentSlotIndex);
    }

    public void SaveToCurrentSlot()
    {
        Save(currentSlotIndex);
    }

    public bool LoadIfNeeded()
    {
        if (IsSlotFilled(currentSlotIndex))
        {
            LoadLevel(currentSlotIndex);
            return true;
        }

        return false;
    }

    private void RemoveSaveSlot(int saveSlotIndex)
    {
        if (IsSlotFilled(saveSlotIndex))
        {
            var dir = new Directory();
            dir.Remove(GetSaveSlotFilePath(saveSlotIndex));
            dir.Remove(GetSaveSlotImagePath(saveSlotIndex));

            // reset currently selected slot
            if (currentSlotIndex == saveSlotIndex)
            {
                currentSlotIndex = -1;
            }
        }
    }

    private void RemoveAllSaveSlots()
    {
        for (int slotIdx = 0; slotIdx < isSlotFilledArray.Count; slotIdx++)
        {
            RemoveSaveSlot(slotIdx);
        }
    }

    private void SaveScreenshot(string filePath)
    {
        var image = GetViewport().GetTexture().GetData();
        image.FlipY();
        image.ShrinkX2(); // TODO: resize image to fixed size
        image.SavePng(filePath);
    }

    private ImageTexture LoadImage(string filePath)
    {
        var file = new File();
        if (file.FileExists(filePath))
        {
            var texture = new ImageTexture();
            var image = new Image();
            image.Load(filePath);
            texture.CreateFromImage(image);
            return texture;
        }

        return null;
    }

    private ImageTexture LoadSlotImage(int saveSlotIndex)
    {
        if (IsSlotFilled(saveSlotIndex))
        {
            return LoadImage(GetSaveSlotImagePath(saveSlotIndex));
        }

        return null;
    }

    private int GetMostRecentlyLoadedSlotIndex()
    {
        int maxSlotIndex = 0;
        long maxSlotTime = -1;

        for (int slotIdx = 0; slotIdx < NUM_SLOTS; slotIdx++)
        {
            if (IsSlotFilled(slotIdx))
            {
                if (slotLastLoadDate[slotIdx] > maxSlotTime)
                {
                    maxSlotTime = slotLastLoadDate[slotIdx].Value;
                    maxSlotIndex = slotIdx;
                }
            }
        }

        return maxSlotIndex;
    }

    private int GetMostRecentSavedSlotIndex()
    {
        int maxSlotIndex = 0;
        long maxSlotTime = -1;

        for (int slotIdx = 0; slotIdx < NUM_SLOTS; slotIdx++)
        {
            if (IsSlotFilled(slotIdx))
            {
                var saveTime = Convert.ToInt64(slotMetaData[slotIdx]["save_time"]);
                if (saveTime > maxSlotTime)
                {
                    maxSlotTime = saveTime;
                    maxSlotIndex = slotIdx;
                }
            }
        }

        return maxSlotIndex;
    }

    private void _SaveGameInfo()
    {
        var data = new Dictionary<string, object>
        {
            { "slot_last_load_date", slotLastLoadDate }
        };

        var saveFile = new File();
        saveFile.Open(SAVE_INFO_PATH, File.ModeFlags.Write);
        saveFile.StoreLine(JsonConvert.SerializeObject(data));
        saveFile.Close();
    }

    private bool _LoadGameInfo()
    {
        var loadFile = new File();
        if (loadFile.FileExists(SAVE_INFO_PATH))
        {
            loadFile.Open(SAVE_INFO_PATH, File.ModeFlags.Read);
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(loadFile.GetLine());
            slotLastLoadDate = JsonConvert.DeserializeObject<List<long?>>(data["slot_last_load_date"].ToString());
            loadFile.Close();
            return true;
        }

        return false;
    }

    private void UpdateSlotLoadDate(int slotIndex)
    {
        slotLastLoadDate[slotIndex] = (long)GetUnixTimestamp();
        _SaveGameInfo();
    }
}
