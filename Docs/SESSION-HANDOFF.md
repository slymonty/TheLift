# THE LIFT — Session Handoff

*Written at the end of the design session. Paste this at the start of a new chat, along with `game-bible-v0.5.md` and `phase1-unity-gauntlet-plan.md`.*

---

# WHERE THINGS STAND

## The project

**THE LIFT** — a melee extraction game. Nine people who owe money get sent into a collapsing tower to steal the same thing. The roof door needs both halves of a mechanism. One helicopter comes at dawn with four seats.

Full design in `Docs/game-bible-v0.5.md`. Build plan in `Docs/phase1-unity-gauntlet-plan.md`.

## Environment

| | |
|---|---|
| Machine | Windows PC (RTX 4090, 7800X3D, 32GB) |
| Engine | Unity 6, Universal 3D (URP) template |
| Project path | `E:\Dev\TheLift` |
| Repo | `github.com/slymonty/TheLift` (private), branch `main` |
| Git | Configured with LFS. `.gitignore` and `.gitattributes` committed first. |
| Editor | VS Code with C# Dev Kit + Unity extensions |
| Build partner | Claude Code running in VS Code terminal |

**Working pattern:** Claude Code writes files → Unity auto-recompiles on window focus → Test Runner (Window → General → Test Runner → EditMode → Run All) → commit and push after every step.

## Architecture — the load-bearing decision

Two assemblies with a hard compiler boundary:

- **`CombatCore`** — `noEngineReferences: true`. Pure C#, zero Unity dependencies. All game rules live here. **Verified: adding `using UnityEngine;` produces a compile error.**
- **`Game`** — normal Unity assembly, references CombatCore. Presentation only, no rules.
- **`CombatCore.Tests`** — Editor-only NUnit, references CombatCore.

**Why:** lets the combat sim run headless for the gauntlet loop, keeps everything deterministic, and makes a future Unreal port a mechanical translation rather than a redesign.

**This boundary must never be crossed.** If anything proposes putting rules in `Game` or importing Unity into `CombatCore`, reject it.

## Progress

| Step | Status |
|---|---|
| 1. Assembly definitions + boundary | ✅ Verified |
| 2. Five-state model (Stamina, Balance, Composure, Rattled, Adrenaline) | ✅ Green |
| 3. Action system + frame data (strikes) | ✅ Green |
| 4. Impact resolution (5-variable model) | ✅ Green |
| 5. Defence (Slip, Cover, Shove, Tie-up, Give Ground) | ⬜ Next |
| 6. Grapples (weak/strong, three tiers, reversals) | ⬜ |
| 7. Compromised verbs (Snag, Cling, Drag down, Post up, Stomp-off) | ⬜ |
| 8. Archetypes (six stat blocks) | ⬜ |
| 9. Gauntlet harness | ⬜ *(deprioritized — see below)* |
| 10. Unity layer (capsules, input, camera, HUD) | ⬜ |
| 11. Feel pass | ⬜ |

**Steps 5 and 6 are batched into one session.** Prompt was already given.

---

# CONVENTIONS — RULINGS MADE THIS SESSION

These resolve ambiguities the bible didn't cover. **Treat as binding.**

## 1. Light chain is hard-capped at 2
- A third light within the combo window is **rejected**, not slowed
- `LightComboWindowFrames` measured from the **end of the previous light's recovery**, not its start
- Window = **30 frames**
- After expiry, the chain resets — next light is a new first light at base startup

*Reasoning: a hard cap gives mashing a defined floor and forces the "now what" moment that pushes players toward grabs and shoves. A soft penalty would just get ignored.*

## 2. Exhausted uses hysteresis
- Flips true at 0 stamina, clears only at **25** — not the instant stamina rises above 0
- *Reasoning: prevents per-frame flickering, which would make the audible-breathing cue useless*

## 3. Time conversion happens in exactly one place
- `FighterConfig` converts seconds → frames (e.g. `StaminaRegenDelayFrames`)
- **No other code does time math.** Everything counts integer frames at fixed 60Hz.

## 4. Determinism rules
- Integer frame counters only, no `Time.deltaTime` anywhere in `CombatCore`
- Seeded `System.Random` passed explicitly — never `UnityEngine.Random`, never an ambient static
- All randomness takes a seed parameter

## 5. Data-driven, always
- Every tuning value in a config class, never a literal in logic
- Tests assert against config values, not hardcoded numbers, so retuning doesn't break the suite

## 6. Tests first, every time
- Tests are the spec. Write them before implementation.
- Run in the open Unity editor via Test Runner (batch mode is blocked while the editor is open)

