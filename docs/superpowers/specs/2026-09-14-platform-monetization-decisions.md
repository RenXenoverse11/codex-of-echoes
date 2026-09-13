# Codex of Echoes — Platform, Account, and Monetization Decisions

**Date:** 2026-09-14
**Status:** Locked decisions
**Scope:** Game-wide. Does **not** affect the vertical slice
(see [word combat vertical slice](2026-09-14-word-combat-vertical-slice-design.md)).

---

## 1. Locked decisions

| Decision | Status |
|---|---|
| Title | Codex of Echoes |
| Combat style | Turn-based (Bookworm core + Story Words) |
| Platforms | Steam (PC) + Google Play + App Store |
| Base pricing | Paid / premium on all platforms |
| Content parity | Same game, same content across platforms |
| Accounts | Required |
| Cross-save | Required (PC ↔ Mobile) |
| Soft currency | **Ink** — earned through gameplay |
| Premium currency | **Echoes** — real-money purchase |
| Echoes behavior | **Platform-specific** |
| Potions & items | Fully synced across platforms |
| Paid power | Allowed via Echoes |
| Hard story paywalls | None |

### Currency roles

| Currency | Type | Obtained | Used for |
|---|---|---|---|
| Ink | Soft | Battles, rewards, drops | Basic potions, common items |
| Echoes | Premium | Real money | Cosmetics, stronger potions, convenience items |

### Monetization philosophy

- The game is designed to make money.
- Players who want to spend to make the game easier may do so.
- Echoes buy meaningful power — stronger potions, better recovery, emergency
  items.
- Ink alone must still allow full completion of the game.
- No hard paywalls on story chapters.

---

## 2. Cross-save rules

| Data | Sync behavior |
|---|---|
| Progress / chapters | Fully synced |
| Discovered Story Words | Fully synced |
| Ink (soft currency) | Fully synced |
| Potions & items | Fully synced |
| Settings | Fully synced |
| **Echoes (premium currency)** | **Platform-specific** |

Echoes bought on Steam are spendable only on Steam; iOS only on iOS; Android
only on Android. Items and potions *purchased with* Echoes sync everywhere.

### What this split actually does

The silo applies to the **unspent balance**, not to the value. A player may buy
Echoes on whichever storefront they prefer, convert them into potions, and
consume those potions on any platform. Purchasing power therefore still crosses
platforms — it simply requires one conversion step first.

This is a normal and defensible structure: it keeps each purchase flow wholly
inside the storefront that hosted it, which is what platform policy is
concerned with, while keeping the player's *inventory* coherent across their
devices. It is also considerably more player-friendly than siloing items too.

Worth being clear-eyed about it, though: if the goal were to keep value
consumed on a given platform tied to revenue earned on that platform, this
structure does not achieve that, and regional price differences between
storefronts make the gap exploitable in principle.

---

## 3. Scope impact

**None on the current vertical slice.** Accounts, cross-save, and the dual
currency system are deferred to a later milestone. The slice remains focused on
answering whether the word combat loop is fun.

---

## 4. Architectural implications

### What the existing architecture already gets right

The engine-free C# core chosen for the slice happens to be the correct shape
for cross-save. Core types are plain C# with no `UnityEngine` dependencies, so
they serialize cleanly to JSON for Cloud Save without adapter layers. Battles
are already deterministic and seeded, so a battle can be described by a seed
plus a command list rather than a state blob.

Chosen for test speed; it pays off twice.

### What to protect now, cheaply

**Core types must stay serialization-friendly.** No Unity types, no circular
references, stable field names. Costs nothing today; expensive to retrofit once
save data exists in the wild and needs migrating.

### Three wallets, not one

The platform-specific decision means a player account holds **up to three
independent Echoes balances**, not one. Consequences that are cheap now and
painful later:

- **Save schema.** Echoes must be stored per-platform from the first version —
  `echoes: { steam: n, ios: n, android: n }`, never a scalar. Migrating a
  shipped scalar balance into three buckets has no correct answer.
