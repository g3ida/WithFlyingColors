using System;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Settings;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class AudioSettingsUI : Control, IUITab {
  private UISliderButton SfxSliderNode;
  private UISliderButton MusicSliderNode;

  public override void _Ready() {
    SfxSliderNode = GetNode<UISliderButton>("GridContainer/SfxSlider");
    MusicSliderNode = GetNode<UISliderButton>("GridContainer/MusicSlider");

    SfxSliderNode.Value = GameSettings.SfxVolume;
    MusicSliderNode.Value = GameSettings.MusicVolume;

    // SfxSliderNode.Connect("drag_ended", this, nameof(_on_SfxSlider_drag_ended));
    // SfxSliderNode.Connect("value_changed", this, nameof(_on_SfxSliderButton_value_changed));
    // SfxSliderNode.Connect("selection_changed", this, nameof(_on_SfxSlider_selection_changed));

    // MusicSliderNode.Connect("value_changed", this, nameof(_on_MusicSlider_value_changed));
    // MusicSliderNode.Connect("selection_changed", this, nameof(_on_MusicSlider_selection_changed));
  }

  private void _on_SfxSlider_drag_ended() {
    GameSettings.SfxVolume = (float)SfxSliderNode.Value;
    EventHandler.Instance.EmitSfxVolumeChanged((float)SfxSliderNode.Value);
  }

  private void _on_SfxSliderButton_value_changed(float value) {
    GameSettings.SfxVolume = value;
    EventHandler.Instance.EmitSfxVolumeChanged(value);
  }

  private void _on_MusicSlider_value_changed(float value) {
    GameSettings.MusicVolume = value;
    EventHandler.Instance.EmitMusicVolumeChanged(value);
  }

  public void on_gain_focus() {
    SfxSliderNode.GrabFocus();
  }

  private void _on_SfxSlider_selection_changed(bool isSelected) {
    EventHandler.Instance.EmitSfxVolumeChanged(GameSettings.SfxVolume);
  }

  private void _on_MusicSlider_selection_changed(bool isSelected) {
    EventHandler.Instance.EmitMusicVolumeChanged(GameSettings.MusicVolume);
  }
}
