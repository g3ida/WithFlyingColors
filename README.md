# With Flying Colors

![line coverage][line-coverage] ![branch coverage][branch-coverage] [![godot][godot-badge]][godot] [![license][license-badge]](./LICENSE)

A 2D color-matching platformer built with Godot 4 and C#.

<p align="center">
<img alt="Game Logo" src="icon.png" width="200">
</p>

## 🎨 The Game

You play a cube with four differently colored faces — blue, pink, purple and yellow.
Every platform in the world is colored too, and a face may only touch a platform of its
own color. Land on the wrong color and the cube explodes back to the last checkpoint, so
moving means rotating the cube mid-air to line the right face up with whatever you are
about to land on.

The game currently is localized into **7 languages** (English, French, German,
Spanish, Italian, Portuguese, Dutch).

## 🎮 Controls

Defaults — every action is rebindable in **Settings → Controller**, and the on-screen
input hints follow whichever device you last touched.

| Action | Keyboard | Gamepad |
| --- | --- | --- |
| Move | <kbd>←</kbd> <kbd>→</kbd> | D-pad ←/→ or left stick |
| Jump | <kbd>↑</kbd> | A |
| Down | <kbd>↓</kbd> | D-pad ↓ or left stick |
| Rotate left | <kbd>Z</kbd> | Left shoulder |
| Rotate right | <kbd>C</kbd> | Right shoulder |
| Dash | <kbd>X</kbd> | X |
| Pause | <kbd>Esc</kbd> | Start |
| Switch settings tab | — | Left / right shoulder |

## 🥚 Getting Started

Requirements:

- **Godot 4.7** — the .NET / Mono build. The exact `Godot.NET.Sdk` version is pinned in
  [`global.json`](./global.json).
- **.NET SDK** — version and roll-forward policy also come from
  [`global.json`](./global.json).

```sh
git clone https://github.com/g3ida/WithFlyingColors.git
cd WithFlyingColors
dotnet build
```

Then open the project in the Godot editor and press <kbd>F5</kbd>, or run it from the
command line:

```sh
$GODOT
```

## 👷 Testing

Tests run inside the game, using [GoDotTest] with [Shouldly] for assertions,
[LightMoq]/[LightMock] for mocks and [godot-test-driver] for driving scenes. There are two
suites:

- `test/src/` — unit tests that need little or no Godot runtime.
- `test/Instrumented/src/` — tests that build real nodes and menu screens in a live tree.
  Fakes for the injected services live in `test/Instrumented/src/Helpers/Fakes/`.

```sh
# Run everything
$GODOT --run-tests --quit-on-finish

# Run a single test class
$GODOT --run-tests=PlayerRotationTest --quit-on-finish
```

Other useful runner flags: `--sequential`, `--stop-on-error`, `--coverage` (forces the
exit code through C# so a failing run actually fails).

The `.csproj` keeps the test scripts and test-only package references out of release
builds.

## 🚦 Test Coverage

Coverage needs two `dotnet` global tools, installed from the project root:

```sh
dotnet tool install --global coverlet.console
dotnet tool install --global dotnet-reportgenerator-globaltool
```

Then run the script, which builds, collects coverage, generates an HTML report under
`coverage/report/` and refreshes the badges in `badges/`:

```sh
chmod +x ./coverage.sh   # first time only
./coverage.sh
```

On Windows, [`coverage.ps1`](./coverage.ps1) is available if `coverlet` has trouble
finding the .NET runtime. Coverage requires `GODOT` to be set, and a `coverlet` version
newer than 3.2.0 — older releases [could not instrument Godot 4 assemblies][coverlet-issues].

## 📄 License

MIT — see [LICENSE](./LICENSE). © 2023 G3idaGames.

Bootstrapped from the [Chickensoft] Godot game template, and still using their tooling for
DI, testing and code style.

---

<!-- Links -->

<!-- Header -->
[line-coverage]: badges/line_coverage.svg
[branch-coverage]: badges/branch_coverage.svg
[godot-badge]: https://img.shields.io/badge/Godot-4.7-blue?logo=godotengine&logoColor=white
[godot]: https://godotengine.org/
[license-badge]: https://img.shields.io/badge/license-MIT-green

<!-- Article -->
[Chickensoft]: https://chickensoft.games
[Chickensoft AutoInject]: https://github.com/chickensoft-games/AutoInject
[GoDotTest]: https://github.com/chickensoft-games/go_dot_test
[godot-test-driver]: https://github.com/derkork/godot-test-driver
[Shouldly]: https://github.com/shouldly/shouldly
[LightMoq]: https://github.com/chickensoft-games/LightMoq
[LightMock]: https://github.com/seesharper/LightMock
[setup-docs]: https://chickensoft.games/docs/setup
[cspell]: https://marketplace.visualstudio.com/items?itemName=streetsidesoftware.code-spell-checker
[Renovatebot]: https://www.mend.io/free-developer-tools/renovate/
[coverlet-issues]: https://github.com/coverlet-coverage/coverlet/issues/1422
