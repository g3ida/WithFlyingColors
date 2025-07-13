namespace Wfc.Utils.Attributes;

using System;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class NodePathAttribute(string path) : Attribute {
  public string Path { get; } = path;
}
