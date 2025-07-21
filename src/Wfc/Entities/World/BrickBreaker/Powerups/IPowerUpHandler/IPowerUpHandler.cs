namespace Wfc.Entities.World.BrickBreaker.Powerups;

public interface IPowerUpHandler {

  void SetActive(bool active);
  void RemoveActivePowerups();
  void RemoveFallingPowerups();

}
