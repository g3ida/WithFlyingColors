namespace Wfc.test.instrumented.Platforms;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Platforms;
using Wfc.Utils;
using Wfc.Utils.Colors;
using Wfc.Utils.Layers;
using Wfc.test.instrumented.Helpers;

// A flat platform is authored entirely from the inspector, and everything the level author sets
// there has to reach three places at once: the shape the shader draws, the box the player stands
// on, and the box that decides whether the colour they landed on was the right one. Nothing on
// screen says when one of the three has drifted from the other two - the platform simply catches
// a face it should not have, or drops one it should.
public class FlatPlatformTests(Node testScene) : TestClass(testScene) {
  private static readonly StringName SizeParam = "u_size";
  private static readonly StringName ChamferParam = "u_chamfer";
  private static readonly StringName ShadedEdgesParam = "u_shaded_edges";
  private static readonly StringName ReachParam = "u_reach";
  private static readonly StringName SpacingParam = "u_spacing";
  private static readonly StringName WidthParam = "u_width";
  private static readonly StringName SeedParam = "u_seed";

  private FlatPlatform _platform = default!;

  [Setup]
  public async Task Setup() {
    _platform = SceneHelpers.InstantiateNode<FlatPlatform>();
    TestScene.AddChild(_platform);
    await PhysicsFrames.Frame(TestScene);
  }

  [Cleanup]
  public void Cleanup() => _platform.QueueFree();

  [Test]
  public async Task ResizingCarriesTheCollisionBoxesWithIt() {
    var size = new Vector2(320f, 48f);
    _platform.Size = size;
    await PhysicsFrames.Frame(TestScene);

    _solidShape().Size.ShouldBe(size, "the player would stand somewhere other than on the platform");
    _colorShape().Size.ShouldBe(size, "a face would be judged over a different stretch than it landed on");
  }

  // The band and the slice are pixel widths the shader measures against this, so a size it never
  // hears about is drawn at whatever the scene was saved with.
  [Test]
  public async Task ResizingReachesTheShader() {
    var size = new Vector2(96f, 200f);
    _platform.Size = size;
    await PhysicsFrames.Frame(TestScene);

    _material().GetShaderParameter(SizeParam).AsVector2().ShouldBe(size);
    _surface().Size.ShouldBe(size);
  }

  // Squaring off the corners a neighbour hides is the whole point of the flags, and the shader is
  // the only thing that acts on them.
  [Test]
  public async Task TheSlicedCornersAndShadedEdgesReachTheShader() {
    _platform.SlicedCorners = FlatPlatform.Corners.TopLeft | FlatPlatform.Corners.BottomLeft;
    _platform.ShadedEdges = FlatPlatform.Edges.Bottom;
    await PhysicsFrames.Frame(TestScene);

    var chamfer = _material().GetShaderParameter(ChamferParam).AsVector4();
    chamfer.X.ShouldBeGreaterThan(0f, "the top-left corner is sliced but the shader squared it");
    chamfer.W.ShouldBeGreaterThan(0f, "the bottom-left corner is sliced but the shader squared it");
    chamfer.Y.ShouldBe(0f, "the top-right corner is squared but the shader sliced it");
    chamfer.Z.ShouldBe(0f, "the bottom-right corner is squared but the shader sliced it");

    _material().GetShaderParameter(ShadedEdgesParam).AsVector2().ShouldBe(new Vector2(0f, 1f));
  }

  [Test]
  public async Task AColoredPlatformOnlyAnswersToItsOwnColor() {
    _platform.Group = ColorUtils.PINK;
    await PhysicsFrames.Frame(TestScene);

    var area = _platform.GetNode<Area2D>("Area2D");
    area.IsInGroup(ColorUtils.PINK).ShouldBeTrue("the pink face has nothing to land on");
    area.IsInGroup(ColorUtils.BLUE).ShouldBeFalse("a blue face would survive a pink platform");
  }

