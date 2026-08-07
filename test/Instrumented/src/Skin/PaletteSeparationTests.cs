namespace Wfc.test.instrumented.Skin;

using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Skin;

// The palette offered to a player who cannot separate the default four has to actually
// be easier to separate, and nothing about a list of hex strings says whether it is.
// So this measures it: every pair of colours is compared as normal sight sees them and
// as each of the three kinds of dichromacy sees them, and a palette is worth no more
// than its closest pair under the least forgiving of those.
//
// Editing any of the palettes without re-checking these numbers is exactly the mistake
// worth failing a build over - the colours are the game's only way of telling one
// platform from another.
public class PaletteSeparationTests(Node testScene) : TestClass(testScene) {
  // How far apart the worst pair of the safe palette must stay, in CIELAB dE. Measured
  // at ~59 when it was chosen; the default sits at ~41 and googl at ~17.
  private const double SAFE_PALETTE_FLOOR = 50.0;

  // How far apart its closest two colours must sit in plain lightness. Hue separation
  // is worth nothing on a washed-out screen or to a player with no colour vision at
  // all, and lightness is what is left.
  private const double LUMINANCE_FLOOR = 0.10;

  private static readonly SkinColor[] FACES =
    [SkinColor.TopFace, SkinColor.LeftFace, SkinColor.BottomFace, SkinColor.RightFace];

  [Test]
  public void TheSafePaletteIsTheMostSeparableOneOffered() {
    var scores = SkinManager.SELECTABLE_SKINS.ToDictionary(name => name, _worstPairSeparation);

    scores["clear"].ShouldBeGreaterThan(SAFE_PALETTE_FLOOR,
      $"the palette offered as the readable one scores {scores["clear"]:F1}");
    foreach (var (name, score) in scores.Where(entry => entry.Key != "clear")) {
      scores["clear"].ShouldBeGreaterThan(score, $"'{name}' separates better than the one meant to");
    }
  }

  [Test]
  public void TheSafePaletteAlsoSeparatesWithNoColourAtAll() {
    var luminances = _basicColors("clear").Select(_luminance).ToList();

    var closest = _pairs(luminances.Count)
      .Min(pair => Math.Abs(luminances[pair.a] - luminances[pair.b]));

    closest.ShouldBeGreaterThan(LUMINANCE_FLOOR,
      $"its closest two colours are {closest:F3} apart in lightness");
  }

  [Test]
  public void EverySelectablePaletteExists() {
    foreach (var name in SkinManager.SELECTABLE_SKINS) {
      SkinManager.Instance.ContainsSkin(name).ShouldBeTrue($"'{name}' is offered but not registered");
      SkinManager.DisplayName(name).ShouldNotBe(name, $"'{name}' has no name to show the player");
    }
  }

  // The worst any two of a palette's colours ever look alike, to anyone.
  private static double _worstPairSeparation(string skinName) {
    var colors = _basicColors(skinName);
    return _visionKinds()
      .SelectMany(matrix => _pairs(colors.Count).Select(pair =>
        _deltaE(_toLab(_simulate(colors[pair.a], matrix)), _toLab(_simulate(colors[pair.b], matrix)))))
      .Min();
  }

  private static List<Color> _basicColors(string skinName) {
    var colors = SkinManager.Instance.GetSkin(skinName).GetColors(SkinColorIntensity.Basic);
    return FACES.Select(face => colors[face]).ToList();
  }

  private static IEnumerable<(int a, int b)> _pairs(int count) =>
    Enumerable.Range(0, count).SelectMany(a => Enumerable.Range(a + 1, count - a - 1).Select(b => (a, b)));

  // Normal sight, then the three dichromacies, as the linear-RGB approximations that
  // colour-vision tooling standardises on.
  private static IEnumerable<double[]> _visionKinds() => [
    [1, 0, 0, 0, 1, 0, 0, 0, 1],
    [0.152286, 1.052583, -0.204868, 0.114503, 0.786281, 0.099216, -0.003882, -0.048116, 1.051998],
    [0.367322, 0.860646, -0.227968, 0.280085, 0.672501, 0.047413, -0.011820, 0.042940, 0.968881],
    [1.255528, -0.076749, -0.178779, -0.078411, 0.930809, 0.147602, 0.004733, 0.691367, 0.303900],
  ];

  private static double[] _simulate(Color color, double[] m) {
    double[] rgb = [_linear(color.R), _linear(color.G), _linear(color.B)];
    return [
      Math.Clamp(m[0] * rgb[0] + m[1] * rgb[1] + m[2] * rgb[2], 0.0, 1.0),
      Math.Clamp(m[3] * rgb[0] + m[4] * rgb[1] + m[5] * rgb[2], 0.0, 1.0),
      Math.Clamp(m[6] * rgb[0] + m[7] * rgb[1] + m[8] * rgb[2], 0.0, 1.0),
    ];
  }

  private static double _linear(double channel) =>
    channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);

  private static double _luminance(Color color) =>
    (0.2126 * _linear(color.R)) + (0.7152 * _linear(color.G)) + (0.0722 * _linear(color.B));

  private static double[] _toLab(double[] linearRgb) {
    var x = ((0.4124 * linearRgb[0]) + (0.3576 * linearRgb[1]) + (0.1805 * linearRgb[2])) / 0.95047;
    var y = (0.2126 * linearRgb[0]) + (0.7152 * linearRgb[1]) + (0.0722 * linearRgb[2]);
    var z = ((0.0193 * linearRgb[0]) + (0.1192 * linearRgb[1]) + (0.9505 * linearRgb[2])) / 1.08883;
    static double Pivot(double t) => t > 0.008856 ? Math.Cbrt(t) : (7.787 * t) + (16.0 / 116.0);
    double fx = Pivot(x), fy = Pivot(y), fz = Pivot(z);
    return [(116 * fy) - 16, 500 * (fx - fy), 200 * (fy - fz)];
  }

  private static double _deltaE(double[] a, double[] b) =>
    Math.Sqrt(Enumerable.Range(0, 3).Sum(i => (a[i] - b[i]) * (a[i] - b[i])));
}
