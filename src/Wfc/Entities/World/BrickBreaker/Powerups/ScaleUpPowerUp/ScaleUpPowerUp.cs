namespace Wfc.Entities.World.BrickBreaker.Powerups;

public partial class ScaleUpPowerUp : PlayerScalePowerUp {
  private const float SCALE_FACTOR = 1.3f;

  public override float ScaleFactor => SCALE_FACTOR;
}
