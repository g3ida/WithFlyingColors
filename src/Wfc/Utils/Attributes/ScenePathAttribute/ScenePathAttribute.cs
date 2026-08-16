namespace Wfc.Utils.Attributes;

using System;

[AttributeUsage(AttributeTargets.Class)]
public class ScenePathAttribute : Attribute {
  private const string SOURCE_EXTENSION = ".cs";
  private const string SCENE_EXTENSION = ".tscn";

  public string Path { get; }

  // The scene beside the file this is written in. A res:// path given outright is taken as
  // it stands; anything else is the compile-time path of that file, which ProjectPath reads
  // without depending on what the checkout directory is called.
  public ScenePathAttribute([System.Runtime.CompilerServices.CallerFilePath] string path = "") {
    if (path.Contains("res://")) {
      Path = path;
      return;
    }
    var resPath = ProjectPath.ResPathOf(path);
    Path = resPath.EndsWith(SOURCE_EXTENSION, StringComparison.Ordinal)
      ? resPath[..^SOURCE_EXTENSION.Length] + SCENE_EXTENSION
      : resPath;
  }
}
