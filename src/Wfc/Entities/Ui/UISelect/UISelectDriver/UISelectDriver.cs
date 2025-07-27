namespace Wfc.Entities.Ui.UISelect;

using System.Collections.Generic;
using Godot;

public partial class UISelectDriver : Node {
  public List<string> Items = new List<string>();
  public List<object> ItemValues = new List<object>();

  public virtual void onItemSelected(string item) {
    // Logic for handling item selection goes here.
  }

  public virtual int GetDefaultSelectedIndex() {
    return 0; // Default index logic can be modified as needed.
  }

  public override void _Ready() {
    base._Ready();
  }
}
