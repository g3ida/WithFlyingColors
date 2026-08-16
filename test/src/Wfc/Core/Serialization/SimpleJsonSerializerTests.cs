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

  // Saves written before completion tracking existed have no ClearedLevels property.
  // They must load as slots with nothing cleared, not be rejected as corrupt.
  [Test]
  public void ReadsAMetaDataWrittenBeforeCompletionTrackingExisted() {
    var json = "{\"SlotId\":1,\"SaveTimestamp\":1700000000,\"LastLoadDate\":1700000001," +
      "\"LevelId\":\"Tutorial\",\"Progress\":7}";

    var restored = _serializer.Deserialize<SlotMetaData>(json);

    restored.ShouldNotBeNull();
    restored.ClearedLevels.ShouldBeEmpty();
  }

  [Test]
  public void RoundTripsTheClearedLevelsByName() {
    var original = new SlotMetaData(0, 1_700_000_000UL, LevelId.Level1, 40, 1_700_000_001UL, [LevelId.Tutorial]);

    var json = _serializer.Serialize(original);
    var restored = _serializer.Deserialize<SlotMetaData>(json);

    json.ShouldContain("\"ClearedLevels\":[\"Tutorial\"]");
    restored!.ClearedLevels.ShouldBe([LevelId.Tutorial]);
  }

  // Same tolerance as ClearedLevels: saves from before gem banking existed must load
  // as slots with nothing banked, not be rejected.
  [Test]
  public void ReadsAMetaDataWrittenBeforeGemTrackingExisted() {
    var json = "{\"SlotId\":1,\"SaveTimestamp\":1700000000,\"LastLoadDate\":1700000001," +
      "\"LevelId\":\"Tutorial\",\"Progress\":7}";

    var restored = _serializer.Deserialize<SlotMetaData>(json);

    restored.ShouldNotBeNull();
    restored.CollectedGems.ShouldBeEmpty();
    restored.GemsCollectedIn(LevelId.Tutorial).ShouldBeEmpty();
  }

  // Levels are keyed by name for the same reason LevelId itself is: names survive
  // members being added to the enum, ordinals do not.
  [Test]
  public void RoundTripsTheCollectedGemsByLevelName() {
    var original = new SlotMetaData(0, 1_700_000_000UL, LevelId.Level1, 40, 1_700_000_001UL,
      collectedGems: new Dictionary<LevelId, HashSet<string>> {
        [LevelId.Tutorial] = ["blue", "pink"],
      });

    var json = _serializer.Serialize(original);
    var restored = _serializer.Deserialize<SlotMetaData>(json);

    json.ShouldContain("\"CollectedGems\":{\"Tutorial\":[");
    restored!.GemsCollectedIn(LevelId.Tutorial).ShouldBe(["blue", "pink"], ignoreOrder: true);
    restored.GemsCollectedIn(LevelId.Level1).ShouldBeEmpty();
  }

  // The one-off hub arrival is only ever worth anything if it survives a quit, and a save
  // from before it existed has to read as a run that has not been shown the room yet.
  [Test]
  public void RoundTripsTheHubArrivalAndDefaultsItForOlderSaves() {
    var original = new SlotMetaData(0, 1_700_000_000UL, LevelId.Hub, 0, 1_700_000_001UL) {
      HasSeenHubArrival = true,
    };

    var restored = _serializer.Deserialize<SlotMetaData>(_serializer.Serialize(original));
    var older = _serializer.Deserialize<SlotMetaData>(
      "{\"SlotId\":1,\"SaveTimestamp\":1700000000,\"LastLoadDate\":1700000001," +
      "\"LevelId\":\"Tutorial\",\"Progress\":7}");

    restored!.HasSeenHubArrival.ShouldBeTrue();
    older!.HasSeenHubArrival.ShouldBeFalse();
  }

  // A save written by a build with extra levels still loads here: the unknown level's
  // gems are dropped, not treated as corruption.
  [Test]
  public void DropsGemsOfALevelThisBuildDoesNotKnow() {
    var json = "{\"SlotId\":1,\"SaveTimestamp\":1700000000,\"LastLoadDate\":1700000001," +
      "\"LevelId\":\"Tutorial\",\"Progress\":7," +
      "\"CollectedGems\":{\"SomeFutureLevel\":[\"blue\"],\"Tutorial\":[\"pink\"]}}";

    var restored = _serializer.Deserialize<SlotMetaData>(json);

    restored.ShouldNotBeNull();
    restored.GemsCollectedIn(LevelId.Tutorial).ShouldBe(["pink"]);
    restored.CollectedGems.Count.ShouldBe(1);
  }

  // SlotMetaDataJsonConverter's own guard, which had never executed.
  [Test]
  public void RejectsMetaDataMissingARequiredProperty() {
    var exception = Should.Throw<JsonException>(
      () => _serializer.Deserialize<SlotMetaData>("{\"SlotId\":0}")
    );

    exception.Message.ShouldBe("Missing required property");
  }

  // A save file whose JSON parses but whose values are the wrong type. The converter reads
  // these scalars off Utf8JsonReader by hand, which answers a token of the wrong kind with
  // InvalidOperationException and a number that will not fit with FormatException - neither of
  // them a JsonException. What makes the slot degrade to "empty" rather than taking start-up
  // down is that System.Text.Json wraps both in a JsonException on the way out of a converter,
  // and JsonException is all SaveSlot.LoadMetaData and SaveManager catch. These pin that down:
  // it is load-bearing, and nothing in the converter itself says so.
  [Test]
  public void RejectsAStringWhereAWholeNumberBelongs() {
    var json = "{\"SlotId\":1,\"SaveTimestamp\":1700000000,\"LastLoadDate\":1700000001," +
      "\"LevelId\":\"Tutorial\",\"Progress\":\"50\"}";

    Should.Throw<JsonException>(() => _serializer.Deserialize<SlotMetaData>(json));
  }

  [Test]
  public void RejectsANullWhereAWholeNumberBelongs() {
    var json = "{\"SlotId\":null,\"SaveTimestamp\":1700000000,\"LastLoadDate\":1700000001," +
      "\"LevelId\":\"Tutorial\",\"Progress\":7}";

    Should.Throw<JsonException>(() => _serializer.Deserialize<SlotMetaData>(json));
  }

  [Test]
  public void RejectsATimestampThatIsNotANumber() {
    var json = "{\"SlotId\":1,\"SaveTimestamp\":\"yesterday\",\"LastLoadDate\":1700000001," +
      "\"LevelId\":\"Tutorial\",\"Progress\":7}";

    Should.Throw<JsonException>(() => _serializer.Deserialize<SlotMetaData>(json));
  }

  // A number too large for the field it lands in is corruption just as much as a string is.
  [Test]
  public void RejectsAWholeNumberThatWillNotFit() {
    var json = "{\"SlotId\":99999999999999,\"SaveTimestamp\":1700000000,\"LastLoadDate\":1700000001," +
      "\"LevelId\":\"Tutorial\",\"Progress\":7}";

    Should.Throw<JsonException>(() => _serializer.Deserialize<SlotMetaData>(json));
  }

  [Test]
  public void RejectsANonBooleanHubArrival() {
    var json = "{\"SlotId\":1,\"SaveTimestamp\":1700000000,\"LastLoadDate\":1700000001," +
      "\"LevelId\":\"Tutorial\",\"Progress\":7,\"HasSeenHubArrival\":\"yes\"}";

    Should.Throw<JsonException>(() => _serializer.Deserialize<SlotMetaData>(json));
  }

  // The counters are read by hand too, so they need the same guard as the fields above.
  [Test]
  public void RejectsACounterThatIsNotANumber() {
    var json = "{\"SlotId\":1,\"SaveTimestamp\":1700000000,\"LastLoadDate\":1700000001," +
      "\"LevelId\":\"Tutorial\",\"Progress\":7,\"Counters\":{\"Jumps\":\"lots\"}}";

    Should.Throw<JsonException>(() => _serializer.Deserialize<SlotMetaData>(json));
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
