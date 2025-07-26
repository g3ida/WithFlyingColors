namespace Wfc.Entities.World.Enemies;

using Godot;

public interface IBullet {
  void Shoot(Vector2 shootDirection);
  void SetColorGroup(string groupName);
}
