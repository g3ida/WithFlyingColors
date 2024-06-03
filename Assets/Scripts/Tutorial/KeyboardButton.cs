using System;
using System.Linq;
using Godot;

[Tool]
public class KeyboardButton : Control
{
    private const int MarginsX = 23;
    private const int ButtonWidth = 103;

    private readonly uint[] ArrowKeys = { 
        (uint)KeyList.Right,
        (uint)KeyList.Down,
        (uint)KeyList.Left,
        (uint)KeyList.Up
    };

    [Export]
    public string key_text { get; set; } = "";

    private Label _labelNode;
    private NinePatchRect _buttonTextureNode;
    private Sprite _arrowSpriteNode;

    public override void _Ready()
    {
        base._Ready();
        _labelNode = GetNode<Label>("Label");
        _buttonTextureNode = GetNode<NinePatchRect>("NinePatchRect");
        _arrowSpriteNode = GetNode<Sprite>("Arrow");

        var actionList = InputMap.GetActionList(key_text);
        if (actionList != null && actionList.Count > 0)
        {
            var key = InputUtils.GetFirstKeyKeyboardEventFromActionList(actionList.Cast<InputEventKey>());
            if (key != null)
            {
                var index = Array.IndexOf(ArrowKeys, key.Scancode);
                if (index != -1)
                {
                    _arrowSpriteNode.Visible = true;
                    _arrowSpriteNode.Rotation = index * Mathf.Pi / 2;
                    _labelNode.Text = "aaa"; // just to fill the necessary space
                    _labelNode.Visible = false;
                }
                else
                {
                    _labelNode.Text = OS.GetScancodeString(key.Scancode);
                }
                _on_Label_resized();
            }
        }
    }

    private void _on_Label_resized()
    {
        var width = Mathf.Max(_labelNode.RectSize.x + MarginsX, ButtonWidth);
        var height = _labelNode.RectSize.y;

        _buttonTextureNode.RectSize = new Vector2(width, height);
        _buttonTextureNode.RectMinSize = _buttonTextureNode.RectSize;
        RectSize = _buttonTextureNode.RectSize;
        RectMinSize = _buttonTextureNode.RectSize;
        _labelNode.RectPosition = new Vector2((width - _labelNode.RectSize.x) * 0.72f, _labelNode.RectPosition.y);
    }
}
