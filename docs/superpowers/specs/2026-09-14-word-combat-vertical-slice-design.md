# Codex of Echoes — Word Combat Vertical Slice

**Date:** 2026-09-14
**Status:** Approved design, ready for implementation planning
**Milestone:** One complete, playable battle with placeholder art

---

## 1. Goal

Produce a single battle that can be played start to finish — tile grid, word
forming, dictionary validation, damage, one Chapter 1 enemy that fights back,
win and lose states — in order to answer one question: **is the combat loop
fun?**

Everything else in this spec exists to serve that question or to avoid
foreclosing later work.

### Non-goals

Explicitly out of scope for this milestone:

- Spine rigging and final character art (blocked on art assets; seam provided)
- Chapter progression, a second enemy, or the Aswang boss fight
- The narrative myth-rewriting sequence (placeholder panel only)
- Audio, menus, save/load, settings
- Mobile touch input (architecture permits it; slice does not ship it)

---

## 2. Locked decisions

| Decision | Choice |
|---|---|
| Milestone | Playable vertical slice, placeholder art |
| Rule fidelity | Bookworm Adventures core + Story Words as the one original mechanic |
| Input | Both, keyboard-first (typing is the primary PC path) |
| Architecture | Engine-free C# core + thin Unity presentation layer |
| Unity version | 6000.5.0f1 |
| Dictionary | ENABLE word list (~173k words, public domain) |

**On the dictionary:** Scrabble's TWL and SOWPODS are licensed word lists and
are not safe to ship commercially. ENABLE is public domain and is the correct
default for a game intended for release.

---

## 3. Architecture

```
codex-of-echoes/
├── Assets/Scripts/
│   ├── Core/          asmdef: CodexOfEchoes.Core, noEngineReferences = true
│   │   ├── Grid/      LetterTile, TileGrid, TileBag, LetterTable
│   │   ├── Words/     IWordDictionary, WordValidator, StoryWordTable
│   │   ├── Combat/    DamageCalculator, Combatant, EnemyBehaviour
│   │   ├── Battle/    BattleEngine, BattleState, BattleCommand, BattleEvent
│   │   └── Rng/       IRandomSource, SeededRandom
│   ├── Game/          asmdef: CodexOfEchoes.Game → Core + UnityEngine
│   │   ├── Data/      EnemyDefinitionSO, StoryWordSetSO, BalanceConfigSO
│   │   ├── Battle/    BattleRunner, BattlePresenter
│   │   ├── Input/     TileInputRouter
│   │   └── View/      TileView, CombatantView, WordCastView, HealthBarView
│   └── Tests/         asmdef: CodexOfEchoes.Tests (PlayMode seam tests)
├── Assets/StreamingAssets/enable.txt
├── Assets/Scenes/Battle.unity
├── Tests.Core/CoreTests.csproj
└── .gitignore
```

### The two-layer split

`Core` contains everything that *is* the game and has zero `using UnityEngine`.
`Game` renders state and forwards input. The rule of thumb: if changing it
would change how the game plays, it belongs in Core; if changing it would only
change how the game looks, it belongs in Game.

Enemy stats, Story Word tables, and balance constants are authored as
ScriptableObjects in the `Game` layer and converted to plain Core structs at
load. This keeps Inspector-based tuning available without the rules depending
on the engine.

**Core types must stay serialization-friendly:** no Unity types, no circular
references, stable field names. Cross-save is a locked requirement for a later
milestone (see [platform and monetization decisions](2026-09-14-platform-monetization-decisions.md)),
and plain serializable Core types are what make Cloud Save a mapping exercise
rather than a rewrite. This costs nothing now and is expensive to retrofit once
save data exists in the wild.

### Fast test loop

`Tests.Core/CoreTests.csproj` lives **outside** `Assets/` so Unity ignores it,
and compiles the same Core sources directly:

```xml
<Compile Include="../Assets/Scripts/Core/**/*.cs" />
```

It targets `net8.0` with NUnit, and runs under `dotnet test` in seconds rather
than the 60–90s Unity batch-mode editor startup.

Two constraints that keep the sidecar honest:

- **`<LangVersion>9.0</LangVersion>`** — Unity 6 compiles C# 9. Without this
  pin, `dotnet test` would happily accept C# 12 syntax that Unity then rejects,
  and the divergence would only surface in the editor.
- **`<Nullable>disable</Nullable>`** — matches Unity's default so the two
  compilers agree.

**Gotcha:** Unity's standard `.gitignore` template contains `*.csproj`, which
would silently exclude the sidecar from version control. The project
`.gitignore` must carry an explicit negation:

```gitignore
!Tests.Core/*.csproj
```

### Packages

- `com.unity.inputsystem` — unified keyboard and pointer handling
- TextMeshPro — bundled with `com.unity.ugui` in Unity 6, no separate install

---

## 4. Combat rules

### Grid

4×4, sixteen tiles. Casting consumes only the tiles used; survivors fall down
their column and new tiles drop in from the top. `Qu` occupies a single tile.

