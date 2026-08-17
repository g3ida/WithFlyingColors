namespace Wfc.Autoload;

using Godot;
using Wfc.Core.Audio;
using Wfc.Core.Display;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.Utils;

public partial class AutoloadManager : Node {

  // TODO: load other scripts
  public IMusicTrackManager MusicTrackManager = null!;
  public ISfxManager SfxManager = null!;
  public InputDeviceDetector InputDeviceDetector = null!;
  public InputFocusGuard InputFocusGuard = null!;
  public WindowAspectGuard WindowAspectGuard = null!;
  public override void _EnterTree() {
    base._EnterTree();
    Instance = GetTree().Root.GetNode<AutoloadManager>("AutoloadManager");
    MusicTrackManager = this.InstantiateChildNode<MusicTrackManager>();
    SfxManager = this.InstantiateChildNode<SfxManager>();
    // Built by hand rather than instantiated from a scene: none of them has a scene
    // of its own, they only listen. The window is dragged from anywhere in the game,
    // so what a drag may end at is watched here rather than by the settings.
    InputDeviceDetector = new InputDeviceDetector { Name = nameof(InputDeviceDetector) };
    AddChild(InputDeviceDetector);
    InputFocusGuard = new InputFocusGuard { Name = nameof(InputFocusGuard) };
    AddChild(InputFocusGuard);
    WindowAspectGuard = new WindowAspectGuard { Name = nameof(WindowAspectGuard) };
    AddChild(WindowAspectGuard);
  }

  public override void _Ready() {
    base._Ready();
    SetProcess(false);
  }

  public static AutoloadManager Instance { get; private set; } = null!;

}
