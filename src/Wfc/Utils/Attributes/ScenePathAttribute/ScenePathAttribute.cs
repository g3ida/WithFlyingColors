namespace Wfc.Utils.Attributes;
using System;
using System.Linq;

[AttributeUsage(AttributeTargets.Class)]
public class ScenePathAttribute : Attribute {
  public string Path { get; }

  public ScenePathAttribute([System.Runtime.CompilerServices.CallerFilePath] string path = "") {
    if (!path.Contains("res://")) {
      // Ensure that the game directory name is "WithFlyingColors"
      path = "res://" + path.Split("WithFlyingColors").Last().Replace(".cs", ".tscn");
    }
    Path = path;
  }
}
