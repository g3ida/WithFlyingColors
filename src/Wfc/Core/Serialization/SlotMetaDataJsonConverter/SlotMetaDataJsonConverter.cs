namespace Wfc.Core.Serialization;

using System;
using System.Collections.Generic;
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
    HashSet<LevelId>? clearedLevels = null;
    Dictionary<LevelId, HashSet<string>>? collectedGems = null;

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
          levelId = JsonSerializer.Deserialize<LevelId>(ref reader, options);
          break;
        case nameof(SlotMetaData.Progress):
          progress = reader.GetInt32();
          break;
        case nameof(SlotMetaData.ClearedLevels):
          clearedLevels = JsonSerializer.Deserialize<HashSet<LevelId>>(ref reader, options);
          break;
        case nameof(SlotMetaData.CollectedGems):
          collectedGems = _readCollectedGems(ref reader, options);
          break;
        default:
          reader.Skip();
          break;
      }
    }

    // ClearedLevels and CollectedGems are deliberately not in this check: saves written
    // before completion or gem tracking existed have no such properties, and they must
    // keep loading as slots with nothing cleared or collected yet.
    if (slotId == null || saveTimestamp == null || levelId == null || progress == null || lastLoadDate == null) {
      throw new JsonException("Missing required property");
    }

    return new SlotMetaData(slotId.Value, saveTimestamp.Value, levelId ?? LevelId.Tutorial, progress.Value, lastLoadDate.Value, clearedLevels, collectedGems);
  }

  // Keys are level names, not ordinals, for the same reason LevelId itself is written
  // by name: a member added to the middle of the enum must not remap every existing
  // save. A key that no longer parses is dropped rather than rejected, so a save from
  // a build with an extra level still loads.
  private static Dictionary<LevelId, HashSet<string>> _readCollectedGems(ref Utf8JsonReader reader, JsonSerializerOptions options) {
    if (reader.TokenType != JsonTokenType.StartObject) {
      throw new JsonException();
    }

    var collectedGems = new Dictionary<LevelId, HashSet<string>>();
    while (reader.Read()) {
      if (reader.TokenType == JsonTokenType.EndObject) {
        break;
      }
      if (reader.TokenType != JsonTokenType.PropertyName) {
        throw new JsonException();
      }

      var levelName = reader.GetString();
      reader.Read();
      if (Enum.TryParse<LevelId>(levelName, out var level)) {
        var gems = JsonSerializer.Deserialize<HashSet<string>>(ref reader, options);
        collectedGems[level] = gems ?? [];
      }
      else {
        reader.Skip();
      }
    }
    return collectedGems;
  }

  public override void Write(Utf8JsonWriter writer, SlotMetaData value, JsonSerializerOptions options) {
    writer.WriteStartObject();
    writer.WriteNumber(nameof(SlotMetaData.SlotId), value.SlotId);
    writer.WriteNumber(nameof(SlotMetaData.SaveTimestamp), value.SaveTimestamp);
    writer.WriteNumber(nameof(SlotMetaData.LastLoadDate), value.LastLoadDate);
    writer.WritePropertyName(nameof(SlotMetaData.LevelId));
    JsonSerializer.Serialize(writer, value.LevelId, options);
    writer.WriteNumber(nameof(SlotMetaData.Progress), value.Progress);
    writer.WritePropertyName(nameof(SlotMetaData.ClearedLevels));
    JsonSerializer.Serialize(writer, value.ClearedLevels, options);
    writer.WritePropertyName(nameof(SlotMetaData.CollectedGems));
    writer.WriteStartObject();
    foreach (var (level, gems) in value.CollectedGems) {
      writer.WritePropertyName(level.ToString());
      JsonSerializer.Serialize(writer, gems, options);
    }
    writer.WriteEndObject();
    writer.WriteEndObject();
  }
}
