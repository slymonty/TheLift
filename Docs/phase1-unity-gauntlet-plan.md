# Phase 1 — Unity Gauntlet Build Plan

*Companion to Game Bible v0.4. Target: local combat prototype with a headless tuning loop.*

**Engine:** Unity 6 (Personal tier — free under $200K revenue/funding)
**Language:** C#
**Scope:** Two capsules, one grey room, offline. No networking, no art, no level.
**Duration:** 6–10 weeks of evenings
**Gate question:** *Does a Heavy vs. Scrapper exchange feel tense, and does throwing someone through a table feel earned?*

---

# PART I — ARCHITECTURE

## The two-assembly split

This is the most important decision in Phase 1. Get it right and everything downstream is easier.

```
Assets/
  Scripts/
    CombatCore/              ← NO UnityEngine imports. Ever.
      CombatCore.asmdef
      States/
      Actions/
      Resolution/
      Archetypes/
      Simulation/
    Game/                    ← Unity layer
      Game.asmdef            (references CombatCore)
      Presentation/
      Input/
      Animation/
      Audio/
  Tests/
    CombatCore.Tests/        ← headless, fast
    Gauntlet/                ← batch simulation harness
docs/
  game-bible-v0.4.md
  tuning-log.md
```

**The rule: `CombatCore` never imports `UnityEngine`.** Enforce it with an assembly definition that doesn't reference Unity modules — then it's a compile error, not a discipline problem.

## Why this matters

**1. The gauntlet loop becomes possible.** A headless harness can run thousands of fights per second with no editor and no human. That's the difference between "I think 55 stamina feels right" and "at 55 the Heavy wins 61% and fights average 43 seconds; at 48 it's 52% and 38 seconds."

**2. Determinism is free.** Integer frame counters, no `Time.deltaTime` in the rules layer, no `UnityEngine.Random`. Same inputs always produce the same fight. This is also what rollback netcode needs later.

**3. The port stops being scary.** If you move to Unreal at the Phase 2 gate, `CombatCore` is pure logic that translates to C++ almost mechanically — and it's already validated. You'd reimplement a proven spec, not redesign one.

## Determinism rules

| Rule | Why |
|---|---|
| Tick on an integer frame counter, fixed 60Hz | No variable timestep drift |
| No `UnityEngine.Random` — use a seeded `System.Random` in core | Reproducible fights |
| Prefer integer or fixed-point math for rules | Float drift across platforms |
| No `Time.deltaTime` inside `CombatCore` | It's a Unity dependency and non-deterministic |
| All randomness takes an explicit seed parameter | Replayable |

---

# PART II — BUILD ORDER

Build in this order. Each step is independently testable. **Do not skip ahead** — the value of Phase 1 is isolating one question at a time.

## Step 1 — Project skeleton *(2–3 days)*

- Unity 6 project, 3D URP template
- Git + LFS with the config files, committed **before** any assets
- Project Settings → Editor → **Asset Serialization = Force Text**, **Version Control = Visible Meta Files**
- Create both assembly definitions and verify `CombatCore` fails to compile if you add `using UnityEngine;`
- Unity Test Framework enabled (Window → Package Manager)
- `docs/game-bible-v0.4.md` committed

**Done when:** an empty test in `CombatCore.Tests` runs green.

## Step 2 — The state model *(4–5 days)*

Pure C#. No Unity, no visuals.

```
Fighter
  ├─ Stamina      (0–100, regen 14/s after 1.8s delay)
  ├─ Balance      (0–100, regen ~40/s)
  ├─ Composure    (0–100, no regen)
  ├─ Rattled      (0–100, regen ~1/s, persists)
  └─ Adrenaline   (0–100, derived from duration + damage taken)
```

Plus derived states: `Exhausted`, `Composed/Hot/Flooded/Gone`, `Shaken/Dazed/Concussed/Down`.

**Tests to write:** stamina regen respects the delay · Exhausted clears at 25 · Adrenaline thresholds fire at the right values · Rattled does not reset between encounters.

**Done when:** you can tick a `Fighter` 600 frames and assert exact state values.

## Step 3 — Actions and frame data *(5–7 days)*

Every action as a data-driven entry, not hardcoded logic:

