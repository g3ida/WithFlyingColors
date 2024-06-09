using Godot;
using System;

public partial class Fallzone : Area2D
{
  private void _on_FallzoneArea_area_entered(Area2D area)
  {
    Event.Instance.EmitPlayerDiying(null, area.GlobalPosition, Constants.EntityType.FALLZONE);
  }
}
