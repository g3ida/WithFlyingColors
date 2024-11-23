using Godot;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class TabLogic : TabBar {
  private void _on_TabContainer_tab_changed(int tab) {
    if (GetIndex() == tab) {
      EventHandler.Instance.EmitTabChanged();
      GetChild<IUITab>(0).on_gain_focus();
    }
  }
}
