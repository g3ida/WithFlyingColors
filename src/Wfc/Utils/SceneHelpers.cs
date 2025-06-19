namespace Wfc.Utils;

using System;
using System.Reflection;
using Godot;
using Wfc.Utils.Attributes;

public static partial class SceneHelpers {
  public static PackedScene LoadScene<T>() where T : Node {
    var type = typeof(T);
    // Get the ScenePath attribute from the class
    var attribute = type.GetCustomAttribute<ScenePathAttribute>()
      ?? throw new InvalidOperationException($"No ScenePath attribute found on class {type.Name}");
    // Load the scene from the path
    return GD.Load<PackedScene>(attribute.Path)
      ?? throw new InvalidOperationException($"Failed to load scene at path: {attribute.Path}");
  }

  public static T InstantiateNode<T>() where T : Node => LoadScene<T>().Instantiate<T>();
}
