# THE LIFT — Session Handoff v2

*Supersedes SESSION-HANDOFF.md. Paste this at the start of a new chat along with `game-bible-v0.5.md` and `phase1-unity-gauntlet-plan.md`.*

---

# THE PROJECT

**THE LIFT** — a melee extraction game. Nine people who owe money get sent into a collapsing tower to steal the same thing. The roof door needs both halves of a mechanism. One helicopter comes at dawn with four seats.

Full design in `Docs/game-bible-v0.5.md`. Build plan in `Docs/phase1-unity-gauntlet-plan.md`.

---

# ENVIRONMENT

| | |
|---|---|
| Machine | Windows PC (RTX 4090, 7800X3D, 32GB) |
| Engine | Unity 6 (6000.5.7f1), Universal 3D / URP |
| Project path | `E:\Dev\TheLift` |
| Repo | `github.com/slymonty/TheLift` (private), branch `main` |
| Git | LFS configured, `.gitignore` + `.gitattributes` committed first |
| Editor | VS Code — C# Dev Kit + Unity extensions |
| Build partner | Claude Code (CLI v2.1.228 + VS Code extension) |
| Node | Installed, npm 11.17.0 |
| uv | 0.12.3 |

## Unity MCP — installed and verified ✅

**CoplayDev MCP for Unity v10.1.2**, HTTP transport at `http://127.0.0.1:8080/mcp`.

Claude Code can now create GameObjects, add components, assign Inspector references, and edit scenes directly. **Verified working** — test cube created successfully.

**Operational notes:**
- Server must be running: Unity → **Window → MCP for Unity → Connect → Start Server**
- MCP tools only load if the server was running **before** the Claude Code session started. If tools are missing, restart Claude Code.
- Expect a few seconds of latency per call — Unity processes on the main thread.
- **Constraint: MCP edits are for `Game` assembly scene/prefab wiring only. Never `CombatCore`.**

## Setup gotchas already solved

Recorded so they aren't re-debugged:
- Unity Hub caches PATH at launch — a full quit (including system tray and Task Manager) is needed after installing git
- PowerShell blocks npm scripts by default — fixed with `Set-ExecutionPolicy RemoteSigned -Scope CurrentUser`
- The Claude Code **VS Code extension is separate from the CLI**. The MCP registers against the CLI. Both are now installed.
- `TheLift.slnx cannot be opened` in VS Code is harmless — C# Dev Kit looking for a solution file. Clear it via Unity → Edit → Preferences → External Tools → Regenerate project files.

---

# ARCHITECTURE — the load-bearing decision

Two assemblies with a hard compiler boundary:

- **`CombatCore`** — `noEngineReferences: true`. Pure C#, zero Unity dependencies. All game rules. **Verified: `using UnityEngine;` produces a compile error.**
- **`Game`** — normal Unity assembly, references CombatCore. Presentation only, no rules.
- **`CombatCore.Tests`** — Editor-only NUnit, references CombatCore.

**Why:** headless gauntlet simulation, guaranteed determinism, and a future Unreal port becomes mechanical translation rather than redesign.

**This boundary must never be crossed.** If anything proposes rules in `Game` or Unity imports in `CombatCore`, reject it.

---

# PROGRESS

| Step | Status |
|---|---|
| 1. Assembly definitions + boundary | ✅ Verified |
| 2. Five-state model | ✅ Green |
| 3. Action system + frame data | ✅ Green |
| 4. Impact resolution (5-variable) | ✅ Green |
| 5. Defence (Slip, Cover, Shove, Tie-up, Give Ground) | ✅ Green |
| 6. Grapples (weak/strong, three tiers, reversals) | ✅ Green |
| 7. Compromised verbs (Snag, Cling, Drag down, Post up, Stomp-off) | ✅ Green |
| 8. Archetypes (six stat blocks) | ✅ Green |
| — Frame boundary convention audit | ✅ Green |
| 9. Gauntlet harness | ⬜ **Deprioritized** — see below |
| 10. Unity layer | 🔄 **IN PROGRESS** — scripts written, scene not yet built |
| 11. Feel pass | ⬜ |

**The combat system is functionally complete.** Every rule in the bible exists as tested, deterministic code.

Everything committed and pushed.

---

# IMMEDIATE NEXT STEP

Step 10's scripts exist in `Assets/Scripts/Game/`. The scene has not been built.

The prompt below was written but **not yet run**. It's piece-by-piece deliberately — the Unity layer is the first thing in this project that can't be verified with tests, so eyeball judgments need isolating.