```
Action { Startup, Active, Recovery, StaminaCost,
         ComposureDmg, BalanceDmg, RattledDmg, Zone }
```

Load from a ScriptableObject or JSON so tuning doesn't require recompiling. Light, Heavy, Weapon Light, Weapon Heavy from the bible's table.

**Tests:** an action cannot start during another's recovery · costs deduct correctly · Exhausted applies the −50% damage modifier.

**Done when:** two `Fighter` objects can trade blows in a headless test and one eventually reaches Composure 0.

## Step 4 — Impact resolution *(4–5 days)*

The five-variable model: Location × Awareness × Surface × Orientation × Force.

All tables data-driven. Awareness is a function of relative facing.

**Tests:** blindside applies 2.2× and doubles Rattled · a head strike at threshold produces Dazed · landing orientation rolls deterministically from a seed.

**Done when:** you can call `ResolveImpact()` with a scenario and get the bible's worked desk-lamp example back exactly — Rattled 53, Dazed.

## Step 5 — Defence *(3–4 days)*

Slip, Cover, Shove, Tie-up, Give Ground. No invincibility frames anywhere.

**Tests:** Slip beats a strike inside its window and loses to a grab · Cover cannot prevent a grapple · Shove against an Exhausted target costs nothing.

## Step 6 — Grapples *(6–8 days)*

Weak (positioning) and Strong (payoff). The collar drag moves the target 2–3m. The 8-frame reversal window. Third-party interruption.

Environmental override requires a `PropAnchor` in range and the target `Staggered || Exhausted`.

**Tests:** reversal succeeds inside 8 frames and fails at 9 · reversal unavailable when Exhausted or Flooded · environmental throw rejected against a healthy target · a light strike from a third fighter cancels a paired move.

**Done when:** a headless fight can reach an environmental throw and the log reads like a fight.

## Step 7 — Compromised verbs *(4–5 days)*

Snag, Cling, Drag down, Post up, Stomp-off.

**Tests:** Cling escalates 8 → 12 → 18 on re-grab within 10s · clothing tears at ~6s · Cling from Downed drains Composure not Stamina · stomp-off costs 12 for a teammate and 15 for self.

## Step 8 — Archetypes *(2–3 days)*

Six stat blocks as data. Heavy, Bruiser, Scrapper, Agile, Technician, Medic.

**Done when:** you can instantiate any two and run a fight.

## Step 9 — THE GAUNTLET *(5–7 days)*

The payoff. See Part III.

## Step 10 — Unity layer *(7–10 days)*

Only now do you touch visuals. Two capsules, gamepad input, a camera with the bible's rules, placeholder animations from Mixamo, hitstop, camera shake, a debug HUD showing all five bars.

**This layer contains no rules.** It reads `CombatCore` state and renders it.

**Done when:** you and a friend can play it with two controllers.

## Step 11 — Feel pass *(ongoing)*

Hitstop duration, camera shake curves, impact frames, audio. This is the part no loop can do for you.

---

# PART III — THE GAUNTLET LOOP

## What it is

A headless batch simulator that runs thousands of fights with scripted AI and reports aggregate statistics. Because `CombatCore` has no Unity dependency, it runs as a plain console app — no editor, no rendering, thousands of fights per second.

## The harness

```
GauntletRunner
  ├─ Config: archetype A, archetype B, AI profiles, seed range, N fights
  ├─ Runs N deterministic fights
  └─ Reports:
       win rate by archetype
       average fight duration (frames)
       average stamina at fight end
       % of fights reaching Exhausted
       % of fights reaching an environmental throw
       % ending by Rattled vs Composure
       Second Wind trigger rate
       average Adrenaline at resolution
```

## AI profiles (scripted, not learned)

Three is enough:

- **Mauler** — always attacks, never blocks, spends everything
- **Turtle** — blocks and gives ground, only strikes on punish
- **Reader** — attempts reversals, waits for Exhausted before grappling

**Why three profiles matter:** if Mauler beats Reader consistently, your reversal windows or stamina costs are wrong. The profiles are a lie detector for your combat design.

## The loop in practice

```
1. Run gauntlet → get numbers
2. Compare against design targets
3. Change ONE value
4. Re-run
5. Log the change and the result in docs/tuning-log.md
```