- **The balance UI cannot show a single number.** A bare "Echoes: 100" is
  actively misleading when the player holds three wallets, and it is the
  shortest path to "the game stole my Echoes" reviews. The player's other
  balances should be *visible but clearly unspendable here* — hiding them reads
  as loss, not as separation.
- **Purchase validation is per-platform**, but the resulting item grant is a
  cross-platform event. Receipt verification and inventory grant are different
  concerns and should not share a code path.

### Account friction

Forcing account creation at first launch on a premium single-player game is
meaningful friction for a player who has already paid. The standard pattern
avoids it:

1. Anonymous authentication by default — the player just plays.
2. Prompt to link a real identity (Steam / Apple / Google) only when the player
   wants cross-save, or at a natural moment once they are invested.

### Economy integrity

Cloud Save on both PlayFab and UGS is client-authoritative by default. For a
single-player premium game that is usually tolerable — a player editing their
own save mostly cheats themselves. It stops being tolerable once **Echoes cost
real money**, because edited balances are lost revenue rather than a private
choice.

Server-side validation of Echoes balances and purchase receipts (Cloud Code on
UGS, CloudScript on PlayFab) belongs in the backend milestone, not in a later
hardening pass.

---

## 5. Open questions for the backend milestone

Not blocking the slice. Recorded so they are not rediscovered late.

- **Backend choice.** UGS has tighter native Unity 6 integration and is the
  lower-friction path for a Unity-only project. Verify current status and
  pricing of both before committing — this document should not be treated as
  current on vendor roadmaps.
- **Conflict resolution.** Two devices played offline, both with progress. Last
  write wins, or merge? Story Word discovery and item acquisition are additive
  and merge naturally; Ink balances do not. Echoes sidestep this entirely now
  that they are platform-scoped — one more point in the split's favor.
- **Offline play.** Mobile players will play without connectivity. How long may
  the game run unsynced, and what happens to Ink earned offline?
- **Stranded balances.** A player who buys a 500-Echo pack on Steam, spends
  400, then moves to mobile has 100 Echoes stranded permanently. This will
  generate support tickets. Worth deciding the policy *before* launch: pack
  sizes that divide cleanly into common purchases, an explicit warning at first
  purchase, or an accepted support burden.
- **Refunds and chargebacks.** Reconciling a refunded Echoes purchase when the
  resulting items have already synced to — and been consumed on — another
  platform. The item sync makes this materially harder than a siloed inventory
  would.
- **Price parity.** Whether Steam and mobile carry the same base price.

---

## 6. Risks recorded

Neither is a recommendation to change course. Both are consequences of locked
decisions, written down so they stay visible.

### Potions are an untested revenue mechanic

Echoes' premium value is concentrated in potions — stronger healing, attack
boosts, emergency items. The vertical slice deliberately excludes potions,
having taken "Bookworm core + Story Words" over the option that included them.

The item category carrying most of the game's monetization is therefore
unprototyped. "Do potions feel good in combat?" is now a revenue question as
much as a design one, and should be answered in the milestone immediately after
the slice rather than late.

The item-sync decision raises the stakes slightly: potions are the mechanism by
which Echo value crosses platforms, which makes them load-bearing for the
economy as well as for combat.

### Difficulty tuning interacts with paid healing

The slice's revised combat tuning makes the first Aswang encounter punishing at
average play — Liora dies on turn 9 without using Story Words (see §4 of the
slice spec for the full model).

Separately, healing potions are purchasable with Echoes.

Each decision is defensible alone. Together they form the pattern most readily
read as manufactured difficulty, and that reading is harsher against a game the
player has already paid for than against a free one. Worth re-checking after
the first playtest, when there is real data on whether the difficulty reads as
teaching or as pressure.

---

## 7. Store policy note

IAP inside paid apps is permitted on the App Store, Google Play, and Steam, so
the premium-plus-currency model is viable on all three. The platform-specific
Echoes decision keeps each purchase flow inside its host storefront, which is
the conservative position.

Commission rates, price tiers, and rules around cross-platform entitlements
should be confirmed against current policy during the backend milestone rather
than assumed from this document.
