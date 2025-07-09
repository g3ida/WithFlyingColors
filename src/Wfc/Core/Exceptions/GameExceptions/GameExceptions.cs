namespace Wfc.Core.Exceptions;

using System;

public static class GameExceptions {
  public class InvalidCallException(string message) : Exception(message) { }
  public class InvalidArgumentException(string message) : Exception(message) { }

}