```
The Unity MCP is connected — you can create GameObjects, add
components, and assign Inspector references directly.

TASK: Build and wire the Step 10 test scene using the scripts you
already wrote in Assets/Scripts/Game/.

Do this ONE PIECE AT A TIME and stop after each so I can verify
in the editor before you continue.

PIECE 1 — Scene setup
- Delete the test cube from the earlier MCP verification
- Create a new scene, save as Assets/Scenes/CombatTest.unity
- Flat ground plane, 30x30
- Directional light
- Two capsules named "Fighter_A" and "Fighter_B", 4m apart,
  facing each other
- Distinct materials so they're tellable apart

Stop and tell me when done.

Then, after I confirm:
PIECE 2 — Attach and wire FighterController to both capsules,
assign archetypes (A = Heavy, B = Scrapper), verify Inspector refs
PIECE 3 — Input System wiring for two gamepads
PIECE 4 — Debug HUD showing all five states per fighter
PIECE 5 — Camera with the §4.11 rules

Report concisely. No summaries.
```

**Hardware note:** two gamepads needed, or one gamepad plus keyboard bindings for player two.

---

# CONVENTIONS — BINDING RULINGS

Resolved during the build. These aren't in the bible.

## 1. Light chain hard-capped at 2
Third light within the window is **rejected**, not slowed. `LightComboWindowFrames` = 30, measured from the **end** of the previous light's recovery. After expiry the chain resets to base startup.

*Why: a hard cap gives mashing a defined floor and forces the "now what" moment that pushes players toward grabs and shoves. Soft penalties get ignored.*

## 2. Exhausted uses hysteresis
Flips true at 0 stamina, clears only at **25** — not the instant stamina rises above 0.

*Why: prevents per-frame flickering, which would make the audible-breathing cue useless.*

## 3. Frame boundary convention
A duration of N frames means the state is active on frames **0 through N-1**, and the fighter is free on frame N.

An 18-frame stagger: staggered ticks 0–17, neutral on tick 18. An 8-frame reversal window: input accepted frames 0–7, rejected on 8.

Applies to **all** timed states. Audited and consistent.

## 4. Shove vs Tie-up
Shove **breaks** an active Tie-up immediately, for the fighter who pays. Costs the full 15 stamina **regardless of the opponent's Exhausted state** — the "free against Exhausted" discount applies only in neutral, never inside a clinch. The other fighter gets no free exit, just loses the hold.

*Why: Tie-up is already a stamina contest. A free exit would break it.*

## 5. Time conversion happens in exactly one place
`FighterConfig` converts seconds → frames. **No other code does time math.** Everything counts integer frames at fixed 60Hz.

## 6. Determinism
Integer frame counters only. No `Time.deltaTime` in `CombatCore`. Seeded `System.Random` passed explicitly — never `UnityEngine.Random`, never ambient statics. All randomness takes a seed parameter.

## 7. Data-driven, always
Every tuning value in a config class, never a literal in logic. Tests assert against config values so retuning doesn't break the suite.

## 8. Tests first, every time
Tests are the spec. Run in the open Unity editor via **Window → General → Test Runner → EditMode → Run All** (batch mode is blocked while the editor is open).

---

# OPEN ITEMS

## Adrenaline decay — unimplemented

The bible says Adrenaline "falls only out of combat," but combat state didn't exist when the state model was built, so decay was deliberately omitted rather than inventing a rate.

**Proposed:** "out of combat" = N frames since damage last taken or dealt, ~5 seconds (300 frames). Testable, deterministic, no proximity check.

**Still needs:** a decay rate.

## Gauntlet deprioritized

Step 9 is valuable but is a detour from getting a controller in hand. **Plan: finish Step 10, feel the fight, then come back for the gauntlet.** Tune by feel first; use the gauntlet later to find what feel missed.

---

# BOOKMARKED FOR v0.6

## Vocabulary rename
- **"Grapple" → "Grab"** (weak) / **"Throw"** or **"Shove"** (strong). "Grapple" implies trained technique; these people have none.
- **"Collar drag" → "shirt drag"** or **"haul"**
- Extend the no-move-names rule to internal naming — what you call a thing shapes how it gets animated

## Untrained body language — hard art constraint
- **If a move looks practiced, it's wrong**
- Wide looping swings, off-balance follow-through, recovery that stumbles rather than resets
- Grapples are "grab and hurl," not executed technique
- Competence should feel accidental — a clean slip is the *player* getting it right, not the character being skilled
- **Filtering problem:** commercial packs are choreographed to look skilled. Reject anything reading as trained.