**Vowel invariant:** the board always holds at least 4 vowels (A, E, I, O, U).
Enforced on initial fill and after every refill. This is the cheapest available
guard against unplayable hands.

### Tile bag

Weighted random draw, vowel-boosted relative to natural English frequency
(42/98 ≈ 43% vowels):

| Weight | Letters |
|---|---|
| 12 | E |
| 9 | A, I |
| 8 | O |
| 6 | N, R, T |
| 4 | L, S, U, D |
| 3 | G |
| 2 | B, C, M, P, F, H, V, W, Y |
| 1 | K, J, X, Z, Qu |

### Damage

```
base   = Σ letterValue(tile)
lenMul = 1.0 + 0.22 × max(0, length − 3)
damage = round(base × lenMul × storyMultiplier)
```

Minimum word length: **3**. Each tile may be used at most once per word.

Rounding is **half away from zero**, not C#'s default banker's rounding — so a
computed 12.5 deals 13, not 12. Use `Math.Round(x, MidpointRounding.AwayFromZero)`.

Letter values — Scrabble-like, compressed:

| Value | Letters |
|---|---|
| 1 | E, A, I, O, N, R, T, L, S, U |
| 2 | D, G |
| 3 | B, C, M, P |
| 4 | F, H, V, W, Y |
| 5 | K |
| 8 | J, X |
| 10 | Z, Qu |

Worked examples:

| Word | Base | Length | Multiplier | Damage |
|---|---|---|---|---|
| CAT | 5 | 3 | 1.00 | 5 |
| SILENCE | 9 | 7 | 1.88 | 17 |
| SALT (Bane) | 4 | 4 | 1.22 × 2.5 | 12 |
| GARLIC (Bane) | 9 | 6 | 1.66 × 2.5 | 37 |

### Story Words

The mechanic that separates this from a Bookworm reimplementation. A player who
reads the myth fights better — which is the game's thesis expressed as a rule.

| Tier | Multiplier | Scope | Chapter 1 examples |
|---|---|---|---|
| Echo | ×1.5 | Chapter-wide | NAME, TRUTH, LIGHT, DAWN, STORY, INK, PAGE |
| Bane | ×2.5 | Per-enemy | vs Aswang: SALT, GARLIC, ASH, OIL, STING, SPINE |

Bane words are drawn from documented folklore weaknesses — salt, garlic, ash,
the stingray-tail whip (*buntot pagi*), and severing the torso.

Tiers do not stack. If a word appears in both sets, Bane wins.

### Turn structure

Player casts → enemy acts → repeat.

**Liora:** 100 HP.

**Aswang:** 75 HP. Normal attack deals 7–11. Every 4th turn it uses **Feast**
— 20 damage and heals itself 10 — telegraphed one full turn in advance so the
player can see it coming.

**Scramble** rerolls the board but consumes the player's turn; the enemy still
attacks.

**Lethal resolves immediately.** If the player's word reduces the enemy to 0
HP, the enemy does not act that turn. This makes a damage race winnable on the
turn Feast would otherwise land, and the difficulty model below assumes it.

### End states

- Enemy HP reaches 0 → victory, followed by a placeholder "rewritten page"
  panel (the seam where the real narrative payoff lands later).
- Liora HP reaches 0 → defeat, retry.

### Expected difficulty (playtest baseline)

Recorded so playtest has something to compare against, because this tuning is
tighter than it looks:

The Aswang's two Feast heals (turns 4 and 8) give it an effective pool of
~95 HP. Liora absorbs ~94 damage across the first eight turns, leaving her
unable to survive a ninth. Modelled out:

- **Average play** — 5-letter words, no Story Words, ~9 damage/turn: by turn 9
  the enemy is still at ~14 HP and Liora **dies**.
- **Strong play** — ~15 damage/turn using Story Words: **win on turn 6** with
  Liora at 44 HP.

The encounter therefore *hard-requires* the Story Word mechanic rather than
rewarding it as a bonus. This is a clean skill gradient but unforgiving for
what is likely Chapter 1's first fight. Flagged deliberately; if playtest shows
it reads as punishing rather than teaching, the first lever to pull is Feast's
heal, then enemy HP.

---

## 5. Data flow

The entire core contract is one method:

```csharp
IReadOnlyList<BattleEvent> BattleEngine.Execute(BattleCommand cmd)
```

**Commands:** `SelectTile(index)`, `DeselectLast()`, `ClearSelection()`,
`CastWord()`, `Scramble()`

**Events:** `TileSelected` · `SelectionCleared` · `WordRejected(word, reason)` ·
`WordCast(word, damage, tier)` · `DamageDealt(target, amount, newHp)` ·
`TilesConsumed(indices)` · `TilesFell(moves)` · `TilesSpawned(entries)` ·
`EnemyTelegraphed(abilityId)` · `EnemyActed(abilityId, damage, heal)` ·
`BattleEnded(outcome)`

