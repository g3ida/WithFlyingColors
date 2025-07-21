namespace Wfc.Entities.World.BrickBreaker.Powerups;

using System;
using Godot;
using Wfc.Utils;

public partial class ProtectionAreaPowerUp : PowerUpScript {

  private bool isRelevant = true;
  private Node2D? protectionArea = null;

  public override void _Ready() {
    base._Ready();
    SetProcess(false);
    protectionArea = SceneHelpers.InstantiateNode<ProtectionArea>();
    if (BrickBreakerNode != null) {
      protectionArea.Position = BrickBreakerNode.ProtectionAreaSpawnerPositionNode.Position;
      BrickBreakerNode.CallDeferred(Node.MethodName.AddChild, protectionArea);
      protectionArea.CallDeferred(Node.MethodName.SetOwner, BrickBreakerNode);
    }

    protectionArea.Connect(
      Node.SignalName.TreeExited,
      new Callable(this, nameof(_OnProtectionAreaDestroyed))
    );
  }

  public override void _ExitTree() {
    if (IsStillRelevant() && protectionArea != null) {
      protectionArea.QueueFree();
    }
  }

  private void _OnProtectionAreaDestroyed() {
    isRelevant = false;
  }

  public override bool IsStillRelevant() {
    return isRelevant;
  }
}
