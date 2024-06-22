namespace Wfc.Utils.Attributes;
using System;

[AttributeUsage(AttributeTargets.Class)]
public class ScenePathAttribute(string path) : Attribute {
  public string Path { get; } = path;
}
