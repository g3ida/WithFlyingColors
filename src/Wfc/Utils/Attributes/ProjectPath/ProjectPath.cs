namespace Wfc.Utils.Attributes;

using System;

// Turns the compile-time path of a source file into the path Godot knows it by.
//
// Anchored on the project's own src directory, not on the name of the folder the repository
// is checked out into. That name is nobody's to rely on: a clone into a directory called
// anything else left every scene resolving to an absolute path on the machine that compiled
// it, with nothing to say so until a scene failed to load. CI already checks out into
// /home/runner/work/WithFlyingColors/WithFlyingColors, one directory of that name inside
// another, which is the shape that broke the level paths once already.
//
// The constraint this trades for: a type carrying one of these attributes lives under src/.
// Every one of them does, and a scene under test/ would resolve against the wrong src.
public static class ProjectPath {
  private const string SOURCE_ROOT = "/src/";

  // Where the file sits relative to the project root - "src/Wfc/...cs" - or empty when it is
  // not under src/ at all, which the caller reports rather than guessing at.
  public static string RelativeToProjectRoot(string callerFilePath) {
    if (string.IsNullOrEmpty(callerFilePath)) {
      return string.Empty;
    }
    // Compiled on Windows, run anywhere: CallerFilePath keeps the separators of the machine
    // that built it, and res:// only ever speaks in forward slashes.
    var normalized = callerFilePath.Replace('\\', '/');
    var index = normalized.LastIndexOf(SOURCE_ROOT, StringComparison.Ordinal);
    return index < 0 ? string.Empty : normalized[(index + 1)..];
  }

  public static string ResPathOf(string callerFilePath) {
    var relative = RelativeToProjectRoot(callerFilePath);
    return relative.Length == 0 ? string.Empty : $"res://{relative}";
  }
}
