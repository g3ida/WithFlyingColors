---
name: wfc-code-style
description: House style for the WithFlyingColors Godot 4 / C# game — file & folder layout, naming, formatting, the Chickensoft AutoInject + [ScenePath]/[NodePath] node patterns, signal lifecycle, localization, and build/test commands. Load this before writing or editing any .cs or .tscn file in this repo, and when reviewing a change for consistency with the surrounding code.
---

# WithFlyingColors code style

Godot 4.7 · .NET 9 · C# 12 · `Nullable` enabled · Chickensoft AutoInject + Introspection.

Match the surrounding file first. Where a file disagrees with this guide, follow the
guide for **new** code and leave the existing code alone unless the task is a cleanup.

Do not write inline comments or docstrings unless logic is mathematically complex or non-obvious. Match minimal comment density. Keep inline comments focused strictly on "Why" instead of "What."

## Layout

One public type per file. The folder is named after the type, and the folder path
mirrors the namespace:

```
src/Wfc/Entities/Ui/Menubox/SubMenu/SubMenu.cs      namespace Wfc.Entities.Ui.Menubox;
src/Wfc/Screens/SettingsMenu/SettingsMenu/SettingsMenu.cs
```

Scene-backed types keep `Type.cs`, `Type.cs.uid` and `Type.tscn` in the same folder.
**When deleting or renaming a type, delete or rename its `.uid` and `.tscn` too** —
Godot tracks resources by uid and a stale one breaks scene loading.

File header order — file-scoped namespace, then `using` directives *inside* the
namespace, `System` first, no blank line between groups:

```csharp
namespace Wfc.Entities.Ui.InputHint;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Input.Controllers;
using Wfc.Utils;
using EventHandler = Wfc.Core.Event.EventHandler;
```

`EventHandler` always needs the alias — it collides with `System.EventHandler`.

## Formatting

Enforced by `.editorconfig`; run `dotnet format` if unsure.

- 2-space indent, LF, final newline.
- Opening brace on the same line. `else` / `catch` / `finally` on a **new** line.
- Braces always, even for one-line bodies.
- `var` everywhere; expression-bodied members, switch expressions and pattern
  matching are preferred over their verbose forms.
- No `this.` qualification except for AutoInject extension calls (`this.WireNodes()`,
  `this.DependOn<T>()`, `this.Notify(what)`, `this.Provide()`).

## Naming

| Kind | Style | Example |
|---|---|---|
| Types, methods, properties, events | PascalCase | `SettingsTabManager` |
| Interfaces | `I` + PascalCase | `IMenuManager` |
| Private / protected fields | `_camelCase` | `_currentState` |
| Public fields | PascalCase | `Duration` |
| Constants | `UPPER_SNAKE_CASE` | `SUB_MENU_POPUP_DURATION` |
| Private methods | `_camelCase` | `_refreshAll()` |
| Node-reference fields | `_camelCase` + `Node` suffix | `_menuBoxNode` |

Private methods are split ~50/50 between `_camelCase` and PascalCase across the repo.
The newest code (`InputHintBar`, `KeyBindingButton`, `ControllerSelectDriver`) uses
`_camelCase` — do that for new code, don't churn existing methods.

Never use GDScript-style `_on_Node_signal_name`. Those are leftovers from the port.

## Node access

Never `GetNode<T>("path")` in a field initializer or scattered through methods.
Declare the field with `[NodePath]` and call `this.WireNodes()` once:

```csharp
#region Nodes
[NodePath("CenterTexture/TextureButton")]
private TextureButton _textureButtonNode = default!;
[NodePath("BlinkTimer")]
private Timer _blinkTimer = default!;
#endregion Nodes

public override void _Ready() {
  base._Ready();
  this.WireNodes();
}
```

Node fields are non-nullable and initialized `= null!` or `= default!` (both are in
use; `default!` in newer code).

Never hardcode a `res://` path. Put `[ScenePath]` on the class and load it through
`SceneHelpers.LoadScene<T>()` / `SceneHelpers.InstantiateNode<T>()` /
`parent.InstantiateChildNode<T>()`, which read the attribute.

## Dependency injection

Chickensoft AutoInject. A node that needs a service:

```csharp
[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class Foo : Control {
  #region Dependencies
  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();
  #endregion Dependencies

  public void OnResolved() {
    // Anything that reads a dependency belongs here, not in _Ready.
  }
}
```

`_Ready()` runs before dependencies resolve. Put node wiring in `_Ready`, and
anything that touches a dependency (localized text, save data) in `OnResolved`.

Providers live in `DependenciesProvider`. `GameMenu` cannot use `[Meta]` directly —
it resolves through its nested `DependenciesWrapper` and exposes `protected` accessors.

