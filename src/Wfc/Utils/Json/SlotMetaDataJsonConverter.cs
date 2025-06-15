namespace Wfc.Utils.Json;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wfc.Core.Persistence;
using Wfc.Screens.Levels;

public class SlotMetaDataJsonConverter : JsonConverter<SlotMetaData> {
  public override SlotMetaData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
    int? slotId = null;
    ulong? saveTimestamp = null;
    ulong? lastLoadDate = null;
    LevelId? levelId = null;
    int? progress = null;

    if (reader.TokenType != JsonTokenType.StartObject) {
      throw new JsonException();
    }

    while (reader.Read()) {
      if (reader.TokenType == JsonTokenType.EndObject) {
        break;
      }

      if (reader.TokenType != JsonTokenType.PropertyName) {
        throw new JsonException();
      }

      var propertyName = reader.GetString();
      reader.Read();

      switch (propertyName) {
        case nameof(SlotMetaData.SlotId):
          slotId = reader.GetInt32();
          break;
        case nameof(SlotMetaData.SaveTimestamp):
          saveTimestamp = reader.GetUInt64();
          break;
        case nameof(SlotMetaData.LastLoadDate):
          lastLoadDate = reader.GetUInt64();
          break;
        case nameof(SlotMetaData.LevelId):
          // Assumes LevelId is serializable as string or int; adjust as needed
          levelId = JsonSerializer.Deserialize<LevelId>(ref reader, options);
          break;
        case nameof(SlotMetaData.Progress):
          progress = reader.GetInt32();
          break;
        default:
          reader.Skip();
          break;
      }
    }

    if (slotId == null || saveTimestamp == null || levelId == null || progress == null || lastLoadDate == null) {
      throw new JsonException("Missing required property");
    }

    return new SlotMetaData(slotId.Value, saveTimestamp.Value, levelId ?? LevelId.Tutorial, progress.Value, lastLoadDate.Value);
  }

  public override void Write(Utf8JsonWriter writer, SlotMetaData value, JsonSerializerOptions options) {
    writer.WriteStartObject();
    writer.WriteNumber(nameof(SlotMetaData.SlotId), value.SlotId);
    writer.WriteNumber(nameof(SlotMetaData.SaveTimestamp), value.SaveTimestamp);
    writer.WriteNumber(nameof(SlotMetaData.LastLoadDate), value.LastLoadDate);
    writer.WritePropertyName(nameof(SlotMetaData.LevelId));
    JsonSerializer.Serialize(writer, value.LevelId, options);
    writer.WriteNumber(nameof(SlotMetaData.Progress), value.Progress);
    writer.WriteEndObject();
  }
}
