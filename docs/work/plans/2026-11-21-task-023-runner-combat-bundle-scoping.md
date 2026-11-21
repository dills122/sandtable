# Task 023: Runner Combat bundle path (T022-Q8 and Q9), scoping (2026-11-21)

**Status:** this is author scoping only. Nothing is built yet and no independent review has run. The
work is test-harness only: it is not Combat activation or certification, and it does not depend on
Task 021.

**Outcome:** stop and report. Resolving T022-Q8 means picking a bundle contract. The repository
already holds two candidates, and they do not agree, so the owner needs to choose (§4).

## 1. What is blocked today

Since 022A–C (#477, #480, #481), the Runner's executor drives a full first-cycle Combat loop. It
does so only through the Runner-internal `CoreExerciseExecutionRuntime.WithCombat` switch. The CLI
and the public `ExerciseExecutor.Execute` still stop at Combat entry. The reason is the bundle:

- After a Combat step, the step's snapshot checkpoint is a Combat state document:
  `CampaignNative*.SerializeStateCanonical`, with about 21 nested shapes from V12 to V28. It is not
  a campaign snapshot V11.
- The bundle reader decodes `final-snapshot.json` through
  `ExerciseEvidenceCodec.DeserializeSnapshot`, which calls `CampaignExercises.ReadCheckpoint`. That
  path accepts V11 only. The writer validates by reading the bundle back, so it fails closed with
  `artifact-failed`.
- Core has **no reader** for any Combat state document. It only has serializers (21 files).

The Harness v1 spec makes the V11 decode a contract:
[exercise-harness-v1](../../specs/exercise-harness-v1.md).

- Reader validation requires that "canonical deserialization of `final-snapshot.json` must yield
  that same position".
- The post-adoption hardening says snapshots are "decoded by Core's complete current snapshot/world
  contract before any executed-or-later success or failure profile is trusted".
- EXR-009, EXR-010 and EXR-012 all name "final snapshot" bytes.
- Change control says: "Any material change requires a spec amendment and review."

## 2. What the plan and existing contracts say 023 is

- [Combined plan](../../design/combat-cycle-implementation-plan.md) task table:
  - `CMB-TASK-023`: "Adopt decisions/terminal in Runner and **strict child bundle readback**."
  - `CMB-CON-006`: "Versioned occurrence/ordinal/position/obligation terminal, checkpoint
    continuation, strict child manifest and parent report, replay/readjudication evidence and
    paired first-divergence semantics. Reject unsupported terminals before running."
- Three **accepted** CMB-CON-006 contracts already exist. Each is "private, unregistered contract
  evidence" with Python verifiers, and none allocates a runtime API or registry label:
  - [B1 occurrence and full checkpoint](../../specs/combat-exercise-occurrence-v1.md), accepted
    2026-09-15.
  - [B2 child evidence](../../specs/combat-exercise-child-evidence-v1.md), accepted 2026-09-16.
  - [B3 parent evidence](../../specs/combat-exercise-parent-evidence-v1.md), accepted 2026-09-18.

B1 and B2 define a different bundle from Harness v1:

| Concern | Harness v1 bundle (Runner today) | CMB-CON-006 B1/B2 |
| --- | --- | --- |
| Final state | `final-snapshot.json`, a campaign snapshot V11 | A "full checkpoint" with its own private digest domain, which "does not mean CoreSnapshot" |
| Terminal | Exact position ID | Occurrence (turn, stage, slot, side, **cycle ordinal**) plus structural position plus closure (`entry`, `completed`, `resumed`, `proof-only`, `phase-finished`, `open`) plus an obligation policy |
| Controller | Policy tokens over kinds and action IDs | Manifest-bound preference profile, e.g. "select Close Assault, decline RBA, full assignment, configured retreat/refusal, guard/escape, inherited release-I, configured repeat/finish" |
| System steps | Plain receipt action IDs | System identity over a closed key (`authorityFamily, scopeId, decisionId, effectKind, reason, disposition`) |
| Hash framing | Harness v1 record hashing | New domains; ASCII domain, NUL, then per record an Int32 big-endian length and the bytes |
| Artifacts | 15-file Harness v1 inventory | Its own `ArtifactManifest`, `run-manifest.json`, framed checks and steps, source and checkpoint records |
| Sources | Live creation-rooted Core sessions | 134 authenticated "native" sources in three execution profiles; synthetic C3 roots allowed |

The B1/B2 sources are an oracle corpus. The real Core Exercise path from 022 is creation-rooted and
does not consume those sources. Mapping B2 onto C# Core is the real design work Task 023 implies.

## 3. Findings

| ID | Finding |
| --- | --- |
| T023-Q1 | **Which bundle contract does 023 adopt?** Option A extends the Harness v1 bundle (§4). Option B implements the accepted B2 child-evidence contract against C# Core. B2 was written for a native oracle corpus, so adopting it needs a mapping decision for each field (source and lineage, controller preferences, System identity), not only a codec. |
| T023-Q2 | **Final-state decoding.** Option A cannot keep the V11-decode rule as written. One choice is to add a Core reader for about 21 nested Combat state shapes. The other is to validate by replay: the reader already replays every executed bundle through Core (`ObserveAuthoritativeReplay` runs reconstruction even for `failed-executed`) and compares hashes. Replay validation is a stronger check than parsing, but the spec text and the hardening clause must be amended. |
| T023-Q3 | **The Q9 opt-in is only loosely coupled to the reader.** `QueryWithCombat`/`SubmitWithCombat` return exactly `Query`/`Submit` outside Combat (a 022A test proves this at every pre-Combat step). The reader and readjudication can therefore always replay through them, for old and new bundles, with no manifest flag. Q9 then only selects the *execution* runtime. Options: (i) always on; (ii) manifest contract v3 field; (iii) controller-policy token; (iv) under B, the B2 manifest's controller-preference record. Under (i), no checked manifest changes behaviour, because they all terminate at or before Combat entry. Only Combat-reaching sweeps such as #448 and `ExerciseRunCoordinatorTests.ANaturallyFailedNoUniqueLegalActionRunProducesAStrictlyReadableBundle` change: they run further. |
| T023-Q4 | **Terminal (T022-Q4).** A position-only terminal is ambiguous inside Combat. For example, Anti Armor and the V22 result share `combat.close-assault`. B1's occurrence/closure terminal is the accepted answer. Option A can postpone it, because the first-cycle loop ends at `truck-convoy-movement`, which is unique. Repeat cycles need it. |
| T023-Q5 | **Maneuver and paired evidence (Task 024).** These consume child bundles through the same reader, so the T023-Q1 choice decides them too. |

## 4. Options for the owner

| Option | What it is | Cost and risk |
| --- | --- | --- |
| **A. Harness v1 amendment (smallest)** | `final-snapshot.json` holds "the final step's Core checkpoint bytes". For a Combat checkpoint, the reader validates by authoritative replay rather than by a V11 decode: bytes must equal Core's reconstruction, and the position comes from the replay checkpoint. The reader always replays through `*WithCombat`. Q9 is always on (or a manifest v3 flag). The position terminal stays, adequate for the first-cycle loop. | Runner-only code plus a Harness v1 spec amendment (EXR-009/010/012 wording and the hardening clause). It does not implement CMB-CON-006's occurrence terminal or B2's child format, so 023 is not "done" per the plan; it is an interim step toward B. |
| **B. Adopt B2 child evidence** | A new Combat bundle profile per B1/B2: full-checkpoint domain, occurrence terminal, preference controller, System identity key, B2 framing and artifact inventory, mapped onto creation-rooted C# Core. | Matches the plan's 023. It is large: it needs a mapping spec for B2 fields that assume oracle sources (source-prefix lineage, synthetic C3 roots, the 134-source registry), plus a Core checkpoint record. That mapping needs owner rulings. |
| C. Separate Combat checkpoint artifact in Harness v1 | Keep `final-snapshot.json` as V11 (frozen at Combat entry) and add a new Combat-checkpoint file anchored by the step evidence. | Bundle schema change with a stale "final" snapshot. It has neither A's simplicity nor B's conformance. Not recommended. |

**Author recommendation:** A now, with Q9 always on, as a clearly labelled interim Harness v1
amendment. That makes the CLI and Combat sweeps (#448, and #446-class coverage) work quickly. Then
plan B as the real 023 once the owner rules on the B2 mapping. A changes no Core bytes and no
frozen Combat contract.

## 5. If the owner picks A: bounded slice 023A

1. **Spec:** a Harness v1 amendment draft covering the final-snapshot wording, the replay-anchored
   Combat checkpoint rule, and the always-on Combat legal-action surface. It is marked as an interim
   step toward CMB-CON-006.
2. **Runner reader** (`ExerciseBundleSemanticValidator`, `ExerciseEvidenceCodec`): accept a
   non-V11 final checkpoint only when the authoritative replay reconstructs it byte-for-byte. Take
   the facts from the replay checkpoint, and replay through `*WithCombat`.
3. **Runner execution:** the default runtime becomes the Combat-aware one. `WithCombat` and the
   `includeCombat` overloads are removed.
4. **Tests:**
   - a CLI bundle for a full first-cycle loop, read back strictly;
   - tamper cases: rehashed final checkpoint, truncated events, swapped Combat step;
   - the `failed-executed` bundle at a Combat frontier;
   - the existing coordinator regression, updated to read back the further-run bundle;
   - every checked manifest still byte-identical.

Q9 (opt-in) as a manifest field would be a separate commit or PR, if the owner prefers (ii) over (i).
