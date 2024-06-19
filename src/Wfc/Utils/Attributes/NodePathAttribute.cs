using System;
using Godot;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class NodePathAttribute : Attribute
{
  public string Path { get; }

  public NodePathAttribute(string path)
  {
    Path = path;
  }
}
