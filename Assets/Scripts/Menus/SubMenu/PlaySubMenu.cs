using Godot;
using System;
using System.Collections.Generic;

[Tool]
public class PlaySubMenu : Control
{
    private PackedScene SubMenuScene = (PackedScene)GD.Load("res://Assets/Scenes/MainMenu/SubMenu.tscn");

    private const bool SHOULD_HIDE_DISABLED_BUTTONS = true;

    private SubMenu SubMenuNode;

    private enum ButtonConditions
    {
        NONE,
        NEED_ACTIVE_SLOT, // an active slot contains a game in progress not a won game
        NEED_NEW_SLOT, // a new slot with progress 0%
    }

    private class ButtonDefinition
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public MenuButtons Button { get; set; }
        public ButtonConditions Conditions { get; set; }
    }

    private List<ButtonDefinition> ButtonsDef = new List<ButtonDefinition>
    {
        new ButtonDefinition
        {
            Id = 0,
            Text = "Continue",
            Button = MenuButtons.CONTINUE_GAME,
            Conditions = ButtonConditions.NEED_ACTIVE_SLOT
        },
        new ButtonDefinition
        {
            Id = 1,
            Text = "New Game",
            Button = MenuButtons.NEW_GAME,
            Conditions = ButtonConditions.NEED_NEW_SLOT
        },
        new ButtonDefinition
        {
            Id = 3,
            Text = "Select Level",
            Button = MenuButtons.SELECT_LEVEL,
            Conditions = ButtonConditions.NEED_ACTIVE_SLOT
        },
        new ButtonDefinition
        {
            Id = 4,
            Text = $"Current Slot: {SaveGame.Instance().currentSlotIndex + 1}",
            Button = MenuButtons.SELECT_SLOT,
            Conditions = ButtonConditions.NONE
        },
    };

    public override void _Ready()
    {
        var colorIndex = ColorUtils.GetGroupColorIndex("blue");
        var blue = ColorUtils.GetSkinBasicColor(SkinLoader.DEFAULT_SKIN, colorIndex);
        SubMenuNode = (SubMenu)SubMenuScene.Instance();
        SubMenuNode.color = ColorUtils.DarkenRGB(blue, 0.0f);
        SubMenuNode.color.a = 1f; // FIXME: this is hack fix in color utils
        SubMenuNode.top_color = ColorUtils.DarkenRGB(blue, 0.115f);

        SubMenuNode.buttons = new List<string>();
        SubMenuNode.buttons_events = new List<MenuButtons>();
        SubMenuNode.buttons_ids = new List<int>();
        SubMenuNode.buttons_disabled = new List<bool>();

        foreach (var btn in ButtonsDef)
        {
            if (SHOULD_HIDE_DISABLED_BUTTONS && ShouldDisableButton(btn)) continue;
            SubMenuNode.buttons.Add(btn.Text);
            SubMenuNode.buttons_events.Add(btn.Button);
            SubMenuNode.buttons_ids.Add(btn.Id);
            SubMenuNode.buttons_disabled.Add(ShouldDisableButton(btn));
        }

        AddChild(SubMenuNode);
        SubMenuNode.Owner = this;
        RectMinSize = SubMenuNode.RectMinSize;
        RectSize = SubMenuNode.RectSize;
    }

    private bool ShouldDisableButton(ButtonDefinition btn)
    {
        if (btn.Conditions == ButtonConditions.NEED_ACTIVE_SLOT)
        {
            return !SaveGame.Instance().DoesSlotHaveProgress(SaveGame.Instance().currentSlotIndex);
        }

        if (btn.Conditions == ButtonConditions.NEED_NEW_SLOT)
        {
            return SaveGame.Instance().DoesSlotHaveProgress(SaveGame.Instance().currentSlotIndex);
        }

        return false;
    }
}
