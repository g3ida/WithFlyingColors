using Godot;

public class BackButton : Button
{
    public override void _Ready()
    {
        SetProcess(false);
    }

    public void UpdatePositionX(float value)
    {
        RectPosition = new Vector2(value, RectPosition.y);
    }
}
