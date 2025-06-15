namespace Wfc.Utils.Json;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

public class DictionaryStringObjectJsonConverter : JsonConverter<Dictionary<string, object>> {
  public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
    if (reader.TokenType != JsonTokenType.StartObject) {
      throw new JsonException("Expected object start");
    }

    var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    while (reader.Read()) {
      if (reader.TokenType == JsonTokenType.EndObject) {
        return dictionary;
      }

      if (reader.TokenType != JsonTokenType.PropertyName) {
        throw new JsonException("Expected property name");
      }

      var propertyName = reader.GetString() ?? throw new JsonException("Null property name");
      reader.Read();

      var value = ReadValue(ref reader, options);

      // Only add non-null values to maintain dictionary consistency
      if (value != null) {
        dictionary[propertyName] = value;
      }
    }

    throw new JsonException("Unexpected end of JSON");
  }

  private static object? ReadValue(ref Utf8JsonReader reader, JsonSerializerOptions options) {
    switch (reader.TokenType) {
      case JsonTokenType.String:
        return reader.GetString();

      case JsonTokenType.Number:
        if (reader.TryGetInt32(out var intValue)) {
          return intValue;
        }
        if (reader.TryGetInt64(out var longValue)) {
          return longValue;
        }
        if (reader.TryGetDouble(out var doubleValue)) {
          return doubleValue;
        }
        return reader.GetDecimal();

      case JsonTokenType.True:
      case JsonTokenType.False:
        return reader.GetBoolean();

      case JsonTokenType.Null:
        return null;

      case JsonTokenType.StartObject:
        return JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options)
            ?? [];

      case JsonTokenType.StartArray:
        var list = new List<object?>();
        while (reader.Read()) {
          if (reader.TokenType == JsonTokenType.EndArray) {
            return list;
          }
          list.Add(ReadValue(ref reader, options));
        }
        throw new JsonException("Unclosed array");
      case JsonTokenType.None:
      case JsonTokenType.EndObject:
      case JsonTokenType.EndArray:
      case JsonTokenType.PropertyName:
      case JsonTokenType.Comment:
      default:
        throw new JsonException("unknown token type: " + reader.TokenType);
    }
  }

  public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options) {
    writer.WriteStartObject();

    foreach (var kvp in value) {
      writer.WritePropertyName(kvp.Key);
      JsonSerializer.Serialize(writer, kvp.Value, options);
    }

    writer.WriteEndObject();
  }
}