### Why events instead of state diffing

Animation requires **causality and ordering**: word flies → enemy flinches → HP
drops → tiles fall. A state diff reports what changed but not in what sequence
or why, forcing the view to reconstruct the causal chain by hand. The event
list carries that ordering for free.

### Synchronous engine, asynchronous view

`BattleEngine.Execute` is instantaneous. **All** timing lives in
`BattlePresenter`, which drains the returned event list as a coroutine queue
while input is locked.

This split is what makes the rules testable: a test calls `Execute` and asserts
on returned events with nothing to await.

### Determinism

`BattleEngine` takes an `IRandomSource`; `SeededRandom` is the production
implementation and the seed is stored in `BattleState`. Seed plus command list
replays any battle exactly, turning "that fight felt unwinnable" into a
reproducible report rather than an impression.

---

## 6. Presentation layer

`BattleRunner` (MonoBehaviour) owns the engine, receives input, pumps commands,
and hands each event list to `BattlePresenter`.

### The Spine seam

`CombatantView` is an abstract MonoBehaviour exposing exactly five calls,
matching Liora's five planned animations:

```csharp
PlayIdle()  PlayCast()  PlayHurt()  PlayVictory()  PlayDefeat()
```

The slice ships `PlaceholderCombatantView` — a colored rectangle with tweens.
`SpineCombatantView` later implements the same five against a
`SkeletonAnimation`. Nothing upstream of the seam changes when real art arrives.

### Word cast VFX

`WordCastView` receives the `WordCast` event, spawns a TextMeshPro object
bearing the cast word, tweens it toward the enemy, and fires an ink particle
burst on impact — returning a completion callback so the presenter can sequence
correctly. Placeholder visuals; the seam is the deliverable.

### Input

`TileInputRouter` funnels keyboard and pointer into identical commands via the
Input System.

| Input | Action |
|---|---|
| Letter key | Claim first unselected tile bearing that letter, reading order |
| Backspace | `DeselectLast()` |
| Enter | `CastWord()` |
| Tab | `Scramble()` |
| Click tile | `SelectTile(index)` |

Identical letters carry identical value, so which duplicate a keypress claims
is cosmetic rather than strategic.

---

## 7. Error handling

Most failure modes here are gameplay, and the design depends on that
distinction holding.

1. **Invalid words are not errors.** `WordRejected` carries a reason
   (`TooShort` / `NotInDictionary`) so the view can give distinct feedback. No
   turn is consumed.

2. **Invalid commands never throw.** Selecting an already-selected tile,
   casting an empty selection, an out-of-range index — the engine returns a
   rejection event. An exception mid-battle would strand the view out of sync
   with the state, which is the worst available failure mode in an
   event-driven presentation layer.

3. **Genuinely exceptional conditions fail loudly at load.** Missing or corrupt
   `enable.txt`, unassigned enemy asset. These are developer errors; a clear
   startup failure beats a battle that limps.

4. **Dead boards are not detected.** Determining that no valid word exists is
   combinatorially expensive. The vowel invariant plus Scramble covers this in
   practice. Recorded as a known limitation rather than treated as solved.

---

## 8. Testing

In descending order of where the value lies:

1. **`dotnet test Tests.Core`** — the primary suite, written test-first.
   Covers: damage formula tables, Story Word tiering and precedence, the vowel
   invariant, grid gravity and refill, complete battles from a fixed seed, and
   every command rejection path.

2. **Unity PlayMode tests** — thin seam checks only. Scene loads, `BattleRunner`
   wires up, an input event produces the expected command, the presenter drains
   a queue without exceptions. Balance is deliberately not tested here.

3. **Manual play** — the actual point of the milestone.

### Verification split

Claude can run (1) directly, and can batch-compile the Unity layer to confirm
the `Game` assembly builds. (3) requires the human — the editor cannot be
driven from the CLI in this setup, and "does this feel fun" was never a
question a test runner could answer.

---

## 9. Known limitations

- Dead boards are undetected (§7.4)
- `HashSet<string>` holding ~173k words costs roughly 10 MB. Acceptable on PC,
  borderline on mobile. A DAWG reduces this to ~1 MB and is the designated
  optimization if mobile memory becomes a constraint.
- No profanity filtering on the dictionary. ENABLE is relatively clean but not
  curated for this.
- Single enemy, single battle, no progression.

---

## 10. Deferred to later milestones

- Spine rigging and final Liora art (seam ready at `CombatantView`)
- Chapter 1 enemy roster and the Aswang boss
- The myth-rewriting narrative sequence (seam ready at the victory panel)
- Mobile touch input (Input System already abstracts the path)
- Chapter-specific palettes and visual motifs
- **Potions** — excluded from the slice, but they carry most of the game's
  premium monetization value, so they are the highest-priority combat addition
  after it
- **Accounts, cross-save, and the Ink / Echoes currencies** — see
  [platform and monetization decisions](2026-09-14-platform-monetization-decisions.md)
