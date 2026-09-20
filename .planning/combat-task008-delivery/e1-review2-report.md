# Task008 E1 independent review

Review instance: 2 of 3. Frozen working tree on codex/combat-task008-inherited-movement, HEAD ba58c430efe692d9acacaee5eef9c9b6d827aad4. Five primary source hashes match e1-source.sha256.

## Preliminary ledger (before author/evidence)

- Canonical movement spec/schema require completed creation-rooted Reserve opening, both resolved owners, seven Clear moves, CP14/Cohesion-4, actual receipt-linked DP, ordered revisits and moving flow. Source implements Replay → actual ReserveOpening.Replay → Initial → Emit, exact event-byte comparison; no cache authority.
- Tests reconstruct real predecessor events, compare 48 golden fingerprints over both owners, inspect all 16 cuts, exact retry at every later cut, eighth-move ceiling, unsupported weather/Reserve/reaction and malformed bytes. Typed forged World tests exercise opponent CP/location, own ammunition and missing causal DP.
- Preliminary concern checked: World writer hardcodes absent fields. ExpectedWorld reconstructs typed World from opening/events and compares structural equality before emission; inspect predecessor initial-profile guarantees before verdict.
- Preliminary concern checked: retry is authorized before occurrence lookup, but authorization intentionally ignores current origin/head; exact accepted input equality supplies original occurrence binding. This permits legitimate old retries while preventing actor/provenance substitution.
- Plan changes preserve E/G parent responsibilities, explicitly share stop/resolution lifecycle, retain mandatory empty-cohort System resolution and keep positive vehicles, Reaction, restore and HOST-PUB-001 open.
- No actionable defect established in first pass. Remaining checks: predecessor/content restriction, canonical oracle source, author evidence reconciliation and proportionate focused execution.
- Independence: no prior E1 reports opened. Required CCE recall exposed unrelated historical review summaries; search exposed a brief future E2/G1 scope snippet, not prior E1 review findings. Neither used as correctness evidence.

## Findings

No actionable findings. Reviewed implementation and execution refinement; no fixes required by this pass.

Evidence: CampaignCombatInheritedMovement.Replay/Apply/ReadState always reconstruct full ReserveOpening history; Initial requires completed first opening, ordinal/turn/stage/slot, Normal Weather and NONE status. Emit checks owner/original UnitKey, provenance, current origin, adjacent featureless Clear destination, occupancy, independent representation, current ledger/member agreement and enemy adjacency before projection. CampaignCombatCreationContext validates Content7; ContentPackV7Validator retains selected two-infantry topology/profile, and CampaignWorldV7Factory establishes absent resources and initial CP0. Reserve designation preserves that initial World; positive designation is rejected by E1. These predecessor guarantees support bounded writer's absent fields.

Event receipt is derived before ChargeOrdinary receives it. ChargeOrdinary applies ordinary CPA10 ceiling15 and incremental excess DP; projected World includes resulting causes, element/representation locations, CP/Cohesion and preserved resources. ExpectedWorld re-derives all accepted inputs, checks retained event bytes, and WriteWorld requires full structural World equality (CampaignWorldV7.cs:157–163), including opponent state and absent-resource collections. No initial writer broadening.

Strictness follows exact comparison against regenerated canonical bytes; unknown/duplicate/reordered fields, malformed values and re-signed effects cannot become authoritative. Retry scans accepted occurrences only after authorization, requires complete input equality, returns original event with current reconstructed state. Track/route/progress agreement comes from immutable history-derived construction. Structural 32-event bound and per-record 1 MiB bound precede replay; positive profile rejects eighth 2CP move.

## Plan Review

Ready for E1 only. Canonical plan's E1 → joint E2/G1 → G2 order matches movement, movement-lifecycle and breakdown-completion contracts. Frozen successor inventory assigns stop/resolution to Breakdown; shared ownership retains that obligation while avoiding circular parent completion. Actual owner stop2 → System empty-cohort resolution2 → owner Movement completion3 is still required before System Breakdown completion2 reaches Combat entry. No fabricated idle or completion inferred from per-move null fields.

