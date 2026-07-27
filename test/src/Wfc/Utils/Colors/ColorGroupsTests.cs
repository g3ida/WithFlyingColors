namespace Wfc.Utils.Colors.Test;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Utils.Colors;

// These four integers are level data: every brick-breaker arena `.tscn` in the project names its
// bricks' colors as tile source ids, and a face of the wrong color is fatal - so the mapping
// decides which bricks kill the player. It used to be nothing but the declaration order of a plain
// string[], which is exactly the kind of literal somebody tidies.
public class ColorGroupsTests(Node testScene) : TestClass(testScene) {
  [Test]
  public void TileSourceIdsNameTheColorsSavedInTheArenas() {
    ColorUtils.FromTileSourceId(0).ShouldBe(ColorUtils.BLUE);
    ColorUtils.FromTileSourceId(1).ShouldBe(ColorUtils.PINK);
    ColorUtils.FromTileSourceId(2).ShouldBe(ColorUtils.YELLOW);
    ColorUtils.FromTileSourceId(3).ShouldBe(ColorUtils.PURPLE);
  }

  [Test]
  public void AnUnknownTileSourceIdIsNotAColor() {
    ColorUtils.FromTileSourceId(-1).ShouldBeNull();
    ColorUtils.FromTileSourceId(ColorUtils.TILE_SOURCE_ID_COUNT).ShouldBeNull();
  }

  // The tilemap loop walks 0 up to this count, so a color that is not reachable that way would
  // simply never be placed.
  [Test]
  public void EveryTileSourceIdInRangeNamesAColor() {
    for (var tileSourceId = 0; tileSourceId < ColorUtils.TILE_SOURCE_ID_COUNT; tileSourceId++) {
      ColorUtils.COLOR_GROUPS.ShouldContain(ColorUtils.FromTileSourceId(tileSourceId));
    }
  }

  [Test]
  public void TheCountCoversEveryColorGroup() {
    ColorUtils.TILE_SOURCE_ID_COUNT.ShouldBe(ColorUtils.COLOR_GROUPS.Length);
  }
}
