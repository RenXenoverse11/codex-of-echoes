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
| Paid power | Allowed via Echoes |
| Hard story paywalls | None |

### Currency roles

| Currency | Type | Obtained | Used for |
|---|---|---|---|
| Ink | Soft | Battles, rewards, drops | Basic potions, common items |
| Echoes | Premium | Real money | Cosmetics, stronger potions, convenience items |

### Monetization philosophy

Stated intent, recorded verbatim in substance:

- The game is designed to make money.
- Players who want to spend to make the game easier may do so.
- Echoes buy meaningful power — stronger potions, better recovery, emergency
  items, extra carry capacity, friction reducers.
- Ink alone must still allow full completion of the game.
- Echoes spenders get clear advantages in power and convenience.
- No hard paywalls on story chapters.
- Paid options are **not** to be artificially nerfed for fairness.

### Cross-save requirements

Progress that must sync: chapters completed, discovered Story Words, currency
balances, settings, inventory.

Recommended backend: PlayFab or Unity Gaming Services (Authentication + Cloud
Save).

---

## 2. Scope impact

**None on the current vertical slice.** Accounts, cross-save, and the dual
currency system are deferred to a later milestone. The slice remains focused on
answering whether the word combat loop is fun.

---

## 3. Architectural implications

### What the existing architecture already gets right

The engine-free C# core chosen for the slice happens to be the correct shape
for cross-save. Core types are plain C# with no `UnityEngine` dependencies,
which means they serialize cleanly to JSON for Cloud Save without adapter
layers. Battles are already deterministic and seeded, so a battle can be
described by a seed plus a command list rather than a state blob.

This was chosen for test speed, but it pays off twice.

### What to protect now, cheaply

One constraint added to the slice spec as a result: **Core types must stay
serialization-friendly.** No Unity types, no circular references, stable field
names. Costs nothing today; expensive to retrofit once save data exists in the
wild and needs migrating.

### Account friction

Forcing account creation at first launch on a premium single-player game is
meaningful friction for a player who has already paid. The standard pattern
avoids it:

1. Anonymous authentication by default — the player just plays.
2. Prompt to link a real identity (Steam / Apple / Google) only when the player
   wants cross-save, or at a natural moment after they are invested.

This keeps cross-save available without gating first launch behind a signup.

### Economy integrity

Cloud Save on both PlayFab and UGS is client-authoritative by default. For a
single-player premium game that is usually tolerable — a player editing their
own save is mostly cheating themselves. It stops being tolerable once **Echoes
are purchasable with real money**, because edited balances are lost revenue
rather than a private choice.

Server-side validation of Echoes balances and purchase receipts (Cloud Code on
UGS, CloudScript on PlayFab) should be treated as in-scope for the backend
milestone, not as a later hardening pass.

---

## 4. Open questions for the backend milestone

Not blocking the slice. Recorded so they are not rediscovered late:

- **Backend choice.** UGS has tighter native Unity 6 integration and is the
  lower-friction path for a Unity-only project. Verify the current status and
  pricing of both before committing — this document should not be treated as
  current on vendor roadmaps.
- **Conflict resolution.** Two devices played offline, both have progress. Last
  write wins, or merge? Story Word discovery is additive and merges naturally;
  currency balances do not.
- **Offline play.** Mobile players will play without connectivity. How long may
  the game run unsynced, and what happens to Ink earned offline?
- **Echoes across storefronts.** Echoes bought on Steam appearing on iOS means
  Apple takes no cut of that purchase but delivers the value. Confirm this
  against current App Store and Google Play policy — cross-platform premium
  currency is an area where store rules are specific and change.
- **Refunds and chargebacks.** Reconciling a refunded Echoes purchase against a
  balance already spent.
- **Price parity.** Whether Steam and mobile carry the same base price.

---

## 5. Risks recorded

Neither of these is a recommendation to change course. Both are consequences of
locked decisions, written down so they stay visible.

### Potions are an untested revenue mechanic

Echoes' premium value is concentrated in potions — stronger healing, attack
boosts, revives, carry capacity. The vertical slice deliberately excludes
potions, having taken "Bookworm core + Story Words" over the option that
included them.

The result is that the item category carrying most of the game's monetization
is currently unprototyped. "Do potions feel good in combat?" is now a revenue
question as much as a design one, and it should be answered in the milestone
immediately after the slice rather than late.

### Difficulty tuning interacts with paid healing

The slice's revised combat tuning makes the first Aswang encounter punishing at
average play — Liora dies on turn 9 without using Story Words (see §4 of the
slice spec for the full model).

Separately, healing potions are purchasable with Echoes.

Each decision is defensible alone. Together they form the pattern most readily
read as manufactured difficulty, and that reading is harsher against a game the
player has already paid for than against a free one. The interaction is worth
re-checking after the first playtest, when there is real data on whether the
difficulty reads as teaching or as pressure.

---

## 6. Store policy note

IAP inside paid apps is permitted on the App Store, Google Play, and Steam, so
the premium-plus-currency model is viable on all three. Platform commission,
price tiers, and cross-platform currency rules should be confirmed against
current policy during the backend milestone rather than assumed from this
document.
