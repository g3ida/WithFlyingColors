namespace Wfc.Autoload;

using Godot;
using Wfc.Core.Audio;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.Utils;

public partial class AutoloadManager : Node {

  // TODO: load other scripts
  public IMusicTrackManager MusicTrackManager = null!;
  public ISfxManager SfxManager = null!;
  public InputDeviceDetector InputDeviceDetector = null!;
  public InputFocusGuard InputFocusGuard = null!;

  // The smallest the settings panel offers, and the smallest it still fits what it holds. The
  // view letterboxes at any shape, so how wide a window ends up is the player's business - how
  // small is not.
  private static readonly Vector2I MINIMUM_WINDOW_SIZE = new(800, 450);

  public override void _EnterTree() {
    base._EnterTree();
    Instance = GetTree().Root.GetNode<AutoloadManager>("AutoloadManager");
    MusicTrackManager = this.InstantiateChildNode<MusicTrackManager>();
    SfxManager = this.InstantiateChildNode<SfxManager>();
    // Built by hand rather than instantiated from a scene: neither has a scene of its own,
    // they only listen.
    InputDeviceDetector = new InputDeviceDetector { Name = nameof(InputDeviceDetector) };
    AddChild(InputDeviceDetector);
    InputFocusGuard = new InputFocusGuard { Name = nameof(InputFocusGuard) };
    AddChild(InputFocusGuard);
    DisplayServer.WindowSetMinSize(MINIMUM_WINDOW_SIZE);
  }

  public override void _Ready() {
    base._Ready();
    SetProcess(false);
  }

  public static AutoloadManager Instance { get; private set; } = null!;

}
