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

  // The directory the calling file sits in, relative to the project root.
  //
  // This used to be found by splitting on the repository's own name, which broke once
  // already on CI - it checks out into /home/runner/work/WithFlyingColors/WithFlyingColors,
  // one directory of that name inside another - and broke outright for a clone into a
  // directory called anything else. ProjectPath anchors on src/ instead.
  private static string GetSceneDirFromCallerFilePath(string callerFilePath) =>
    Path.GetDirectoryName(ProjectPath.RelativeToProjectRoot(callerFilePath)) ?? string.Empty;
}
