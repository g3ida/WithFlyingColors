namespace Wfc.Core.Persistence;

using System.Collections.Generic;
using System.Threading;
using Godot;
using Wfc.Core.Logger;

// Where save writes leave the main thread.
//
// Gathering a slot's state costs almost nothing; the write that follows it usually costs almost
// nothing either, and occasionally does not - the filesystem decides when it flushes, and a
// checkpoint landing on one of those dropped a frame. So a serialized line is handed over here
// and the frame carries on.
//
// What the game reads has to be what it last wrote whether or not the disk has caught up, so a
// line still waiting answers reads of its own path. Without that, the metadata re-read that
// follows every save would overwrite what was just recorded with the previous contents, and a
// slot written for the first time would still look empty.
public static class SaveWriter {
  private static readonly object _gate = new();

  // One line per path rather than a queue of them: a whole file is rewritten each time, so a
  // line still waiting is worth nothing once the next one for that path arrives.
  private static readonly Dictionary<string, string> _pending = [];
  private static readonly AutoResetEvent _hasWork = new(false);

  // Set only once the queue is empty *and* nothing is mid-write, which is what makes Flush a
  // promise about the disk rather than about the queue.
  private static readonly ManualResetEventSlim _idle = new(true);
  private static Thread? _worker;

  public static void Write(string path, string line) {
    lock (_gate) {
      _pending[path] = line;
      _idle.Reset();
      _startWorker();
    }
    _hasWork.Set();
  }

  // What a read of this path should see, or null to go to the disk for it.
  public static string? PendingLine(string path) {
    lock (_gate) {
      return _pending.TryGetValue(path, out var line) ? line : null;
    }
  }

  public static bool Exists(string path) {
    lock (_gate) {
      if (_pending.ContainsKey(path)) {
        return true;
      }
    }
    return FileAccess.FileExists(path);
  }

  // Blocks until every queued line has reached the disk. For the moments that cannot be left
  // to catch up later: the window closing, and anything about to go behind this queue's back.
  public static void Flush() => _idle.Wait();

  // Gives up a line that has not been written yet, for a path whose file is about to go. A write
  // already under way still finishes - it cannot be called off once the handle is open - so this
  // waits for it rather than racing the caller's delete and losing.
  public static void Discard(string path) {
    lock (_gate) {
      _pending.Remove(path);
    }
    Flush();
  }

  // Gets the thread up before the first save asks for it, so that the checkpoint the player is
  // standing on does not pay for starting it. Boot is nobody's frame.
  public static void Prepare() {
    lock (_gate) {
      _startWorker();
    }
  }

  // Only ever reached with the gate held. A run that never saves and never boots a save manager
  // is left with the thread count it had.
  private static void _startWorker() {
    if (_worker != null) {
      return;
    }
    _worker = new Thread(_drainForever) {
      IsBackground = true,
      Name = "SaveWriter",
    };
    _worker.Start();
  }

  private static void _drainForever() {
    while (true) {
      _hasWork.WaitOne();
      while (_takeOne(out var path, out var line)) {
        // Nothing here is allowed to end the thread. A write that throws has lost that one save,
        // which is bad; a thread that dies leaves Flush waiting on a drain that will never come,
        // and the game hangs on the way out, which is worse.
        try {
          _writeLineAtomic(path, line);
        }
        catch (System.Exception error) {
          Log.Exception(error);
        }
      }
    }
  }

  private static bool _takeOne(out string path, out string line) {
    lock (_gate) {
      foreach (var entry in _pending) {
        path = entry.Key;
        line = entry.Value;
        _pending.Remove(path);
        return true;
      }
      // Nothing left, and the write before this one has already returned.
      _idle.Set();
      path = string.Empty;
      line = string.Empty;
      return false;
    }
  }

  // Gather the data first, then write it, and never open the destination directly.
  //
  // ModeFlags.Write truncates the instant it succeeds, so writing beside the file and renaming
  // over it means an interrupted save leaves the old one untouched.
  //
  // The per-slot directory is created here too: nothing else ever created it, so the first save
  // into a fresh slot handed back a null handle - the "first save on a new install crashes" bug.
  private static void _writeLineAtomic(string path, string line) {
    var directory = path.GetBaseDir();
    var directoryError = DirAccess.MakeDirRecursiveAbsolute(directory);
    if (directoryError is not Error.Ok and not Error.AlreadyExists) {
      Log.Error($"Could not create {directory}: {directoryError}");
      return;
    }

    var tempPath = $"{path}.tmp";
    using (var file = FileAccess.Open(tempPath, FileAccess.ModeFlags.Write)) {
      if (file == null) {
        Log.Error($"Could not open {tempPath} for writing: {FileAccess.GetOpenError()}");
        return;
      }
      file.StoreLine(line);
    }

    var renameError = DirAccess.RenameAbsolute(tempPath, path);
    if (renameError != Error.Ok) {
      Log.Error($"Could not move {tempPath} onto {path}: {renameError}");
    }
  }
}