**Claude Code drives steps 1–4.** You decide the targets and read the report.

## Design targets to tune against

From the bible, made testable:

| Metric | Target | Bible reference |
|---|---|---|
| Fight duration | 25–60 seconds | "Real fights are short" |
| Fights reaching Exhausted | > 60% | Exhaustion is the real enemy |
| Fights with an environmental throw | 30–50% | Should be earned, not routine |
| Archetype win rates (mirror-adjusted) | 45–55% each | No dominant pick |
| Mauler vs Reader | Reader favoured ~60/40 | Reads should beat mashing |
| Second Wind trigger rate | 20–35% | Comeback, not crutch |
| Fights ending by Rattled | 25–40% | Head damage matters but isn't everything |
| **1v3 win rate** | **2–4%** | The story, protected |

## What the gauntlet cannot tell you

Be clear-eyed. It measures **balance**, not **feel**.

- Whether hitstop lands right
- Whether the camera reads in a corridor
- Whether a throw is satisfying
- Whether the game is *fun*

Those need hands on controllers. The gauntlet gets your numbers into the right neighbourhood so that human playtesting is spent on feel rather than on obvious imbalance.

---

# PART IV — WORKING WITH CLAUDE CODE

## Setup

- Point it at the repo root
- `docs/game-bible-v0.4.md` in the repo so it can read the spec directly
- `docs/tuning-log.md` for the change history

## Prompting rules for this project

**Feed the spec, not the vibe.** "Implement the stamina economy from the table in docs/game-bible-v0.4.md §4.3" beats "make a stamina system."

**One step per session.** The build order above is the session list. Combining steps produces code that compiles and doesn't work.

**Tests first, always.** For a deterministic simulation, tests are the specification. Ask for the test file before the implementation.

**Enforce the assembly boundary.** If Claude Code adds `using UnityEngine;` to anything in `CombatCore`, reject it. That import is the whole architecture leaking.

**Demand verification on Unity APIs.** Model knowledge of Unity 6 specifics can be stale. Ask for confirmation against current Unity documentation for anything you haven't personally used — the same discipline you enforce on Portal work.

## Session template

```
Read docs/game-bible-v0.4.md §[section].
Implement [one system] in Assets/Scripts/CombatCore/[folder].
Constraints:
  - No UnityEngine imports
  - Deterministic: integer frames, seeded random only
  - Data-driven: values from config, not hardcoded
Write the tests in Tests/CombatCore.Tests first, then the implementation.
Do not modify any file outside those two folders.
```

---

# PART V — THE GATE

At the end of Phase 1 you should have:

- A deterministic combat simulation matching the bible
- A gauntlet harness producing tuned numbers
- Two capsules a human can fight with
- A tuning log documenting every change and its effect

**Then answer honestly:**

1. Is a Heavy vs. Scrapper exchange tense?
2. Does the environmental throw feel earned?
3. Do you want to keep working on this?

**If yes → Phase 2.** Networked 1v1, and the engine decision gets revisited: stay in Unity with Mirror/FishNet, or port `CombatCore` to Unreal for native replication and GAS. The port is cheap because the logic is validated and engine-agnostic by construction.

**If no →** you spent 6–10 weeks and know exactly which numbers were wrong. That's a good outcome, not a failed one.

---

# APPENDIX — HONEST CAVEATS

**The gauntlet cannot validate networking.** Client-side prediction under real latency is the project's critical risk and no amount of headless simulation touches it. A smooth Phase 1 is not evidence that Phase 2 will work.

**Replicating GAS is the tractable 70%.** Attributes, effects, stacking, and durations are straightforward. GAS's actual difficulty is its prediction and replication layer, which took Epic years and which you'd be rebuilding from scratch if you stay in Unity for multiplayer. Decide that at the Phase 2 gate with real information.

**Balance ≠ fun.** Perfectly balanced combat can be boring. The gauntlet's job is to remove obvious imbalance so human playtesting can be spent on feel.

**Scope discipline.** No levels, no art, no assets, no menus, no networking in Phase 1. The temptation with fast tooling is to build everything because you can. Resist it — the prototype's value comes from isolating one question.

---

*Companion to Game Bible v0.4.*
