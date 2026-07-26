namespace Wfc.Autoload;

using Godot;
using Wfc.Core.Audio;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.Utils;

public partial class AutoloadManager : Node {

  // TODO: load other scripts
  public IEventHandler EventHandler => Core.Event.EventHandler.Instance;
  public IMusicTrackManager MusicTrackManager = null!;
  public ISfxManager SfxManager = null!;
  public InputDeviceDetector InputDeviceDetector = null!;
  public override void _EnterTree() {
    base._EnterTree();
    Instance = GetTree().Root.GetNode<AutoloadManager>("AutoloadManager");
    MusicTrackManager = this.InstantiateChildNode<MusicTrackManager>();
    SfxManager = this.InstantiateChildNode<SfxManager>();
    // Built by hand rather than instantiated from a scene: it has no scene of
    // its own, it only listens.
    InputDeviceDetector = new InputDeviceDetector { Name = nameof(InputDeviceDetector) };
    AddChild(InputDeviceDetector);
  }

  public override void _Ready() {
    base._Ready();
    SetProcess(false);
  }

  public static AutoloadManager Instance { get; private set; } = null!;

}
