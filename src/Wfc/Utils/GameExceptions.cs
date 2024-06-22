namespace Wfc.Utils;
using System;

public static class GameExceptions {
  public class InvalidCallException(string message) : Exception(message) { }
}