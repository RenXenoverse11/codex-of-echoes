# Word Combat Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build one complete, playable battle — tile grid, word forming, dictionary validation, damage, an Aswang that fights back, win and lose states — to answer whether the combat loop is fun.

**Architecture:** An engine-free C# core (`CodexOfEchoes.Core`, zero `UnityEngine` references) holds every rule; a thin Unity layer (`CodexOfEchoes.Game`) renders state and forwards input. The core exposes one method — `Execute(BattleCommand) → IReadOnlyList<BattleEvent>` — which is synchronous and instantaneous, with all timing isolated in the presentation layer. A sidecar `.csproj` outside `Assets/` compiles the same core sources so `dotnet test` runs in seconds instead of Unity's 60–90s batch-mode startup.

**Tech Stack:** Unity 6000.5.0f1 (Built-in Render Pipeline), C# 9, NUnit, Unity Input System, TextMeshPro, .NET 8 SDK for the test sidecar.

**Spec:** [`docs/superpowers/specs/2026-09-14-word-combat-vertical-slice-design.md`](../specs/2026-09-14-word-combat-vertical-slice-design.md)

## Global Constraints

Every task's requirements implicitly include this section.

- **Unity version:** 6000.5.0f1. Already installed at `C:\Program Files\Unity\Hub\Editor\6000.5.0f1`.
- **C# language version:** 9.0. Unity 6 compiles C# 9; the sidecar `.csproj` MUST pin `<LangVersion>9.0</LangVersion>` or it will accept C# 12 syntax that Unity later rejects.
- **No records, no `init` setters.** Unity 6's C# 9 support for these is unreliable. Use plain `sealed class` with constructor-assigned readonly properties.
- **`<Nullable>disable</Nullable>`** in the sidecar, matching Unity's default.
- **Core has zero `using UnityEngine`.** The `CodexOfEchoes.Core.asmdef` sets `"noEngineReferences": true`, which makes this a compile error rather than a convention.
- **Core types stay serialization-friendly:** no Unity types, no circular references, stable field names. Cross-save is a locked later requirement.
- **Rounding is half-away-from-zero:** `Math.Round(x, MidpointRounding.AwayFromZero)`. C#'s default banker's rounding would shave damage off roughly half of all midpoint results.
- **Minimum word length:** 3.
- **Length multiplier step:** 0.22.
- **Story Word multipliers:** Echo ×1.5, Bane ×2.5, None ×1.0. Bane takes precedence when a word is in both sets.
- **Liora:** 100 HP. **Aswang:** 75 HP, Strike 7–11, Feast every 4th turn (20 damage, heals self 10), telegraphed one turn ahead.
- **Lethal resolves immediately:** if the player's word reduces the enemy to 0 HP, the enemy does not act that turn.
- **Grid indexing:** index = `row * 4 + col`, index 0 = top-left, index 15 = bottom-right. Gravity moves tiles toward higher indices; new tiles spawn at the top of their column.
- **Commit after every task.** Conventional commit prefixes (`feat:`, `test:`, `chore:`).

### Spec ambiguity resolved in this plan

The spec defines `Qu` as a single tile but does not say whether "word length" in the damage formula means tile count or character count. **This plan uses character count** for both the minimum-length check and the length multiplier — it matches what the player sees ("I made a 5-letter word"), and the `Qu` tile's value of 10 already rewards its specialness. `base` still sums per-*tile* values. All four worked examples in the spec are Qu-free, so they are unaffected either way.

---

## File Structure

**Core** (`Assets/Scripts/Core/`, asmdef `CodexOfEchoes.Core`, no engine references):

| File | Responsibility |
|---|---|
| `Rng/IRandomSource.cs` | Seeded randomness abstraction |
| `Rng/SeededRandom.cs` | Deterministic implementation |
| `Grid/LetterTile.cs` | One tile: letter string + point value |
| `Grid/LetterTable.cs` | Letter values, bag weights, vowel test |
| `Grid/TileBag.cs` | Weighted draw |
| `Grid/TileGrid.cs` | 4×4 board, gravity, refill, vowel invariant |
| `Grid/GridRefillResult.cs` | Moves + spawns produced by a consume |
| `Words/IWordDictionary.cs` | Word lookup abstraction |
| `Words/HashSetWordDictionary.cs` | In-memory implementation |
| `Words/StoryWordTable.cs` | Echo / Bane tiering |
| `Words/WordValidator.cs` | Length + dictionary checks |
| `Combat/DamageCalculator.cs` | The damage formula |
| `Combat/Combatant.cs` | HP, damage, healing |
| `Combat/AswangBehaviour.cs` | Strike / Feast scheduling |
| `Battle/BattleCommand.cs` | Command types |
| `Battle/BattleEvent.cs` | Event types |
| `Battle/BattleState.cs` | Observable battle state |
| `Battle/BattleSetup.cs` | Engine construction parameters |
| `Battle/BattleEngine.cs` | Command → events |

**Game** (`Assets/Scripts/Game/`, asmdef `CodexOfEchoes.Game` → Core + UnityEngine):

| File | Responsibility |
|---|---|
| `Data/EnemyDefinitionSO.cs` | Enemy stats as an authorable asset |
| `Data/StoryWordSetSO.cs` | Echo / Bane word lists as an asset |
| `Data/BalanceConfigSO.cs` | Player HP and tuning constants |
| `Data/DictionaryLoader.cs` | Loads `enable.txt` into a `HashSetWordDictionary` |
| `Battle/BattleRunner.cs` | Owns the engine, pumps commands |
| `Battle/BattlePresenter.cs` | Drains events as a coroutine queue |
| `Input/TileInputRouter.cs` | Keyboard + pointer → commands |
| `View/TileView.cs` | One tile's visual |
| `View/CombatantView.cs` | Abstract five-call animation seam |
| `View/PlaceholderCombatantView.cs` | Rectangle + tween implementation |
| `View/WordCastView.cs` | Word flies at the enemy |
| `View/HealthBarView.cs` | HP display |

**Tests:** `Tests.Core/CoreTests.csproj` + test files (outside `Assets/`), and `Assets/Scripts/Tests/` for PlayMode seam tests.

---

## Task List

| # | Task | Deliverable |
|---|---|---|
| 1 | Project scaffolding + test loop | `dotnet test` runs and passes |
| 2 | Deterministic RNG | Reproducible sequences from a seed |
| 3 | Letter table + tile bag | Weighted draws |
| 4 | Tile grid | Fill, consume, gravity, refill, vowel invariant |
| 5 | Word validation | Dictionary, Story Word tiers, rejection reasons |
| 6 | Damage calculator | The formula, matching all spec examples |
| 7 | Combatant + Aswang behaviour | HP and the Feast schedule |
| 8 | Battle engine: types + selection | Select / deselect / clear |
| 9 | Battle engine: cast word | The full turn pipeline |
| 10 | Battle engine: scramble + end states | Victory and defeat |
| 11 | Tuning validation | Spec's difficulty model asserted in code |
| 12 | Unity data layer | ScriptableObjects + dictionary loading |
| 13 | Runner + presenter | Events drained as animations |
| 14 | Input router | Keyboard and pointer |
| 15 | Views + Spine seam | Placeholder visuals |
| 16 | Scene assembly + seam tests | A playable battle |

---

## Prerequisite: the ENABLE word list

Tasks 1–11 need no network — every core test builds its dictionary from an in-memory fixture. Task 12 needs the real list.

Obtain `enable.txt` (~173k words, public domain) and place it at `Assets/StreamingAssets/enable.txt`. It is widely mirrored; one source is `https://raw.githubusercontent.com/dolph/dictionary/master/enable1.txt`. If the executing environment has no network, Task 12's loader test uses a fixture and the real file can be dropped in before first manual playtest. **Do not substitute TWL or SOWPODS** — both are licensed and unsafe to ship commercially.

---

### Task 1: Project scaffolding and the `dotnet test` loop

Nothing else in this plan is verifiable until this task is done. It creates the Unity project, both assembly definitions, the sidecar test project, and proves the fast loop works with one trivial test.

**Files:**
- Create: `.gitignore`
- Create: `Assets/Scripts/Core/CodexOfEchoes.Core.asmdef`
- Create: `Assets/Scripts/Core/Placeholder.cs`
- Create: `Tests.Core/CoreTests.csproj`
- Create: `Tests.Core/ScaffoldingTests.cs`
- Create (via Unity CLI): `ProjectSettings/`, `Packages/manifest.json`

**Interfaces:**
- Consumes: nothing
- Produces: a working `dotnet test Tests.Core` command; the `CodexOfEchoes.Core` assembly name and root namespace used by every later task

- [ ] **Step 1: Create the Unity project via CLI**

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.0f1/Editor/Unity.exe" \
  -batchmode -quit -nographics \
  -createProject "C:/Users/ADMIN/Desktop/Projects/Game Development/codex-of-echoes" \
  -logFile -
```

This takes 1–3 minutes. It generates `ProjectSettings/`, `Packages/`, and an empty `Assets/`. It does not touch `docs/`.

If it fails on licensing, create the project through Unity Hub instead (New Project → 2D Built-In Render Pipeline → same folder) and continue from Step 2.

- [ ] **Step 2: Verify the project was created**

Run: `ls ProjectSettings/ProjectVersion.txt && cat ProjectSettings/ProjectVersion.txt`
Expected: `m_EditorVersion: 6000.5.0f1`

- [ ] **Step 3: Write the `.gitignore`**

Create `.gitignore`. The final line is the one that matters — Unity's standard template ignores `*.csproj`, which would silently exclude the test sidecar from version control.

```gitignore
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/
[Mm]emoryCaptures/
[Rr]ecordings/

*.csproj
*.unityproj
*.sln
*.suo
*.user
*.userprefs
*.pidb
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db

.vs/
.vscode/
.idea/
.consulo/

sysinfo.txt
*.apk
*.aab
*.unitypackage
*.app
crashlytics-build.properties

# Test sidecar must survive the *.csproj rule above
!Tests.Core/*.csproj
Tests.Core/bin/
Tests.Core/obj/
```

- [ ] **Step 4: Create the Core assembly definition**

Create `Assets/Scripts/Core/CodexOfEchoes.Core.asmdef`. `noEngineReferences` turns "don't use UnityEngine in Core" from a convention into a compile error.

```json
{
    "name": "CodexOfEchoes.Core",
    "rootNamespace": "CodexOfEchoes.Core",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": true
}
```

- [ ] **Step 5: Create a placeholder Core type**

Create `Assets/Scripts/Core/Placeholder.cs`. This exists only so the sidecar has something to compile in Step 6; Task 2 deletes it.

```csharp
namespace CodexOfEchoes.Core
{
    /// <summary>Temporary. Deleted in Task 2 once real Core types exist.</summary>
    public static class Placeholder
    {
        public static string AssemblyName => "CodexOfEchoes.Core";
    }
}
```

- [ ] **Step 6: Create the test sidecar project**

Create `Tests.Core/CoreTests.csproj`. It lives outside `Assets/` so Unity ignores it entirely, and compiles the Core sources directly rather than referencing a built assembly.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <Nullable>disable</Nullable>
    <IsPackable>false</IsPackable>
    <RootNamespace>CodexOfEchoes.Core.Tests</RootNamespace>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="../Assets/Scripts/Core/**/*.cs" />
    <Compile Include="*.cs" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="NUnit" Version="4.2.2" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.6.0" />
  </ItemGroup>