---

# OPEN ITEMS

## Adrenaline decay — unimplemented, needs a decision

The bible says Adrenaline "falls only out of combat," but combat state doesn't exist yet, so decay was deliberately left out rather than inventing an undocumented rate.

**Proposed:** "out of combat" = **N frames since damage last taken or dealt**, probably ~5 seconds (300 frames). Testable, deterministic, no proximity check needed.

**Needs:** a decay rate. Not yet chosen.

## Frame-boundary convention — unsettled

For an 18-frame stagger: is frame 18 the last staggered frame, or the first free one? Currently inconsistent risk across stagger, reversal windows, and combo windows.

**Recommendation:** pick one convention and apply it to every timed state. Settle before Step 6 (reversal windows) or it will cause bugs.

## Gauntlet deprioritized

Step 9 (the headless batch simulator) is valuable but is a detour from getting a controller in hand. **Plan: build Steps 5–8, jump to Step 10, come back for the gauntlet after feeling the fight.**

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
- **Filtering problem:** commercial animation packs are choreographed to look skilled. Reject anything that reads as trained.

## Sloppy animation sources
- Slow bought clips down, extend recovery, remove snap-back to guard, add overbalance — curve edits in Unity, not Blender work
- Prefer drunken/brawl packs over combat packs
- Text-to-motion for connective tissue: stumbling, holding on, grabbing, staggering, carrying

## NVIDIA tools — evaluated, deferred to Phase 3

**Kimodo** (text-to-motion diffusion, released March 2026) — **the better fit.** Offline authoring tool: prompt in, joint rotations and root motion out, exported as normal clips. No runtime neural net, so no determinism or networking problem. Trained on 700 hours of commercially-friendly mocap. Accepts pose keyframes and end-effector constraints, which *might* make paired attacker/victim clips feasible by constraining both to a shared anchor.
→ Worth one evening in Phase 3: prompt for three untrained-brawler clips and see if it produces genuinely amateur motion or just polished mocap.

**MotionBricks** (real-time generative motion, April 2026) — promising but wrong shape for now. Solves locomotion, not paired two-body grapples. Research preview, UE5 demos, needs a Python neural backbone per client (networking/determinism problem). Trained on production mocap, so it fights the untrained-motion constraint. Possible Phase 3+ use: ambient locomotion only.

## Unity MCP servers
Community MCP servers give AI control of the Unity editor. **Not now** — Phase 1 barely touches the editor and adding an unfamiliar integration means debugging two things at once. **Revisit at Phase 3** for level building, prop placement, and hazard volumes, where repetitive editor work is at real volume.

## Open design questions from the bible
1. Can you Cling while being carried? (Grabbing a doorframe to stop your own rescue — brilliant or griefing vector?)
2. Should the alert name the crew that found the asset?
3. Can you sabotage an asset you've found but not taken — move it, hide it, trap the container?
4. How visible is your own debt to other players? A desperate player is a dangerous player.

---

# CHARACTER ROSTER — LOCKED

Six archetypes, concept art complete and committed to `Docs/concept-art/`.

**Bruiser · Heavy · Agile · Scrapper · Technician · Medic**

Full descriptions and iteration history in `character-concept-prompts.md`.

**Rules established during that process:**
1. **No real trademarks.** Invent company names, band names, everything.
2. **No protected emblems.** Red cross specifically — Geneva Conventions, actively enforced.
3. **Posture carries the character, not wardrobe.** Hands pocketed or hanging, weight uneven, never a confident stance.
4. **One specific object per character** does more work than any amount of costume detail. *(The clogs. The pen. The company patch.)*

---

# HOW TO WORK WITH THIS

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
- **Turn off per-file write permissions** (`/permissions` in Claude Code); git is the real safety net
- **Commit after every step** — keeps the blast radius small if something goes wrong

## Division of labour
- **Claude Code** — reads the bible, writes C#, runs tests, fixes errors. Has the repo.
- **This chat** — design decisions, reviewing outcomes, resolving ambiguity, what's next.
- Report **outcomes**, not transcripts. "All green" or "one failed: [test name, assertion]" is enough.

---

# TONE OF THE COLLABORATION

Worth preserving: this design got better through pushback, not agreement.

The salvage-crew premise was **wrong** and got replaced. The first character art was **too clean** and got redone twice. The gauntlet loop got **deprioritized** because it wasn't serving the goal. The 6v6 mode got **cut** on readability grounds.

Honest assessment beats enthusiasm. If something in the design doesn't hold up, say so.

---

*Handoff written at end of design session. Steps 1–4 complete, all tests green, everything committed and pushed.*
