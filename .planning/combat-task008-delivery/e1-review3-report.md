# Task008 E1 independent review

Review instance: 3 of 3.

## Preliminary ledger (before author/evidence)

- Branch `codex/combat-task008-inherited-movement`, HEAD `ba58c430efe692d9acacaee5eef9c9b6d827aad4`; five primary source hashes verified. Working-tree scope includes four untracked implementation/test files, test fixture registration, execution notes and synchronized product/plan documents.
- Read canonical inherited Movement requirements, complete new implementation and tests, existing ordinary spending kernel, and E/G plan delta before author explanation. Initial evidence supports creation-rooted replay, both-owner literal golden parity, exact retries, strict canonical effect comparison, CP14 limit and DP causes. No actionable defect identified in preliminary pass.
- Follow-up checks: predecessor immutability/authority and structural World equality underpin writer guard; successor inventory must justify shared E2/G1 ownership; reported tests must match frozen source.
- Independence disclosure: no implementation conversation inherited. Required CCE recall returned older project decision summaries. CCE search unexpectedly exposed introductory snippets from E1 review1/review2 and e1-evidence, plus unrelated old review summaries; these were not opened or relied on. Targeted CCE did not retrieve implementation bodies, so exact named files were inspected directly for frozen-source review.

## Findings

No actionable findings. No code or contract changes requested.

- `CampaignCombatInheritedMovement.Replay` delegates predecessor admission to actual Reserve-opening replay, then re-emits each Move4 and compares complete canonical bytes. A re-signed receipt or matching cache cannot substitute for effects derived from retained inputs.
- `Apply` authenticates owner, original UnitKey and creation/cycle/position before occurrence lookup. Exact earlier retries return original accepted event with current reconstructed state; conflicting occurrence reuse and duplicate retained events reject.
- `Emit` checks origin, adjacency, occupancy, ordinary NONE infantry, current integer ledger, closed World obligations and enemy adjacency before effects. Existing terrain kernel supplies Clear2; existing ordinary spending kernel enforces ceiling15 and incremental excess-CPA DP. Event receipt is computed before its causal DP record, without cyclic hashing.
- Current element and independent representation move together. Member CP, route, chronological revisits, receipts, prefix and material progress append together. Route identity stays anchored at first movement. Immutable copied collections and parsed strings prevent caller byte-array mutation from rewriting returned state.
- `CampaignCombatInheritedMovementCodec.WriteWorld` reconstructs expected World from actual opening and typed accepted events. `CampaignWorldSnapshotV7.Equals` compares every World collection; typed resource/DP forgeries cannot pass through hardcoded absent fields. General initial World and Snapshot readers stay unchanged.

## Plan Review

Refinement is faithful to original E/G responsibilities and frozen successor inventory. Original inventory assigns `element-movement-stopped`2 and `breakdown-stop-resolved`2 to Breakdown; actual lifecycle contract requires owner stop, System resolution even with zero cohorts, then owner Movement completion3. Joint E2/G1 packet removes coarse parent dependency ambiguity while preserving each causal obligation. G2 separately consumes actual Movement-end proof and advances Breakdown Determination to Combat entry.

E1 supports ordinary Move4 only. Parent E/G remain open; F Reaction, H full restore, positive vehicles/Reserve movement and runtime/public activation remain separate. Five-primary-file cap retained. README, technical design, naming and roadmap describe same boundary. HOST-PUB-001 durable publication/recovery proof remains explicitly open. No heavy pivot or new approval gate warranted.

## Author-Claim Reconciliation