</Project>
```

- [ ] **Step 7: Write the failing scaffolding test**

Create `Tests.Core/ScaffoldingTests.cs`:

```csharp
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class ScaffoldingTests
    {
        [Test]
        public void CoreSourcesAreCompiledByTheSidecar()
        {
            Assert.That(Placeholder.AssemblyName, Is.EqualTo("CodexOfEchoes.Core"));
        }
    }
}
```

- [ ] **Step 8: Run the test**

Run: `dotnet test Tests.Core/CoreTests.csproj -v minimal`
Expected: PASS, 1 test. First run restores packages and takes ~30s; subsequent runs are ~2s.

If this fails with "SDK not found", verify `dotnet --version` reports 8.0 or later.

- [ ] **Step 9: Confirm the sidecar is not gitignored**

Run: `git check-ignore -v Tests.Core/CoreTests.csproj; echo "exit=$?"`
Expected: `exit=1` (no output, meaning NOT ignored). If it prints a matching rule, the `!Tests.Core/*.csproj` negation in Step 3 is wrong or out of order.

- [ ] **Step 10: Commit**

```bash
git add -A
git commit -m "chore: scaffold Unity project and dotnet test sidecar"
```

---

### Task 2: Deterministic RNG

Every battle must be reproducible from a seed — this is what turns "that fight felt unwinnable" into a replayable report rather than an impression.

**Files:**
- Create: `Assets/Scripts/Core/Rng/IRandomSource.cs`
- Create: `Assets/Scripts/Core/Rng/SeededRandom.cs`
- Delete: `Assets/Scripts/Core/Placeholder.cs`
- Create: `Tests.Core/SeededRandomTests.cs`
- Modify: `Tests.Core/ScaffoldingTests.cs` (drop the Placeholder reference)

**Interfaces:**
- Consumes: nothing
- Produces: `IRandomSource` with `int Next(int maxExclusive)`, `int NextInclusive(int min, int max)`, `int Seed { get; }`; `SeededRandom(int seed)`

- [ ] **Step 1: Write the failing tests**

Create `Tests.Core/SeededRandomTests.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using CodexOfEchoes.Core.Rng;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class SeededRandomTests
    {
        [Test]
        public void SameSeedProducesSameSequence()
        {
            var a = new SeededRandom(12345);
            var b = new SeededRandom(12345);

            var first = Enumerable.Range(0, 50).Select(_ => a.Next(100)).ToList();
            var second = Enumerable.Range(0, 50).Select(_ => b.Next(100)).ToList();

            Assert.That(first, Is.EqualTo(second));
        }

        [Test]
        public void DifferentSeedsProduceDifferentSequences()
        {
            var a = new SeededRandom(1);
            var b = new SeededRandom(2);

            var first = Enumerable.Range(0, 50).Select(_ => a.Next(100)).ToList();
            var second = Enumerable.Range(0, 50).Select(_ => b.Next(100)).ToList();

            Assert.That(first, Is.Not.EqualTo(second));
        }

        [Test]
        public void NextStaysBelowMaxExclusive()
        {
            var rng = new SeededRandom(7);

            for (var i = 0; i < 1000; i++)
            {
                var value = rng.Next(10);
                Assert.That(value, Is.InRange(0, 9));
            }
        }

        [Test]
        public void NextInclusiveCoversBothEnds()
        {
            var rng = new SeededRandom(99);
            var seen = new HashSet<int>();

            for (var i = 0; i < 1000; i++)
            {
                seen.Add(rng.NextInclusive(7, 11));
            }

            Assert.That(seen, Is.EquivalentTo(new[] { 7, 8, 9, 10, 11 }));
        }

        [Test]
        public void SeedIsRecoverable()
        {
            var rng = new SeededRandom(4242);
            rng.Next(10);

            Assert.That(rng.Seed, Is.EqualTo(4242));
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Tests.Core/CoreTests.csproj -v minimal`
Expected: FAIL — `The type or namespace name 'Rng' does not exist`.

- [ ] **Step 3: Write the interface**

Create `Assets/Scripts/Core/Rng/IRandomSource.cs`:

```csharp
namespace CodexOfEchoes.Core.Rng
{
    /// <summary>
    /// All randomness in the battle engine flows through this, so any battle can be
    /// replayed exactly from its seed.
    /// </summary>
    public interface IRandomSource
    {
        int Seed { get; }

        /// <summary>Returns a value in [0, maxExclusive).</summary>
        int Next(int maxExclusive);

        /// <summary>Returns a value in [min, max], both ends reachable.</summary>
        int NextInclusive(int min, int max);
    }
}
```

- [ ] **Step 4: Write the implementation**

Create `Assets/Scripts/Core/Rng/SeededRandom.cs`. This uses xorshift32 rather than `System.Random`, because `System.Random`'s algorithm is not guaranteed stable across .NET versions — a saved seed must reproduce the same battle years from now.

```csharp
using System;

namespace CodexOfEchoes.Core.Rng
{
    /// <summary>
    /// xorshift32. Chosen over System.Random because System.Random's internal
    /// algorithm is not contractually stable across .NET versions, and a stored
    /// seed has to reproduce the same battle indefinitely.
    /// </summary>
    public sealed class SeededRandom : IRandomSource
    {
        private uint _state;

        public SeededRandom(int seed)
        {
            Seed = seed;
            // A zero state would make xorshift emit zeros forever.
            _state = seed == 0 ? 0x9E3779B9u : unchecked((uint)seed);
        }

        public int Seed { get; }

        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxExclusive), "maxExclusive must be positive.");
            }

            return (int)(NextUInt() % (uint)maxExclusive);
        }

        public int NextInclusive(int min, int max)
        {
            if (max < min)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(max), "max must be greater than or equal to min.");
            }

            return min + Next(max - min + 1);
        }

        private uint NextUInt()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state;
        }
    }
}
```

- [ ] **Step 5: Remove the placeholder**

Delete `Assets/Scripts/Core/Placeholder.cs`, then replace the body of `Tests.Core/ScaffoldingTests.cs`:

```csharp
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class ScaffoldingTests
    {
        [Test]
        public void SidecarCompilesCoreSources()
        {
            Assert.That(typeof(Rng.SeededRandom).Namespace,
                Is.EqualTo("CodexOfEchoes.Core.Rng"));
        }
    }
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test Tests.Core/CoreTests.csproj -v minimal`
Expected: PASS, 6 tests.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat: add seeded deterministic random source"
```

---

### Task 3: Letter table and tile bag

**Files:**
- Create: `Assets/Scripts/Core/Grid/LetterTile.cs`
- Create: `Assets/Scripts/Core/Grid/LetterTable.cs`
- Create: `Assets/Scripts/Core/Grid/TileBag.cs`
- Create: `Tests.Core/LetterTableTests.cs`
- Create: `Tests.Core/TileBagTests.cs`

**Interfaces:**
- Consumes: `IRandomSource` from Task 2
- Produces: `LetterTile` (readonly struct, `string Letter`, `int Value`); `LetterTable` statics `AllLetters`, `ValueOf(string)`, `WeightOf(string)`, `IsVowel(string)`, `TotalWeight`; `TileBag(IRandomSource)` with `LetterTile Draw()` and `LetterTile DrawVowel()`

- [ ] **Step 1: Write the failing letter table tests**

Create `Tests.Core/LetterTableTests.cs`:

```csharp
using System.Linq;
using CodexOfEchoes.Core.Grid;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class LetterTableTests
    {
        [Test]
        public void CoversTwentySixLetterSlotsIncludingQu()
        {
            Assert.That(LetterTable.AllLetters.Count, Is.EqualTo(26));
            Assert.That(LetterTable.AllLetters, Contains.Item("Qu"));
            Assert.That(LetterTable.AllLetters, Does.Not.Contain("Q"));
        }

        [TestCase("E", 1)]
        [TestCase("U", 1)]
        [TestCase("D", 2)]
        [TestCase("G", 2)]
        [TestCase("C", 3)]
        [TestCase("H", 4)]
        [TestCase("K", 5)]
        [TestCase("J", 8)]
        [TestCase("X", 8)]
        [TestCase("Z", 10)]
        [TestCase("Qu", 10)]
        public void ValuesMatchTheSpecTable(string letter, int expected)
        {
            Assert.That(LetterTable.ValueOf(letter), Is.EqualTo(expected));
        }

        [Test]
        public void ValueLookupIsCaseInsensitive()
        {
            Assert.That(LetterTable.ValueOf("qu"), Is.EqualTo(10));
            Assert.That(LetterTable.ValueOf("e"), Is.EqualTo(1));
        }

        [Test]
        public void WeightsSumToNinetyEight()
        {
            var total = LetterTable.AllLetters.Sum(LetterTable.WeightOf);

            Assert.That(total, Is.EqualTo(98));
            Assert.That(LetterTable.TotalWeight, Is.EqualTo(98));
        }

        [Test]
        public void VowelWeightIsFortyTwo()
        {
            var vowelWeight = LetterTable.AllLetters
                .Where(LetterTable.IsVowel)
                .Sum(LetterTable.WeightOf);

            Assert.That(vowelWeight, Is.EqualTo(42));
        }

        [Test]
        public void VowelsAreAeiouOnly()
        {
            var vowels = LetterTable.AllLetters.Where(LetterTable.IsVowel).ToList();

            Assert.That(vowels, Is.EquivalentTo(new[] { "A", "E", "I", "O", "U" }));
            Assert.That(LetterTable.IsVowel("Y"), Is.False);
            Assert.That(LetterTable.IsVowel("Qu"), Is.False);
        }

        [Test]
        public void EveryLetterHasPositiveValueAndWeight()
        {
            foreach (var letter in LetterTable.AllLetters)
            {
                Assert.That(LetterTable.ValueOf(letter), Is.GreaterThan(0), letter);
                Assert.That(LetterTable.WeightOf(letter), Is.GreaterThan(0), letter);
            }
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Tests.Core/CoreTests.csproj -v minimal`
Expected: FAIL — `The type or namespace name 'Grid' does not exist`.

- [ ] **Step 3: Write `LetterTile`**

Create `Assets/Scripts/Core/Grid/LetterTile.cs`:

```csharp
namespace CodexOfEchoes.Core.Grid
{
    /// <summary>
    /// One tile on the board. Letter is a string rather than a char because "Qu"
    /// occupies a single tile.
    /// </summary>
    public readonly struct LetterTile
    {
        public LetterTile(string letter, int value)
        {
            Letter = letter;
            Value = value;
        }

        public string Letter { get; }

        public int Value { get; }

        public override string ToString() => $"{Letter}({Value})";
    }
}
```

- [ ] **Step 4: Write `LetterTable`**

Create `Assets/Scripts/Core/Grid/LetterTable.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodexOfEchoes.Core.Grid
{
    /// <summary>
    /// Letter point values and draw weights. Values are Scrabble-like but compressed;
    /// weights are vowel-boosted relative to natural English frequency (42/98 vowels)
    /// because a word game with realistic frequencies produces unplayable boards.
    /// </summary>
    public static class LetterTable
    {
        private static readonly Dictionary<string, int> Values =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "E", 1 }, { "A", 1 }, { "I", 1 }, { "O", 1 }, { "N", 1 },
                { "R", 1 }, { "T", 1 }, { "L", 1 }, { "S", 1 }, { "U", 1 },
                { "D", 2 }, { "G", 2 },
                { "B", 3 }, { "C", 3 }, { "M", 3 }, { "P", 3 },
                { "F", 4 }, { "H", 4 }, { "V", 4 }, { "W", 4 }, { "Y", 4 },
                { "K", 5 },
                { "J", 8 }, { "X", 8 },
                { "Z", 10 }, { "Qu", 10 },
            };

        private static readonly Dictionary<string, int> Weights =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "E", 12 },
                { "A", 9 }, { "I", 9 },
                { "O", 8 },
                { "N", 6 }, { "R", 6 }, { "T", 6 },
                { "L", 4 }, { "S", 4 }, { "U", 4 }, { "D", 4 },
                { "G", 3 },
                { "B", 2 }, { "C", 2 }, { "M", 2 }, { "P", 2 }, { "F", 2 },
                { "H", 2 }, { "V", 2 }, { "W", 2 }, { "Y", 2 },
                { "K", 1 }, { "J", 1 }, { "X", 1 }, { "Z", 1 }, { "Qu", 1 },
            };

        private static readonly HashSet<string> Vowels =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A", "E", "I", "O", "U" };

        public static IReadOnlyList<string> AllLetters { get; } = Values.Keys.ToList();

        public static int TotalWeight { get; } = Weights.Values.Sum();

        public static int ValueOf(string letter) => Values[letter];

        public static int WeightOf(string letter) => Weights[letter];

        public static bool IsVowel(string letter) => Vowels.Contains(letter);

        public static LetterTile CreateTile(string letter) =>
            new LetterTile(Normalize(letter), ValueOf(letter));

        /// <summary>Maps any casing onto the canonical spelling used on tiles.</summary>
        private static string Normalize(string letter)
        {
            foreach (var canonical in Values.Keys)
            {
                if (string.Equals(canonical, letter, StringComparison.OrdinalIgnoreCase))
                {
                    return canonical;
                }
            }

            throw new ArgumentException($"Unknown letter '{letter}'.", nameof(letter));
        }
    }
}
```

- [ ] **Step 5: Run the letter table tests**

Run: `dotnet test Tests.Core/CoreTests.csproj --filter LetterTableTests -v minimal`
Expected: PASS, 17 tests (the `[TestCase]` attributes expand).

- [ ] **Step 6: Write the failing tile bag tests**

Create `Tests.Core/TileBagTests.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Core.Rng;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class TileBagTests
    {
        [Test]
        public void DrawsOnlyKnownLetters()
        {
            var bag = new TileBag(new SeededRandom(1));

            for (var i = 0; i < 500; i++)
            {
                var tile = bag.Draw();
                Assert.That(LetterTable.AllLetters, Contains.Item(tile.Letter));
                Assert.That(tile.Value, Is.EqualTo(LetterTable.ValueOf(tile.Letter)));
            }
        }

        [Test]
        public void SameSeedProducesSameDraws()
        {
            var first = DrawMany(new TileBag(new SeededRandom(555)), 100);
            var second = DrawMany(new TileBag(new SeededRandom(555)), 100);

            Assert.That(first, Is.EqualTo(second));
        }

        [Test]
        public void DrawVowelAlwaysReturnsAVowel()
        {
            var bag = new TileBag(new SeededRandom(3));

            for (var i = 0; i < 200; i++)
            {
                Assert.That(LetterTable.IsVowel(bag.DrawVowel().Letter), Is.True);
            }
        }

        [Test]
        public void VowelShareApproximatesTheWeightTable()
        {
            var bag = new TileBag(new SeededRandom(20260914));
            var vowels = 0;
            const int draws = 20000;

            for (var i = 0; i < draws; i++)
            {
                if (LetterTable.IsVowel(bag.Draw().Letter))
                {
                    vowels++;
                }
            }

            // Expected share is 42/98 = 42.9%. Generous band: this asserts the
            // weighting is applied at all, not that the RNG is perfectly uniform.
            var share = (double)vowels / draws;
            Assert.That(share, Is.InRange(0.39, 0.47));
        }

        [Test]
        public void CommonLettersOutnumberRareOnes()
        {
            var bag = new TileBag(new SeededRandom(808));
            var counts = new Dictionary<string, int>();

            for (var i = 0; i < 20000; i++)
            {
                var letter = bag.Draw().Letter;
                counts.TryGetValue(letter, out var current);
                counts[letter] = current + 1;
            }

            // E has weight 12, Z has weight 1.
            Assert.That(counts["E"], Is.GreaterThan(counts["Z"] * 4));
        }

        private static List<string> DrawMany(TileBag bag, int count) =>
            Enumerable.Range(0, count).Select(_ => bag.Draw().Letter).ToList();
    }
}
```

- [ ] **Step 7: Run the tests to verify they fail**

Run: `dotnet test Tests.Core/CoreTests.csproj --filter TileBagTests -v minimal`
Expected: FAIL — `The name 'TileBag' does not exist`.

- [ ] **Step 8: Write `TileBag`**

Create `Assets/Scripts/Core/Grid/TileBag.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using CodexOfEchoes.Core.Rng;

namespace CodexOfEchoes.Core.Grid
{
    /// <summary>
    /// Weighted infinite draw. Not a finite bag — tiles are never exhausted, so the
    /// board can always refill.
    /// </summary>
    public sealed class TileBag
    {
        private readonly IRandomSource _rng;
        private readonly List<string> _letters;
        private readonly List<int> _cumulativeWeights;
        private readonly List<string> _vowels;

        public TileBag(IRandomSource rng)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));

            _letters = LetterTable.AllLetters.ToList();
            _vowels = _letters.Where(LetterTable.IsVowel).ToList();

            _cumulativeWeights = new List<int>(_letters.Count);
            var running = 0;
            foreach (var letter in _letters)
            {
                running += LetterTable.WeightOf(letter);
                _cumulativeWeights.Add(running);
            }
        }

        public LetterTile Draw() => DrawFrom(_letters, _cumulativeWeights, LetterTable.TotalWeight);

        public LetterTile DrawVowel()
        {
            var vowelWeights = new List<int>(_vowels.Count);
            var running = 0;
            foreach (var vowel in _vowels)
            {
                running += LetterTable.WeightOf(vowel);
                vowelWeights.Add(running);
            }

            return DrawFrom(_vowels, vowelWeights, running);
        }

        private LetterTile DrawFrom(
            IReadOnlyList<string> letters, IReadOnlyList<int> cumulative, int total)
        {
            var roll = _rng.Next(total);

            for (var i = 0; i < cumulative.Count; i++)
            {
                if (roll < cumulative[i])
                {
                    return LetterTable.CreateTile(letters[i]);
                }
            }

            return LetterTable.CreateTile(letters[letters.Count - 1]);
        }
    }
}
```

- [ ] **Step 9: Run the full suite**

Run: `dotnet test Tests.Core/CoreTests.csproj -v minimal`
Expected: PASS, 28 tests.

- [ ] **Step 10: Commit**

```bash
git add -A
git commit -m "feat: add letter value table and weighted tile bag"
```

---

### Task 4: Tile grid

The 4×4 board, its vowel invariant, and the gravity/refill cycle that runs after every cast. The `GridRefillResult` this produces becomes the `TilesFell` and `TilesSpawned` events in Task 9, so the view can animate the drop.

**Files:**
- Create: `Assets/Scripts/Core/Grid/GridRefillResult.cs`
- Create: `Assets/Scripts/Core/Grid/TileGrid.cs`
- Create: `Tests.Core/TileGridTests.cs`

**Interfaces:**
- Consumes: `TileBag`, `LetterTile`, `LetterTable` from Task 3
- Produces: `TileMove` (`int From`, `int To`), `TileSpawn` (`int Index`, `LetterTile Tile`), `GridRefillResult` (`IReadOnlyList<TileMove> Moves`, `IReadOnlyList<TileSpawn> Spawns`); `TileGrid(TileBag)` with `Tiles`, `this[int]`, `VowelCount`, `Consume(IReadOnlyList<int>)`, `Scramble()`, and constants `Size`=16, `Columns`=4, `Rows`=4, `MinVowels`=4

- [ ] **Step 1: Write the failing tests**

Create `Tests.Core/TileGridTests.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Core.Rng;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class TileGridTests
    {
        private static TileGrid NewGrid(int seed) => new TileGrid(new TileBag(new SeededRandom(seed)));

        [Test]
        public void StartsWithSixteenTiles()
        {
            Assert.That(NewGrid(1).Tiles.Count, Is.EqualTo(TileGrid.Size));
            Assert.That(TileGrid.Size, Is.EqualTo(16));
            Assert.That(TileGrid.Columns, Is.EqualTo(4));
            Assert.That(TileGrid.Rows, Is.EqualTo(4));
        }

        [Test]
        public void AlwaysStartsWithAtLeastFourVowels()
        {
            for (var seed = 1; seed <= 300; seed++)
            {
                Assert.That(NewGrid(seed).VowelCount,
                    Is.GreaterThanOrEqualTo(TileGrid.MinVowels), $"seed {seed}");
            }
        }

        [Test]
        public void HoldsTheVowelInvariantAfterConsumingEveryVowel()
        {
            for (var seed = 1; seed <= 100; seed++)
            {
                var grid = NewGrid(seed);
                var vowelIndices = Enumerable.Range(0, TileGrid.Size)
                    .Where(i => LetterTable.IsVowel(grid[i].Letter))
                    .ToList();

                grid.Consume(vowelIndices);

                Assert.That(grid.VowelCount,
                    Is.GreaterThanOrEqualTo(TileGrid.MinVowels), $"seed {seed}");
            }
        }

        [Test]
        public void StaysFullAfterConsuming()
        {
            var grid = NewGrid(42);

            grid.Consume(new[] { 0, 5, 10, 15 });

            Assert.That(grid.Tiles.Count, Is.EqualTo(TileGrid.Size));
        }

        [Test]
        public void ConsumingReportsOneSpawnPerConsumedTile()
        {
            var grid = NewGrid(42);

            var result = grid.Consume(new[] { 1, 2, 3 });

            Assert.That(result.Spawns.Count, Is.EqualTo(3));
        }

        [Test]
        public void SurvivorsFallToTheBottomOfTheirColumn()
        {
            var grid = NewGrid(77);
            // Column 0 is indices 0, 4, 8, 12 (top to bottom).
            var bottomOfColumnZero = grid[12];

            // Consume index 8, directly above the bottom tile.
            var result = grid.Consume(new[] { 8 });

            // The bottom tile never moves; the tiles above index 8 shift down one row.
            Assert.That(grid[12].Letter, Is.EqualTo(bottomOfColumnZero.Letter));
            Assert.That(result.Moves.Any(m => m.From == 4 && m.To == 8), Is.True);
            Assert.That(result.Moves.Any(m => m.From == 0 && m.To == 4), Is.True);
            Assert.That(result.Spawns.Single().Index, Is.EqualTo(0));
        }

        [Test]
        public void ConsumingAWholeColumnSpawnsFourNewTilesInIt()
        {
            var grid = NewGrid(11);

            var result = grid.Consume(new[] { 2, 6, 10, 14 });

            Assert.That(result.Moves, Is.Empty);
            Assert.That(result.Spawns.Select(s => s.Index),
                Is.EquivalentTo(new[] { 2, 6, 10, 14 }));
        }

        [Test]
        public void ConsumingAcceptsIndicesInAnyOrder()
        {
            var grid = NewGrid(5);
            var shuffled = new[] { 10, 0, 5 };

            var result = grid.Consume(shuffled);

            Assert.That(result.Spawns.Count, Is.EqualTo(3));
            Assert.That(grid.Tiles.Count, Is.EqualTo(TileGrid.Size));
        }

        [Test]
        public void ConsumingNothingChangesNothing()
        {
            var grid = NewGrid(9);
            var before = grid.Tiles.Select(t => t.Letter).ToList();

            var result = grid.Consume(new List<int>());

            Assert.That(grid.Tiles.Select(t => t.Letter), Is.EqualTo(before));
            Assert.That(result.Moves, Is.Empty);
            Assert.That(result.Spawns, Is.Empty);
        }

        [Test]
        public void ScrambleReplacesEveryTileAndKeepsTheInvariant()
        {
            var grid = NewGrid(303);
            var before = grid.Tiles.Select(t => t.Letter).ToList();

            grid.Scramble();

            Assert.That(grid.Tiles.Count, Is.EqualTo(TileGrid.Size));
            Assert.That(grid.VowelCount, Is.GreaterThanOrEqualTo(TileGrid.MinVowels));
            Assert.That(grid.Tiles.Select(t => t.Letter), Is.Not.EqualTo(before));
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Tests.Core/CoreTests.csproj --filter TileGridTests -v minimal`
Expected: FAIL — `The name 'TileGrid' does not exist`.

- [ ] **Step 3: Write `GridRefillResult`**

Create `Assets/Scripts/Core/Grid/GridRefillResult.cs`:

```csharp
using System.Collections.Generic;

namespace CodexOfEchoes.Core.Grid
{
    /// <summary>A tile sliding from one board index to another under gravity.</summary>
    public readonly struct TileMove
    {
        public TileMove(int from, int to)
        {
            From = from;
            To = to;
        }

        public int From { get; }

        public int To { get; }
    }

    /// <summary>A newly drawn tile entering the board at the top of a column.</summary>
    public readonly struct TileSpawn
    {
        public TileSpawn(int index, LetterTile tile)
        {
            Index = index;
            Tile = tile;
        }

        public int Index { get; }

        public LetterTile Tile { get; }
    }

    /// <summary>
    /// What a consume did to the board. Carries enough ordering for the view to
    /// animate the fall: apply every move, then every spawn.
    /// </summary>
    public sealed class GridRefillResult
    {
        public GridRefillResult(IReadOnlyList<TileMove> moves, IReadOnlyList<TileSpawn> spawns)
        {
            Moves = moves;
            Spawns = spawns;
        }

        public IReadOnlyList<TileMove> Moves { get; }

        public IReadOnlyList<TileSpawn> Spawns { get; }
    }
}
```

- [ ] **Step 4: Write `TileGrid`**

Create `Assets/Scripts/Core/Grid/TileGrid.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodexOfEchoes.Core.Grid
{
    /// <summary>
    /// The 4x4 board. Index 0 is top-left, index 15 is bottom-right;
    /// index = row * Columns + column. Gravity pulls tiles toward higher indices.
    /// </summary>
    public sealed class TileGrid
    {
        public const int Columns = 4;
        public const int Rows = 4;
        public const int Size = Columns * Rows;
        public const int MinVowels = 4;

        private readonly TileBag _bag;
        private readonly LetterTile[] _tiles = new LetterTile[Size];

        public TileGrid(TileBag bag)
        {
            _bag = bag ?? throw new ArgumentNullException(nameof(bag));
            FillEmptySlots();
            EnforceVowelInvariant();
        }

        public IReadOnlyList<LetterTile> Tiles => _tiles;

        public LetterTile this[int index] => _tiles[index];

        public int VowelCount => _tiles.Count(t => LetterTable.IsVowel(t.Letter));

        /// <summary>
        /// Removes the given tiles, drops survivors to the bottom of their column,
        /// and refills from the top. Indices may arrive in any order.
        /// </summary>
        public GridRefillResult Consume(IReadOnlyList<int> indices)
        {
            if (indices == null)
            {
                throw new ArgumentNullException(nameof(indices));
            }

            if (indices.Count == 0)
            {
                return new GridRefillResult(Array.Empty<TileMove>(), Array.Empty<TileSpawn>());
            }

            var removed = new HashSet<int>(indices);
            var moves = new List<TileMove>();
            var spawns = new List<TileSpawn>();

            for (var column = 0; column < Columns; column++)
            {
                CollapseColumn(column, removed, moves, spawns);
            }

            EnforceVowelInvariant();

            return new GridRefillResult(moves, spawns);
        }

        /// <summary>Replaces the whole board. Costs the player a turn; see BattleEngine.</summary>
        public void Scramble()
        {
            for (var i = 0; i < Size; i++)
            {
                _tiles[i] = _bag.Draw();
            }

            EnforceVowelInvariant();
        }

        private void CollapseColumn(
            int column, HashSet<int> removed, List<TileMove> moves, List<TileSpawn> spawns)
        {
            // Survivors, read bottom-up, keep their relative order as they settle.
            var survivors = new List<int>();
            for (var row = Rows - 1; row >= 0; row--)
            {
                var index = (row * Columns) + column;
                if (!removed.Contains(index))
                {
                    survivors.Add(index);
                }
            }

            var settled = new LetterTile[Rows];
            var writeRow = Rows - 1;

            foreach (var sourceIndex in survivors)
            {
                var destination = (writeRow * Columns) + column;
                settled[writeRow] = _tiles[sourceIndex];

                if (sourceIndex != destination)
                {
                    moves.Add(new TileMove(sourceIndex, destination));
                }

                writeRow--;
            }

            // Everything above the settled survivors is new.
            for (var row = writeRow; row >= 0; row--)
            {
                var index = (row * Columns) + column;
                var tile = _bag.Draw();
                settled[row] = tile;
                spawns.Add(new TileSpawn(index, tile));
            }

            for (var row = 0; row < Rows; row++)
            {
                _tiles[(row * Columns) + column] = settled[row];
            }
        }

        private void FillEmptySlots()
        {
            for (var i = 0; i < Size; i++)
            {
                _tiles[i] = _bag.Draw();
            }
        }

        /// <summary>
        /// Guarantees at least MinVowels vowels on the board. Cheapest available guard
        /// against unplayable hands; real dead-board detection is combinatorial and
        /// deliberately out of scope.
        /// </summary>
        private void EnforceVowelInvariant()
        {
            var deficit = MinVowels - VowelCount;
            if (deficit <= 0)
            {
                return;
            }

            var consonantIndices = Enumerable.Range(0, Size)
                .Where(i => !LetterTable.IsVowel(_tiles[i].Letter))
                .ToList();

            for (var i = 0; i < deficit && i < consonantIndices.Count; i++)
            {
                _tiles[consonantIndices[i]] = _bag.DrawVowel();
            }
        }
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test Tests.Core/CoreTests.csproj --filter TileGridTests -v minimal`
Expected: PASS, 10 tests.

- [ ] **Step 6: Run the full suite**

Run: `dotnet test Tests.Core/CoreTests.csproj -v minimal`
Expected: PASS, 38 tests.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat: add tile grid with gravity, refill, and vowel invariant"
```

---

### Task 5: Word validation

Three small classes that together answer "is this a word, and does it matter to this myth?" Tests build dictionaries from in-memory fixtures, so nothing here needs `enable.txt` or a network.

**Files:**
- Create: `Assets/Scripts/Core/Words/IWordDictionary.cs`
- Create: `Assets/Scripts/Core/Words/HashSetWordDictionary.cs`
- Create: `Assets/Scripts/Core/Words/StoryWordTable.cs`
- Create: `Assets/Scripts/Core/Words/WordValidator.cs`
- Create: `Tests.Core/WordValidationTests.cs`

**Interfaces:**
- Consumes: nothing
- Produces: `IWordDictionary.Contains(string)`; `HashSetWordDictionary(IEnumerable<string>)` with `Count`; `StoryWordTier` enum (`None`, `Echo`, `Bane`); `StoryWordTable(IEnumerable<string> echo, IEnumerable<string> bane)` with `TierOf(string)` and static `MultiplierFor(StoryWordTier)`; `WordRejectionReason` enum (`None`, `TooShort`, `NotInDictionary`); `WordValidator(IWordDictionary)` with `Validate(string)` and const `MinimumLength` = 3

- [ ] **Step 1: Write the failing tests**

Create `Tests.Core/WordValidationTests.cs`:

```csharp
using CodexOfEchoes.Core.Words;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class WordValidationTests
    {
        private static HashSetWordDictionary Fixture() =>
            new HashSetWordDictionary(new[] { "cat", "salt", "garlic", "silence", "quiz", "qua" });

        [Test]
        public void DictionaryFindsKnownWords()
        {
            Assert.That(Fixture().Contains("cat"), Is.True);
            Assert.That(Fixture().Contains("aardvark"), Is.False);
        }

        [Test]
        public void DictionaryIsCaseInsensitive()
        {
            Assert.That(Fixture().Contains("CAT"), Is.True);
            Assert.That(Fixture().Contains("SiLeNcE"), Is.True);
        }

        [Test]
        public void DictionaryReportsItsSize()
        {
            Assert.That(Fixture().Count, Is.EqualTo(6));
        }

        [Test]
        public void DictionaryIgnoresBlankAndDuplicateEntries()
        {
            var dictionary = new HashSetWordDictionary(
                new[] { "cat", "CAT", "  ", "", null, " dog " });

            Assert.That(dictionary.Count, Is.EqualTo(2));
            Assert.That(dictionary.Contains("dog"), Is.True);
        }

        [Test]
        public void ValidatorAcceptsAKnownWordOfLegalLength()
        {
            var validator = new WordValidator(Fixture());

            Assert.That(validator.Validate("cat"), Is.EqualTo(WordRejectionReason.None));
        }

        [Test]
        public void ValidatorRejectsShortWordsBeforeCheckingTheDictionary()
        {
            // "at" is absent from the fixture, but length is the reason reported.
            var validator = new WordValidator(Fixture());

            Assert.That(validator.Validate("at"), Is.EqualTo(WordRejectionReason.TooShort));
        }

        [Test]
        public void ValidatorRejectsUnknownWords()
        {
            var validator = new WordValidator(Fixture());

            Assert.That(validator.Validate("zzzz"),
                Is.EqualTo(WordRejectionReason.NotInDictionary));
        }

        [Test]
        public void ValidatorTreatsNullAndEmptyAsTooShort()
        {
            var validator = new WordValidator(Fixture());

            Assert.That(validator.Validate(null), Is.EqualTo(WordRejectionReason.TooShort));
            Assert.That(validator.Validate(""), Is.EqualTo(WordRejectionReason.TooShort));
        }

        [Test]
        public void MinimumLengthIsThree()
        {
            Assert.That(WordValidator.MinimumLength, Is.EqualTo(3));
        }

        [Test]
        public void StoryTableTiersEchoAndBaneWords()
        {
            var table = new StoryWordTable(
                echoWords: new[] { "name", "truth", "light" },
                baneWords: new[] { "salt", "garlic", "ash" });

            Assert.That(table.TierOf("truth"), Is.EqualTo(StoryWordTier.Echo));
            Assert.That(table.TierOf("garlic"), Is.EqualTo(StoryWordTier.Bane));
            Assert.That(table.TierOf("cat"), Is.EqualTo(StoryWordTier.None));
        }

        [Test]
        public void BaneBeatsEchoWhenAWordIsInBothSets()
        {
            var table = new StoryWordTable(
                echoWords: new[] { "light" },
                baneWords: new[] { "light" });

            Assert.That(table.TierOf("light"), Is.EqualTo(StoryWordTier.Bane));
        }

        [Test]
        public void StoryTableIsCaseInsensitive()
        {
            var table = new StoryWordTable(new[] { "dawn" }, new[] { "SALT" });

            Assert.That(table.TierOf("DAWN"), Is.EqualTo(StoryWordTier.Echo));
            Assert.That(table.TierOf("salt"), Is.EqualTo(StoryWordTier.Bane));
        }

        [TestCase(StoryWordTier.None, 1.0f)]
        [TestCase(StoryWordTier.Echo, 1.5f)]
        [TestCase(StoryWordTier.Bane, 2.5f)]
        public void MultipliersMatchTheSpec(StoryWordTier tier, float expected)
        {
            Assert.That(StoryWordTable.MultiplierFor(tier), Is.EqualTo(expected).Within(0.0001f));
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Tests.Core/CoreTests.csproj --filter WordValidationTests -v minimal`
Expected: FAIL — `The type or namespace name 'Words' does not exist`.

- [ ] **Step 3: Write the dictionary interface and implementation**

Create `Assets/Scripts/Core/Words/IWordDictionary.cs`:

```csharp
namespace CodexOfEchoes.Core.Words
{
    /// <summary>
    /// Word lookup. An interface because Core cannot read files — the Unity layer
    /// loads enable.txt and injects the result, while tests inject a small fixture.
    /// </summary>
    public interface IWordDictionary
    {
        bool Contains(string word);
    }
}
```

Create `Assets/Scripts/Core/Words/HashSetWordDictionary.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace CodexOfEchoes.Core.Words
{
    /// <summary>
    /// In-memory word set. ~173k ENABLE words cost roughly 10 MB here, which is fine
    /// on PC and borderline on mobile; a DAWG is the designated optimization if that
    /// becomes a constraint.
    /// </summary>
    public sealed class HashSetWordDictionary : IWordDictionary
    {
        private readonly HashSet<string> _words =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public HashSetWordDictionary(IEnumerable<string> words)
        {
            if (words == null)
            {
                throw new ArgumentNullException(nameof(words));
            }

            foreach (var word in words)
            {
                if (string.IsNullOrWhiteSpace(word))
                {
                    continue;
                }

                _words.Add(word.Trim());
            }
        }

        public int Count => _words.Count;

        public bool Contains(string word) =>
            !string.IsNullOrWhiteSpace(word) && _words.Contains(word.Trim());
    }
}
```

- [ ] **Step 4: Write `StoryWordTable`**

Create `Assets/Scripts/Core/Words/StoryWordTable.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace CodexOfEchoes.Core.Words
{
    public enum StoryWordTier
    {
        None = 0,
        Echo = 1,
        Bane = 2,
    }

    /// <summary>
    /// The mechanic that separates this from a Bookworm reimplementation: a player who
    /// reads the myth fights better. Echo words are chapter-wide; Bane words are the
    /// specific folklore weaknesses of one enemy.
    /// </summary>
    public sealed class StoryWordTable
    {
        private readonly HashSet<string> _echo =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> _bane =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public StoryWordTable(IEnumerable<string> echoWords, IEnumerable<string> baneWords)
        {
            AddAll(_echo, echoWords);
            AddAll(_bane, baneWords);
        }

        public static float MultiplierFor(StoryWordTier tier)
        {
            switch (tier)
            {
                case StoryWordTier.Bane: return 2.5f;
                case StoryWordTier.Echo: return 1.5f;
                default: return 1.0f;
            }
        }

        /// <summary>Bane takes precedence when a word appears in both sets.</summary>
        public StoryWordTier TierOf(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return StoryWordTier.None;
            }

            var trimmed = word.Trim();

            if (_bane.Contains(trimmed))
            {
                return StoryWordTier.Bane;
            }

            return _echo.Contains(trimmed) ? StoryWordTier.Echo : StoryWordTier.None;
        }

        private static void AddAll(HashSet<string> target, IEnumerable<string> words)
        {
            if (words == null)
            {
                return;
            }

            foreach (var word in words)
            {
                if (!string.IsNullOrWhiteSpace(word))
                {
                    target.Add(word.Trim());
                }
            }
        }
    }
}
```

- [ ] **Step 5: Write `WordValidator`**

Create `Assets/Scripts/Core/Words/WordValidator.cs`:

```csharp
using System;

namespace CodexOfEchoes.Core.Words
{
    public enum WordRejectionReason
    {
        None = 0,
        TooShort = 1,
        NotInDictionary = 2,
    }

    /// <summary>
    /// Rejections are gameplay, not errors — the engine reports a reason and consumes
    /// no turn, so the view can give distinct feedback for each case.
    /// </summary>
    public sealed class WordValidator
    {
        public const int MinimumLength = 3;

        private readonly IWordDictionary _dictionary;

        public WordValidator(IWordDictionary dictionary)
        {
            _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        }

        public WordRejectionReason Validate(string word)
        {
            if (string.IsNullOrWhiteSpace(word) || word.Trim().Length < MinimumLength)
            {
                return WordRejectionReason.TooShort;
            }

            return _dictionary.Contains(word)
                ? WordRejectionReason.None
                : WordRejectionReason.NotInDictionary;
        }
    }
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test Tests.Core/CoreTests.csproj --filter WordValidationTests -v minimal`
Expected: PASS, 15 tests.

- [ ] **Step 7: Run the full suite**

Run: `dotnet test Tests.Core/CoreTests.csproj -v minimal`
Expected: PASS, 53 tests.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "feat: add word dictionary, story word tiers, and validator"
```

---

### Task 6: Damage calculator

The balance-critical piece. Every worked example in the spec is asserted here, so a change to the formula fails loudly rather than drifting.

**Files:**
- Create: `Assets/Scripts/Core/Combat/DamageCalculator.cs`
- Create: `Tests.Core/DamageCalculatorTests.cs`

**Interfaces:**
- Consumes: `LetterTile`, `LetterTable` (Task 3); `StoryWordTier`, `StoryWordTable` (Task 5)
- Produces: `DamageCalculator.Compute(IReadOnlyList<LetterTile> tiles, int wordLength, StoryWordTier tier) → int`; const `LengthStep` = 0.22f

- [ ] **Step 1: Write the failing tests**

Create `Tests.Core/DamageCalculatorTests.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using CodexOfEchoes.Core.Combat;
using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Core.Words;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class DamageCalculatorTests
    {
        private static IReadOnlyList<LetterTile> Spell(params string[] letters) =>
            letters.Select(LetterTable.CreateTile).ToList();

        private static int Damage(StoryWordTier tier, params string[] letters)
        {
            var tiles = Spell(letters);
            var wordLength = letters.Sum(l => l.Length);
            return DamageCalculator.Compute(tiles, wordLength, tier);
        }

        [Test]
        public void CatDealsFive()
        {
            // C(3) + A(1) + T(1) = 5 base, length 3, multiplier 1.00
            Assert.That(Damage(StoryWordTier.None, "C", "A", "T"), Is.EqualTo(5));
        }

        [Test]
        public void SilenceDealsSeventeen()
        {
            // 9 base, length 7, multiplier 1.88 -> 16.92 -> 17
            Assert.That(
                Damage(StoryWordTier.None, "S", "I", "L", "E", "N", "C", "E"),
                Is.EqualTo(17));
        }

        [Test]
        public void SaltAsABaneWordDealsTwelve()
        {
            // 4 base, length 4, multiplier 1.22, Bane 2.5 -> 12.2 -> 12
            Assert.That(Damage(StoryWordTier.Bane, "S", "A", "L", "T"), Is.EqualTo(12));
        }

        [Test]
        public void GarlicAsABaneWordDealsThirtySeven()
        {
            // 9 base, length 6, multiplier 1.66, Bane 2.5 -> 37.35 -> 37
            Assert.That(
                Damage(StoryWordTier.Bane, "G", "A", "R", "L", "I", "C"),
                Is.EqualTo(37));
        }

        [Test]
        public void EchoTierMultipliesByOnePointFive()
        {
            var plain = Damage(StoryWordTier.None, "D", "A", "W", "N");
            var echo = Damage(StoryWordTier.Echo, "D", "A", "W", "N");

            // D(2)+A(1)+W(4)+N(1) = 8 base, length 4, x1.22 = 9.76 -> 10
            // Echo: 9.76 x 1.5 = 14.64 -> 15
            Assert.That(plain, Is.EqualTo(10));
            Assert.That(echo, Is.EqualTo(15));
        }

        [Test]
        public void RoundsHalfAwayFromZeroNotToEven()
        {
            // Two 'E' tiles and an 'S': base 2... construct an exact .5 case instead.
            // K(5) + E(1) = 6 base is length 2 (illegal), so use a 3-letter build:
            // I(1) + C(3) + E(1) = 5 base, length 3, x1.00, Echo x1.5 = 7.5
            // Banker's rounding would give 8 here too, so force a .5 that differs:
            // N(1)+A(1)+P(3) = 5 base, length 3, Echo 1.5 -> 7.5 -> 8 (away from zero)
            Assert.That(Damage(StoryWordTier.Echo, "N", "A", "P"), Is.EqualTo(8));

            // E(1)+A(1)+T(1) = 3 base, length 3, Echo 1.5 -> 4.5 -> 5 (away from zero).
            // Banker's rounding would give 4. This is the case that distinguishes them.
            Assert.That(Damage(StoryWordTier.Echo, "E", "A", "T"), Is.EqualTo(5));
        }

        [Test]
        public void LengthMultiplierNeverDropsBelowOne()
        {
            // Length 3 is the minimum, so the step is never applied negatively.
            Assert.That(Damage(StoryWordTier.None, "E", "A", "T"), Is.EqualTo(3));
        }

        [Test]
        public void QuTileCountsTenPointsButTwoCharactersOfLength()
        {
            // Qu(10) + I(1) + Z(10) = 21 base. "QUIZ" is 4 characters, so x1.22.
            // 21 x 1.22 = 25.62 -> 26
            Assert.That(Damage(StoryWordTier.None, "Qu", "I", "Z"), Is.EqualTo(26));
        }

        [Test]
        public void LengthStepMatchesTheSpec()
        {
            Assert.That(DamageCalculator.LengthStep, Is.EqualTo(0.22f).Within(0.0001f));
        }

        [Test]
        public void EmptyWordDealsNoDamage()
        {
            Assert.That(DamageCalculator.Compute(new List<LetterTile>(), 0,
                StoryWordTier.None), Is.EqualTo(0));
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Tests.Core/CoreTests.csproj --filter DamageCalculatorTests -v minimal`
Expected: FAIL — `The type or namespace name 'Combat' does not exist`.

- [ ] **Step 3: Write `DamageCalculator`**

Create `Assets/Scripts/Core/Combat/DamageCalculator.cs`:

```csharp
using System;
using System.Collections.Generic;
using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Core.Words;

namespace CodexOfEchoes.Core.Combat
{
    /// <summary>
    /// damage = round(base x lengthMultiplier x storyMultiplier)
    ///
    /// base sums per-tile point values; wordLength counts CHARACTERS, not tiles, so a
    /// "Qu" tile contributes 2 to length. The spec defines Qu as one tile but does not
    /// say which measure the formula uses; characters match what the player sees, and
    /// Qu's value of 10 already rewards its rarity.
    /// </summary>
    public static class DamageCalculator
    {
        public const float LengthStep = 0.22f;
        private const int LengthFloor = 3;

        public static int Compute(
            IReadOnlyList<LetterTile> tiles, int wordLength, StoryWordTier tier)
        {
            if (tiles == null || tiles.Count == 0)
            {
                return 0;
            }

            var baseDamage = 0;
            foreach (var tile in tiles)
            {
                baseDamage += tile.Value;
            }

            var lengthMultiplier = 1.0f + (LengthStep * Math.Max(0, wordLength - LengthFloor));
            var raw = baseDamage * lengthMultiplier * StoryWordTable.MultiplierFor(tier);

            // Half-away-from-zero, not C#'s default banker's rounding, which would
            // shave damage off roughly half of all midpoint results.
            return (int)Math.Round(raw, MidpointRounding.AwayFromZero);
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test Tests.Core/CoreTests.csproj --filter DamageCalculatorTests -v minimal`
Expected: PASS, 10 tests.

If `RoundsHalfAwayFromZeroNotToEven` fails at 4 instead of 5, `MidpointRounding.AwayFromZero` is missing — that is precisely the bug this test exists to catch.

- [ ] **Step 5: Run the full suite**

Run: `dotnet test Tests.Core/CoreTests.csproj -v minimal`
Expected: PASS, 63 tests.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: add damage calculator matching spec worked examples"
```

---

### Task 7: Combatant and Aswang behaviour

**Files:**
- Create: `Assets/Scripts/Core/Combat/Combatant.cs`
- Create: `Assets/Scripts/Core/Combat/AswangBehaviour.cs`
- Create: `Tests.Core/CombatantTests.cs`
- Create: `Tests.Core/AswangBehaviourTests.cs`

**Interfaces:**
- Consumes: `IRandomSource` (Task 2)
- Produces: `Combatant(string name, int maxHp)` with `Name`, `MaxHp`, `Hp`, `IsDefeated`, `TakeDamage(int) → int`, `Heal(int) → int`; `EnemyAbility` enum (`None`, `Strike`, `Feast`); `EnemyAction` (`EnemyAbility Ability`, `int Damage`, `int Heal`); `AswangBehaviour(IRandomSource)` with `AbilityForTurn(int)`, `TelegraphsFeastNextTurn(int)`, `Act(int) → EnemyAction`, consts `FeastInterval`=4, `FeastDamage`=20, `FeastHeal`=10, `StrikeMin`=7, `StrikeMax`=11

- [ ] **Step 1: Write the failing combatant tests**

Create `Tests.Core/CombatantTests.cs`:

```csharp
using CodexOfEchoes.Core.Combat;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class CombatantTests
    {
        [Test]
        public void StartsAtFullHealth()
        {
            var liora = new Combatant("Liora", 100);

            Assert.That(liora.Hp, Is.EqualTo(100));
            Assert.That(liora.MaxHp, Is.EqualTo(100));
            Assert.That(liora.Name, Is.EqualTo("Liora"));
            Assert.That(liora.IsDefeated, Is.False);
        }

        [Test]
        public void TakingDamageReducesAndReturnsHp()
        {
            var aswang = new Combatant("Aswang", 75);

            var remaining = aswang.TakeDamage(37);

            Assert.That(remaining, Is.EqualTo(38));
            Assert.That(aswang.Hp, Is.EqualTo(38));
        }

        [Test]
        public void HpClampsAtZeroAndMarksDefeat()
        {
            var aswang = new Combatant("Aswang", 75);

            aswang.TakeDamage(999);

            Assert.That(aswang.Hp, Is.EqualTo(0));
            Assert.That(aswang.IsDefeated, Is.True);
        }

        [Test]
        public void HealingNeverExceedsMaxHp()
        {
            var aswang = new Combatant("Aswang", 75);
            aswang.TakeDamage(5);

            var result = aswang.Heal(10);

            Assert.That(result, Is.EqualTo(75));
            Assert.That(aswang.Hp, Is.EqualTo(75));
        }

        [Test]
        public void HealingRestoresPartialDamage()
        {
            var aswang = new Combatant("Aswang", 75);
            aswang.TakeDamage(40);

            aswang.Heal(10);

            Assert.That(aswang.Hp, Is.EqualTo(45));
        }

        [Test]
        public void NegativeAmountsAreIgnored()
        {
            var liora = new Combatant("Liora", 100);

            liora.TakeDamage(-50);
            Assert.That(liora.Hp, Is.EqualTo(100));

            liora.TakeDamage(30);
            liora.Heal(-10);
            Assert.That(liora.Hp, Is.EqualTo(70));
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Tests.Core/CoreTests.csproj --filter CombatantTests -v minimal`
Expected: FAIL — `The name 'Combatant' does not exist`.

- [ ] **Step 3: Write `Combatant`**

Create `Assets/Scripts/Core/Combat/Combatant.cs`:

```csharp
using System;

namespace CodexOfEchoes.Core.Combat
{
    /// <summary>A fighter's health. Plain and serialization-friendly for cross-save.</summary>
    public sealed class Combatant
    {
        public Combatant(string name, int maxHp)
        {
            if (maxHp <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHp), "maxHp must be positive.");
            }

            Name = name;
            MaxHp = maxHp;
            Hp = maxHp;
        }

        public string Name { get; }

        public int MaxHp { get; }

        public int Hp { get; private set; }

        public bool IsDefeated => Hp <= 0;

        public int TakeDamage(int amount)
        {
            if (amount > 0)
            {
                Hp = Math.Max(0, Hp - amount);
            }

            return Hp;
        }

        public int Heal(int amount)
        {
            if (amount > 0)
            {
                Hp = Math.Min(MaxHp, Hp + amount);
            }

            return Hp;
        }
    }
}
```

- [ ] **Step 4: Write the failing Aswang tests**

Create `Tests.Core/AswangBehaviourTests.cs`:

```csharp
using CodexOfEchoes.Core.Combat;
using CodexOfEchoes.Core.Rng;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class AswangBehaviourTests
    {
        private static AswangBehaviour NewBehaviour(int seed = 1) =>
            new AswangBehaviour(new SeededRandom(seed));

        [TestCase(1, EnemyAbility.Strike)]
        [TestCase(2, EnemyAbility.Strike)]
        [TestCase(3, EnemyAbility.Strike)]
        [TestCase(4, EnemyAbility.Feast)]
        [TestCase(5, EnemyAbility.Strike)]
        [TestCase(7, EnemyAbility.Strike)]
        [TestCase(8, EnemyAbility.Feast)]
        [TestCase(12, EnemyAbility.Feast)]
        public void FeastsOnEveryFourthTurn(int turn, EnemyAbility expected)
        {
            Assert.That(NewBehaviour().AbilityForTurn(turn), Is.EqualTo(expected));
        }

        [TestCase(3, true)]
        [TestCase(7, true)]
        [TestCase(11, true)]
        [TestCase(1, false)]
        [TestCase(4, false)]
        [TestCase(8, false)]
        public void TelegraphsFeastOneTurnAhead(int turn, bool expected)
        {
            Assert.That(NewBehaviour().TelegraphsFeastNextTurn(turn), Is.EqualTo(expected));
        }

        [Test]
        public void StrikeDamageStaysInSevenToEleven()
        {
            var behaviour = NewBehaviour(4242);

            for (var turn = 1; turn <= 300; turn++)
            {
                if (turn % AswangBehaviour.FeastInterval == 0)
                {
                    continue;
                }

                var action = behaviour.Act(turn);

                Assert.That(action.Ability, Is.EqualTo(EnemyAbility.Strike));
                Assert.That(action.Damage, Is.InRange(7, 11));
                Assert.That(action.Heal, Is.EqualTo(0));
            }
        }

        [Test]
        public void FeastDealsTwentyAndHealsTen()
        {
            var action = NewBehaviour().Act(4);

            Assert.That(action.Ability, Is.EqualTo(EnemyAbility.Feast));
            Assert.That(action.Damage, Is.EqualTo(20));
            Assert.That(action.Heal, Is.EqualTo(10));
        }

        [Test]
        public void ConstantsMatchTheSpec()
        {
            Assert.That(AswangBehaviour.FeastInterval, Is.EqualTo(4));
            Assert.That(AswangBehaviour.FeastDamage, Is.EqualTo(20));
            Assert.That(AswangBehaviour.FeastHeal, Is.EqualTo(10));
            Assert.That(AswangBehaviour.StrikeMin, Is.EqualTo(7));
            Assert.That(AswangBehaviour.StrikeMax, Is.EqualTo(11));
        }

        [Test]
        public void SameSeedProducesSameStrikeDamage()
        {
            var first = NewBehaviour(99).Act(1).Damage;
            var second = NewBehaviour(99).Act(1).Damage;

            Assert.That(first, Is.EqualTo(second));
        }
    }
}
```

- [ ] **Step 5: Run the tests to verify they fail**

Run: `dotnet test Tests.Core/CoreTests.csproj --filter AswangBehaviourTests -v minimal`
Expected: FAIL — `The name 'AswangBehaviour' does not exist`.

- [ ] **Step 6: Write `AswangBehaviour`**

Create `Assets/Scripts/Core/Combat/AswangBehaviour.cs`:

```csharp
using System;
using CodexOfEchoes.Core.Rng;

namespace CodexOfEchoes.Core.Combat
{
    public enum EnemyAbility
    {
        None = 0,
        Strike = 1,
        Feast = 2,
    }

    /// <summary>One enemy action: what it did, and its damage and self-heal.</summary>
    public readonly struct EnemyAction
    {
        public EnemyAction(EnemyAbility ability, int damage, int heal)
        {
            Ability = ability;
            Damage = damage;
            Heal = heal;
        }

        public EnemyAbility Ability { get; }

        public int Damage { get; }

        public int Heal { get; }
    }

    /// <summary>
    /// Chapter 1's Aswang. Strikes for 7-11, and every fourth turn Feasts for 20 while
    /// healing 10 — telegraphed a full turn ahead so the player can see it coming.
    /// </summary>
    public sealed class AswangBehaviour
    {
        public const int FeastInterval = 4;
        public const int FeastDamage = 20;
        public const int FeastHeal = 10;
        public const int StrikeMin = 7;
        public const int StrikeMax = 11;

        private readonly IRandomSource _rng;

        public AswangBehaviour(IRandomSource rng)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        }

        /// <summary>Turn numbers are 1-based.</summary>
        public EnemyAbility AbilityForTurn(int turnNumber) =>
            turnNumber > 0 && turnNumber % FeastInterval == 0
                ? EnemyAbility.Feast
                : EnemyAbility.Strike;

        public bool TelegraphsFeastNextTurn(int turnNumber) =>
            AbilityForTurn(turnNumber + 1) == EnemyAbility.Feast;

        public EnemyAction Act(int turnNumber)
        {
            if (AbilityForTurn(turnNumber) == EnemyAbility.Feast)
            {
                return new EnemyAction(EnemyAbility.Feast, FeastDamage, FeastHeal);
            }

            return new EnemyAction(
                EnemyAbility.Strike, _rng.NextInclusive(StrikeMin, StrikeMax), 0);
        }
    }
}
```

- [ ] **Step 7: Run the full suite**

Run: `dotnet test Tests.Core/CoreTests.csproj -v minimal`
Expected: PASS, 87 tests.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "feat: add combatant health and Aswang feast schedule"
```

---

### Task 8: Battle engine — types and selection commands

Defines the command/event/state vocabulary the whole presentation layer speaks, then implements the three selection commands against it. Casting arrives in Task 9.

**Files:**
- Create: `Assets/Scripts/Core/Battle/BattleCommand.cs`
- Create: `Assets/Scripts/Core/Battle/BattleEvent.cs`
- Create: `Assets/Scripts/Core/Battle/BattleState.cs`
- Create: `Assets/Scripts/Core/Battle/BattleSetup.cs`
- Create: `Assets/Scripts/Core/Battle/BattleEngine.cs`
- Create: `Tests.Core/BattleSelectionTests.cs`

**Interfaces:**
- Consumes: `TileGrid`, `LetterTile`, `TileMove`, `TileSpawn` (Task 4); `IWordDictionary`, `StoryWordTable`, `StoryWordTier`, `WordRejectionReason` (Task 5); `Combatant`, `EnemyAbility`, `AswangBehaviour` (Task 7)
- Produces:
  - Commands deriving from `BattleCommand`: `SelectTileCommand(int index)`, `DeselectLastCommand`, `ClearSelectionCommand`, `CastWordCommand`, `ScrambleCommand`
  - Events deriving from `BattleEvent`: `TileSelectedEvent(int)`, `TileDeselectedEvent(int)`, `SelectionClearedEvent`, `CommandRejectedEvent(string)`, `WordRejectedEvent(string, WordRejectionReason)`, `WordCastEvent(string, int, StoryWordTier)`, `DamageDealtEvent(CombatantId, int, int)`, `HealedEvent(CombatantId, int, int)`, `TilesConsumedEvent(IReadOnlyList<int>)`, `TilesFellEvent(IReadOnlyList<TileMove>)`, `TilesSpawnedEvent(IReadOnlyList<TileSpawn>)`, `GridScrambledEvent(IReadOnlyList<LetterTile>)`, `EnemyTelegraphedEvent(EnemyAbility)`, `EnemyActedEvent(EnemyAbility, int, int)`, `BattleEndedEvent(BattleOutcome)`
  - `CombatantId` enum (`Liora`, `Enemy`); `BattleOutcome` enum (`InProgress`, `Victory`, `Defeat`)
  - `BattleSetup(IWordDictionary, StoryWordTable, string enemyName, int enemyMaxHp, int playerMaxHp, int seed)`
  - `BattleState` with `Grid`, `Selection`, `Player`, `Enemy`, `TurnNumber`, `Outcome`, `Seed`, `CurrentWord`
  - `BattleEngine(BattleSetup)` with `State` and `Execute(BattleCommand)`

- [ ] **Step 1: Write the failing tests**

Create `Tests.Core/BattleSelectionTests.cs`:

```csharp
using System.Linq;
using CodexOfEchoes.Core.Battle;
using CodexOfEchoes.Core.Words;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class BattleSelectionTests
    {
        private static BattleEngine NewEngine(int seed = 1)
        {
            var setup = new BattleSetup(
                dictionary: new HashSetWordDictionary(new[] { "cat", "salt" }),
                storyWords: new StoryWordTable(new[] { "dawn" }, new[] { "salt" }),
                enemyName: "Aswang",
                enemyMaxHp: 75,
                playerMaxHp: 100,
                seed: seed);

            return new BattleEngine(setup);
        }

        [Test]
        public void StartsInProgressAtTurnOneWithFullHealth()
        {
            var engine = NewEngine();

            Assert.That(engine.State.Outcome, Is.EqualTo(BattleOutcome.InProgress));
            Assert.That(engine.State.TurnNumber, Is.EqualTo(1));
            Assert.That(engine.State.Player.Hp, Is.EqualTo(100));
            Assert.That(engine.State.Enemy.Hp, Is.EqualTo(75));
            Assert.That(engine.State.Enemy.Name, Is.EqualTo("Aswang"));
            Assert.That(engine.State.Selection, Is.Empty);
        }

        [Test]
        public void StoresItsSeedForReplay()
        {
            Assert.That(NewEngine(20260914).State.Seed, Is.EqualTo(20260914));
        }

        [Test]
        public void SelectingATileEmitsTileSelected()
        {
            var engine = NewEngine();

            var events = engine.Execute(new SelectTileCommand(5));

            Assert.That(events.Single(), Is.TypeOf<TileSelectedEvent>());
            Assert.That(((TileSelectedEvent)events.Single()).Index, Is.EqualTo(5));
            Assert.That(engine.State.Selection, Is.EqualTo(new[] { 5 }));
        }

        [Test]
        public void SelectionPreservesOrder()
        {
            var engine = NewEngine();

            engine.Execute(new SelectTileCommand(9));
            engine.Execute(new SelectTileCommand(2));
            engine.Execute(new SelectTileCommand(14));

            Assert.That(engine.State.Selection, Is.EqualTo(new[] { 9, 2, 14 }));
        }

        [Test]
        public void CurrentWordConcatenatesSelectedTilesInOrder()
        {
            var engine = NewEngine();
            engine.Execute(new SelectTileCommand(0));
            engine.Execute(new SelectTileCommand(1));

            var expected =
                (engine.State.Grid[0].Letter + engine.State.Grid[1].Letter).ToUpperInvariant();

            Assert.That(engine.State.CurrentWord, Is.EqualTo(expected));
        }

        [Test]
        public void SelectingTheSameTileTwiceIsRejected()
        {
            var engine = NewEngine();
            engine.Execute(new SelectTileCommand(3));

            var events = engine.Execute(new SelectTileCommand(3));

            Assert.That(events.Single(), Is.TypeOf<CommandRejectedEvent>());
            Assert.That(engine.State.Selection, Is.EqualTo(new[] { 3 }));
        }

        [TestCase(-1)]
        [TestCase(16)]
        [TestCase(999)]
        public void SelectingAnOutOfRangeIndexIsRejectedWithoutThrowing(int index)
        {
            var engine = NewEngine();

            var events = engine.Execute(new SelectTileCommand(index));

            Assert.That(events.Single(), Is.TypeOf<CommandRejectedEvent>());
            Assert.That(engine.State.Selection, Is.Empty);
        }

        [Test]
        public void DeselectLastRemovesTheMostRecentTile()
        {
            var engine = NewEngine();
            engine.Execute(new SelectTileCommand(1));
            engine.Execute(new SelectTileCommand(7));

            var events = engine.Execute(new DeselectLastCommand());

            Assert.That(events.Single(), Is.TypeOf<TileDeselectedEvent>());
            Assert.That(((TileDeselectedEvent)events.Single()).Index, Is.EqualTo(7));
            Assert.That(engine.State.Selection, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void DeselectLastOnAnEmptySelectionIsRejected()
        {
            var engine = NewEngine();

            var events = engine.Execute(new DeselectLastCommand());

            Assert.That(events.Single(), Is.TypeOf<CommandRejectedEvent>());
        }

        [Test]
        public void ClearSelectionEmptiesTheSelection()
        {
            var engine = NewEngine();
            engine.Execute(new SelectTileCommand(4));
            engine.Execute(new SelectTileCommand(8));

            var events = engine.Execute(new ClearSelectionCommand());

            Assert.That(events.Single(), Is.TypeOf<SelectionClearedEvent>());
            Assert.That(engine.State.Selection, Is.Empty);
        }

        [Test]
        public void ClearSelectionIsIdempotent()
        {
            var engine = NewEngine();

            var events = engine.Execute(new ClearSelectionCommand());

            Assert.That(events.Single(), Is.TypeOf<SelectionClearedEvent>());
            Assert.That(engine.State.Selection, Is.Empty);
        }

        [Test]
        public void AnUnknownCommandIsRejectedRatherThanThrowing()
        {
            var engine = NewEngine();

            var events = engine.Execute(new UnknownTestCommand());

            Assert.That(events.Single(), Is.TypeOf<CommandRejectedEvent>());
        }

        private sealed class UnknownTestCommand : BattleCommand
        {
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Tests.Core/CoreTests.csproj --filter BattleSelectionTests -v minimal`
Expected: FAIL — `The type or namespace name 'Battle' does not exist`.

- [ ] **Step 3: Write the command types**

Create `Assets/Scripts/Core/Battle/BattleCommand.cs`:

```csharp
namespace CodexOfEchoes.Core.Battle
{
    /// <summary>
    /// Everything the player can do. Plain classes rather than records because Unity 6
    /// has unreliable C# 9 support for records and init setters.
    /// </summary>
    public abstract class BattleCommand
    {
    }

    public sealed class SelectTileCommand : BattleCommand
    {
        public SelectTileCommand(int index)
        {
            Index = index;
        }

        public int Index { get; }
    }

    public sealed class DeselectLastCommand : BattleCommand
    {
    }

    public sealed class ClearSelectionCommand : BattleCommand
    {
    }

    public sealed class CastWordCommand : BattleCommand
    {
    }

    public sealed class ScrambleCommand : BattleCommand
    {
    }
}
```

- [ ] **Step 4: Write the event types**

Create `Assets/Scripts/Core/Battle/BattleEvent.cs`:

```csharp
using System.Collections.Generic;
using CodexOfEchoes.Core.Combat;
using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Core.Words;

namespace CodexOfEchoes.Core.Battle
{
    public enum CombatantId
    {
        Liora = 0,
        Enemy = 1,
    }

    public enum BattleOutcome
    {
        InProgress = 0,
        Victory = 1,
        Defeat = 2,
    }

    /// <summary>
    /// What happened, in causal order. The view replays these as an animation queue;
    /// a state diff would report what changed but not in what sequence or why.
    /// </summary>
    public abstract class BattleEvent
    {
    }

    public sealed class TileSelectedEvent : BattleEvent
    {
        public TileSelectedEvent(int index)
        {
            Index = index;
        }

        public int Index { get; }
    }

    public sealed class TileDeselectedEvent : BattleEvent
    {
        public TileDeselectedEvent(int index)
        {
            Index = index;
        }

        public int Index { get; }
    }

    public sealed class SelectionClearedEvent : BattleEvent
    {
    }

    /// <summary>
    /// An illegal command. Never thrown — a mid-battle exception would strand the view
    /// out of sync with the state, which is the worst failure mode available here.
    /// </summary>
    public sealed class CommandRejectedEvent : BattleEvent
    {
        public CommandRejectedEvent(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; }
    }

    /// <summary>A legal command that formed an unusable word. Costs no turn.</summary>
    public sealed class WordRejectedEvent : BattleEvent
    {
        public WordRejectedEvent(string word, WordRejectionReason reason)
        {
            Word = word;
            Reason = reason;
        }

        public string Word { get; }

        public WordRejectionReason Reason { get; }
    }

    public sealed class WordCastEvent : BattleEvent
    {
        public WordCastEvent(string word, int damage, StoryWordTier tier)
        {
            Word = word;
            Damage = damage;
            Tier = tier;
        }

        public string Word { get; }

        public int Damage { get; }

        public StoryWordTier Tier { get; }
    }

    public sealed class DamageDealtEvent : BattleEvent
    {
        public DamageDealtEvent(CombatantId target, int amount, int newHp)
        {
            Target = target;
            Amount = amount;
            NewHp = newHp;
        }

        public CombatantId Target { get; }

        public int Amount { get; }

        public int NewHp { get; }
    }

    public sealed class HealedEvent : BattleEvent
    {
        public HealedEvent(CombatantId target, int amount, int newHp)
        {
            Target = target;
            Amount = amount;
            NewHp = newHp;
        }

        public CombatantId Target { get; }

        public int Amount { get; }

        public int NewHp { get; }
    }

    public sealed class TilesConsumedEvent : BattleEvent
    {
        public TilesConsumedEvent(IReadOnlyList<int> indices)
        {
            Indices = indices;
        }

        public IReadOnlyList<int> Indices { get; }
    }

    public sealed class TilesFellEvent : BattleEvent
    {
        public TilesFellEvent(IReadOnlyList<TileMove> moves)
        {
            Moves = moves;
        }

        public IReadOnlyList<TileMove> Moves { get; }
    }

    public sealed class TilesSpawnedEvent : BattleEvent
    {
        public TilesSpawnedEvent(IReadOnlyList<TileSpawn> spawns)
        {
            Spawns = spawns;
        }

        public IReadOnlyList<TileSpawn> Spawns { get; }
    }

    public sealed class GridScrambledEvent : BattleEvent
    {
        public GridScrambledEvent(IReadOnlyList<LetterTile> tiles)
        {
            Tiles = tiles;
        }

        public IReadOnlyList<LetterTile> Tiles { get; }
    }

    public sealed class EnemyTelegraphedEvent : BattleEvent
    {
        public EnemyTelegraphedEvent(EnemyAbility ability)
        {
            Ability = ability;
        }

        public EnemyAbility Ability { get; }
    }

    public sealed class EnemyActedEvent : BattleEvent
    {
        public EnemyActedEvent(EnemyAbility ability, int damage, int heal)
        {
            Ability = ability;
            Damage = damage;
            Heal = heal;
        }

        public EnemyAbility Ability { get; }

        public int Damage { get; }

        public int Heal { get; }
    }

    public sealed class BattleEndedEvent : BattleEvent
    {
        public BattleEndedEvent(BattleOutcome outcome)
        {
            Outcome = outcome;
        }

        public BattleOutcome Outcome { get; }
    }
}
```

- [ ] **Step 5: Write `BattleSetup` and `BattleState`**

Create `Assets/Scripts/Core/Battle/BattleSetup.cs`:

```csharp
using System;
using CodexOfEchoes.Core.Words;

namespace CodexOfEchoes.Core.Battle
{
    /// <summary>
    /// Everything the engine needs to start a battle. The Unity layer builds this from
    /// ScriptableObjects; tests build it inline.
    /// </summary>
    public sealed class BattleSetup
    {
        public BattleSetup(
            IWordDictionary dictionary,
            StoryWordTable storyWords,
            string enemyName,
            int enemyMaxHp,
            int playerMaxHp,
            int seed)
        {
            Dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            StoryWords = storyWords ?? throw new ArgumentNullException(nameof(storyWords));
            EnemyName = enemyName;
            EnemyMaxHp = enemyMaxHp;
            PlayerMaxHp = playerMaxHp;
            Seed = seed;
        }

        public IWordDictionary Dictionary { get; }

        public StoryWordTable StoryWords { get; }

        public string EnemyName { get; }

        public int EnemyMaxHp { get; }

        public int PlayerMaxHp { get; }

        public int Seed { get; }
    }
}
```

Create `Assets/Scripts/Core/Battle/BattleState.cs`:

```csharp
using System.Collections.Generic;
using System.Text;
using CodexOfEchoes.Core.Combat;
using CodexOfEchoes.Core.Grid;

namespace CodexOfEchoes.Core.Battle
{
    /// <summary>
    /// The observable battle. Mutated only by BattleEngine; the view reads it but
    /// animates from events rather than from diffs of this.
    /// </summary>
    public sealed class BattleState
    {
        private readonly List<int> _selection = new List<int>();

        public BattleState(TileGrid grid, Combatant player, Combatant enemy, int seed)
        {
            Grid = grid;
            Player = player;
            Enemy = enemy;
            Seed = seed;
            TurnNumber = 1;
            Outcome = BattleOutcome.InProgress;
        }

        public TileGrid Grid { get; }

        public Combatant Player { get; }

        public Combatant Enemy { get; }

        public int Seed { get; }

        public int TurnNumber { get; internal set; }

        public BattleOutcome Outcome { get; internal set; }

        public IReadOnlyList<int> Selection => _selection;

        /// <summary>Selected tiles concatenated in selection order, uppercased.</summary>
        public string CurrentWord
        {
            get
            {
                var builder = new StringBuilder();
                foreach (var index in _selection)
                {
                    builder.Append(Grid[index].Letter);
                }

                return builder.ToString().ToUpperInvariant();
            }
        }

        internal void Select(int index) => _selection.Add(index);

        internal bool IsSelected(int index) => _selection.Contains(index);

        internal int RemoveLastSelected()
        {
            var last = _selection[_selection.Count - 1];
            _selection.RemoveAt(_selection.Count - 1);
            return last;
        }

        internal void ClearSelection() => _selection.Clear();

        internal List<int> SnapshotSelection() => new List<int>(_selection);

        internal List<LetterTile> SelectedTiles()
        {
            var tiles = new List<LetterTile>(_selection.Count);
            foreach (var index in _selection)
            {
                tiles.Add(Grid[index]);
            }

            return tiles;
        }
    }
}
```

- [ ] **Step 6: Write `BattleEngine` with selection commands only**

Create `Assets/Scripts/Core/Battle/BattleEngine.cs`. Tasks 9 and 10 add `CastWordCommand` and `ScrambleCommand`; until then both fall through to rejection, so selection can be tested in isolation.

```csharp
using System;
using System.Collections.Generic;
using CodexOfEchoes.Core.Combat;
using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Core.Rng;
using CodexOfEchoes.Core.Words;

namespace CodexOfEchoes.Core.Battle
{
    /// <summary>
    /// The entire core contract: one command in, an ordered list of events out.
    /// Synchronous and instantaneous — all timing lives in the presentation layer,
    /// which is what makes the rules testable without waiting on anything.
    /// </summary>
    public sealed class BattleEngine
    {
        private readonly WordValidator _validator;
        private readonly StoryWordTable _storyWords;
        private readonly AswangBehaviour _enemyBehaviour;

        public BattleEngine(BattleSetup setup)
        {
            if (setup == null)
            {
                throw new ArgumentNullException(nameof(setup));
            }

            var rng = new SeededRandom(setup.Seed);

            _validator = new WordValidator(setup.Dictionary);
            _storyWords = setup.StoryWords;
            _enemyBehaviour = new AswangBehaviour(rng);

            State = new BattleState(
                grid: new TileGrid(new TileBag(rng)),
                player: new Combatant("Liora", setup.PlayerMaxHp),
                enemy: new Combatant(setup.EnemyName, setup.EnemyMaxHp),
                seed: setup.Seed);
        }

        public BattleState State { get; }

        public IReadOnlyList<BattleEvent> Execute(BattleCommand command)
        {
            var events = new List<BattleEvent>();

            if (command == null)
            {
                events.Add(new CommandRejectedEvent("Command was null."));
                return events;
            }

            if (State.Outcome != BattleOutcome.InProgress)
            {
                events.Add(new CommandRejectedEvent("The battle is already over."));
                return events;
            }

            switch (command)
            {
                case SelectTileCommand select:
                    HandleSelect(select.Index, events);
                    break;

                case DeselectLastCommand _:
                    HandleDeselectLast(events);
                    break;

                case ClearSelectionCommand _:
                    State.ClearSelection();
                    events.Add(new SelectionClearedEvent());
                    break;

                default:
                    events.Add(new CommandRejectedEvent(
                        $"Unsupported command '{command.GetType().Name}'."));
                    break;
            }

            return events;
        }

        private void HandleSelect(int index, List<BattleEvent> events)
        {
            if (index < 0 || index >= TileGrid.Size)
            {
                events.Add(new CommandRejectedEvent($"Tile index {index} is out of range."));
                return;
            }

            if (State.IsSelected(index))
            {
                events.Add(new CommandRejectedEvent($"Tile {index} is already selected."));
                return;
            }

            State.Select(index);
            events.Add(new TileSelectedEvent(index));
        }

        private void HandleDeselectLast(List<BattleEvent> events)
        {
            if (State.Selection.Count == 0)
            {
                events.Add(new CommandRejectedEvent("Nothing is selected."));
                return;
            }

            events.Add(new TileDeselectedEvent(State.RemoveLastSelected()));
        }
    }
}
```

- [ ] **Step 7: Run the tests to verify they pass**

Run: `dotnet test Tests.Core/CoreTests.csproj --filter BattleSelectionTests -v minimal`
Expected: PASS, 14 tests.

- [ ] **Step 8: Run the full suite**

Run: `dotnet test Tests.Core/CoreTests.csproj -v minimal`
Expected: PASS, 101 tests.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "feat: add battle command and event vocabulary with selection handling"
```

---

### Task 9: Battle engine — casting a word

The full turn pipeline: validate, damage, consume, gravity, refill, enemy responds. Event *ordering* is as much the deliverable as the state changes, because the presenter animates straight from this list.

**Files:**
- Modify: `Assets/Scripts/Core/Battle/BattleEngine.cs`
- Modify: `Assets/Scripts/Core/Grid/TileGrid.cs` (adds a test-only seam, `SetTileForTesting`)
- Create: `Tests.Core/BattleCastTests.cs`

**Interfaces:**
- Consumes: everything from Task 8, plus `DamageCalculator.Compute` (Task 6) and `AswangBehaviour.Act` / `TelegraphsFeastNextTurn` (Task 7)
- Produces: `CastWordCommand` handling. Event order on a successful non-lethal cast is exactly: `WordCastEvent` → `DamageDealtEvent(Enemy)` → `TilesConsumedEvent` → `TilesFellEvent` → `TilesSpawnedEvent` → `SelectionClearedEvent` → `EnemyActedEvent` → `DamageDealtEvent(Liora)` → [`HealedEvent(Enemy)` if the enemy healed] → [`EnemyTelegraphedEvent` if next turn Feasts]

- [ ] **Step 1: Write the failing tests**

Create `Tests.Core/BattleCastTests.cs`. The helper selects tiles by letter so tests read as words rather than index soup.

```csharp
using System.Collections.Generic;
using System.Linq;
using CodexOfEchoes.Core.Battle;
using CodexOfEchoes.Core.Combat;
using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Core.Words;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class BattleCastTests
    {
        private static BattleEngine NewEngine(
            int seed = 1, IEnumerable<string> words = null, IEnumerable<string> bane = null)
        {
            var setup = new BattleSetup(
                dictionary: new HashSetWordDictionary(
                    words ?? new[] { "cat", "salt", "dawn", "eat" }),
                storyWords: new StoryWordTable(
                    echoWords: new[] { "dawn" },
                    baneWords: bane ?? new[] { "salt" }),
                enemyName: "Aswang",
                enemyMaxHp: 75,
                playerMaxHp: 100,
                seed: seed);

            return new BattleEngine(setup);
        }

        /// <summary>
        /// Overwrites the board so tests can cast a chosen word deterministically.
        /// Selection then takes indices 0..n-1.
        /// </summary>
        private static void StackDeck(BattleEngine engine, params string[] letters)
        {
            for (var i = 0; i < letters.Length; i++)
            {
                engine.State.Grid.SetTileForTesting(i, LetterTable.CreateTile(letters[i]));
            }
        }

        private static void SelectFirst(BattleEngine engine, int count)
        {
            for (var i = 0; i < count; i++)
            {
                engine.Execute(new SelectTileCommand(i));
            }
        }

        [Test]
        public void CastingAValidWordDamagesTheEnemy()
        {
            var engine = NewEngine();
            StackDeck(engine, "C", "A", "T");
            SelectFirst(engine, 3);

            var events = engine.Execute(new CastWordCommand());

            var cast = events.OfType<WordCastEvent>().Single();
            Assert.That(cast.Word, Is.EqualTo("CAT"));
            Assert.That(cast.Damage, Is.EqualTo(5));
            Assert.That(engine.State.Enemy.Hp, Is.EqualTo(70));
        }

        [Test]
        public void EventOrderIsCausal()
        {
            var engine = NewEngine();
            StackDeck(engine, "C", "A", "T");
            SelectFirst(engine, 3);

            var types = engine.Execute(new CastWordCommand())
                .Select(e => e.GetType().Name).ToList();

            var wordCast = types.IndexOf(nameof(WordCastEvent));
            var enemyHit = types.IndexOf(nameof(DamageDealtEvent));
            var consumed = types.IndexOf(nameof(TilesConsumedEvent));
            var fell = types.IndexOf(nameof(TilesFellEvent));
            var spawned = types.IndexOf(nameof(TilesSpawnedEvent));
            var enemyActed = types.IndexOf(nameof(EnemyActedEvent));

            Assert.That(wordCast, Is.LessThan(enemyHit));
            Assert.That(enemyHit, Is.LessThan(consumed));
            Assert.That(consumed, Is.LessThan(fell));
            Assert.That(fell, Is.LessThan(spawned));
            Assert.That(spawned, Is.LessThan(enemyActed));
        }

        [Test]
        public void BaneWordsApplyTheirMultiplier()
        {
            var engine = NewEngine();
            StackDeck(engine, "S", "A", "L", "T");
            SelectFirst(engine, 4);

            var cast = engine.Execute(new CastWordCommand()).OfType<WordCastEvent>().Single();

            Assert.That(cast.Tier, Is.EqualTo(StoryWordTier.Bane));
            Assert.That(cast.Damage, Is.EqualTo(12));
        }

        [Test]
        public void EchoWordsApplyTheirMultiplier()
        {
            var engine = NewEngine();
            StackDeck(engine, "D", "A", "W", "N");
            SelectFirst(engine, 4);

            var cast = engine.Execute(new CastWordCommand()).OfType<WordCastEvent>().Single();

            Assert.That(cast.Tier, Is.EqualTo(StoryWordTier.Echo));
            Assert.That(cast.Damage, Is.EqualTo(15));
        }

        [Test]
        public void CastingConsumesTheSelectedTilesAndRefillsTheBoard()
        {
            var engine = NewEngine();
            StackDeck(engine, "C", "A", "T");
            SelectFirst(engine, 3);

            var events = engine.Execute(new CastWordCommand());

            var consumed = events.OfType<TilesConsumedEvent>().Single();
            Assert.That(consumed.Indices, Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(events.OfType<TilesSpawnedEvent>().Single().Spawns.Count, Is.EqualTo(3));
            Assert.That(engine.State.Grid.Tiles.Count, Is.EqualTo(TileGrid.Size));
            Assert.That(engine.State.Selection, Is.Empty);
        }

        [Test]
        public void TheEnemyStrikesBackAndAdvancesTheTurn()
        {
            var engine = NewEngine();
            StackDeck(engine, "C", "A", "T");
            SelectFirst(engine, 3);

            var events = engine.Execute(new CastWordCommand());

            var acted = events.OfType<EnemyActedEvent>().Single();
            Assert.That(acted.Ability, Is.EqualTo(EnemyAbility.Strike));
            Assert.That(acted.Damage, Is.InRange(7, 11));
            Assert.That(engine.State.Player.Hp, Is.EqualTo(100 - acted.Damage));
            Assert.That(engine.State.TurnNumber, Is.EqualTo(2));
        }

        [Test]
        public void AShortWordIsRejectedAndCostsNoTurn()
        {
            var engine = NewEngine();
            StackDeck(engine, "A", "T");
            SelectFirst(engine, 2);

            var events = engine.Execute(new CastWordCommand());

            var rejected = events.OfType<WordRejectedEvent>().Single();
            Assert.That(rejected.Reason, Is.EqualTo(WordRejectionReason.TooShort));
            Assert.That(engine.State.TurnNumber, Is.EqualTo(1));
            Assert.That(engine.State.Player.Hp, Is.EqualTo(100));
            Assert.That(engine.State.Enemy.Hp, Is.EqualTo(75));
            Assert.That(events.OfType<EnemyActedEvent>(), Is.Empty);
        }

        [Test]
        public void AnUnknownWordIsRejectedAndCostsNoTurn()
        {
            var engine = NewEngine();
            StackDeck(engine, "Z", "Z", "Z");
            SelectFirst(engine, 3);

            var events = engine.Execute(new CastWordCommand());

            var rejected = events.OfType<WordRejectedEvent>().Single();
            Assert.That(rejected.Reason, Is.EqualTo(WordRejectionReason.NotInDictionary));
            Assert.That(rejected.Word, Is.EqualTo("ZZZ"));
            Assert.That(engine.State.TurnNumber, Is.EqualTo(1));
        }

        [Test]
        public void ARejectedWordKeepsTheSelectionSoThePlayerCanEditIt()
        {
            var engine = NewEngine();
            StackDeck(engine, "Z", "Z", "Z");
            SelectFirst(engine, 3);

            engine.Execute(new CastWordCommand());

            // Backspace has to remain useful after a miss.
            Assert.That(engine.State.Selection, Is.EqualTo(new[] { 0, 1, 2 }));
        }

        [Test]
        public void CastingWithNothingSelectedIsRejectedAsTooShort()
        {
            var engine = NewEngine();

            var events = engine.Execute(new CastWordCommand());

            Assert.That(events.OfType<WordRejectedEvent>().Single().Reason,
                Is.EqualTo(WordRejectionReason.TooShort));
        }

        [Test]
        public void FeastLandsOnTurnFourDealingTwentyAndHealing()
        {
            var engine = NewEngine();

            for (var turn = 1; turn <= 3; turn++)
            {
                StackDeck(engine, "C", "A", "T");
                SelectFirst(engine, 3);
                engine.Execute(new CastWordCommand());
            }

            Assert.That(engine.State.TurnNumber, Is.EqualTo(4));
            var enemyHpBeforeFeastTurn = engine.State.Enemy.Hp;

            StackDeck(engine, "C", "A", "T");
            SelectFirst(engine, 3);
            var events = engine.Execute(new CastWordCommand());

            var acted = events.OfType<EnemyActedEvent>().Single();
            Assert.That(acted.Ability, Is.EqualTo(EnemyAbility.Feast));
            Assert.That(acted.Damage, Is.EqualTo(20));
            Assert.That(acted.Heal, Is.EqualTo(10));

            var healed = events.OfType<HealedEvent>().Single();
            Assert.That(healed.Target, Is.EqualTo(CombatantId.Enemy));
            // 5 damage from CAT, then 10 healed back.
            Assert.That(engine.State.Enemy.Hp, Is.EqualTo(enemyHpBeforeFeastTurn - 5 + 10));
        }

        [Test]
        public void FeastIsTelegraphedOnTheTurnBefore()
        {
            var engine = NewEngine();

            for (var turn = 1; turn <= 2; turn++)
            {
                StackDeck(engine, "C", "A", "T");
                SelectFirst(engine, 3);
                engine.Execute(new CastWordCommand());
            }

            // Resolving turn 3 should warn that turn 4 Feasts.
            StackDeck(engine, "C", "A", "T");
            SelectFirst(engine, 3);
            var events = engine.Execute(new CastWordCommand());

            var telegraph = events.OfType<EnemyTelegraphedEvent>().Single();
            Assert.That(telegraph.Ability, Is.EqualTo(EnemyAbility.Feast));
        }

        [Test]
        public void NoTelegraphOnAnOrdinaryTurn()
        {
            var engine = NewEngine();
            StackDeck(engine, "C", "A", "T");
            SelectFirst(engine, 3);

            var events = engine.Execute(new CastWordCommand());

            Assert.That(events.OfType<EnemyTelegraphedEvent>(), Is.Empty);
        }

        [Test]
        public void LethalEndsTheBattleBeforeTheEnemyCanAct()
        {
            var engine = NewEngine(seed: 1, words: new[] { "salt" }, bane: new[] { "salt" });
            // Drop the enemy to 12 HP so one SALT (12 damage) is exactly lethal.
            engine.State.Enemy.TakeDamage(63);

            StackDeck(engine, "S", "A", "L", "T");
            SelectFirst(engine, 4);
            var events = engine.Execute(new CastWordCommand());

            Assert.That(engine.State.Enemy.Hp, Is.EqualTo(0));
            Assert.That(events.OfType<EnemyActedEvent>(), Is.Empty);
            Assert.That(engine.State.Player.Hp, Is.EqualTo(100));
            Assert.That(events.OfType<BattleEndedEvent>().Single().Outcome,
                Is.EqualTo(BattleOutcome.Victory));
            Assert.That(engine.State.Outcome, Is.EqualTo(BattleOutcome.Victory));
        }

        [Test]
        public void LethalStillResolvesOnAFeastTurn()
        {
            var engine = NewEngine(seed: 1, words: new[] { "salt", "cat" });

            for (var turn = 1; turn <= 3; turn++)
            {
                StackDeck(engine, "C", "A", "T");
                SelectFirst(engine, 3);
                engine.Execute(new CastWordCommand());
            }

            Assert.That(engine.State.TurnNumber, Is.EqualTo(4));
            engine.State.Enemy.TakeDamage(engine.State.Enemy.Hp - 12);
            var playerHpBefore = engine.State.Player.Hp;

            StackDeck(engine, "S", "A", "L", "T");
            SelectFirst(engine, 4);
            var events = engine.Execute(new CastWordCommand());

            // Winning the race means eating no Feast.
            Assert.That(engine.State.Player.Hp, Is.EqualTo(playerHpBefore));
            Assert.That(events.OfType<EnemyActedEvent>(), Is.Empty);
            Assert.That(engine.State.Outcome, Is.EqualTo(BattleOutcome.Victory));
        }

        [Test]
        public void CommandsAfterTheBattleEndsAreRejected()
        {
            var engine = NewEngine(seed: 1, words: new[] { "salt" }, bane: new[] { "salt" });
            engine.State.Enemy.TakeDamage(63);
            StackDeck(engine, "S", "A", "L", "T");
            SelectFirst(engine, 4);
            engine.Execute(new CastWordCommand());

            var events = engine.Execute(new SelectTileCommand(0));

            Assert.That(events.Single(), Is.TypeOf<CommandRejectedEvent>());
        }
    }
}
```

- [ ] **Step 2: Add the test-only grid hook**

`StackDeck` needs to place known letters. Add to `Assets/Scripts/Core/Grid/TileGrid.cs`, inside the class:

```csharp
        /// <summary>
        /// Test seam: places a known tile so a test can cast a chosen word. Not used by
        /// game code — battles are made reproducible by seeding, not by poking tiles.
        /// </summary>
        public void SetTileForTesting(int index, LetterTile tile)
        {
            _tiles[index] = tile;
        }
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test Tests.Core/CoreTests.csproj --filter BattleCastTests -v minimal`
Expected: FAIL — every cast test reports `CommandRejectedEvent` because `CastWordCommand` still falls through to the default branch.

- [ ] **Step 4: Add casting to `BattleEngine`**

In `Assets/Scripts/Core/Battle/BattleEngine.cs`, add a case to the switch in `Execute`, immediately before `default:`:

```csharp
                case CastWordCommand _:
                    HandleCastWord(events);
                    break;
```

Then add these three methods to the class:

```csharp
        private void HandleCastWord(List<BattleEvent> events)
        {
            var word = State.CurrentWord;
            var rejection = _validator.Validate(word);

            if (rejection != WordRejectionReason.None)
            {
                // Not an error — gameplay. No turn is consumed, and the selection stays
                // intact so Backspace remains useful after a miss.
                events.Add(new WordRejectedEvent(word, rejection));
                return;
            }

            var tier = _storyWords.TierOf(word);
            var damage = DamageCalculator.Compute(State.SelectedTiles(), word.Length, tier);

            events.Add(new WordCastEvent(word, damage, tier));
            events.Add(new DamageDealtEvent(
                CombatantId.Enemy, damage, State.Enemy.TakeDamage(damage)));

            var consumed = State.SnapshotSelection();
            var refill = State.Grid.Consume(consumed);

            events.Add(new TilesConsumedEvent(consumed));
            events.Add(new TilesFellEvent(refill.Moves));
            events.Add(new TilesSpawnedEvent(refill.Spawns));

            State.ClearSelection();
            events.Add(new SelectionClearedEvent());

            ResolveEnemyTurn(events);
        }

        /// <summary>
        /// Runs the enemy half of a turn. Lethal resolves immediately: if the player's
        /// word finished the enemy, it does not act, which is what makes a damage race
        /// winnable on the turn Feast would otherwise land.
        /// </summary>
        private void ResolveEnemyTurn(List<BattleEvent> events)
        {
            if (State.Enemy.IsDefeated)
            {
                EndBattle(BattleOutcome.Victory, events);
                return;
            }

            var action = _enemyBehaviour.Act(State.TurnNumber);
            events.Add(new EnemyActedEvent(action.Ability, action.Damage, action.Heal));
            events.Add(new DamageDealtEvent(
                CombatantId.Liora, action.Damage, State.Player.TakeDamage(action.Damage)));

            if (action.Heal > 0)
            {
                events.Add(new HealedEvent(
                    CombatantId.Enemy, action.Heal, State.Enemy.Heal(action.Heal)));
            }

            if (State.Player.IsDefeated)
            {
                EndBattle(BattleOutcome.Defeat, events);
                return;
            }

            if (_enemyBehaviour.TelegraphsFeastNextTurn(State.TurnNumber))
            {
                events.Add(new EnemyTelegraphedEvent(EnemyAbility.Feast));
            }

            State.TurnNumber++;
        }

        private void EndBattle(BattleOutcome outcome, List<BattleEvent> events)
        {
            State.Outcome = outcome;
            State.ClearSelection();
            events.Add(new BattleEndedEvent(outcome));
        }
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test Tests.Core/CoreTests.csproj --filter BattleCastTests -v minimal`
Expected: PASS, 16 tests.

- [ ] **Step 6: Run the full suite**

Run: `dotnet test Tests.Core/CoreTests.csproj -v minimal`
Expected: PASS, 117 tests.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat: add word casting turn pipeline with immediate lethal"
```

---

### Task 10: Battle engine — scramble and defeat

**Files:**
- Modify: `Assets/Scripts/Core/Battle/BattleEngine.cs`
- Create: `Tests.Core/BattleScrambleTests.cs`

**Interfaces:**
- Consumes: `ResolveEnemyTurn` and `EndBattle` from Task 9; `TileGrid.Scramble()` from Task 4
- Produces: `ScrambleCommand` handling. Event order: `GridScrambledEvent` → `SelectionClearedEvent` → the same enemy-turn sequence as a cast

- [ ] **Step 1: Write the failing tests**

Create `Tests.Core/BattleScrambleTests.cs`:

```csharp
using System.Linq;
using CodexOfEchoes.Core.Battle;
using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Core.Words;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class BattleScrambleTests
    {
        private static BattleEngine NewEngine(int seed = 1, int playerMaxHp = 100)
        {
            var setup = new BattleSetup(
                dictionary: new HashSetWordDictionary(new[] { "cat" }),
                storyWords: new StoryWordTable(new string[0], new string[0]),
                enemyName: "Aswang",
                enemyMaxHp: 75,
                playerMaxHp: playerMaxHp,
                seed: seed);

            return new BattleEngine(setup);
        }

        [Test]
        public void ScrambleReplacesTheBoard()
        {
            var engine = NewEngine();
            var before = engine.State.Grid.Tiles.Select(t => t.Letter).ToList();

            var events = engine.Execute(new ScrambleCommand());

            var scrambled = events.OfType<GridScrambledEvent>().Single();
            Assert.That(scrambled.Tiles.Count, Is.EqualTo(TileGrid.Size));
            Assert.That(engine.State.Grid.Tiles.Select(t => t.Letter), Is.Not.EqualTo(before));
        }

        [Test]
        public void ScrambleKeepsTheVowelInvariant()
        {
            for (var seed = 1; seed <= 50; seed++)
            {
                var engine = NewEngine(seed);
                engine.Execute(new ScrambleCommand());

                Assert.That(engine.State.Grid.VowelCount,
                    Is.GreaterThanOrEqualTo(TileGrid.MinVowels), $"seed {seed}");
            }
        }

        [Test]
        public void ScrambleCostsTheTurnAndTheEnemyStillAttacks()
        {
            var engine = NewEngine();

            var events = engine.Execute(new ScrambleCommand());

            var acted = events.OfType<EnemyActedEvent>().Single();
            Assert.That(acted.Damage, Is.InRange(7, 11));
            Assert.That(engine.State.Player.Hp, Is.EqualTo(100 - acted.Damage));
            Assert.That(engine.State.TurnNumber, Is.EqualTo(2));
        }

        [Test]
        public void ScrambleClearsAnyExistingSelection()
        {
            var engine = NewEngine();
            engine.Execute(new SelectTileCommand(0));
            engine.Execute(new SelectTileCommand(1));

            var events = engine.Execute(new ScrambleCommand());

            Assert.That(events.OfType<SelectionClearedEvent>().Any(), Is.True);
            Assert.That(engine.State.Selection, Is.Empty);
        }

        [Test]
        public void ScrambleDealsNoDamageToTheEnemy()
        {
            var engine = NewEngine();

            engine.Execute(new ScrambleCommand());

            Assert.That(engine.State.Enemy.Hp, Is.EqualTo(75));
        }

        [Test]
        public void ScramblingIntoDefeatEndsTheBattle()
        {
            // 11 HP cannot survive a Strike of 7-11 in the worst case; use 1 to be certain.
            var engine = NewEngine(playerMaxHp: 1);

            var events = engine.Execute(new ScrambleCommand());

            Assert.That(engine.State.Player.Hp, Is.EqualTo(0));
            Assert.That(engine.State.Player.IsDefeated, Is.True);
            Assert.That(events.OfType<BattleEndedEvent>().Single().Outcome,
                Is.EqualTo(BattleOutcome.Defeat));
            Assert.That(engine.State.Outcome, Is.EqualTo(BattleOutcome.Defeat));
        }

        [Test]
        public void NoTelegraphIsEmittedAfterTheBattleEnds()
        {
            var engine = NewEngine(playerMaxHp: 1);

            var events = engine.Execute(new ScrambleCommand());

            Assert.That(events.OfType<EnemyTelegraphedEvent>(), Is.Empty);
        }

        [Test]
        public void CommandsAfterDefeatAreRejected()
        {
            var engine = NewEngine(playerMaxHp: 1);
            engine.Execute(new ScrambleCommand());

            var events = engine.Execute(new ScrambleCommand());

            Assert.That(events.Single(), Is.TypeOf<CommandRejectedEvent>());
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Tests.Core/CoreTests.csproj --filter BattleScrambleTests -v minimal`
Expected: FAIL — `ScrambleCommand` still falls through to `CommandRejectedEvent`.

- [ ] **Step 3: Add scrambling to `BattleEngine`**

In `Execute`, add a case immediately before `default:`:

```csharp
                case ScrambleCommand _:
                    HandleScramble(events);
                    break;
```

Then add the handler to the class:

```csharp
        /// <summary>
        /// Rerolls the board at the cost of the turn — the enemy still swings. This plus
        /// the vowel invariant is the whole answer to dead boards; real detection is
        /// combinatorial and deliberately out of scope for the slice.
        /// </summary>
        private void HandleScramble(List<BattleEvent> events)
        {
            State.Grid.Scramble();
            events.Add(new GridScrambledEvent(State.Grid.Tiles));

            State.ClearSelection();
            events.Add(new SelectionClearedEvent());

            ResolveEnemyTurn(events);
        }
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test Tests.Core/CoreTests.csproj --filter BattleScrambleTests -v minimal`
Expected: PASS, 8 tests.

- [ ] **Step 5: Run the full suite**

Run: `dotnet test Tests.Core/CoreTests.csproj -v minimal`
Expected: PASS, 125 tests.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: add scramble command and defeat resolution"
```

---

### Task 11: Tuning validation

The spec's §4 difficulty model — average play dies on turn 9, strong play wins on turn 6 — asserted in code. This is the task that catches a balance change silently invalidating the design, and it is the closest thing to a regression test the tuning will ever have.

**Files:**
- Create: `Tests.Core/TuningModelTests.cs`

**Interfaces:**
- Consumes: the complete `BattleEngine` from Tasks 8–10
- Produces: nothing; this task adds only tests

- [ ] **Step 1: Write the tuning tests**

Create `Tests.Core/TuningModelTests.cs`. These simulate damage per turn directly rather than forming real words, because the model under test is the *turn economy*, not the dictionary.

```csharp
using System.Linq;
using CodexOfEchoes.Core.Battle;
using CodexOfEchoes.Core.Combat;
using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Core.Words;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    /// <summary>
    /// Guards the difficulty model recorded in spec section 4. If the tuning is
    /// deliberately changed, these expectations change with it — but they should never
    /// drift silently.
    /// </summary>
    public class TuningModelTests
    {
        private const string FillerWord = "NAP";

        private static BattleEngine NewEngine(int seed) =>
            new BattleEngine(new BattleSetup(
                dictionary: new HashSetWordDictionary(new[] { FillerWord }),
                storyWords: new StoryWordTable(new string[0], new string[0]),
                enemyName: "Aswang",
                enemyMaxHp: 75,
                playerMaxHp: 100,
                seed: seed));

        /// <summary>Casts NAP, then tops the enemy damage up to the modelled figure.</summary>
        private static void PlayTurn(BattleEngine engine, int targetDamage)
        {
            engine.State.Grid.SetTileForTesting(0, LetterTable.CreateTile("N"));
            engine.State.Grid.SetTileForTesting(1, LetterTable.CreateTile("A"));
            engine.State.Grid.SetTileForTesting(2, LetterTable.CreateTile("P"));

            engine.Execute(new SelectTileCommand(0));
            engine.Execute(new SelectTileCommand(1));
            engine.Execute(new SelectTileCommand(2));

            var hpBefore = engine.State.Enemy.Hp;
            engine.Execute(new CastWordCommand());

            if (engine.State.Outcome != BattleOutcome.InProgress)
            {
                return;
            }

            var dealt = hpBefore - engine.State.Enemy.Hp;
            var shortfall = targetDamage - dealt;
            if (shortfall > 0)
            {
                engine.State.Enemy.TakeDamage(shortfall);
            }
        }

        [Test]
        public void AveragePlayLosesAroundTurnNine()
        {
            // ~9 damage/turn: five-letter words, no Story Words.
            var engine = NewEngine(20260914);

            for (var turn = 1; turn <= 12; turn++)
            {
                if (engine.State.Outcome != BattleOutcome.InProgress)
                {
                    break;
                }

                PlayTurn(engine, 9);
            }

            Assert.That(engine.State.Outcome, Is.EqualTo(BattleOutcome.Defeat));
            Assert.That(engine.State.TurnNumber, Is.InRange(8, 10),
                "Spec models defeat on turn 9; a drift of more than one turn means the "
                + "tuning changed.");
            Assert.That(engine.State.Enemy.Hp, Is.GreaterThan(0));
        }

        [Test]
        public void StrongPlayWinsAroundTurnSixWithHealthToSpare()
        {
            // ~15 damage/turn: Story Words in regular use.
            var engine = NewEngine(20260914);

            for (var turn = 1; turn <= 12; turn++)
            {
                if (engine.State.Outcome != BattleOutcome.InProgress)
                {
                    break;
                }

                PlayTurn(engine, 15);
            }

            Assert.That(engine.State.Outcome, Is.EqualTo(BattleOutcome.Victory));
            Assert.That(engine.State.TurnNumber, Is.InRange(5, 7));
            Assert.That(engine.State.Player.Hp, Is.GreaterThan(30));
        }

        [Test]
        public void TheEnemyEffectivePoolIncludesTwoFeastHeals()
        {
            // 75 HP plus a 10 heal on turns 4 and 8 is the ~95 figure in the spec.
            var engine = NewEngine(7);
            var totalDealt = 0;

            for (var turn = 1; turn <= 8; turn++)
            {
                if (engine.State.Outcome != BattleOutcome.InProgress)
                {
                    break;
                }

                var before = engine.State.Enemy.Hp;
                PlayTurn(engine, 9);
                totalDealt += before - engine.State.Enemy.Hp;
            }

            // Eight turns at 9 damage is 72 dealt, yet the enemy survives, because
            // 20 HP came back.
            Assert.That(engine.State.Enemy.IsDefeated, Is.False);
            Assert.That(totalDealt, Is.LessThan(95));
        }

        [Test]
        public void TheModelIsReproducibleFromItsSeed()
        {
            var first = NewEngine(4242);
            var second = NewEngine(4242);

            for (var turn = 1; turn <= 6; turn++)
            {
                PlayTurn(first, 9);
                PlayTurn(second, 9);
            }

            Assert.That(first.State.Player.Hp, Is.EqualTo(second.State.Player.Hp));
            Assert.That(first.State.Enemy.Hp, Is.EqualTo(second.State.Enemy.Hp));
            Assert.That(first.State.TurnNumber, Is.EqualTo(second.State.TurnNumber));
        }

        [Test]
        public void StoryWordsAreWhatSeparateWinningFromLosing()
        {
            // The same player, differing only by Story Word usage, flips the result.
            var withoutStoryWords = NewEngine(20260914);
            var withStoryWords = NewEngine(20260914);

            for (var turn = 1; turn <= 12; turn++)
            {
                if (withoutStoryWords.State.Outcome == BattleOutcome.InProgress)
                {
                    PlayTurn(withoutStoryWords, 9);
                }

                if (withStoryWords.State.Outcome == BattleOutcome.InProgress)
                {
                    PlayTurn(withStoryWords, 15);
                }
            }

            Assert.That(withoutStoryWords.State.Outcome, Is.EqualTo(BattleOutcome.Defeat));
            Assert.That(withStoryWords.State.Outcome, Is.EqualTo(BattleOutcome.Victory));
        }
    }
}
```

- [ ] **Step 2: Run the tuning tests**

Run: `dotnet test Tests.Core/CoreTests.csproj --filter TuningModelTests -v minimal`
Expected: PASS, 5 tests.

If `AveragePlayLosesAroundTurnNine` reports victory instead, the damage formula or enemy numbers have drifted from the spec — check `DamageCalculator.LengthStep`, `AswangBehaviour.FeastHeal`, and the enemy's max HP before changing the test.

- [ ] **Step 3: Run the full suite**

Run: `dotnet test Tests.Core/CoreTests.csproj -v minimal`
Expected: PASS, 130 tests. **The entire combat rule set is now verified without opening Unity.**

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "test: assert the spec difficulty model in code"
```

---

## Unity layer

Tasks 12–16 build the skin. Verification changes character here: `dotnet test` no longer covers this code, so each task ends with a Unity batch-mode compile check instead. That command takes 60–90 seconds — the cost the Core/Game split exists to keep off the critical path.

**The compile check used throughout:**

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.0f1/Editor/Unity.exe" \
  -batchmode -quit -nographics \
  -projectPath "C:/Users/ADMIN/Desktop/Projects/Game Development/codex-of-echoes" \
  -logFile - | grep -Ei "error CS|Compilation failed"
```

Expected: no output. Any `error CS####` line is a compile failure with the file and line attached.

---

### Task 12: Unity data layer

ScriptableObjects for authoring plus the dictionary loader — the bridge that turns Inspector-editable assets into the plain Core types the engine wants.

**Files:**
- Create: `Assets/Scripts/Game/CodexOfEchoes.Game.asmdef`
- Create: `Assets/Scripts/Game/Data/EnemyDefinitionSO.cs`
- Create: `Assets/Scripts/Game/Data/StoryWordSetSO.cs`
- Create: `Assets/Scripts/Game/Data/BalanceConfigSO.cs`
- Create: `Assets/Scripts/Game/Data/DictionaryLoader.cs`
- Create: `Assets/Scripts/Game/Data/BattleSetupFactory.cs`
- Modify: `Packages/manifest.json`

**Interfaces:**
- Consumes: `BattleSetup`, `IWordDictionary`, `HashSetWordDictionary`, `StoryWordTable` from Core
- Produces: `EnemyDefinitionSO` (`EnemyName`, `MaxHp`, `BaneWords`), `StoryWordSetSO` (`EchoWords`), `BalanceConfigSO` (`PlayerMaxHp`), `DictionaryLoader.LoadFromStreamingAssets(string fileName) → IWordDictionary`, `BattleSetupFactory.Create(...) → BattleSetup`

- [ ] **Step 1: Add the Input System package**

Edit `Packages/manifest.json` and add to the `dependencies` object:

```json
    "com.unity.inputsystem": "1.11.2",
```

TextMeshPro needs no entry — it ships inside `com.unity.ugui` on Unity 6.

- [ ] **Step 2: Create the Game assembly definition**

Create `Assets/Scripts/Game/CodexOfEchoes.Game.asmdef`:

```json
{
    "name": "CodexOfEchoes.Game",
    "rootNamespace": "CodexOfEchoes.Game",
    "references": [
        "CodexOfEchoes.Core",
        "Unity.InputSystem",
        "Unity.TextMeshPro",
        "UnityEngine.UI"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3: Write the ScriptableObjects**

Create `Assets/Scripts/Game/Data/EnemyDefinitionSO.cs`:

```csharp
using UnityEngine;

namespace CodexOfEchoes.Game.Data
{
    /// <summary>
    /// Authorable enemy stats. Converted to plain Core values at load — the rules never
    /// see a Unity type.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Enemy_", menuName = "Codex of Echoes/Enemy Definition", order = 0)]
    public sealed class EnemyDefinitionSO : ScriptableObject
    {
        [SerializeField] private string enemyName = "Aswang";
        [SerializeField] private int maxHp = 75;

        [Tooltip("Folklore weaknesses. Bane words deal x2.5 damage against this enemy.")]
        [SerializeField]
        private string[] baneWords =
        {
            "SALT", "GARLIC", "ASH", "OIL", "STING", "SPINE",
        };

        public string EnemyName => enemyName;

        public int MaxHp => maxHp;

        public string[] BaneWords => baneWords;
    }
}
```

Create `Assets/Scripts/Game/Data/StoryWordSetSO.cs`:

```csharp
using UnityEngine;

namespace CodexOfEchoes.Game.Data
{
    /// <summary>Chapter-wide Echo words. x1.5 damage anywhere in the chapter.</summary>
    [CreateAssetMenu(
        fileName = "StoryWords_", menuName = "Codex of Echoes/Story Word Set", order = 1)]
    public sealed class StoryWordSetSO : ScriptableObject
    {
        [SerializeField]
        private string[] echoWords =
        {
            "NAME", "TRUTH", "LIGHT", "DAWN", "STORY", "INK", "PAGE",
        };

        public string[] EchoWords => echoWords;
    }
}
```

Create `Assets/Scripts/Game/Data/BalanceConfigSO.cs`:

```csharp
using UnityEngine;

namespace CodexOfEchoes.Game.Data
{
    /// <summary>Player-side tuning. Combat constants live in Core; this is what a
    /// designer adjusts without a recompile.</summary>
    [CreateAssetMenu(
        fileName = "BalanceConfig", menuName = "Codex of Echoes/Balance Config", order = 2)]
    public sealed class BalanceConfigSO : ScriptableObject
    {
        [SerializeField] private int playerMaxHp = 100;

        [Tooltip("Leave at 0 to seed each battle from the clock.")]
        [SerializeField] private int fixedSeed;

        public int PlayerMaxHp => playerMaxHp;

        public int FixedSeed => fixedSeed;
    }
}
```

- [ ] **Step 4: Write the dictionary loader**

Create `Assets/Scripts/Game/Data/DictionaryLoader.cs`. Core cannot read files, so this is the seam where the word list enters:

```csharp
using System;
using System.IO;
using CodexOfEchoes.Core.Words;
using UnityEngine;

namespace CodexOfEchoes.Game.Data
{
    /// <summary>
    /// Loads the ENABLE word list into Core. A missing or empty file is a developer
    /// error and fails loudly at load — a clear startup failure beats a battle that
    /// limps with an empty dictionary.
    /// </summary>
    public static class DictionaryLoader
    {
        public const string DefaultFileName = "enable.txt";

        public static IWordDictionary LoadFromStreamingAssets(
            string fileName = DefaultFileName)
        {
            var path = Path.Combine(Application.streamingAssetsPath, fileName);

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"Word list not found at '{path}'. Place the ENABLE word list there. "
                    + "Do not substitute TWL or SOWPODS: both are licensed and unsafe to "
                    + "ship commercially.",
                    path);
            }

            var lines = File.ReadAllLines(path);
            var dictionary = new HashSetWordDictionary(lines);

            if (dictionary.Count == 0)
            {
                throw new InvalidDataException($"Word list at '{path}' contained no words.");
            }

            Debug.Log($"[Codex] Loaded {dictionary.Count:N0} words from {fileName}.");
            return dictionary;
        }
    }
}
```

- [ ] **Step 5: Write the setup factory**

Create `Assets/Scripts/Game/Data/BattleSetupFactory.cs`:

```csharp
using System;
using CodexOfEchoes.Core.Battle;
using CodexOfEchoes.Core.Words;

namespace CodexOfEchoes.Game.Data
{
    /// <summary>
    /// The single place Unity assets become plain Core values. Keeping this conversion
    /// in one method is what lets Core stay serialization-friendly for cross-save.
    /// </summary>
    public static class BattleSetupFactory
    {
        public static BattleSetup Create(
            IWordDictionary dictionary,
            EnemyDefinitionSO enemy,
            StoryWordSetSO storyWords,
            BalanceConfigSO balance,
            int seed)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
            if (enemy == null) throw new ArgumentNullException(nameof(enemy));
            if (storyWords == null) throw new ArgumentNullException(nameof(storyWords));
            if (balance == null) throw new ArgumentNullException(nameof(balance));

            return new BattleSetup(
                dictionary: dictionary,
                storyWords: new StoryWordTable(storyWords.EchoWords, enemy.BaneWords),
                enemyName: enemy.EnemyName,
                enemyMaxHp: enemy.MaxHp,
                playerMaxHp: balance.PlayerMaxHp,
                seed: seed);
        }
    }
}
```

- [ ] **Step 6: Place the word list**

Create `Assets/StreamingAssets/` and put `enable.txt` in it (see the prerequisite section above).

Run: `wc -l "Assets/StreamingAssets/enable.txt"`
Expected: roughly 172,000 lines. If the file is absent, the slice still compiles — it fails at play time with the message from Step 4.

- [ ] **Step 7: Verify the Unity layer compiles**

Run the batch-mode compile check from the section header.
Expected: no output.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "feat: add Unity data assets and dictionary loading"
```

---

### Task 13: Battle runner and presenter

`BattleRunner` owns the engine and pumps commands. `BattlePresenter` drains each event list as a coroutine queue, locking input while it plays. This is the split that keeps all timing out of Core.

**Files:**
- Create: `Assets/Scripts/Game/Battle/BattlePresenter.cs`
- Create: `Assets/Scripts/Game/Battle/BattleRunner.cs`

**Interfaces:**
- Consumes: `BattleEngine`, `BattleCommand`, `BattleEvent` and every concrete event from Task 8; `BattleSetupFactory`, `DictionaryLoader` from Task 12
- Produces: `BattleRunner` with `Engine`, `IsBusy`, `Submit(BattleCommand)`, and `event Action<BattleEvent> EventPlayed`; `BattlePresenter` with `Enqueue(IReadOnlyList<BattleEvent>)` and `IsDraining`

- [ ] **Step 1: Write the presenter**

Create `Assets/Scripts/Game/Battle/BattlePresenter.cs`:

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using CodexOfEchoes.Core.Battle;
using UnityEngine;

namespace CodexOfEchoes.Game.Battle
{
    /// <summary>
    /// Plays events in order with delays between them. Every bit of timing in the game
    /// lives here — the engine is instantaneous, which is what makes the rules testable
    /// without waiting on anything.
    /// </summary>
    public sealed class BattlePresenter : MonoBehaviour
    {
        [Header("Beat lengths (seconds)")]
        [SerializeField] private float instantBeat = 0.02f;
        [SerializeField] private float castBeat = 0.55f;
        [SerializeField] private float damageBeat = 0.35f;
        [SerializeField] private float tileBeat = 0.25f;
        [SerializeField] private float endBeat = 0.8f;

        private readonly Queue<BattleEvent> _pending = new Queue<BattleEvent>();
        private Coroutine _drain;

        /// <summary>Raised per event as it plays, so views can react in sequence.</summary>
        public event Action<BattleEvent> EventPlayed;

        public bool IsDraining => _drain != null;

        public void Enqueue(IReadOnlyList<BattleEvent> events)
        {
            if (events == null)
            {
                return;
            }

            foreach (var battleEvent in events)
            {
                _pending.Enqueue(battleEvent);
            }

            if (_drain == null && isActiveAndEnabled)
            {
                _drain = StartCoroutine(Drain());
            }
        }

        private IEnumerator Drain()
        {
            while (_pending.Count > 0)
            {
                var battleEvent = _pending.Dequeue();

                EventPlayed?.Invoke(battleEvent);

                yield return new WaitForSeconds(BeatFor(battleEvent));
            }

            _drain = null;
        }

        private float BeatFor(BattleEvent battleEvent)
        {
            switch (battleEvent)
            {
                case WordCastEvent _:
                    return castBeat;
                case DamageDealtEvent _:
                case HealedEvent _:
                case EnemyActedEvent _:
                    return damageBeat;
                case TilesConsumedEvent _:
                case TilesFellEvent _:
                case TilesSpawnedEvent _:
                case GridScrambledEvent _:
                    return tileBeat;
                case BattleEndedEvent _:
                    return endBeat;
                default:
                    return instantBeat;
            }
        }
    }
}
```

- [ ] **Step 2: Write the runner**

Create `Assets/Scripts/Game/Battle/BattleRunner.cs`:

```csharp
using System;
using CodexOfEchoes.Core.Battle;
using CodexOfEchoes.Game.Data;
using UnityEngine;

namespace CodexOfEchoes.Game.Battle
{
    /// <summary>
    /// Owns the engine and forwards commands into it. Input is refused while the
    /// presenter is still animating, so the player can never act on a board that is
    /// mid-fall.
    /// </summary>
    [RequireComponent(typeof(BattlePresenter))]
    public sealed class BattleRunner : MonoBehaviour
    {
        [SerializeField] private EnemyDefinitionSO enemy;
        [SerializeField] private StoryWordSetSO storyWords;
        [SerializeField] private BalanceConfigSO balance;

        private BattlePresenter _presenter;

        public BattleEngine Engine { get; private set; }

        /// <summary>True while animations are still playing.</summary>
        public bool IsBusy => _presenter != null && _presenter.IsDraining;

        public event Action<BattleEvent> EventPlayed;

        private void Awake()
        {
            _presenter = GetComponent<BattlePresenter>();
            _presenter.EventPlayed += OnEventPlayed;

            RequireAsset(enemy, nameof(enemy));
            RequireAsset(storyWords, nameof(storyWords));
            RequireAsset(balance, nameof(balance));

            var dictionary = DictionaryLoader.LoadFromStreamingAssets();
            var seed = balance.FixedSeed != 0
                ? balance.FixedSeed
                : Environment.TickCount;

            Engine = new BattleEngine(BattleSetupFactory.Create(
                dictionary, enemy, storyWords, balance, seed));

            Debug.Log($"[Codex] Battle seed {seed}. Replay this fight with it.");
        }

        private void OnDestroy()
        {
            if (_presenter != null)
            {
                _presenter.EventPlayed -= OnEventPlayed;
            }
        }

        public void Submit(BattleCommand command)
        {
            if (Engine == null || IsBusy)
            {
                return;
            }

            _presenter.Enqueue(Engine.Execute(command));
        }

        private void OnEventPlayed(BattleEvent battleEvent) => EventPlayed?.Invoke(battleEvent);

        /// <summary>
        /// An unassigned asset is a developer error. Fail at load with the field name
        /// rather than throwing a NullReferenceException on the first keystroke.
        /// </summary>
        private void RequireAsset(UnityEngine.Object asset, string fieldName)
        {
            if (asset == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(BattleRunner)} on '{name}' has no '{fieldName}' assigned. "
                    + "Assign it in the Inspector.");
            }
        }
    }
}
```

- [ ] **Step 3: Verify it compiles**

Run the batch-mode compile check.
Expected: no output.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: add battle runner and event-draining presenter"
```

---

### Task 14: Input router

Keyboard and pointer funnel into identical commands. Typing is the primary PC path — it is most of why Bookworm feels good — and the pointer path is what becomes touch later.

**Files:**
- Create: `Assets/Scripts/Game/Input/TileInputRouter.cs`

**Interfaces:**
- Consumes: `BattleRunner.Submit` (Task 13); `SelectTileCommand`, `DeselectLastCommand`, `ClearSelectionCommand`, `CastWordCommand`, `ScrambleCommand` (Task 8)
- Produces: `TileInputRouter` with `public void OnTileClicked(int index)` for `TileView` to call in Task 15

- [ ] **Step 1: Write the router**

Create `Assets/Scripts/Game/Input/TileInputRouter.cs`:

```csharp
using System;
using CodexOfEchoes.Core.Battle;
using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Game.Battle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CodexOfEchoes.Game.Input
{
    /// <summary>
    /// One funnel for both input paths. Keyboard and pointer produce identical commands,
    /// so the engine never learns which device the player used — and a touch path can be
    /// added later without the rules noticing.
    ///
    /// Bindings: letter keys claim a tile, Backspace undoes, Enter casts, Tab scrambles,
    /// Escape clears.
    /// </summary>
    [RequireComponent(typeof(BattleRunner))]
    public sealed class TileInputRouter : MonoBehaviour
    {
        private BattleRunner _runner;

        private void Awake()
        {
            _runner = GetComponent<BattleRunner>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _runner.IsBusy)
            {
                return;
            }

            if (keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                _runner.Submit(new CastWordCommand());
                return;
            }

            if (keyboard.backspaceKey.wasPressedThisFrame)
            {
                _runner.Submit(new DeselectLastCommand());
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                _runner.Submit(new ClearSelectionCommand());
                return;
            }

            if (keyboard.tabKey.wasPressedThisFrame)
            {
                _runner.Submit(new ScrambleCommand());
                return;
            }

            HandleLetterKeys(keyboard);
        }

        /// <summary>Pointer path. TileView forwards clicks here.</summary>
        public void OnTileClicked(int index)
        {
            if (!_runner.IsBusy)
            {
                _runner.Submit(new SelectTileCommand(index));
            }
        }

        private void HandleLetterKeys(Keyboard keyboard)
        {
            for (var key = Key.A; key <= Key.Z; key++)
            {
                if (!keyboard[key].wasPressedThisFrame)
                {
                    continue;
                }

                var typed = key.ToString().ToUpperInvariant();
                var index = FindFirstUnselected(typed);

                if (index >= 0)
                {
                    _runner.Submit(new SelectTileCommand(index));
                }

                return;
            }
        }

        /// <summary>
        /// First matching unselected tile in reading order. Which duplicate a keypress
        /// claims is cosmetic — identical letters carry identical value — so the simplest
        /// predictable rule is the right one. Typing Q claims a "Qu" tile.
        /// </summary>
        private int FindFirstUnselected(string typedLetter)
        {
            var state = _runner.Engine.State;
            var wanted = typedLetter == "Q" ? "Qu" : typedLetter;

            for (var index = 0; index < TileGrid.Size; index++)
            {
                if (state.Selection.Contains(index))
                {
                    continue;
                }

                if (string.Equals(
                        state.Grid[index].Letter, wanted, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

Run the batch-mode compile check.
Expected: no output. A `Keyboard` not-found error means the Input System package from Task 12 Step 1 did not resolve — check `Packages/manifest.json`.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "feat: add keyboard and pointer input routing"
```

---

### Task 15: Views and the Spine seam

Placeholder visuals, plus the abstraction that lets real Spine assets arrive later without touching anything upstream.

**Files:**
- Create: `Assets/Scripts/Game/View/CombatantView.cs`
- Create: `Assets/Scripts/Game/View/PlaceholderCombatantView.cs`
- Create: `Assets/Scripts/Game/View/TileView.cs`
- Create: `Assets/Scripts/Game/View/HealthBarView.cs`
- Create: `Assets/Scripts/Game/View/WordCastView.cs`
- Create: `Assets/Scripts/Game/View/BattleView.cs`

**Interfaces:**
- Consumes: every event type from Task 8; `BattleRunner.EventPlayed` (Task 13); `TileInputRouter.OnTileClicked` (Task 14)
- Produces: abstract `CombatantView` with `PlayIdle()`, `PlayCast()`, `PlayHurt()`, `PlayVictory()`, `PlayDefeat()`; `TileView.Bind(int index, LetterTile tile, TileInputRouter router)` and `SetSelected(bool)`; `HealthBarView.Set(int hp, int maxHp)` and `SetTelegraph(string message)`; `WordCastView.Play(string word, StoryWordTier tier)`; `BattleView` as the event subscriber

- [ ] **Step 1: Write the Spine seam**

Create `Assets/Scripts/Game/View/CombatantView.cs`. These five calls are exactly Liora's five planned animations:

```csharp
using UnityEngine;

namespace CodexOfEchoes.Game.View
{
    /// <summary>
    /// The Spine seam. Exactly five calls, matching Liora's five planned animations.
    /// The slice ships PlaceholderCombatantView; SpineCombatantView will implement the
    /// same five against a SkeletonAnimation, and nothing upstream changes.
    /// </summary>
    public abstract class CombatantView : MonoBehaviour
    {
        public abstract void PlayIdle();

        public abstract void PlayCast();

        public abstract void PlayHurt();

        public abstract void PlayVictory();

        public abstract void PlayDefeat();
    }
}
```

Create `Assets/Scripts/Game/View/PlaceholderCombatantView.cs`:

```csharp
using System.Collections;
using UnityEngine;

namespace CodexOfEchoes.Game.View
{
    /// <summary>
    /// A colored rectangle that nudges and flashes. Exists only to make the five seam
    /// calls visible during the slice; every line of it is expected to be deleted when
    /// Spine assets arrive.
    /// </summary>
    public sealed class PlaceholderCombatantView : CombatantView
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Color idleColor = new Color(0.35f, 0.42f, 0.65f);
        [SerializeField] private Color hurtColor = new Color(0.85f, 0.25f, 0.25f);
        [SerializeField] private float nudgeDistance = 0.35f;
        [SerializeField] private float beat = 0.18f;

        private Vector3 _home;
        private Coroutine _running;

        private void Awake()
        {
            _home = transform.localPosition;

            if (body == null)
            {
                body = GetComponentInChildren<SpriteRenderer>();
            }
        }

        public override void PlayIdle()
        {
            Restart(Tint(idleColor));
        }

        public override void PlayCast()
        {
            Restart(Nudge(nudgeDistance));
        }

        public override void PlayHurt()
        {
            Restart(Flash(hurtColor));
        }

        public override void PlayVictory()
        {
            Restart(Nudge(nudgeDistance * 0.5f));
        }

        public override void PlayDefeat()
        {
            Restart(Fade());
        }

        private void Restart(IEnumerator routine)
        {
            if (_running != null)
            {
                StopCoroutine(_running);
                transform.localPosition = _home;
            }

            _running = StartCoroutine(routine);
        }

        private IEnumerator Tint(Color color)
        {
            if (body != null)
            {
                body.color = color;
            }

            yield break;
        }

        private IEnumerator Nudge(float distance)
        {
            var target = _home + new Vector3(distance, 0f, 0f);

            yield return Lerp(_home, target, beat);
            yield return Lerp(target, _home, beat);

            transform.localPosition = _home;
            _running = null;
        }

        private IEnumerator Flash(Color color)
        {
            if (body == null)
            {
                yield break;
            }

            body.color = color;
            yield return new WaitForSeconds(beat);
            body.color = idleColor;
            _running = null;
        }

        private IEnumerator Fade()
        {
            if (body == null)
            {
                yield break;
            }

            var elapsed = 0f;
            var start = body.color;

            while (elapsed < beat * 4f)
            {
                elapsed += Time.deltaTime;
                var alpha = Mathf.Lerp(start.a, 0.15f, elapsed / (beat * 4f));
                body.color = new Color(start.r, start.g, start.b, alpha);
                yield return null;
            }

            _running = null;
        }

        private IEnumerator Lerp(Vector3 from, Vector3 to, float duration)
        {
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localPosition = Vector3.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            transform.localPosition = to;
        }
    }
}
```

- [ ] **Step 2: Write the tile and health views**

Create `Assets/Scripts/Game/View/TileView.cs`:

```csharp
using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Game.Input;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CodexOfEchoes.Game.View
{
    /// <summary>One tile. Clicking it produces the same command a keypress would.</summary>
    public sealed class TileView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TMP_Text letterLabel;
        [SerializeField] private TMP_Text valueLabel;
        [SerializeField] private UnityEngine.UI.Image background;
        [SerializeField] private Color normalColor = new Color(0.93f, 0.90f, 0.82f);
        [SerializeField] private Color selectedColor = new Color(0.98f, 0.78f, 0.35f);

        private TileInputRouter _router;

        public int Index { get; private set; }

        public void Bind(int index, LetterTile tile, TileInputRouter router)
        {
            Index = index;
            _router = router;

            if (letterLabel != null)
            {
                letterLabel.text = tile.Letter.ToUpperInvariant();
            }

            if (valueLabel != null)
            {
                valueLabel.text = tile.Value.ToString();
            }

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (background != null)
            {
                background.color = selected ? selectedColor : normalColor;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_router != null)
            {
                _router.OnTileClicked(Index);
            }
        }
    }
}
```

Create `Assets/Scripts/Game/View/HealthBarView.cs`:

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CodexOfEchoes.Game.View
{
    public sealed class HealthBarView : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text telegraph;

        public void Set(int hp, int maxHp)
        {
            if (fill != null && maxHp > 0)
            {
                fill.fillAmount = Mathf.Clamp01((float)hp / maxHp);
            }

            if (label != null)
            {
                label.text = $"{hp} / {maxHp}";
            }
        }

        /// <summary>Feast warning. Shown a full turn before it lands.</summary>
        public void SetTelegraph(string message)
        {
            if (telegraph != null)
            {
                telegraph.text = message;
                telegraph.gameObject.SetActive(!string.IsNullOrEmpty(message));
            }
        }
    }
}
```

- [ ] **Step 3: Write the word cast effect**

Create `Assets/Scripts/Game/View/WordCastView.cs`:

```csharp
using System.Collections;
using CodexOfEchoes.Core.Words;
using TMPro;
using UnityEngine;

namespace CodexOfEchoes.Game.View
{
    /// <summary>
    /// The word materialises and flies at the enemy. Placeholder visuals; the seam that
    /// matters is that it receives a WordCastEvent and owns its own timing.
    /// </summary>
    public sealed class WordCastView : MonoBehaviour
    {
        [SerializeField] private TMP_Text wordLabel;
        [SerializeField] private Transform origin;
        [SerializeField] private Transform target;
        [SerializeField] private float flightTime = 0.45f;
        [SerializeField] private Color plainColor = new Color(0.95f, 0.95f, 0.92f);
        [SerializeField] private Color echoColor = new Color(0.62f, 0.84f, 1f);
        [SerializeField] private Color baneColor = new Color(1f, 0.55f, 0.35f);

        private void Awake()
        {
            if (wordLabel != null)
            {
                wordLabel.gameObject.SetActive(false);
            }
        }

        public void Play(string word, StoryWordTier tier)
        {
            if (wordLabel == null || origin == null || target == null)
            {
                return;
            }

            StopAllCoroutines();
            StartCoroutine(Fly(word, tier));
        }

        private IEnumerator Fly(string word, StoryWordTier tier)
        {
            wordLabel.text = word;
            wordLabel.color = ColorFor(tier);
            wordLabel.gameObject.SetActive(true);

            var elapsed = 0f;
            while (elapsed < flightTime)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / flightTime;
                wordLabel.transform.position =
                    Vector3.Lerp(origin.position, target.position, t);
                wordLabel.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.25f, t);
                yield return null;
            }

            wordLabel.gameObject.SetActive(false);
        }

        private Color ColorFor(StoryWordTier tier)
        {
            switch (tier)
            {
                case StoryWordTier.Bane: return baneColor;
                case StoryWordTier.Echo: return echoColor;
                default: return plainColor;
            }
        }
    }
}
```

- [ ] **Step 4: Write the event subscriber**

Create `Assets/Scripts/Game/View/BattleView.cs`. This is where events become visuals:

```csharp
using System.Collections.Generic;
using CodexOfEchoes.Core.Battle;
using CodexOfEchoes.Core.Combat;
using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Core.Words;
using CodexOfEchoes.Game.Battle;
using CodexOfEchoes.Game.Input;
using TMPro;
using UnityEngine;

namespace CodexOfEchoes.Game.View
{
    /// <summary>
    /// Turns the event stream into visuals. Subscribes to BattleRunner and reacts one
    /// event at a time, which is why the order the engine emits them in matters.
    /// </summary>
    public sealed class BattleView : MonoBehaviour
    {
        [SerializeField] private BattleRunner runner;
        [SerializeField] private TileInputRouter router;
        [SerializeField] private Transform tileParent;
        [SerializeField] private TileView tilePrefab;
        [SerializeField] private CombatantView lioraView;
        [SerializeField] private CombatantView enemyView;
        [SerializeField] private HealthBarView lioraHealth;
        [SerializeField] private HealthBarView enemyHealth;
        [SerializeField] private WordCastView wordCast;
        [SerializeField] private TMP_Text currentWordLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private GameObject rewrittenPagePanel;

        private readonly List<TileView> _tiles = new List<TileView>();

        private void Start()
        {
            BuildGrid();
            RefreshAll();

            runner.EventPlayed += OnEvent;
            lioraView.PlayIdle();
            enemyView.PlayIdle();

            if (rewrittenPagePanel != null)
            {
                rewrittenPagePanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (runner != null)
            {
                runner.EventPlayed -= OnEvent;
            }
        }

        private void BuildGrid()
        {
            var state = runner.Engine.State;

            for (var index = 0; index < TileGrid.Size; index++)
            {
                var tile = Instantiate(tilePrefab, tileParent);
                tile.Bind(index, state.Grid[index], router);
                _tiles.Add(tile);
            }
        }

        private void OnEvent(BattleEvent battleEvent)
        {
            switch (battleEvent)
            {
                case TileSelectedEvent selected:
                    _tiles[selected.Index].SetSelected(true);
                    RefreshCurrentWord();
                    break;

                case TileDeselectedEvent deselected:
                    _tiles[deselected.Index].SetSelected(false);
                    RefreshCurrentWord();
                    break;

                case SelectionClearedEvent _:
                    foreach (var tile in _tiles)
                    {
                        tile.SetSelected(false);
                    }

                    RefreshCurrentWord();
                    break;

                case WordRejectedEvent rejected:
                    ShowMessage(rejected.Reason == WordRejectionReason.TooShort
                        ? "Too short."
                        : $"\"{rejected.Word}\" isn't a word.");
                    break;

                case WordCastEvent cast:
                    lioraView.PlayCast();
                    wordCast.Play(cast.Word, cast.Tier);
                    ShowMessage(MessageFor(cast));
                    break;

                case DamageDealtEvent damage:
                    ApplyDamage(damage);
                    break;

                case HealedEvent healed:
                    enemyHealth.Set(healed.NewHp, runner.Engine.State.Enemy.MaxHp);
                    ShowMessage($"The Aswang feasts. +{healed.Amount} HP.");
                    break;

                case TilesFellEvent _:
                case TilesSpawnedEvent _:
                case GridScrambledEvent _:
                    RefreshTiles();
                    break;

                case EnemyTelegraphedEvent _:
                    enemyHealth.SetTelegraph("FEAST NEXT TURN");
                    break;

                case EnemyActedEvent acted:
                    if (acted.Ability == EnemyAbility.Feast)
                    {
                        enemyHealth.SetTelegraph(string.Empty);
                    }

                    enemyView.PlayCast();
                    break;

                case BattleEndedEvent ended:
                    ShowEnding(ended.Outcome);
                    break;
            }
        }

        private void ApplyDamage(DamageDealtEvent damage)
        {
            var state = runner.Engine.State;

            if (damage.Target == CombatantId.Enemy)
            {
                enemyHealth.Set(damage.NewHp, state.Enemy.MaxHp);
                enemyView.PlayHurt();
            }
            else
            {
                lioraHealth.Set(damage.NewHp, state.Player.MaxHp);
                lioraView.PlayHurt();
            }
        }

        private void ShowEnding(BattleOutcome outcome)
        {
            if (outcome == BattleOutcome.Victory)
            {
                lioraView.PlayVictory();
                enemyView.PlayDefeat();
                ShowMessage("The myth is yours to rewrite.");

                // Seam for the real narrative payoff.
                if (rewrittenPagePanel != null)
                {
                    rewrittenPagePanel.SetActive(true);
                }
            }
            else
            {
                lioraView.PlayDefeat();
                enemyView.PlayVictory();
                ShowMessage("The Silence takes the page.");
            }
        }

        private string MessageFor(WordCastEvent cast)
        {
            switch (cast.Tier)
            {
                case StoryWordTier.Bane:
                    return $"{cast.Word}! It burns. {cast.Damage} damage.";
                case StoryWordTier.Echo:
                    return $"{cast.Word} — the story answers. {cast.Damage} damage.";
                default:
                    return $"{cast.Word}: {cast.Damage} damage.";
            }
        }

        private void RefreshAll()
        {
            var state = runner.Engine.State;
            lioraHealth.Set(state.Player.Hp, state.Player.MaxHp);
            enemyHealth.Set(state.Enemy.Hp, state.Enemy.MaxHp);
            enemyHealth.SetTelegraph(string.Empty);
            RefreshCurrentWord();
        }

        private void RefreshTiles()
        {
            var state = runner.Engine.State;

            for (var index = 0; index < _tiles.Count; index++)
            {
                _tiles[index].Bind(index, state.Grid[index], router);
                _tiles[index].SetSelected(state.Selection.Contains(index));
            }
        }

        private void RefreshCurrentWord()
        {
            if (currentWordLabel != null)
            {
                currentWordLabel.text = runner.Engine.State.CurrentWord;
            }
        }

        private void ShowMessage(string message)
        {
            if (messageLabel != null)
            {
                messageLabel.text = message;
            }
        }
    }
}
```

- [ ] **Step 5: Verify it compiles**

Run the batch-mode compile check.
Expected: no output.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: add placeholder views and the Spine animation seam"
```

---

### Task 16: Scene assembly and seam tests

The scene is built by hand in the editor — this is the one task that cannot be scripted. PlayMode tests then check the wiring holds, without testing balance.

**Files:**
- Create: `Assets/Scenes/Battle.unity`
- Create: `Assets/Data/Enemy_Aswang.asset`
- Create: `Assets/Data/StoryWords_Chapter1.asset`
- Create: `Assets/Data/BalanceConfig.asset`
- Create: `Assets/Prefabs/Tile.prefab`
- Create: `Assets/Scripts/Tests/CodexOfEchoes.Tests.asmdef`
- Create: `Assets/Scripts/Tests/BattleSeamTests.cs`

**Interfaces:**
- Consumes: everything from Tasks 12–15
- Produces: a playable `Battle.unity`

- [ ] **Step 1: Create the data assets**

In the Unity editor: **Assets → Create → Codex of Echoes →** each of Enemy Definition, Story Word Set, and Balance Config. Save them into `Assets/Data/` as `Enemy_Aswang`, `StoryWords_Chapter1`, and `BalanceConfig`.

Defaults are already correct (Aswang 75 HP with the six Bane words; seven Echo words; Liora 100 HP). Set `Fixed Seed` to `20260914` for now — a fixed seed makes early playtests comparable; set it back to 0 for random battles.

- [ ] **Step 2: Build the tile prefab**

Create a UI `Image` named `Tile` with two `TextMeshPro - Text (UI)` children, `Letter` and `Value`. Add the `TileView` component and assign all three serialized fields. Drag it into `Assets/Prefabs/Tile.prefab`, then delete it from the scene.

- [ ] **Step 3: Assemble the scene**

Create `Assets/Scenes/Battle.unity` with this hierarchy:

```
Battle (scene)
├── Main Camera
├── EventSystem                    (required for pointer clicks)
├── BattleSystem
│     BattleRunner       → Enemy_Aswang, StoryWords_Chapter1, BalanceConfig
│     BattlePresenter
│     TileInputRouter
├── Liora                          PlaceholderCombatantView + SpriteRenderer
├── Aswang                         PlaceholderCombatantView + SpriteRenderer
└── Canvas
    ├── TileGrid                   GridLayoutGroup, 4 columns
    ├── CurrentWord                TMP_Text
    ├── Message                    TMP_Text
    ├── LioraHealth                HealthBarView
    ├── EnemyHealth                HealthBarView (assign the Telegraph child)
    ├── WordCast                   WordCastView
    ├── RewrittenPagePanel         inactive; placeholder for the story payoff
    └── BattleView                 → every reference above
```

Liora sits on the left facing right, the Aswang on the right — matching the spec's battle framing.

- [ ] **Step 4: Play it**

Press Play. Type a word, press Enter, and confirm: the word flies at the Aswang, its health drops, tiles fall and refill, the Aswang strikes back, and "FEAST NEXT TURN" appears at the end of turn 3.

This is the milestone's actual deliverable. The console logs the battle seed at startup — note it if a fight feels wrong.

- [ ] **Step 5: Create the PlayMode test assembly**

Create `Assets/Scripts/Tests/CodexOfEchoes.Tests.asmdef`:

```json
{
    "name": "CodexOfEchoes.Tests",
    "rootNamespace": "CodexOfEchoes.Tests",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "CodexOfEchoes.Core",
        "CodexOfEchoes.Game"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [ "nunit.framework.dll" ],
    "autoReferenced": false,
    "defineConstraints": [ "UNITY_INCLUDE_TESTS" ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 6: Write the seam tests**

Create `Assets/Scripts/Tests/BattleSeamTests.cs`. These check wiring only — balance is Task 11's job:

```csharp
using System.Collections;
using CodexOfEchoes.Core.Battle;
using CodexOfEchoes.Game.Battle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CodexOfEchoes.Tests
{
    /// <summary>
    /// Seam checks: the scene loads, the engine is constructed, commands reach it, and
    /// the presenter drains without exceptions. Combat rules are verified in Tests.Core.
    /// </summary>
    public class BattleSeamTests
    {
        [UnityTest]
        public IEnumerator BattleSceneLoadsAndBuildsAnEngine()
        {
            yield return SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Single);

            var runner = Object.FindFirstObjectByType<BattleRunner>();

            Assert.That(runner, Is.Not.Null, "No BattleRunner in the Battle scene.");
            Assert.That(runner.Engine, Is.Not.Null);
            Assert.That(runner.Engine.State.Outcome, Is.EqualTo(BattleOutcome.InProgress));
            Assert.That(runner.Engine.State.Grid.Tiles.Count, Is.EqualTo(16));
        }

        [UnityTest]
        public IEnumerator SubmittingACommandReachesTheEngine()
        {
            yield return SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Single);
            var runner = Object.FindFirstObjectByType<BattleRunner>();

            runner.Submit(new SelectTileCommand(0));
            yield return null;

            Assert.That(runner.Engine.State.Selection, Is.EqualTo(new[] { 0 }));
        }

        [UnityTest]
        public IEnumerator ThePresenterDrainsAndReleasesInput()
        {
            yield return SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Single);
            var runner = Object.FindFirstObjectByType<BattleRunner>();

            runner.Submit(new ScrambleCommand());

            // Drain the queue; the longest beat is well under two seconds.
            yield return new WaitForSeconds(3f);

            Assert.That(runner.IsBusy, Is.False, "Presenter never finished draining.");
            Assert.That(runner.Engine.State.TurnNumber, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator InputIsRefusedWhileAnimating()
        {
            yield return SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Single);
            var runner = Object.FindFirstObjectByType<BattleRunner>();

            runner.Submit(new ScrambleCommand());
            yield return null;

            var turnDuringAnimation = runner.Engine.State.TurnNumber;
            runner.Submit(new ScrambleCommand());

            Assert.That(runner.Engine.State.TurnNumber, Is.EqualTo(turnDuringAnimation));
        }
    }
}
```

- [ ] **Step 7: Add the scene to the build settings**

**File → Build Profiles → Scene List →** add `Assets/Scenes/Battle.unity`. Without this, `LoadSceneAsync("Battle")` fails in the tests.

- [ ] **Step 8: Run the PlayMode tests**

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.0f1/Editor/Unity.exe" \
  -batchmode -runTests -nographics \
  -projectPath "C:/Users/ADMIN/Desktop/Projects/Game Development/codex-of-echoes" \
  -testPlatform PlayMode \
  -testResults "$PWD/playmode-results.xml" \
  -logFile -
```

Expected: 4 passing tests in `playmode-results.xml`. Check with `grep -o 'result="[A-Za-z]*"' playmode-results.xml | head`.

- [ ] **Step 9: Run the full Core suite once more**

Run: `dotnet test Tests.Core/CoreTests.csproj -v minimal`
Expected: PASS, 130 tests.

- [ ] **Step 10: Commit**

```bash
git add -A
git commit -m "feat: assemble playable battle scene with seam tests"
```

---

## Definition of done

- [ ] `dotnet test Tests.Core/CoreTests.csproj` passes, 130 tests
- [ ] Unity PlayMode seam tests pass, 4 tests
- [ ] `Battle.unity` plays start to finish: type a word, press Enter, see it fly, watch the Aswang answer
- [ ] Feast telegraphs on turn 3 and lands on turn 4
- [ ] Both endings reachable — victory shows the placeholder rewritten-page panel, defeat shows its message
- [ ] The battle seed is logged at startup
- [ ] **Then play it.** The milestone's real question — is this fun? — is the one thing no test in this plan can answer.

## What this plan does not build

Carried from spec §10, listed here so nothing is mistaken for an oversight:

- Spine rigging and final Liora art — the seam is ready at `CombatantView`
- Potions — excluded from the slice, but the highest-priority combat addition after it, since they carry most of the game's premium monetization value
- Chapter 1's enemy roster and the Aswang boss
- The myth-rewriting narrative sequence — the seam is ready at `rewrittenPagePanel`
- Mobile touch input — `TileInputRouter` already abstracts the path
- Accounts, cross-save, and the Ink / Echoes currencies
- Dead-board detection — deliberately unsolved; the vowel invariant plus Scramble is the whole answer
