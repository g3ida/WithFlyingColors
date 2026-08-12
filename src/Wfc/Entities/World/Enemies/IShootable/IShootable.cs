namespace Wfc.Entities.World.Enemies;

using Godot;

// Something a shot knocks a hole in rather than merely stopping against. The projectile does not
// decide what being hit means: it says where it landed and the target answers for itself.
public interface IShootable {
  void OnShot(Vector2 globalPosition);
}
