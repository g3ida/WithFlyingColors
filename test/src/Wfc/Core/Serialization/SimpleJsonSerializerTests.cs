namespace Wfc.Core.Serialization.Test;

using System.Collections.Generic;
using System.Text.Json;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using Wfc.Screens.Levels;

// The serializer registers three converters and then used to call the parameterless
// Serialize/Deserialize overloads, so none of them ever ran. Every assertion here fails
// against that version: enums came out as ordinals, so inserting a level anywhere but
// the end of LevelId silently pointed every existing save at a different level.
public class SimpleJsonSerializerTests(Node testScene) : TestClass(testScene) {
  private readonly SimpleJsonSerializer _serializer = new();

  private static SlotMetaData _metaData() =>
    new(0, 1_700_000_000UL, LevelId.Level1, 40, 1_700_000_001UL);

  [Test]
  public void WritesTheLevelIdByNameNotByOrdinal() {
    var json = _serializer.Serialize(_metaData());

    json.ShouldContain("\"LevelId\":\"Level1\"");
  }

  [Test]
  public void RoundTripsEveryMetaDataField() {
    var original = _metaData();

    var restored = _serializer.Deserialize<SlotMetaData>(_serializer.Serialize(original));

    restored.ShouldNotBeNull();
    restored.SlotId.ShouldBe(original.SlotId);
    restored.SaveTimestamp.ShouldBe(original.SaveTimestamp);
    restored.LastLoadDate.ShouldBe(original.LastLoadDate);
    restored.LevelId.ShouldBe(original.LevelId);
    restored.Progress.ShouldBe(original.Progress);
  }

  // A name survives members being added to LevelId; an ordinal does not. Reading a save
  // written by the pre-fix build would give whatever level now sits at that index.
  [Test]
  public void ReadsALevelIdWrittenAsAName() {
    var json = "{\"SlotId\":1,\"SaveTimestamp\":1700000000,\"LastLoadDate\":1700000001," +
      "\"LevelId\":\"Tutorial\",\"Progress\":7}";

    _serializer.Deserialize<SlotMetaData>(json)!.LevelId.ShouldBe(LevelId.Tutorial);
  }

  // SlotMetaDataJsonConverter's own guard, which had never executed.
  [Test]
  public void RejectsMetaDataMissingARequiredProperty() {
    var exception = Should.Throw<JsonException>(
      () => _serializer.Deserialize<SlotMetaData>("{\"SlotId\":0}")
    );

    exception.Message.ShouldBe("Missing required property");
  }

  // The level-state file is a string->object map. Without the converter the values come
  // back as JsonElement, which no IPersistent.Load can read.
  [Test]
  public void ReadsDictionaryValuesAsPlainClrTypes() {
    var restored = _serializer.Deserialize<Dictionary<string, object>>(
      "{\"count\":3,\"name\":\"gem\",\"collected\":true}"
    );

    restored.ShouldNotBeNull();
    restored["count"].ShouldBe(3);
    restored["name"].ShouldBe("gem");
    restored["collected"].ShouldBe(true);
  }
}
