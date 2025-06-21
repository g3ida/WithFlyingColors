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

  public string Serialize<T>(T obj) => System.Text.Json.JsonSerializer.Serialize(obj);
  public T? Deserialize<T>(string data) => System.Text.Json.JsonSerializer.Deserialize<T>(data);
}

