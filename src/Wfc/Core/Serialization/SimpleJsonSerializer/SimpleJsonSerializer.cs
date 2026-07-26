namespace Wfc.Core.Serialization;

using System.Text.Json;
using System.Text.Json.Serialization;

public class SimpleJsonSerializer : ISerializer {

  private readonly JsonSerializerOptions _serializationOptions = new();

  public SimpleJsonSerializer() {
    _serializationOptions.Converters.Add(new DictionaryStringObjectJsonConverter());
    _serializationOptions.Converters.Add(new JsonStringEnumConverter());
    _serializationOptions.Converters.Add(new SlotMetaDataJsonConverter());
  }

  // Both calls have to pass the options or the three converters registered above are
  // dead: enums would persist as ordinals, so inserting a level in the middle of LevelId
  // would silently point every existing save at a different level.
  public string Serialize<T>(T obj) => JsonSerializer.Serialize(obj, _serializationOptions);
  public T? Deserialize<T>(string data) => JsonSerializer.Deserialize<T>(data, _serializationOptions);
}

