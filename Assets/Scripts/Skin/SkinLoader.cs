using Godot;
using System.Text.Json;
using System.Collections.Generic;

public static class SkinLoader {
    public static readonly string[] KEYS = { "basic", "dark", "light", "dark2", "light2", "background" };

    public static readonly Dictionary<string, string[]> DEFAULT_SKIN = new Dictionary<string, string[]>
    {
        { "dark2", new string[] { "00c8d9", "d80071", "8808cf", "b5e300" } },
        { "dark", new string[] { "00d3e5", "e50078", "9208dd", "beed00" } },
        { "basic", new string[] { "00ebff", "ff0085", "a209f6", "ccff00" } },
        { "light", new string[] { "37efff", "ff2597", "ac25f6", "d8ff38" } },
        { "light2", new string[] { "5cf1ff", "ff38a0", "b236f6", "dfff5c" } },
        { "background", new string[] { "8cf4ff", "ff87c6", "ba7add", "e8ff8c" } }
    };

    public static readonly Dictionary<string, string[]> GOOGL_SKIN = new Dictionary<string, string[]>
    {
        { "dark2", new string[] { "3597d9", "2e9148", "e3a52b", "c93c29" } },
        { "dark", new string[] { "38a0e5", "319c4d", "f0ae2e", "d43f2b" } },
        { "basic", new string[] { "3eb2ff", "37b057", "ffb831", "e2432e" } },
        { "light", new string[] { "37efff", "49b566", "ffbf45", "e5513d" } },
        { "light2", new string[] { "61c0ff", "51bd6f", "ffc454", "eb5a47" } },
        { "background", new string[] { "8cd1ff", "6ec784", "ffd78c", "f29285" } }
    };

    public static Dictionary<string, List<string>> LoadSkin(string fileName) {
        if (FileAccess.FileExists(fileName)) {
            var file = FileAccess.Open(fileName, FileAccess.ModeFlags.Read);
            string jsonData = file.GetLine();
            file.Close();

            JsonSerializerOptions _serializationOptions = new JsonSerializerOptions();
            _serializationOptions.Converters.Add(new DictionaryStringObjectJsonConverter());
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData, _serializationOptions);
            if (HasAllKeys(data)) {
                return ConvertToDictionary(data);
            }
        }
        return null;
    }

    private static bool HasAllKeys(Dictionary<string, object> data) {
        foreach (string key in KEYS) {
            if (!data.ContainsKey(key)) {
                return false;
            }
        }
        return true;
    }

    private static Dictionary<string, List<string>> ConvertToDictionary(Dictionary<string, object> data) {
        var result = new Dictionary<string, List<string>>();
        foreach (var key in data.Keys) {
            var colorArray = data[key] as Godot.Collections.Array;
            var colorList = new List<string>();
            foreach (var color in colorArray) {
                colorList.Add(color.ToString());
            }
            result[key] = colorList;
        }
        return result;
    }
}