| Claim | Evidence inspected | Status | Consequence |
| --- | --- | --- | --- |
| Full creation-to-opening authority; no caller base | Movement Replay/Initial, ReserveOpening.Replay, canonical oracle initial/replay | Confirmed | Closed predecessor boundary preserved |
| Both owners, 14 moves, 16 cuts, 48 artifacts | Two theory rows; per-owner request/predecessor + eight state + seven input + seven event fingerprint assertions; focused log | Confirmed | Literal external parity, not serializer self-roundtrip alone |
| CP2..14 and two causal DP records | Emit, event arithmetic, ChargeOrdinary, golden/literal assertions | Confirmed | Eighth move rejected by ordinary ceiling |
| Exact retries and canonical tamper rejection | Apply ordering, whole-event/state comparison, retry loops and raw/re-signed tests | Confirmed | Cached hashes confer no authority |
| Bounded World guard and buffer ownership | ExpectedWorld, structural Equals, typed-forgery and caller-buffer tests | Confirmed | No general World restoration claimed |
| Shared E/G lifecycle ownership | Canonical plan diff, successor inventory and lifecycle/completion contracts | Confirmed | Deferred work remains explicitly owned |
| Focused31/full2020/build/format evidence | Retained logs named below | Confirmed for visible logs; format exit status supplied by lead | Review did not rerun integration gates |
| Earlier independent reviews Ready | Author evidence mentions earlier verdicts | Not relied upon | Current verdict independently derived |

## Verification Performed

Reviewer executed:

- `git status --short`, `git branch --show-current`, `git rev-parse HEAD`: scope and base match bootstrap.
- `shasum -a 256 -c .planning/combat-task008-delivery/e1-source.sha256`: all five OK before and after review.
- `git diff --check`: pass, including final check.
- `git diff --name-only -- docs/specs`: empty; canonical oracle/schema/fixture unchanged.
- Read frozen new files, test fixture registration, canonical Movement specification/schema and oracle initial/emit/replay/apply/read_state, existing spending/Reserve-opening/World-equality code, relevant lifecycle/inventory/completion requirements, and documentation deltas.

Retained executed evidence inspected, not rerun by this reviewer:

- `/tmp/e1-movement-baseline.log`: Python oracle PASS, 2 traces/14 moves/16 cuts/14 retries/384 mutations/66 raw/81 boundaries/18 source pins. Reported command: `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-inherited-movement-v1.py`.
- `/tmp/e1-final.log`: 31 succeeded, 0 failed/skipped. Reported command: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatInheritedMovementTests' --filter-class '*CombatReserveOpeningTests' '-bl:/tmp/e1-final-{}.binlog'`.
- `/tmp/e1-build.log`: build succeeded, 0 warnings/errors. Reported command: `dotnet build Sandtable.slnx --no-restore '-bl:/tmp/e1-build-{}.binlog'`.
- `/tmp/e1-suite.log`: 2,020 succeeded, 0 failed/skipped, all three test assemblies passed. Reported command: `dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/e1-suite-{}.binlog'`.
- `/tmp/e1-format.log`: empty, consistent with successful quiet format check; lead records exit0 for `dotnet format Sandtable.slnx --verify-no-changes --no-restore`. Empty log alone cannot independently prove exit status.

No build, test rerun, experiment, source edit, staging, commit or delegation performed. Only this report written. Existing passing focused and root integration evidence plus unchanged frozen source made repeated execution unnecessary.

## Open Questions And Residual Risks

No blocking open questions. Bounded writer duplicates finite World layout, and event math duplicates fixed-profile arithmetic; literal external fixtures and structural guard constrain drift. Repeated replay adds bounded work; seven legal moves do not establish general performance/capacity claims. Typed internal projection constructors are not general untrusted-state admission APIs; trusted entry remains full-history Replay/Apply/ReadState.

No proof supplied or implied for route stop/completion, positive Reaction/vehicle/Reserve profiles, later cycles, general Snapshot12, public command registration, durable publication or process recovery. These remain planned gates.

## Verdict

**Ready** for frozen E1 ordinary Movement packet and documented E/G execution refinement. No blocker requiring lead-owned conditional experiment or final re-review.

## Recommended Next Actions

Lead may close E1 review evidence and package authorized branch work. Continue next planned E2/G1 only through actual stop/resolution/completion history. Review instance 3 of 3 consumed; no further independent review instance started or requested.