  // The neutral platforms between the puzzles: whichever face reaches them lands. A group left
  // blank counts as neutral too, so a platform nobody got round to painting can be landed on
  // rather than being a lethal surface no one meant to author.
  [Test]
  public async Task ANeutralPlatformAnswersToEveryColor() {
    foreach (var group in new[] { FlatPlatform.NEUTRAL, string.Empty }) {
      var neutral = SceneHelpers.InstantiateNode<FlatPlatform>();
      neutral.Group = group;
      TestScene.AddChild(neutral);
      await PhysicsFrames.Frame(TestScene);

      var area = neutral.GetNode<Area2D>("Area2D");
      foreach (var colorGroup in ColorUtils.COLOR_GROUPS) {
        area.IsInGroup(colorGroup).ShouldBeTrue($"a {colorGroup} face falls through a \"{group}\" platform");
      }
      neutral.QueueFree();
    }
  }

  // A coat of paint is the top of the platform rather than a decoration on it, so what the cube is
  // judged against up there is the coat. A white platform under purple paint that still answered to
  // every face would be a purple surface the purple face is not needed for - which the player
  // learns by crossing it on the wrong one and living.
  [Test]
  public async Task AnInkedPlatformAnswersToTheColourOfItsCoat() {
    _platform.Group = FlatPlatform.NEUTRAL;
    _platform.Inked = true;
    _platform.InkColor = ColorUtils.PURPLE;
    await PhysicsFrames.Frame(TestScene);

    var coat = _platform.GetNode<Area2D>("InkArea");
    coat.IsInGroup(ColorUtils.PURPLE).ShouldBeTrue("the purple face has nothing to land on");
    coat.IsInGroup(ColorUtils.BLUE).ShouldBeFalse("a blue face survives paint it should die on");
    coat.Monitorable.ShouldBeTrue("no face ever reaches the coat");
    _surface().Color.ShouldBe(Colors.White, "the platform under the paint is not its own colour");
  }

  // The paint lies on the top and nowhere else. Brushing the side of a painted platform is touching
  // the platform, and being killed by a colour that is not on the surface you touched is a death
  // the player has no way to read.
  [Test]
  public async Task TheSidesOfAnInkedPlatformAnswerToThePlatformUnderThePaint() {
    _platform.Group = FlatPlatform.NEUTRAL;
    _platform.Inked = true;
    _platform.InkColor = ColorUtils.PURPLE;
    await PhysicsFrames.Frame(TestScene);

    var body = _platform.GetNode<Area2D>("Area2D");
    foreach (var colorGroup in ColorUtils.COLOR_GROUPS) {
      body.IsInGroup(colorGroup).ShouldBeTrue($"a {colorGroup} face dies against a side nobody painted");
    }
  }

  // A face is killed by any area it enters that is not its colour, so the two of them overlapping
  // would have the platform judging a landing the coat had already answered for - and on a coloured
  // platform under a coat of another colour, nothing could land on it at all.
  [Test]
  public async Task TheCoatAndTheBodyDivideThePlatformBetweenThem() {
    _platform.Size = new Vector2(256f, 96f);
    _platform.Group = ColorUtils.PINK;
    _platform.Inked = true;
    _platform.InkColor = ColorUtils.PURPLE;
    await PhysicsFrames.Frame(TestScene);

    var coat = _platform.GetNode<CollisionShape2D>("InkArea/InkAreaShape");
    var coatHeight = ((RectangleShape2D)coat.Shape).Size.Y;
    var body = _platform.GetNode<CollisionShape2D>("Area2D/ColorAreaShape");
    var bodyHeight = _colorShape().Size.Y;
    var top = -_platform.Size.Y / 2f;

    (coat.Position.Y - (coatHeight / 2f)).ShouldBe(top, "the coat is not lying on the surface");
    coatHeight.ShouldBeLessThan(_platform.Size.Y, "the coat answers for the whole platform");
    (body.Position.Y - (bodyHeight / 2f))
      .ShouldBe(coat.Position.Y + (coatHeight / 2f), "the coat and the body overlap");
    (body.Position.Y + (bodyHeight / 2f)).ShouldBe(-top, "the body stops short of the bottom");
  }

