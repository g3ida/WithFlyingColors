namespace Wfc.Core.Audio;

using System.Collections.Generic;

public record MusicTrackInfo(
  string Path,
  string License,
  string WebLink,
  float Volume
);

public static class GameMusicTracks {
  public static readonly Dictionary<string, MusicTrackInfo> Data = new() {
    ["brickBreaker"] = new MusicTrackInfo(
      "res://Assets/Music/Enigma-Long-Version-Complete-Version.mp3",
      "Creative Commons CC BY 3.0",
      "https://www.chosic.com/download-audio/32067/",
      -8.0f),
    ["fight"] = new MusicTrackInfo(
            "res://Assets/Music/Loyalty_Freak_Music_-_04_-_Cant_Stop_My_Feet_.mp3",
            "Public domain CC0",
            "https://www.chosic.com/download-audio/25495/",
            -5.0f),
    ["level1"] = new MusicTrackInfo(
            "res://Assets/Music/Loyalty Freak Music - Monarch of the street.ogg",
            "Public domain CC0",
            "https://freemusicarchive.org/Music/Loyalty_Freak_Music/TO_CHILL_AND_STAY_AWAKE/Loyalty_Freak_Music_-_TO_CHILL_AND_STAY_AWAKE_-_07_Monarch_of_the_street/",
            -7.0f),
    ["tetris"] = new MusicTrackInfo(
            "res://Assets/Music/Myuu-Tetris-Dark-Version.mp3",
            "free to use as long as credit is given",
            "https://www.youtube.com/watch?v=eunhYtd8agE&ab_channel=Myuu",
            -5.0f),
    ["cards"] = new MusicTrackInfo(
            "res://Assets/Music/Sneaky-Snitch.mp3",
            "Creative Commons CC BY 3.0",
            "https://www.chosic.com/download-audio/39325/",
            -5.0f)
  };
}
