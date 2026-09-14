# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Core tests (fast loop — use this while iterating on combat rules)

```bash
dotnet test Tests.Core/CoreTests.csproj -v minimal
```

Runs in ~2 seconds (132 tests) because `Tests.Core/CoreTests.csproj` is a plain .NET 8 project living *outside* `Assets/` that globs and compiles `Assets/Scripts/Core/**/*.cs` directly — it never opens Unity. This is the loop to use for any change under `Assets/Scripts/Core/`.

Single test class or method:

```bash
dotnet test Tests.Core/CoreTests.csproj --filter TileGridTests
dotnet test Tests.Core/CoreTests.csproj --filter TileGridTests.ScrambleReplacesTheBoard
```

`Tests.Core/CoreTests.csproj` pins `<LangVersion>9.0</LangVersion>` deliberately — Unity 6 compiles C# 9, so anything newer would compile here but fail in the Editor.

### Unity batch-mode compile check (verifies Assets/Scripts/Game and the whole project)

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.0f1/Editor/Unity.exe" \
  -batchmode -quit -nographics \
  -projectPath "$(pwd)" \
  -logFile - | grep -Ei "error CS|Compilation failed"
```

No output = clean. Takes 60–90 seconds; there is no faster way to verify `Assets/Scripts/Game/**` changes, since that layer references `UnityEngine` and can't go through the `dotnet test` sidecar.

### Unity PlayMode tests (scene wiring only — not game rules)

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.0f1/Editor/Unity.exe" \
  -batchmode -runTests -nographics \
  -projectPath "$(pwd)" \
  -testPlatform PlayMode \
  -testResults "$(pwd)/playmode-results.xml" \
  -logFile -
grep -o 'testcasecount="[0-9]*" result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' playmode-results.xml
```

4 tests in `Assets/Scripts/Tests/BattleSeamTests.cs`, checking that the `Battle` scene loads, an engine constructs from the wired assets, commands reach it, and the presenter drains without exceptions. These deliberately do not test balance or combat rules — that's what the Core suite is for.

## Architecture

Codex of Echoes is a word-combat battle prototype (a Bookworm-Adventures-style word game) built as two Unity assemblies with a hard boundary between them.

### The Core / Game split

- **`Assets/Scripts/Core/`** (asmdef `CodexOfEchoes.Core`, `"noEngineReferences": true`) — every game rule: RNG, the tile grid, word validation, the damage formula, combatant HP, enemy AI, and the battle engine. Zero `UnityEngine` references anywhere in this tree — the asmdef setting makes that a compile error, not a convention. This is what the `dotnet test` sidecar compiles.
- **`Assets/Scripts/Game/`** (asmdef `CodexOfEchoes.Game`, references `CodexOfEchoes.Core` + `Unity.InputSystem` + `Unity.TextMeshPro` + `UnityEngine.UI`) — the Unity-side presentation layer: ScriptableObject data assets, input routing, MonoBehaviours, and views. This layer contains no combat rules of its own; it only constructs a `BattleEngine` from Core and reacts to what it reports.

When changing game balance or rules, work in Core and verify with `dotnet test`. When changing how the game looks or is controlled, work in Game and verify with the batch-mode compile check (PlayMode tests only catch wiring breakage, not visual or balance regressions).

### BattleEngine: synchronous command → event core

`Assets/Scripts/Core/Battle/BattleEngine.cs` exposes one method: `Execute(BattleCommand) → IReadOnlyList<BattleEvent>`. It is fully synchronous and instantaneous — a whole turn (cast a word, damage the enemy, refill the grid, resolve the enemy's response) resolves in one call, in one thread, with no waiting. This is deliberate: it's what makes the rules testable without a Unity scene or any timing infrastructure, and it's why `Tests.Core` can run a battle from a seed and assert on it directly.

**All animation timing lives in `Assets/Scripts/Game/Battle/BattlePresenter.cs`**, which receives the event list and drains it as a coroutine with a per-event-type delay (`BeatFor`). `BattleRunner` owns the actual `BattleEngine` instance and refuses to `Submit` a new command while the presenter is still draining (`IsBusy`), so the player can never act on a mid-animation board. If you need to change pacing or add a new animation, that belongs in the Game layer, not Core — Core should never know how long anything takes to *show*.

Event order is part of the contract, not an implementation detail: for a successful cast, events land in a specific causal sequence (word cast → enemy damaged → tiles consumed/fall/spawn → selection cleared → enemy acts → player damaged → optional heal → optional telegraph). `BattleView.OnEvent` and the presenter both depend on this order to animate correctly; changing the sequence in `BattleEngine` without checking both consumers will silently desync the visuals from the model.

### Damage formula: no floating point

`Assets/Scripts/Core/Combat/DamageCalculator.cs` computes damage using scaled-integer arithmetic (`long numerator = base * (100 + 22k) * story10`, then `(numerator*2+1000)/2000`), not `float` or `double`. This is deliberate, not incidental: an earlier float-based version silently mis-rounded roughly a quarter of all true-midpoint words, because `0.22f` isn't exactly representable in binary. `DamageCalculator.LengthStep` (the public `0.22f` constant) still exists because a test reads it, but the actual `Compute` arithmetic never touches it — don't reintroduce floating point into this method for readability; the exact-integer path is the fix.

Word length in the formula means **character count, not tile count** — a "Qu" tile occupies one board slot but counts as 2 characters toward the length multiplier. This resolves a genuine ambiguity in the original design spec and is load-bearing for `DamageCalculatorTests.cs`'s `QuTileCountsTenPointsButTwoCharactersOfLength` test.

### The Spine seam

`Assets/Scripts/Game/View/CombatantView.cs` is an abstract class with exactly five methods (`PlayIdle`, `PlayCast`, `PlayHurt`, `PlayVictory`, `PlayDefeat`). `PlaceholderCombatantView.cs` is the only current implementation, using colored sprites and tweens. Nothing outside `PlaceholderCombatantView.cs` should ever reference that concrete class — `BattleView` and the scene's serialized fields are typed as the abstract `CombatantView` specifically so a future `SpineCombatantView` can be swapped in against real character animation without changing anything upstream. If you add a sixth method to this class or a call site that special-cases the placeholder, you've broken the seam.

### Unity ↔ Core boundary: BattleSetupFactory

`Assets/Scripts/Game/Data/BattleSetupFactory.cs` is the **single place** where Unity `ScriptableObject` data (enemy stats, Story Word lists, balance config) gets converted into Core's plain `BattleSetup`/`StoryWordTable` types. If you're adding new authorable data, it should flow through here — not through a second conversion path — so Core's inputs stay serialization-friendly and engine-independent.

### Dictionary

`Assets/Scripts/Core/Words/IWordDictionary.cs` is an interface because Core cannot read files. `Assets/Scripts/Game/Data/DictionaryLoader.cs` loads the real word list — the public-domain ENABLE word list (~173k words) at `Assets/StreamingAssets/enable.txt` — into a `HashSetWordDictionary` at runtime. Tests inject small in-memory fixtures instead. Do not substitute a Scrabble-official word list (TWL/SOWPODS): both are licensed and unsafe to ship commercially.

### Design docs

`docs/superpowers/specs/2026-09-14-word-combat-vertical-slice-design.md` is the approved design spec (combat formula, event ordering contract, Aswang AI numbers, known limitations). `docs/superpowers/plans/2026-09-14-word-combat-vertical-slice.md` is the task-by-task implementation plan this codebase was built from. Both are worth reading before making a rules change — several bugs caught during implementation were violations of specific, deliberate decisions recorded there (e.g. the vowel invariant, "lethal resolves immediately," half-away-from-zero rounding).