## Sloppy animation sources
- Slow bought clips down, extend recovery, remove snap-back to guard, add overbalance — curve edits in Unity, not Blender
- Prefer drunken/brawl packs over combat packs
- Text-to-motion for connective tissue: stumbling, holding on, grabbing, staggering, carrying

## The disaster needs rethinking
Flagged as the weakest part of the fiction. Questions worth answering:
- Why is the building coming apart *tonight*, the same night three crews are inside? Coincidence is weak. A **scheduled demolition** — charges already placed, a countdown someone set — turns the disaster from weather into a clock.
- Four variants (fire/flood/earthquake/tornado) in one building on different nights reads game-y. Could be different **towers** in the Verge, each failing its own way — or a consequence of the demolition process itself.

## NVIDIA tools — evaluated, deferred to Phase 3

**Kimodo** (text-to-motion diffusion, March 2026) — **the better fit.** Offline authoring: prompt in, joint rotations and root motion out, exported as normal clips. No runtime neural net, so no determinism or networking problem. 700 hours of commercially-friendly mocap. Accepts pose keyframes and end-effector constraints, which *might* make paired attacker/victim clips feasible by constraining both to a shared anchor.
→ One evening in Phase 3: prompt three untrained-brawler clips, see if it produces genuinely amateur motion or just polished mocap.

**MotionBricks** (real-time generative motion, April 2026) — wrong shape for now. Solves locomotion, not paired two-body grapples. Research preview, UE5 demos, needs a Python neural backbone per client (networking/determinism problem). Trained on production mocap, so it fights the untrained-motion constraint. Possible Phase 3+ use: ambient locomotion only.

## Open design questions from the bible
1. Can you Cling while being carried? (Grabbing a doorframe to stop your own rescue — brilliant or griefing vector?)
2. Should the alert name the crew that found the asset?
3. Can you sabotage an asset you've found but not taken — move it, hide it, trap the container?
4. How visible is your own debt to other players? A desperate player is a dangerous player.

---

# CHARACTER ROSTER — LOCKED

Six archetypes, concept art complete, committed to `Docs/concept-art/`.

**Bruiser · Heavy · Agile · Scrapper · Technician · Medic**

Full descriptions and iteration history in `character-concept-prompts.md`.

**Rules established during that process:**
1. **No real trademarks.** Invent company names, band names, everything.
2. **No protected emblems.** Red cross specifically — Geneva Conventions, actively enforced.
3. **Posture carries the character, not wardrobe.** Hands pocketed or hanging, weight uneven, never a confident stance.
4. **One specific object per character** does more work than any amount of costume detail. *(The clogs. The pen. The company patch.)*

---

# HOW TO WORK

## Prompt template for build steps

```
Read Docs/game-bible-v0.5.md §[section].

TASK: [one system]

Constraints:
- No UnityEngine imports in CombatCore
- Deterministic: integer frames, seeded random only
- Data-driven: values from config, not hardcoded
- Namespace: TheLift.CombatCore

[specific requirements]

TESTS FIRST in Assets/Tests/CombatCore.Tests/, then implement.
Report test results only. No summaries.
```

## Efficiency notes
- **"Report test results only, no summaries"** on every prompt — explanatory prose costs real tokens
- **Batch related steps** — one context load instead of two
- Per-file write permissions are off; **git is the safety net**
- **Commit after every step** — keeps the blast radius small
- Report **outcomes**, not transcripts: "all green" or "one failed: [test name, assertion]"

## Division of labour
- **Claude Code** — reads the bible, writes C#, runs tests, drives the Unity editor via MCP. Has the repo.
- **This chat** — design decisions, reviewing outcomes, resolving ambiguity, what's next.

---

# TONE OF THE COLLABORATION

Worth preserving: this design got better through pushback, not agreement.

The salvage-crew premise was **wrong** and got replaced with the debt premise. The first character art was **too clean** and got redone twice. The gauntlet loop got **deprioritized** because it wasn't serving the goal. 6v6 got **cut** on readability grounds. The user pushes back too, and has been right to.

Honest assessment beats enthusiasm. If something in the design doesn't hold up, say so.

---

*Handoff v2. Steps 1–8 complete, all tests green, Unity MCP verified, everything committed and pushed. Next: build the Step 10 scene.*
