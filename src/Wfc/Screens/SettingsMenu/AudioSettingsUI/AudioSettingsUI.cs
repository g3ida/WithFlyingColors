namespace Wfc.Screens.SettingsMenu;

using System;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Settings;
using Wfc.Entities.Ui;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class AudioSettingsUI : Control, IUITab {
  [NodePath("GridContainer/SfxSlider")]
  private UISliderButton SfxSliderNode = default!;
  [NodePath("GridContainer/MusicSlider")]
  private UISliderButton MusicSliderNode = default!;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();

    SfxSliderNode.Value = GameSettings.SfxVolume;
    MusicSliderNode.Value = GameSettings.MusicVolume;
    // SfxSliderNode.Connect("drag_ended", this, nameof(_onSfxSliderDragEnded));
  }

  private void _onSfxSliderDragEnded() {
    GameSettings.SfxVolume = (float)SfxSliderNode.Value;
    EventHandler.Instance.EmitSfxVolumeChanged((float)SfxSliderNode.Value);
  }

  private static void _on_SfxSliderButtonValueChanged(float value) {
    GameSettings.SfxVolume = value;
    EventHandler.Instance.EmitSfxVolumeChanged(value);
  }

  private static void _onMusicSliderValueChanged(float value) {
    GameSettings.MusicVolume = value;
    EventHandler.Instance.EmitMusicVolumeChanged(value);
  }

  public void OnGainFocus() {
    SfxSliderNode.GrabFocus();
  }

  private static void _onSfxSliderSelectionChanged(bool isSelected) {
    EventHandler.Instance.EmitSfxVolumeChanged(GameSettings.SfxVolume);
  }

  private static void _onMusicSliderSelectionChanged(bool isSelected) {
    EventHandler.Instance.EmitMusicVolumeChanged(GameSettings.MusicVolume);
  }
}
