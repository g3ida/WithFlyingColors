namespace Wfc.Entities.World.Hub;

using Godot;

// Where a run is set down the first time it steps into the hub: out past the doors, far
// enough back that walking in has the room to show. Its own type rather than a node the
// orchestrator looks up by name, so the hub declares the spot the same way it declares
// its doors.
public partial class HubArrivalMark : Marker2D {
  // How short of the door the walk hands back. The room has introduced itself by then, and
  // the last steps into the doorway are the player's own. Authored beside the spot the walk
  // starts from, since the two are one piece of staging.
  [Export]
  public float StopDistance { get; set; } = 280.0f;
}
