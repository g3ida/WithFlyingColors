using Godot;
using System;
using Wfc.Core.Event;

public partial class TabLogic : TabBar {
  private void _on_TabContainer_tab_changed(int tab) {
    if (GetIndex() == tab) {
      Event.Instance.EmitTabChanged();
      GetChild<IUITab>(0).on_gain_focus();
    }
  }
}