  // A ledge hanging over a drop is the one place the drips are worth authoring, and it is also the
  // thinnest thing in the level - so the length has to come off the field rather than the height.
  // The quad has to grow with it too, or the shader draws the longest drips cut off square.
  [Test]
  public async Task AnAuthoredDripLengthOutrunsTheHeightAndTheQuadGrowsWithIt() {
    _platform.Size = new Vector2(320f, 32f);
    _platform.Inked = true;
    _platform.InkColor = ColorUtils.PINK;
    _platform.InkDripLength = 260f;
    await PhysicsFrames.Frame(TestScene);

    var ink = _platform.GetNode<ColorRect>("Ink");
    _inkMaterial().GetShaderParameter(ReachParam).AsSingle().ShouldBe(260f);
    ink.Size.Y.ShouldBeGreaterThan(260f, "the longest drips are cut off by the quad drawing them");
    _inkMaterial().GetShaderParameter(SizeParam).AsVector2().ShouldBe(ink.Size);
  }

  // Both are authored as a multiple of the coat every platform already wears, so they mean the same
  // thing on a narrow ledge as on a wide one. The shader is handed the spacing it draws one drip
  // per, which is density the other way up.
  //
  // The two are held apart on purpose: asking for more drips used to give the same number of fatter
  // ones, because the shader measured a drip's thickness against the gap to the next.
  [Test]
  public async Task DensityDrawsMoreDripsWithoutFatteningThem() {
    _platform.Inked = true;
    _platform.InkColor = ColorUtils.PINK;
    _platform.InkDripDensity = 1f;
    await PhysicsFrames.Frame(TestScene);
    var spread = _inkMaterial().GetShaderParameter(SpacingParam).AsSingle();
    var thickness = _inkMaterial().GetShaderParameter(WidthParam).AsSingle();

    _platform.InkDripDensity = 2f;
    await PhysicsFrames.Frame(TestScene);

    _inkMaterial().GetShaderParameter(SpacingParam).AsSingle()
      .ShouldBe(spread / 2f, "twice as dense did not draw twice as many drips");
    _inkMaterial().GetShaderParameter(WidthParam).AsSingle()
      .ShouldBe(thickness, "asking for more drips made them thicker instead");
  }

  // A coat sat by position redraws itself every time the platform is nudged, which is no way to
  // keep a run the author picked. A seed set by hand outlives being moved.
  [Test]
  public async Task AnAuthoredSeedOutlastsMovingThePlatform() {
    _platform.Inked = true;
    _platform.InkColor = ColorUtils.PINK;
    _platform.Position = new Vector2(640f, 320f);
    await PhysicsFrames.Frame(TestScene);
    var byPosition = _inkMaterial().GetShaderParameter(SeedParam).AsSingle();

    _platform.InkSeed = 7;
    await PhysicsFrames.Frame(TestScene);
    _inkMaterial().GetShaderParameter(SeedParam).AsSingle()
      .ShouldBe(7f, "the coat wears a run nobody chose");

    _platform.Position = new Vector2(1920f, 960f);
    await PhysicsFrames.Frame(TestScene);
    _inkMaterial().GetShaderParameter(SeedParam).AsSingle()
      .ShouldBe(7f, "moving the platform repainted a coat that was chosen");

    _platform.InkSeed = 0;
    await PhysicsFrames.Frame(TestScene);
    _inkMaterial().GetShaderParameter(SeedParam).AsSingle()
      .ShouldNotBe(byPosition, "back on its own, the coat did not follow the platform");
  }

  [Test]
  public async Task WidthThickensTheDripsWithoutMovingThem() {
    _platform.Inked = true;
    _platform.InkColor = ColorUtils.PINK;
    _platform.InkDripWidth = 1f;
    await PhysicsFrames.Frame(TestScene);
    var spread = _inkMaterial().GetShaderParameter(SpacingParam).AsSingle();
    var thickness = _inkMaterial().GetShaderParameter(WidthParam).AsSingle();

    _platform.InkDripWidth = 2f;
    await PhysicsFrames.Frame(TestScene);

    _inkMaterial().GetShaderParameter(WidthParam).AsSingle().ShouldBe(thickness * 2f);
    _inkMaterial().GetShaderParameter(SpacingParam).AsSingle()
      .ShouldBe(spread, "thickening the drips thinned out how many there are");
  }

