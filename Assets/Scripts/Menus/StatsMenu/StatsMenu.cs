using Godot;

public class StatsMenu : GameMenu
{
    public override void _Ready()
    {
        base._Ready();
    }

    private void _on_BackButton_pressed()
    {
        Event.Instance().EmitMenuButtonPressed(MenuButtons.BACK);
    }
}