Parent E/G, Reaction F, full restore H, later/public gameplay and HOST-PUB-001 remain open. Five-primary-file limit respected: three production files, tests and fixture-link project file. README, tech-design, naming overview, roadmap and execution index accurately describe dormant movement and remaining gates. No wire/oracle/fixture changes or general Snapshot12 admission.

## Author-Claim Reconciliation

| Claim | Inspected evidence | Status / consequence |
| --- | --- | --- |
| Two owners, 14 moves, 16 cuts, 48 frozen artifacts | FrozenMovesPreserveResourcesAndProveCumulativeOrdinaryCost; fixture fingerprint comparisons; reviewer 31-test run | Confirmed; exact current C# byte parity |
| Current World/member/route/progress preservation | Emit; full state fingerprints; ImmutableFields; route identity, CP/Cohesion, causal receipt assertions | Confirmed within frozen selected profile |
| Real receipt-linked DP and ceiling15 | Event.ReceiptId; Emit ordering; ChargeOrdinary; DP cause assertions and eighth-move rejection | Confirmed |
| Bounded World guard prevents resource erasure | ExpectedWorld; structural World equality; typed opponent/ammo/missing-DP forgery tests | Confirmed |
| Exact retry, strict bytes and cache rejection | Apply/ReadState; retries at every later cut; mutations, re-signing and buffer tests | Confirmed |
| No public activation, stop/end proof or durable publication | New files internal; unchanged public serializer rejects Move4; plan/docs preserve gates | Confirmed |
| Focused31, root build/full gate | Reviewer independently 31/31; inspected /tmp/e1-final.log, /tmp/e1-build.log and /tmp/e1-suite.log | Focused confirmed independently; root build/full 2020 retained logs corroborate author |
| Format passes | Author evidence reports exit0; git diff --check independently passes | Format not independently rerun, per review scope |

## Verification Performed

- `shasum -a 256 -c .planning/combat-task008-delivery/e1-source.sha256`: all five match at start and after checks.
- `git branch --show-current`, `git rev-parse HEAD`, `git status --short`: stated branch/base and explicit untracked implementation/test delta confirmed.
- `git diff --check`: exit0.
- `dotnet --version`: 10.0.400. global.json native MTP and SDK-style xUnit MTP project checked using run-tests/platform-detection/binlog-generation guidance.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatInheritedMovementTests' --filter-class '*CombatReserveOpeningTests' '-bl:/tmp/e1-review2-{}.binlog'`: sandbox attempt exit134 before tests, local IPC Permission denied; same authorized command outside sandbox exit0, 31 succeeded, 0 failed/skipped, 6.978s. Successful binlog `/tmp/e1-review2-20260919-234608--66788--k2q8ir-dotnet-test.binlog` exists.
- Canonical unchanged oracle source initial/authorize/_emit/replay/apply/read_state inspected alongside schema and /tmp/e1-movement-baseline.log: retained Python result 2 traces,14 moves,16 cuts,384 mutations,66 raw,14 retries,81 boundaries,18 source pins. Not rerun; no concern requiring extra oracle execution.
- Retained root logs: build 0 warnings/errors; full suite 2020 succeeded,0 failed/skipped. No reviewer build, full-suite rerun, implementation edit, commit or delegation.

## Open Questions And Residual Risks

No blocking questions. Typed internal state constructors are not external restore boundaries; authority remains full-history Replay/Apply/ReadState. Bounded World serialization repeats at most seven legal moves and duplicates finite selected-profile layout; this does not prove general performance/capacity or future-family handling. Broader vehicle/Reaction/Reserve/later-cycle coverage and general Snapshot12, authenticated public dispatch and actual durable publication remain separately gated.

After preliminary ledger, e1-evidence.md exposed a one-line prior-round Ready statement. No prior review report read; statement excluded from verdict evidence. CCE exposure noted above likewise excluded.

## Verdict

**Ready** — bounded Task008 E1 implementation and E/G execution refinement meet inspected requirements with independently passing focused tests. This verdict does not close Task008 parent or other open gates.

## Recommended Next Actions

Lead retains this report, finishes configured review sequence and integration checkpoint. Continue joint E2/G1 only from accepted E1 history with actual mandatory lifecycle events. No new review instance or experiment started by this reviewer.