## Signal lifecycle

Subscribe in `_EnterTree`, unsubscribe in `_ExitTree`, guarded by a flag:

```csharp
private bool _isSubscribed;

public override void _EnterTree() {
  base._EnterTree();
  if (!_isSubscribed) {
    EventHandler.Instance.Events.LastUsedControllerChanged += _onLastUsedControllerChanged;
    _isSubscribed = true;
  }
}

public override void _ExitTree() {
  base._ExitTree();
  if (_isSubscribed) {
    EventHandler.Instance.Events.LastUsedControllerChanged -= _onLastUsedControllerChanged;
    _isSubscribed = false;
  }
}
```

Not `_Ready` — `UIGridRow` reparents its content while the settings screen builds
itself, which fires `_ExitTree` on a node whose `_Ready` will never run again.

Emit through the typed helpers on `EventHandler` (`EmitMenuActionPressed`,
`EmitLastUsedControllerChanged`, …), never `EmitSignal("SomeString")`. Add a new
`[Signal]` to `Events` plus an `EmitXxx` method on `EventHandler` and `IEventHandler`.

## Input

Prefer event-driven input over per-frame polling.

- In `_Input(InputEvent @event)` use `InputManager.IsEventActionJustPressed(action, @event)`.
  `IsJustPressed` polls global state and returns true for *every* event delivered in
  that frame — with a gamepad connected it fires repeatedly off analog-stick noise.
- `IsJustPressed` is fine in `_Process` / `_PhysicsProcess`.
- Go through `IInputManager` and `IInputManager.Action`, not `Input.IsActionJustPressed("literal")`.
- `GetViewport().SetInputAsHandled()` stops propagation to every other `_input`
  handler. Only call it when this node genuinely owns the event.

Avoid `_Process` for layout or animation state that a signal could drive
(`Resized`, `FocusEntered`, `Tween.Finished`).

## Regions

Used for grouping in bigger node classes, in this order, each closed with a
matching `#endregion Name`:

```
#region Constants / #region Dependencies / #region Signals
#region Exports / #region Fields / #region Nodes
```

Small classes don't need them.

## Comments

Explain *why*, not *what*. Plain `//` above the member, wrapped near 80 columns.
`InputHintBar`, `InputGlyphView` and `KeyBindingButton` are the reference:

```csharp
// While this button is capturing, what gets pressed is a binding rather than
// the player reaching for another device: the automatic keyboard/gamepad
// switch has to stay out of it, or binding a gamepad button would swap the
// panel over to the keyboard halfway through the capture.
private static void _setDetectionEnabled(bool enabled) { ... }
```

XML doc comments are the minority — don't add them to private members. Never leave
`GD.Print` debug output or commented-out code blocks behind.

**Keep them short.** One or two lines is the norm and most code needs none at all. A
paragraph has to earn itself. Don't narrate the history of a bug, don't explain the fix
(the code *is* the fix), don't restate what the next line already says. If a comment is
getting long, the usual cause is a name that should be better.

**Never write concrete values into a comment** — sizes, speeds, radii, pixel offsets,
layer masks, durations, angles. The value moves and the comment doesn't, and nothing
catches the drift. Name the constant, or state the relationship, and let the reader read
the number off the code:

```csharp
// Bad:  the area is 12.61 against a 12 body, so touching bodies already overlap
// Good: every area shape is slightly larger than its body, so touching bodies overlap
```

The same rule applies to commit messages and PR descriptions.

## Localization

Every player-visible string goes through
`LocalizationService.GetLocalizedString(TranslationKey.some_key)`. `TranslationKey` is
an enum exported to scenes, so **its member order is serialized into `.tscn` files as
integers** — only ever append new members.

## Build & test

```bash
dotnet build
```

Tests: `test/src/` (plain unit tests) and `test/Instrumented/src/` (need a Godot
runtime). GoDotTest + Shouldly + LightMoq; fakes in `test/Instrumented/src/Helpers/Fakes/`.

Spelling is checked in CI against `cspell.json`. A new identifier with an unusual word
may need adding to its `words` list.

## Gotchas

- Removing a member from an enum that is `[Export]`ed shifts the integer written into
  every `.tscn` that uses it. Check scene files before touching `TranslationKey`,
  `SkinColor`, `InputHintCard.HintKind`.
- `Debug.Assert` is compiled out of Release builds — don't rely on it for validation
  that matters at runtime.
- `GetTree().Paused` is global. Check who else owns it before setting it.
- `QueueFree()` is deferred; a freed node still runs `_Process`/`_Input` for the rest
  of the frame.
