namespace Wfc.Entities.Ui;

using Godot;

public partial class BackButton : Button {
  public override void _Ready() {
    SetProcess(false);
  }

  public void UpdatePositionX(float value) {
    Position = new Vector2(value, Position.Y);
  }
}
