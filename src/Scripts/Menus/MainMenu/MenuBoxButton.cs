using System.Runtime.InteropServices;
using Godot;

public partial class MenuBoxButton : Control {
    private static readonly Color LABEL_COLOR = new Color("96464646");
    private static readonly Color LABEL_COLOR_HOVER = new Color("dc464646");
    private static readonly Color LABEL_COLOR_CLICK = new Color("f0464646");
    private static readonly Color LABEL_COLOR_DISABLED = new Color("46464646");

    [Signal] public delegate void pressedEventHandler();


    private string _text;
    [Export]
    public string text {
        get => _text;
        set {
            _text = value;
            // FIXME: node was not yet ready ? C# migration
            if (LabelNode != null) {
                LabelNode.Text = value;
                UpdateLabelColor();
            }
        }
    }

    private bool _disabled;
    [Export]
    public bool disabled {
        get => _disabled;
        set => SetDisabled(value);
    }

    private TextureButton TextureButtonNode;
    private Label LabelNode;
    private Timer BlinkTimer;

    private bool hovering = false;

    public override void _Ready() {
        TextureButtonNode = GetNode<TextureButton>("CenterTexture/TextureButton");
        LabelNode = GetNode<Label>("CenterLabel/Label");
        BlinkTimer = GetNode<Timer>("BlinkTimer");

        text = _text;

        // FIXME: should we connect in the code instead of the editor? c# migration.
        // TextureButtonNode.Connect("pressed", this, nameof(_on_TextureButton_pressed));
        // TextureButtonNode.Connect("mouse_entered", this, nameof(_on_TextureButton_mouse_entered));
        // TextureButtonNode.Connect("mouse_exited", this, nameof(_on_TextureButton_mouse_exited));
        // BlinkTimer.Connect("timeout", this, nameof(_on_BlinkTimer_timeout));
    }

    private void _on_TextureButton_pressed() {
        LabelNode.Modulate = LABEL_COLOR_CLICK;
        BlinkTimer.Start();
        EmitSignal(nameof(pressed));
    }

    private void _on_TextureButton_mouse_entered() {
        hovering = true;
        if (_disabled)
            return;
        LabelNode.Modulate = LABEL_COLOR_HOVER;
    }

    private void _on_TextureButton_mouse_exited() {
        hovering = false;
        if (_disabled)
            return;
        LabelNode.Modulate = LABEL_COLOR;
    }

    private void _on_BlinkTimer_timeout() {
        LabelNode.Modulate = hovering ? LABEL_COLOR_HOVER : LABEL_COLOR;
        if (_disabled)
            LabelNode.Modulate = LABEL_COLOR_DISABLED;
    }

    private void SetDisabled(bool value) {
        _disabled = value;
        if (_disabled) {
            TextureButtonNode.Disabled = true;
            LabelNode.Modulate = LABEL_COLOR_DISABLED;
        }
        else {
            TextureButtonNode.Disabled = false;
            LabelNode.Modulate = hovering ? LABEL_COLOR_HOVER : LABEL_COLOR;
        }
    }

    private void UpdateLabelColor() {
        LabelNode.Modulate = _disabled ? LABEL_COLOR_DISABLED : LABEL_COLOR;
    }
}
