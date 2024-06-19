using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

public class DictionaryStringObjectJsonConverter : JsonConverter<Dictionary<string, object>>
{
    public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        var dictionary = new Dictionary<string, object>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return dictionary;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }

            string propertyName = reader.GetString();

            reader.Read();
            dictionary[propertyName] = ReadValue(ref reader, options);
        }

        throw new JsonException();
    }

    private object ReadValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.Number:
                if (reader.TryGetInt32(out int intValue))
                    return intValue;
                if (reader.TryGetInt64(out long longValue))
                    return longValue;
                if (reader.TryGetDouble(out double doubleValue))
                    return doubleValue;
                return reader.GetDecimal();
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.StartObject:
                return JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options);
            case JsonTokenType.StartArray:
                var list = new List<object>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    list.Add(ReadValue(ref reader, options));
                }
                return list;
            default:
                throw new JsonException();
        }
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
