using Godot;
using System;

public class AudioSettingsUI : Control
{
    private UISliderButton SfxSliderNode;
    private UISliderButton MusicSliderNode;

    public override void _Ready()
    {
        SfxSliderNode = GetNode<UISliderButton>("GridContainer/SfxSlider");
        MusicSliderNode = GetNode<UISliderButton>("GridContainer/MusicSlider");

        SfxSliderNode.Value = GameSettings.Instance().SfxVolume;
        MusicSliderNode.Value = GameSettings.Instance().MusicVolume;

        // SfxSliderNode.Connect("drag_ended", this, nameof(_on_SfxSlider_drag_ended));
        // SfxSliderNode.Connect("value_changed", this, nameof(_on_SfxSliderButton_value_changed));
        // SfxSliderNode.Connect("selection_changed", this, nameof(_on_SfxSlider_selection_changed));

        // MusicSliderNode.Connect("value_changed", this, nameof(_on_MusicSlider_value_changed));
        // MusicSliderNode.Connect("selection_changed", this, nameof(_on_MusicSlider_selection_changed));
    }

    private void _on_SfxSlider_drag_ended()
    {
        GameSettings.Instance().SfxVolume = (float)SfxSliderNode.Value;
        Event.Instance().EmitSfxVolumeChanged((float)SfxSliderNode.Value);
    }

    private void _on_SfxSliderButton_value_changed(float value)
    {
        GameSettings.Instance().SfxVolume = value;
        Event.Instance().EmitSfxVolumeChanged(value);
    }

    private void _on_MusicSlider_value_changed(float value)
    {
        GameSettings.Instance().MusicVolume = value;
        Event.Instance().EmitMusicVolumeChanged(value);
    }

    private void on_gain_focus()
    {
        SfxSliderNode.GrabFocus();
    }

    private void _on_SfxSlider_selection_changed(bool isSelected)
    {
        Event.Instance().EmitSfxVolumeChanged(GameSettings.Instance().SfxVolume);
    }

    private void _on_MusicSlider_selection_changed(bool isSelected)
    {
        Event.Instance().EmitMusicVolumeChanged(GameSettings.Instance().MusicVolume);
    }
}
