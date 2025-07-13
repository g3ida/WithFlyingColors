namespace Wfc.Utils;

using Godot;

public static class AudioHelpers {

  public static void SetLooping(this AudioStream stream, bool looping) {
    if (stream is AudioStreamOggVorbis oggStream) {
      oggStream.Loop = looping;
    }
    else if (stream is AudioStreamMP3 mp3Stream) {
      mp3Stream.Loop = looping;
    }
    else if (stream is AudioStreamWav sampleStream) {
      if (looping) {
        sampleStream.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
      }
      else {
        sampleStream.LoopMode = AudioStreamWav.LoopModeEnum.Disabled;
      }
    }
  }

}
