using System.Numerics;
namespace Wfc.Utils
{
  public static class MathUtils
  {
    public static bool isPositive<T>(T value) where T : INumber<T>
    {
      return value > T.Zero;
    }
  }
}