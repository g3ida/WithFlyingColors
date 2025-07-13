namespace Wfc.Utils.Attributes;

using System;
using System.IO;
using System.Linq;
using Godot;
using DirAccess = Godot.DirAccess;
using FileAccess = Godot.FileAccess;

[AttributeUsage(AttributeTargets.Field)]
public class LevelPathAttribute : Attribute {
  private enum PathType {
    Absolute,
    Directory,
    Relative
  }

  private string _path { get; }
  private PathType _type;

  private string _sceneDir { get; }

  public LevelPathAttribute(string path = "", [System.Runtime.CompilerServices.CallerFilePath] string sceneDir = "") {
    _sceneDir = sceneDir;

    if (!string.IsNullOrEmpty(path)) {
      if (path.StartsWith("res://")) {
        if (FileAccess.FileExists(path)) {
          // Absolute scene file path
          _path = path;
          _type = PathType.Absolute;
        }
        else if (DirAccess.DirExistsAbsolute(path)) {
          // Directory path
          _path = path;
          _type = PathType.Directory;
        }
        else {
          GD.PushError($"No file or directory found at the path: {path}");
          _path = String.Empty;
        }
      }
      else {
        // Relative path (just a file name or subdir/file)
        _path = path;
        _type = PathType.Relative;
      }
    }
    else {
      // Empty path, will be resolved later using the enum field name
      _path = "";
      _type = PathType.Relative;
    }
  }

  // Helper to resolve the final scene path
  public string ResolvePath(string enumFieldName) {
    switch (_type) {
      case PathType.Absolute:
        return _path;
      case PathType.Directory:
        // Assume the scene file is named after the enum field
        return $"{_path}/{enumFieldName}.tscn";
      case PathType.Relative:
      default:
        if (string.IsNullOrEmpty(_path)) {
          // No path provided, use enum field name and the directory of the source file
          var dir = GetSceneDirFromCallerFilePath(_sceneDir);
          return $"res://{Path.Combine(dir, enumFieldName)}.tscn";
        }
        else {
          // Relative file name provided, combine with sceneDir
          var dir = GetSceneDirFromCallerFilePath(_sceneDir);
          return $"res://{Path.Combine(dir, _path.Replace(".tscn", ""))}.tscn";
        }
    }
  }

  // Extracts the directory path after "WithFlyingColors" from the file path
  private static string GetSceneDirFromCallerFilePath(string callerFilePath) {
    if (string.IsNullOrEmpty(callerFilePath)) {
      return string.Empty;
    }

    // Find the part after "WithFlyingColors"
    var index = callerFilePath.IndexOf("WithFlyingColors", StringComparison.OrdinalIgnoreCase);
    if (index == -1) {
      return string.Empty;
    }

    var afterProject = callerFilePath.Substring(index + "WithFlyingColors".Length);
    var dir = Path.GetDirectoryName(afterProject.Replace("\\", "/").TrimStart('/'));
    return dir ?? string.Empty;
  }
}