  // Left alone the height still decides, so every platform inked before there was a field to set
  // wears the coat it was authored with.
  [Test]
  public async Task WithoutOneTheHeightStillDecidesTheDrips() {
    _platform.Inked = true;
    _platform.InkColor = ColorUtils.PINK;
    _platform.Size = new Vector2(320f, 32f);
    await PhysicsFrames.Frame(TestScene);
    var offAThinLedge = _inkMaterial().GetShaderParameter(ReachParam).AsSingle();

    _platform.Size = new Vector2(320f, 160f);
    await PhysicsFrames.Frame(TestScene);

    _inkMaterial().GetShaderParameter(ReachParam).AsSingle()
      .ShouldBeGreaterThan(offAThinLedge, "the height stopped deciding the drips");
  }

  // An uninked platform is what it always was, whatever colour the coat it is not wearing names.
  [Test]
  public async Task ACoatColourOnItsOwnPaintsNothing() {
    _platform.Group = ColorUtils.PINK;
    _platform.InkColor = ColorUtils.YELLOW;
    await PhysicsFrames.Frame(TestScene);

    var area = _platform.GetNode<Area2D>("Area2D");
    area.IsInGroup(ColorUtils.PINK).ShouldBeTrue();
    area.IsInGroup(ColorUtils.YELLOW).ShouldBeFalse("a coat nobody painted decides the landing");
    _platform.GetNode<ColorRect>("Ink").Visible.ShouldBeFalse();
  }

  // The ground is drawn plain white, never taken through the skin, so a neutral platform set
  // against it only reads as the same surface while it is drawn the same way.
  [Test]
  public async Task ANeutralPlatformIsTheGroundsOwnWhite() {
    _platform.Group = FlatPlatform.NEUTRAL;
    await PhysicsFrames.Frame(TestScene);

    _surface().Color.ShouldBe(Colors.White);
  }

  // Neutral is the absence of a colour, not a fifth one: a group named "white" would have the
  // player's faces looking for a colour no face has.
  [Test]
  public async Task NeutralIsNotAGroupOfItsOwn() {
    _platform.Group = FlatPlatform.NEUTRAL;
    await PhysicsFrames.Frame(TestScene);

    _platform.GetNode<Area2D>("Area2D").IsInGroup(FlatPlatform.NEUTRAL).ShouldBeFalse();
  }

  // The same two layers every other platform sits on: the body is what the cube cannot walk
  // through, the area is what its faces are judged against.
  [Test]
  public void ItSitsOnTheLayersThePlayerLooksFor() {
    (_platform.CollisionLayer & PhysicsLayers.Platform.Mask).ShouldBe(
      PhysicsLayers.Platform.Mask, "the cube walks through it");

    var area = _platform.GetNode<Area2D>("Area2D");
    area.CollisionLayer.ShouldBe(PhysicsLayers.Platform.Mask, "no face ever reports landing on it");
    (area.CollisionMask & PhysicsLayers.BoxFace.Mask).ShouldBe(
      PhysicsLayers.BoxFace.Mask, "it cannot see the faces it is supposed to judge");
  }

