namespace Wfc.Core.Persistence;

// What a run has done, counted for the stats board in the hub. Written to the save by
// name rather than by ordinal, so a member added here cannot remap an existing save,
// and one dropped leaves the rest of the counters where they were.
//
// Only what nothing else already records belongs here. Play time, cleared levels and
// banked gems are the slot's own state and are counted off that instead.
public enum RunStat {
  Jumps,
  Dashes,
  RotationsLeft,
  RotationsRight,
  Deaths,
}
