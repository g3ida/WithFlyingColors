namespace Wfc.Entities.Ui;

using Godot;
using Wfc.Utils;

public partial class Checkbox : CheckBox, IDarkBackgroundAware {
  #region Exports
  // The tick and its empty box are drawn dark for a light surface and inverted
  // for a dark one. The row underneath says which pair is showing.
  [Export]
  public Texture2D? CheckedIcon { get; set; }
  [Export]
  public Texture2D? UncheckedIcon { get; set; }
  [Export]
  public Texture2D? CheckedIconOnDark { get; set; }
  [Export]
  public Texture2D? UncheckedIconOnDark { get; set; }
  #endregion Exports

  public bool OnDarkBackground {
    set {
      _setIcon("checked", value ? CheckedIconOnDark : CheckedIcon);
      _setIcon("unchecked", value ? UncheckedIconOnDark : UncheckedIcon);
    }
  }

  // A pair left unfilled leaves the icon to the theme rather than clearing it.
  private void _setIcon(string name, Texture2D? icon) {
    if (icon != null) {
      AddThemeIconOverride(name, icon);
    }
  }

  public override void _Ready() {
    base._Ready();
    FocusMode = FocusModeEnum.All;
    this.GrabFocusOnHover();
  }
}
