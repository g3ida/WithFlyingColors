namespace Wfc.Entities.Ui.SettingsUI;

using Godot;

public interface IEditableControl {
    bool IsInEditMode();
    void setEditing(bool isEditing);
}