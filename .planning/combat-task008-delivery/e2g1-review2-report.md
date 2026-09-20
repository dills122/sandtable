# Independent review — E2/G1

Review instance: 2 of 3.

## Preliminary ledger (before author packet)

- Frozen five-file hashes verified; branch `codex/combat-task008-movement-lifecycle`, HEAD/base `e641bd3b4b14fd1798f607a44a713df2aba47b54`. Four new untracked source/test files plus fixture-link csproj change; documentation edits outside frozen implementation scope remain visible.
- Read canonical lifecycle specification/schema, predecessor specification, implementation/tests, causal E1→E2/G1→G2 plan, and canonical baseline result before author explanation. No previous review reports read.
- No demonstrated defect. Full predecessor replay, exact event-byte comparison, actor/provenance authorization before retries, captured Movement scope, typed retained Breakdown flow, and immutable inherited state are explicit in source.
- Checks requiring final reconciliation: exclusion BFS versus canonical distance predicate; first proof receipt distinct from Reserve receipt; both-owner literal golden coverage; no-build/full-gate provenance and exact frozen scope.
- Arrays and malformed cache/event effects fail exact reserialization/replay comparison; bounded supported predecessor plus suffix prevents reaching a 512-item accepted-state boundary. Public runtime registration, positive Breakdown cohorts, Reaction, G2, and general Snapshot restore remain excluded.

## Findings

No actionable findings within frozen E2/G1 scope.

`CampaignCombatMovementLifecycle.Replay` reconstructs creation through actual E1 moves before deriving each lifecycle event; full serialized event equality prevents self-consistent forged receipts/effects from becoming authority. `Apply` validates actor and creation/cycle identity before looking up consumed occurrences; exact retry returns original event plus current replayed state. Fresh commands must equal current typed command, including audience capability, action, version, and position.

Stop uses existing `CampaignBreakdownStop.Create`, retains actual route, and captures cycle plus suspended position. System resolution consumes actual stop/context and restores Movement with unchanged RNG, empty checks/lots, and exact rule sources. Completion derives all original-unit locations, preserving both sides, then selects only own exclusions beyond two edges. `WithinTwo` BFS matches canonical cycle-control predicate for this fixed Content7 graph and empty prior exclusions; first-scope constants are justified by E1 admission (`CampaignCombatInheritedMovement.Initial`). Proof uses newly emitted completion receipt; inherited Reserve completion ID remains unchanged.

## Plan Review

Ready for bounded E2/G1 delivery. Causal order `019A → E1 → E2/G1 → G2` fixes ownership dependency correctly: mandatory Breakdown resolution occurs before Movement completion, while G2 separately consumes actual completed proof for Combat entry. Five primary files suffice; no contract mutation, synthetic idle/proof, or added material-progress event. Existing typed route/stop/flow reuse is appropriate. Retaining inherited `Movement` object preserves World, DP causes, resources, members, tracks, progress, and opening authority.

Parent E/G, Reaction F, full restore H, positive vehicles/Reserve exceptions, later cycles, and HOST-PUB-001 remain open. No plan pivot needed. Documentation's current “in progress” status is consistent with review-stage work; lead owns final completion/status reconciliation and full integration gate.

## Author-Claim Reconciliation

| Claim | Evidence | Status |
| --- | --- | --- |
| Complete predecessor history, three-event real lifecycle | `Replay`, `Emit`, canonical `_emit`, missing/reordered/interleaved-history tests | Confirmed |
| Owner/System/owner capabilities and authenticated retries | `Command`, `Authorize`, `Apply`; independent capability/action assertions; later-cut retries and altered identities | Confirmed |
| Exact retained bytes, captured interrupt, actual proof | Frozen schema, codec writers, eight golden traces; coherent context/progress/exclusion mutations | Confirmed |
| Full inherited state and old receipt retained, no extra progress | `SerializeState`, `ImmutableFields`, prefix/receipt assertions, completion progress writer | Confirmed |
| Two-layer exclusion port adequate for admitted profile | Canonical `excluded`/distance predicate, BFS, both-owner distance2/distance3 golden cases | Confirmed |
| 20 focused tests pass; build clean | Existing final/build logs and independent focused run below | Confirmed |
| Full repository integration complete | Later lead update and inspected `/tmp/e2g1-suite.log`: 2032 passed, zero failed/skipped | Confirmed retained execution evidence; not independently rerun |

## Verification Performed

- Independently executed `shasum -a 256 -c .planning/combat-task008-delivery/e2g1-source.sha256` before/after review: all five files OK.
- Independently executed `git diff --check`: exit 0.
- Confirmed SDK-style net10.0, native Microsoft.Testing.Platform, xUnit v3 MTP; installed SDK 10.0.400.
- Independently executed `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatMovementLifecycleTests' --filter-class '*CombatInheritedMovementTests' '-bl:/tmp/e2g1-review2-{}.binlog'` with approved escalation for known local IPC requirement: **20 passed, 0 failed, 0 skipped**, exit 0, 17.598s. Binlog: `/tmp/e2g1-review2-20260920-000829--69402--lNlWo1-dotnet-test.binlog`.
- Inspected existing `/tmp/e2g1-final.log` and `/tmp/e2g1-build.log`: focused20 pass; solution build zero warnings/errors. Reviewer performed no build.
- Inspected unchanged canonical source/schema and `/tmp/e2g1-lifecycle-baseline.log`: 8 traces,24 events,32 cuts,24 retries,946 mutations,152 raw,304 boundaries,16 source pins. Did not rerun canonical oracle.
- Final lead update reconciled against `/tmp/e2g1-suite.log`: 2032 passed, zero failed/skipped, 3m25.998s. Format log is empty; exit0 remains lead-reported rather than derivable from empty log alone.

## Open Questions And Residual Risks

No blocking question. Repeated replay and explicit inherited serialization are bounded implementation costs; extending admission requires revisiting first-scope constants, historical exclusion union, exception expiry, capacity, and positive cohorts. Current tests establish closed profile only. No live host, general persistence, or public authentication claim follows. No previous review reports consulted; author explanation read only after preliminary ledger.

## Verdict

**Ready** — frozen bounded E2/G1 implementation and causal plan supported by source, canonical byte goldens, negative tests, and independent focused execution. Full-suite evidence subsequently confirmed by inspected log.

## Recommended Next Actions

Lead completes integration gate and status reconciliation, retains this exact frozen scope for delivery, then advances separately scoped G2 from actual lifecycle history. No corrective implementation work requested.
