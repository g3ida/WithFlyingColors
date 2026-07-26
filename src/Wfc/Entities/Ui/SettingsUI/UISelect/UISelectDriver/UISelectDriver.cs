namespace Wfc.Entities.Ui.SettingsUI.UISelect;

using System.Collections.Generic;
using Godot;

public partial class UISelectDriver : Node {
  public List<string> Items = new List<string>();
  public List<Variant> ItemValues = new List<Variant>();

  [Signal]
  public delegate void ItemListChangedEventHandler();

  public virtual void onItemSelected(Variant? item) {
    // Logic for handling item selection goes here.
  }

  // Called when the player moved the select themselves, rather than it being set
  // up or refreshed in code. Both paths go through onItemSelected, so a driver
  // that has to tell the two apart listens here instead.
  public virtual void OnUserSelectionChanged() { }

  public virtual int GetDefaultSelectedIndex() {
    return 0; // Default index logic can be modified as needed.
  }

  public override void _Ready() {
    base._Ready();
  }
}
