namespace Wfc.Autoload;

using Godot;
using Wfc.Core.Event;

public partial class AutoloadManager : Node {

  public override void _EnterTree() {
    base._EnterTree();
    Instance = GetTree().Root.GetNode<AutoloadManager>("AutoloadManager");
  }


  // TODO: load other scripts
  public IEvent EventHandler => Event.Instance;

  public override void _Ready() {
    base._Ready();
    SetProcess(false);
  }

  public static AutoloadManager Instance { get; private set; } = null!;

}
