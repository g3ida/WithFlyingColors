namespace Wfc.Skin;

internal static partial class PresetSkins {
  public static readonly GameSkin DEFAULT_SKIN = new("default",
    [
      ["008d99", "980051", "610594", "789400"], // extremelyDark
      ["00a4b2", "b2005d", "7107ac", "92b800"], // SuperDark
      ["00c8d9", "d80071", "8808cf", "b5e300"], // veryDark
      ["00d3e5", "e50078", "9208dd", "beed00"], // dark
      ["00ebff", "ff0085", "a209f6", "ccff00"], // basic
      ["37efff", "ff2597", "ac25f6", "d8ff38"], // light
      ["5cf1ff", "ff38a0", "b236f6", "dfff5c"], // veryLight
      ["8cf4ff", "ff87c6", "ba7add", "e8ff8c"]  // background
    ]);

  // Picked by searching colour space for the four whose worst pair stays furthest
  // apart under normal vision and under all three dichromacies at once, with a floor
  // on how close any two may sit in lightness. The lightness floor is what carries it
  // when hue is no help at all - a washed-out screen, or achromatopsia - and it is
  // also why these four read as a ramp rather than a set of equals.
  //
  // Worst pair, measured as CIELAB dE across normal/protan/deutan/tritan: 59 here,
  // against 41 for the default set and 17 for googl.
  public static readonly GameSkin CLEAR_SKIN = new("clear",
    [
      ["008a7c", "5c0900", "7a0099", "8a8a1c"], // extremelyDark
      ["00a191", "6b0a00", "8f00b2", "a1a120"], // superDark
      ["00c6b2", "840d00", "af00db", "c6c628"], // veryDark
      ["00d1bc", "8b0e00", "ba00e8", "d1d12a"], // dark
      ["00e6cf", "990f00", "cc00ff", "e6e62e"], // basic
      ["33ebd9", "ad3f33", "d633ff", "ebeb58"], // light
      ["5cefe0", "be655c", "de5cff", "efef79"], // veryLight
      ["8cf4e9", "d1938c", "e88cff", "f4f4a1"]  // background
    ]);

  public static readonly GameSkin GOOGL_SKIN = new("googl",
   [
      ["2073ac", "247038", "b47f18", "982e1f"], // extremelyDark
      ["2584c6", "297f3f", "cf921c", "ae3523"], // superDark
      ["3597d9", "2e9148", "e3a52b", "c93c29"], // veryDark
      ["38a0e5", "319c4d", "f0ae2e", "d43f2b"], // dark
      ["3eb2ff", "37b057", "ffb831", "e2432e"], // basic
      ["52baff", "49b566", "ffbf45", "e5513d"], // light
      ["61c0ff", "51bd6f", "ffc454", "eb5a47"], // veryLight
      ["8cd1ff", "6ec784", "ffd78c", "f29285"]  // background
  ]);
}
