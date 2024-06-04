using Godot;

public partial class PauseMenuButtons : Button
{
    private AnimationPlayer animationPlayer;
    private enum State { HIDDEN, HIDING, SHOWN, SHOWING }
    private State currentState = State.HIDDEN;

    public override void _Ready()
    {
        base._Ready();
        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        Visible = false;
        animationPlayer.Play("Hidden");
    }

    public void _Hide()
    {
        if (currentState == State.SHOWN)
        {
            currentState = State.HIDING;
            animationPlayer.Play("Hide");
            animationPlayer.Connect("animation_finished", new Callable(this, nameof(OnHideAnimationDone)), flags: (uint)ConnectFlags.OneShot);
        }
        else if (currentState == State.SHOWING)
        {
            animationPlayer.Disconnect("animation_finished", new Callable(this, nameof(OnShowAnimationDone)));
            currentState = State.SHOWN;
            Hide();
        }
    }

    public void _Show()
    {
        if (currentState == State.HIDDEN)
        {
            currentState = State.SHOWING;
            Visible = true;
            animationPlayer.PlayBackwards("Hide");
            animationPlayer.Connect("animation_finished", new Callable(this, nameof(OnShowAnimationDone)), flags: (uint)ConnectFlags.OneShot);
        }
        else if (currentState == State.HIDING)
        {
            animationPlayer.Disconnect("animation_finished", new Callable(this, nameof(OnHideAnimationDone)));
            currentState = State.HIDDEN;
            Show();
        }
    }

    private void OnHideAnimationDone(string animation)
    {
        Visible = false;
        animationPlayer.Play("Hidden");
        currentState = State.HIDDEN;
    }

    private void OnShowAnimationDone(string animation)
    {
        Visible = true;
        animationPlayer.Play("Shown");
        currentState = State.SHOWN;
    }

    private void _on_PauseMenuButtons_mouse_entered()
    {
        GrabFocus();
    }
}
