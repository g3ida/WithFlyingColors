using Godot;
using System;

public class TabLogic : Tabs
{
    private void _on_TabContainer_tab_changed(int tab)
    {
        if (GetIndex() == tab)
        {
            Event.Instance().EmitTabChanged();
            GetChild<IUITab>(0).on_gain_focus();
        }
    }
}