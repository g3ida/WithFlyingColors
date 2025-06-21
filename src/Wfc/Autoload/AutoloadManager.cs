namespace Wfc.Autoload;

using Godot;
using Wfc.Core.Audio;
using Wfc.Core.Event;
using Wfc.Utils;

public partial class AutoloadManager : Node {

  // TODO: load other scripts
  public IEventHandler EventHandler => Core.Event.EventHandler.Instance;
  public required IMusicTrackManager MusicTrackManager;
  public required ISfxManager SfxManager;


  public override void _EnterTree() {
    base._EnterTree();
    Instance = GetTree().Root.GetNode<AutoloadManager>("AutoloadManager");
    MusicTrackManager = this.InstantiateChildNode<MusicTrackManager>();
    SfxManager = this.InstantiateChildNode<SfxManager>();
  }

  public override void _Ready() {
    base._Ready();
    SetProcess(false);
  }

  public static AutoloadManager Instance { get; private set; } = null!;

}
