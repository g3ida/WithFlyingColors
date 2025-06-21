namespace Wfc.Core.Audio;

public interface IMusicTrackManager {
  public void PlayTrack(string name);
  public void Stop();
  public void RemoveTrack(string name);
  public void AddTrack(string name, string path, float volume);
  public void LoadTrack(string name);
  public void SetPitchScale(float _pitch_scale);
  public void SetPauseMenuEffect(bool isOn);
}
