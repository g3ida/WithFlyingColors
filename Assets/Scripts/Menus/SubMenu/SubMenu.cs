using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

[Tool]
public class SubMenu : Control
{
    private PackedScene SubMenuItemScene = (PackedScene)GD.Load("res://Assets/Scenes/MainMenu/SubMenuItem.tscn");

    public List<string> buttons = new List<string>();
    public List<int> buttons_ids = new List<int>();
    public List<MenuButtons> buttons_events = new List<MenuButtons>();
    public List<bool> buttons_disabled = new List<bool>();

    [Export] public Color color;
    [Export] public Color top_color;

    private VBoxContainer ContainerNode;
    private Control TopNode;

    private List<SubMenuItem> ButtonNodes = new List<SubMenuItem>();

    public override void _Ready()
    {
        SetProcess(false);
        CheckState();
        ContainerNode = GetNode<VBoxContainer>("VBoxContainer");
        TopNode = GetNode<Control>("VBoxContainer/Top");
        InitItems();
        InitContainer();
        SetFocusDependencies();
        SetFocusButton();
    }

    private void CheckState()
    {
        Debug.Assert(buttons.Count == buttons_ids.Count);
        Debug.Assert(buttons.Count == buttons_events.Count);
        Debug.Assert(buttons.Count == buttons_disabled.Count);
    }

    private void InitContainer()
    {
        ContainerNode.MarginTop = 0;
        ContainerNode.MarginBottom = 0;
        TopNode.Modulate = top_color;
    }

    private void InitItems()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            var item = (SubMenuItem)SubMenuItemScene.Instance();
            item.color = color;
            item.text = buttons[i];
            item.id = buttons_ids[i];
            item.@event = buttons_events[i];
            item.disabled = buttons_disabled[i];
            ContainerNode.AddChild(item);
            ButtonNodes.Add(item);
            item.Owner = ContainerNode;
            RectSize = new Vector2(RectSize.x, RectSize.y + item.RectSize.y);
            RectMinSize = new Vector2(RectMinSize.x, RectMinSize.y + item.RectMinSize.y);
        }
    }

    private void SetFocusButton()
    {
        foreach (var button in ButtonNodes)
        {
            if (!(bool)button.Get("disabled"))
            {
                button.ButtonGrabFocus();
                break;
            }
        }
    }

    private void SetFocusDependencies()
    {
        List<int> activeIndexes = new List<int>();
        for (int i = 0; i < ButtonNodes.Count; i++)
        {
            if (!(bool)ButtonNodes[i].Get("disabled"))
            {
                activeIndexes.Add(i);
            }
        }

        for (int i = 0; i < activeIndexes.Count - 1; i++)
        {
            var current = ButtonNodes[activeIndexes[i]];
            var next = ButtonNodes[activeIndexes[i + 1]];

            current.FocusNext = next.GetPath();
            next.FocusPrevious = current.GetPath();
            current.FocusNeighbourBottom = next.GetPath();
            next.FocusNeighbourTop = current.GetPath();
        }
    }

    public void UpdateColors()
    {
        foreach (Node ch in ContainerNode.GetChildren())
        {
            if (ch is TextureRect textureRect)
            {
                textureRect.Modulate = top_color;
            }
            else if (ch is SubMenuItem item)
            {
                item.color = color;
                item.UpdateColors();
            }
        }
    }
}
