namespace Wfc.test.instrumented.Helpers;

// Waits in this suite are budgeted in physics frames, which say nothing about how long a frame
// takes to run: under coverage, and on CI hardware, one costs well more than the time it stands
// for. GoDotTest caps a test method by wall clock instead, and its default is tighter than the
// longest of those budgets, so the heavier tests were killed mid-run rather than failing
// anything. Tests that drive a whole screen or a whole piece descent carry this instead, which
// clears their budget with room to spare and still catches one that has genuinely hung.
public static class SlowTest {
  public const int TIMEOUT_MILLISECONDS = 45000;
}
