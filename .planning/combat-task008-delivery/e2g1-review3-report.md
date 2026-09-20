# Independent review — E2/G1

Review instance: 3 of 3.

## Preliminary ledger — before author explanation

- Scope verified: branch `codex/combat-task008-movement-lifecycle`, HEAD/base `e641bd3b4b14fd1798f607a44a713df2aba47b54`; all five SHA256 manifest entries match. Four new untracked files plus test project fixture link. Six documentation/planning modifications outside primary freeze noted.
- Canonical lifecycle spec and execution plan require actual E1 Move4 → owner stop → System empty resolution → owner completion. Implementation preserves E1 object, authenticates before retry, compares reconstructed event bytes, captures/restores original context, and creates actual completion receipt before proof.
- Tests inspected before author explanation: eight fixture cases, four cuts each, capability reconstruction, historical retry at later cuts, actor rejection, retained fields, receipt and prefix identities, both-side proof locations and distance2/3 exclusions, malformed histories, scalar/raw mutations and re-signed context/progress/exclusion tampering.
- No actionable defect established. Remaining checks: canonical exclusion predicate and predecessor restriction, inventory/fixture completeness, author claims and recorded verification. No builds/tests run by reviewer.
- Independence qualification: fresh reviewer received neutral bootstrap only. Required CCE search incidentally returned headings and initial scope lines from earlier review reports; no findings/verdicts or report bodies read. Switched to explicit frozen/canonical paths to avoid further report retrieval.

## Findings

No actionable findings within frozen E2/G1 scope.

`CampaignCombatMovementLifecycle.Replay/Emit` reconstruct full E1 history before admitting lifecycle records. Full canonical event comparison rejects altered effects even when receipts are recomputed. `Apply` validates actor and creation/cycle provenance before consumed-version lookup; exact prior input returns original event with current state. Current commands bind audience/version capabilities, distinct from persisted route/stop identifiers.

Stop captures exact original cycle and suspended Movement position. Resolution requires that actual context and pending empty stop, restores Movement, and emits empty checks/lots with unchanged RNG and pinned source. Completion derives sorted locations for both original sides, excludes only own units outside two graph edges of every enemy, installs new receipt in proof, preserves original Reserve receipt and all actual progress references. No synthetic idle authority or new material progress found.

## Plan Review

Canonical execution plan explicitly orders 019A → E1 → joint E2/G1 → G2. Shared route stop/resolution ownership removes circular E/G dependency. Five primary files fit planned packet; existing typed Breakdown route/stop/flow and writers reused. Internal replay/codec seam remains dormant, with rejection by legacy event and Snapshot12 readers tested. Completion ends at Breakdown Determination; G2, Reaction, positive vehicles, later cycles, full restore, production admission and HOST-PUB-001 remain open. No material plan deviation found.

Exclusion implementation privately ports Python cycle-control predicate because no C# reusable kernel exists. Two-layer graph search is equivalent for admitted first-ordinal profile with empty historical exclusion set. Immutable predecessor restrictions establish hard-coded turn/stage/slot and Normal weather assumptions. Extension beyond this profile requires separate work; current plan accurately retains that boundary.

## Author-Claim Reconciliation

| Claim | Evidence inspected | Status |
| --- | --- | --- |
| Complete E1 history mandatory; no fabricated idle | Replay delegates inherited Movement replay; predecessor Initial/Emit guards; missing/reordered/interleaved-history tests | Confirmed |
| Owner/System/owner and identity before retry | Authorize, Apply, Command; actor permutations and changed-identity/capability tests | Confirmed |
| Captured scope, real empty resolution, no World/RNG change | Emit transitions; SerializeEvent; immutable-field comparison at all cuts; canonical golden parity | Confirmed |
| Actual all-unit proof with correct exclusions and separate completion receipt | Emit location projection/WithinTwo; Python cycle-control excluded; fixture rear/supply outcomes; proof assertions | Confirmed |
| No added material progress | Preserved Movement object; SerializeState and completion progress enumerate existing ActualProgressRefs; re-signed progress mutation test | Confirmed |
| 8 traces, 24 events, 32 cuts, 88 artifacts | Fixture-driven test indexes one predecessor, three inputs, three events, four states; canonical fixture completeness guard | Confirmed |
| Focused 20, full 2032, build/format success | Saved logs inspected; build 0 warnings/errors, focused 20/20, full 2032/2032 and no skipped tests | Confirmed as recorded execution; not rerun |

## Verification Performed

Reviewer executed `git status --short`, `git branch --show-current`, `git rev-parse HEAD`, `shasum -a 256 -c .planning/combat-task008-delivery/e2g1-source.sha256` twice, `git diff --check`, and `git diff --name-only -- docs/specs`. Frozen hashes pass; diff check clean; canonical specs unchanged. Reviewed five primary files, canonical lifecycle specification/schema/oracle, cycle-control exclusion function, predecessor guards, successor inventory and execution plan. Reviewed baseline log and final/build/suite/format logs. No builds or tests executed by reviewer, per task boundary.

Recorded verification:

- Canonical oracle: 8 traces, 24 events, 32 cuts, 24 retries, 946 mutations, 152 raw cases, 304 boundaries, 16 pins; `/tmp/e2g1-lifecycle-baseline.log`.
- Focused command recorded: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatMovementLifecycleTests' --filter-class '*CombatInheritedMovementTests' '-bl:/tmp/e2g1-final-{}.binlog'`; `/tmp/e2g1-final.log` reports 20 succeeded, 0 failed/skipped.
- Full command recorded: `dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/e2g1-suite-{}.binlog'`; `/tmp/e2g1-suite.log` reports 2032 succeeded, 0 failed/skipped.
- Build command recorded: `dotnet build Sandtable.slnx --no-restore '-bl:/tmp/e2g1-build-{}.binlog'`; `/tmp/e2g1-build.log` reports success, 0 warnings/errors.
- Format command recorded: `dotnet format Sandtable.slnx --verify-no-changes --no-restore`; empty `/tmp/e2g1-format.log`, exit0 reported in evidence packet. Empty log alone does not independently establish exit status.

## Open Questions And Residual Risks

No blocking questions. Evidence is bounded to accepted first-cycle independent infantry profile. Full-history replay cost and duplicated canonical state layout remain intentional small-profile costs. Array-count rejection is enforced by reconstructed fixed-profile bytes rather than general-purpose array parser; does not grant broader admission. Trusted actor wrapper requires authenticated adapter when production work begins. Saved execution logs are evidence from lead runs, not independently repeated runs. No inference of general Snapshot recovery, combat activation or durable publication.

## Verdict

Ready — frozen E2/G1 implementation and causal plan satisfy bounded acceptance criteria. No experiment or remediation indicated.

## Recommended Next Actions

Lead may finish current delivery and advance separately gated G2 using full retained history. Preserve open parent/runtime/publication gates. Review instance 3 of 3 completes configured loop; no further review instance started or requested.
