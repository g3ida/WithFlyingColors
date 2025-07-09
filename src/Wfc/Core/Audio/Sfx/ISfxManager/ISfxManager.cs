namespace Wfc.Core.Audio;

public interface ISfxManager {
  public void ResumeAll();
  public void PauseAll();
  public void StopAll();
  public void StopAllExcept(string[] sfxList);

}
