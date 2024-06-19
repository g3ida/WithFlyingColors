using System;
using System.Linq;
using Godot;

[Tool]
public partial class KeyboardButton : Control
{
    private const int MarginsX = 23;
    private const int ButtonWidth = 103;

    private readonly uint[] ArrowKeys = { 
        (uint)Key.Right,
        (uint)Key.Down,
        (uint)Key.Left,
        (uint)Key.Up
    };

    [Export]
    public string key_text { get; set; } = "";

    private Label _labelNode;
    private NinePatchRect _buttonTextureNode;
    private Sprite2D _arrowSpriteNode;

    public override void _Ready()
    {
        base._Ready();
        _labelNode = GetNode<Label>("Label");
        _buttonTextureNode = GetNode<NinePatchRect>("NinePatchRect");
        _arrowSpriteNode = GetNode<Sprite2D>("Arrow");

        var actionList = InputMap.ActionGetEvents(key_text);
        if (actionList != null && actionList.Count > 0)
        {
            var key = InputUtils.GetFirstKeyKeyboardEventFromActionList(actionList.Cast<InputEventKey>());
            if (key != null)
            {
                var index = Array.IndexOf(ArrowKeys, (uint)key.Keycode);
                if (index != -1)
                {
                    _arrowSpriteNode.Visible = true;
                    _arrowSpriteNode.Rotation = index * Mathf.Pi / 2;
                    _labelNode.Text = "aaa"; // just to fill the necessary space
                    _labelNode.Visible = false;
                }
                else
                {
                    _labelNode.Text = OS.GetKeycodeString(key.Keycode);
                }
                _on_Label_resized();
            }
        }
    }

    private void _on_Label_resized()
    {
        var width = Mathf.Max(_labelNode.Size.X + MarginsX, ButtonWidth);
        var height = _labelNode.Size.Y;

        _buttonTextureNode.Size = new Vector2(width, height);
        _buttonTextureNode.CustomMinimumSize = _buttonTextureNode.Size;
        SetDeferred(nameof(Size), _buttonTextureNode.Size);
        CustomMinimumSize = _buttonTextureNode.Size;
        _labelNode.Position = new Vector2((width - _labelNode.Size.X) * 0.72f, _labelNode.Position.Y);
    }
}