  // Every platform in a level shares one scene file. Uniforms and shapes that are not local to it
  // are one resource between the lot of them, so the last one placed would size all the rest.
  [Test]
  public async Task TwoPlatformsKeepTheirOwnShapeAndTheirOwnMaterial() {
    var other = SceneHelpers.InstantiateNode<FlatPlatform>();
    TestScene.AddChild(other);
    await PhysicsFrames.Frame(TestScene);

    _platform.Size = new Vector2(400f, 32f);
    other.Size = new Vector2(64f, 300f);
    await PhysicsFrames.Frame(TestScene);

    _solidShape().Size.ShouldBe(new Vector2(400f, 32f));
    other.GetNode<CollisionShape2D>("CollisionShape").Shape.ShouldBeOfType<RectangleShape2D>()
      .Size.ShouldBe(new Vector2(64f, 300f));

    var otherMaterial = (ShaderMaterial)other.GetNode<ColorRect>("Surface").Material;
    _material().GetShaderParameter(SizeParam).AsVector2().ShouldBe(new Vector2(400f, 32f));
    otherMaterial.GetShaderParameter(SizeParam).AsVector2().ShouldBe(new Vector2(64f, 300f));

    other.QueueFree();
  }

  // The bug this guards: a platform dropped into a gap in the ground at an odd height put its own
  // top edge on a half-pixel, and the player walked into that lip and stopped dead. Nothing about
  // it is visible - the platform looks flush and the level looks finished.
  [Test]
  public async Task APlatformFillingAGapInTheGroundLandsFlushWithIt() {
    const float GROUND_SURFACE = 320f;
    const float GROUND_BOTTOM = 960f;

    _platform.Position = new Vector2(9792f, 639f);
    _platform.Size = new Vector2(256f, 639f);
    await PhysicsFrames.Frame(TestScene);

    var top = _platform.Position.Y - (_platform.Size.Y / 2f);
    var bottom = _platform.Position.Y + (_platform.Size.Y / 2f);
    top.ShouldBe(GROUND_SURFACE, "the player walks into the lip between the ground and the platform");
    bottom.ShouldBe(GROUND_BOTTOM);
  }

  // Only the corner is held. A platform is free to be any height, which the thin ledges are.
  [Test]
  public async Task SnappingHoldsTheTopLeftCornerOnTheCellAndLeavesTheSizeAlone() {
    _platform.Position = new Vector2(300f, 200f);
    _platform.Size = new Vector2(250f, 70f);
    await PhysicsFrames.Frame(TestScene);

    _platform.Size.ShouldBe(new Vector2(250f, 70f), "a platform was forced to a whole number of cells");
    (_platform.Position - (_platform.Size / 2f)).ShouldBe(new Vector2(160f, 160f));
  }

  // Off the grid a platform is free to be any size and sit anywhere, but never on a half-pixel:
  // an odd size puts a centred box's own edges between pixels however carefully it is placed.
  [Test]
  public async Task WithoutSnappingTheSizeIsStillHeldEven() {
    _platform.SnapToGrid = false;
    _platform.Position = new Vector2(101f, 57f);
    _platform.Size = new Vector2(101f, 45f);
    await PhysicsFrames.Frame(TestScene);

    _platform.Size.ShouldBe(new Vector2(102f, 46f));
    _platform.Position.ShouldBe(new Vector2(101f, 57f), "an unsnapped platform was moved anyway");
  }

  [Test]
  public async Task TurningSnappingOnBringsAHandPlacedPlatformOntoTheGrid() {
    _platform.SnapToGrid = false;
    _platform.Position = new Vector2(300f, 200f);
    _platform.Size = new Vector2(250f, 70f);
    await PhysicsFrames.Frame(TestScene);

    _platform.SnapToGrid = true;
    await PhysicsFrames.Frame(TestScene);

    (_platform.Position - (_platform.Size / 2f)).ShouldBe(new Vector2(160f, 160f));
  }

  private ColorRect _surface() => _platform.GetNode<ColorRect>("Surface");

  private ShaderMaterial _material() => (ShaderMaterial)_surface().Material;

  private ShaderMaterial _inkMaterial() =>
    (ShaderMaterial)_platform.GetNode<ColorRect>("Ink").Material;

  private RectangleShape2D _solidShape() =>
    (RectangleShape2D)_platform.GetNode<CollisionShape2D>("CollisionShape").Shape;

  private RectangleShape2D _colorShape() =>
    (RectangleShape2D)_platform.GetNode<CollisionShape2D>("Area2D/ColorAreaShape").Shape;
}
