# F4 independent review

Review instance: 2 of 3.

## Blind preliminary ledger (persisted before author/evidence/recall)

Scope: HEAD f0cb5ca9aa256eb330a46c0ff86fe8f326174d3c on codex/combat-task008-reaction-lifecycle; four untracked implementation/test files and tracked csproj match all five f4-source.sha256 entries. Supporting documentation changes match declared boundary.

Inspected canonical specification, schema, oracle initial sections, complete three implementation files, focused tests, implementation-plan F4 row and dependency refinement. No actionable defect established. History replay reconstructs F2 first move, closes into recorded stop, and resumes only after mandatory resolution. Authorization precedes cached retry. Canonical byte reconstruction rejects malformed or re-signed event/cache mutations. Tests cover four forks, exact artifact hashes, both-owner retries, whole-World retention, detached buffers and explicit limits.

Open checks: confirm predecessor's full World validation, oracle stop construction, author claims and retained execution logs. Plan correctly keeps F5/F6/H and public/durable activation open.

Independence: no author packet, evidence file, session recall, or previous review report read before this ledger. Parent bootstrap already stated expected scope and passing focused/build/format outcomes; these were not treated as execution evidence. Narrow CCE query incidentally returned snippets from f4-dispatch.md, f4-scope.md and old contract-planning findings.md; none contained review verdicts. CCE initially provided only compressed snippets, so exact known new-file paths were read for full review.

## Findings

No actionable findings. Final inspection confirmed stop construction matches canonical oracle, and inherited WriteWorld checks complete history-derived World equality before emitting bounded projection. No unsupported state admission was introduced.

## Plan Review

F4 satisfies its defined causal slice: F2 first-participant state14, either System close3 at15, mandatory resolve2 at16. Implementation-plan lines812/827 correctly distinguish F2 causal prerequisite from F3 delivery order. Five-primary-file budget met; README, roadmap, naming and technical-design changes accurately describe current progress and stop semantics. F5/F6/H, public admission, scheduler and HOST-PUB-001 remain open. Keeping F4 in-progress until lead acceptance is correct. No migration, deployment, or runtime scheduling change belongs in this dormant adapter.

## Author-Claim Reconciliation

| Claim | Evidence inspected | Status / consequence |
| --- | --- | --- |
| Actual first participant history admits four owner/reason forks | Replay calls F2 lifecycle replay, requires one event/state14/active sole opportunity/open route/one remaining move; fixture has4 cases and32 goldens | Confirmed; no caller-state admission |
| Close records mandatory stop, resolution resumes exact phasing route | Emit; codec event branches; oracle initial/_emit; StopsPreserveMaterialAndResumeOnlyAfterMandatoryResolution | Confirmed; no early resume or extra closure |
| Public window13 and stop15 differ from persisted IDs | WindowCapability reuse, StopCapability, independent identity assertions | Confirmed |
| System authorization precedes cached retries | Apply→Authorize before Events lookup; both-owner actor/fork/terminal retry tests | Confirmed |
| World/RNG/progress and history retained | State delegates immutable predecessor material; SerializeState→F2 WriteWorld guard; fieldwise preservation, forged World and detached buffer tests | Confirmed |
| Strict canonical and re-signed forgery rejection | Input canonical byte equality; Replay compares complete emitted event; ReadState compares complete replay-derived state; every-leaf raw/re-signed tests | Confirmed |
| Focused29/build/full-format passed | Focused/build logs independently read; full-format log empty, exit0 attested by lead evidence | Focused/build confirmed; format exit accepted as retained lead evidence, not independently rerun |
| Broader runtime/public/full-restore claims excluded | Internal types only; explicit old-reader negative assertions; plan retains future gates | Confirmed |

## Verification Performed

Read-only commands executed: git status --short; git diff --stat f0cb5ca; git diff f0cb5ca over supporting docs/csproj; git branch --show-current; git rev-parse HEAD; shasum -a 256 -c .planning/combat-task008-delivery/f4-source.sha256 twice (all5 OK); Python fixture inventory and SHA256 verification (4 cases,32 artifacts,17 source pins match current source). Reviewed canonical spec/schema/oracle and known source/test files, narrow CCE symbol searches, and required session_recall after blind ledger.

Retained execution evidence inspected, not rerun:

- `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-inherited-reaction-active-fallback-v1.py`: /tmp/f4-reaction-baseline.log reports PASS,4traces/8events/12cuts/8retries/612mutations/200raw/132boundaries/17pins.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatReactionFallbackTests' '*CombatReactionLifecycleTests' '*CombatReactionClosureTests' --no-restore '-bl:/tmp/f4-focused-{}.binlog'`: /tmp/f4-focused.log,29passed/0failed/0skipped,1m38s722ms.
- `dotnet build Sandtable.slnx --no-restore '-bl:/tmp/f4-build-{}.binlog'`: /tmp/f4-build.log,0warnings/0errors,4.05s.
- `dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/f4-suite-{}.binlog'`: /tmp/f4-suite.log now reports2,080passed/0failed/0skipped,3m13s731ms.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: /tmp/f4-format-full.log empty; lead evidence records exit0.

No build, test rerun, git mutation, implementation edit or delegation performed. Only this report written.

## Open Questions And Residual Risks

No blocking open question. Review establishes dormant, bounded F4 behavior, not public integration or durable recovery. Repeated predecessor replay and packet-local canonical writers remain deliberate bounded costs. Source hash checks tie review to frozen files; retained test logs are execution evidence from lead rather than reviewer-generated runs. Required post-ledger recall returned prior contract decisions. Author evidence incidentally included prior review1 Ready status; no earlier review report was opened, and blind ledger was already persisted.

## Verdict

Ready — F4 implementation and plan supported by canonical parity, adversarial coverage and retained successful suite/build evidence. This verdict covers explicit working-tree F4 boundary over f0cb5ca only.

## Recommended Next Actions

Lead completes configured remaining review/acceptance and updates delivery status before publication. Preserve F5/F6/H and HOST-PUB-001 as open gates. No code remediation or extra review instance requested.
