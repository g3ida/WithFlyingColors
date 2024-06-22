namespace Wfc.Utils;
using System.Numerics;

public static class MathUtils {
  public static bool IsPositive<T>(T value) where T : INumber<T> => value > T.Zero;
}