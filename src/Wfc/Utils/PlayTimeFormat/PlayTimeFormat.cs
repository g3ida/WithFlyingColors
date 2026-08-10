namespace Wfc.Utils;

// How long a run has been played, worded the same way wherever it is shown.
public static class PlayTimeFormat {
  // Minutes alone stay readable for a while, so the hours only appear once there are
  // enough of them to be worth reading - below that "0h 40min" says less than "40min".
  private const ulong MINUTES_BEFORE_HOURS = 99;

  public static string Format(ulong seconds) {
    var minutes = seconds / 60;
    return minutes > MINUTES_BEFORE_HOURS ? $"{minutes / 60}h {minutes % 60}min" : $"{minutes}min";
  }
}
