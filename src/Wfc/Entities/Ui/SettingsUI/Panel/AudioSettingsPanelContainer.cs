namespace Wfc.Entities.Ui.SettingsUI.Panel;

using Godot;
using Wfc.Core.Event;
using Wfc.Core.Settings;
using Wfc.Utils;
using Wfc.Utils.Attributes;

public partial class AudioSettingsPanelContainer : PanelContainer {

  #region Nodes
  // I had to add the "Content" node in the path since the UGridRow adds an extra container
  [NodePath("MarginContainer/UiGridContainer/SfxVolume/Content/SfxSlider")]
  private HSlider _sfxSliderNode = default!;
  [NodePath("MarginContainer/UiGridContainer/MusicVolume/Content/MusicSlider")]
  private HSlider _musicSliderNode = default!;
  #endregion Nodes


  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _sfxSliderNode.Value = GameSettings.SfxVolume;
    _musicSliderNode.Value = GameSettings.MusicVolume;
    _sfxSliderNode.ValueChanged += _on_SfxSliderButtonValueChanged;
    _sfxSliderNode.DragEnded += _onSfxSliderDragEnded;
    _musicSliderNode.ValueChanged += _onMusicSliderValueChanged;
  }

  public override void _ExitTree() {
    base._ExitTree();
    _sfxSliderNode.ValueChanged -= _on_SfxSliderButtonValueChanged;
    _sfxSliderNode.DragEnded -= _onSfxSliderDragEnded;
    _musicSliderNode.ValueChanged -= _onMusicSliderValueChanged;
  }

  private void _onSfxSliderDragEnded(bool valueChanged) {
    var newValue = (float)_sfxSliderNode.Value;
    GameSettings.SfxVolume = newValue;
    EventHandler.Instance.EmitSfxVolumeChanged(newValue);
  }

  private static void _on_SfxSliderButtonValueChanged(double value) {
    var newValue = (float)value;
    GameSettings.SfxVolume = newValue;
    EventHandler.Instance.EmitSfxVolumeChanged(newValue);
  }

  private static void _onMusicSliderValueChanged(double value) {
    var newValue = (float)value;
    GameSettings.MusicVolume = newValue;
    EventHandler.Instance.EmitMusicVolumeChanged(newValue);
  }
}
