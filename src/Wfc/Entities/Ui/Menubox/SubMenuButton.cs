namespace Wfc.Entities.Ui.Menubox;
using System;

public class SubMenuButton {
  public required string Text;
  public required Action OnClick;
  public bool IsDisabled;
}