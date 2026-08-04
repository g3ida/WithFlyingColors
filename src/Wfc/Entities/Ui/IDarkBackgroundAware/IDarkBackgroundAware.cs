namespace Wfc.Entities.Ui;

// A control whose art ships twice, drawn for a light surface and inverted for a
// dark one. Whoever paints the surface underneath owns which of the two shows:
// a settings row fills with the shade its panel is not when it takes focus, and
// everything standing on it swaps over.
public interface IDarkBackgroundAware {
  bool OnDarkBackground { set; }
}
