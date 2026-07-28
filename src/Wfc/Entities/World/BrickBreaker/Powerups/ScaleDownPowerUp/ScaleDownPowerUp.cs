namespace Wfc.Entities.World.BrickBreaker.Powerups;

public partial class ScaleDownPowerUp : PlayerScalePowerUp {
  private const float SCALE_FACTOR = 0.7f;

  public override float ScaleFactor => SCALE_FACTOR;
}
