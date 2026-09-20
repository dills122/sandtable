# F3 independent review

Review instance: 1 of 3.

## Preliminary ledger — recorded before author/evidence packets

Scope verified: branch `codex/combat-task008-reaction-lifecycle`, HEAD/base `c83d7c4e18ddb03692deec303c66621da839f2f8`; all five `f3-source.sha256` hashes match. Four untracked source/test paths plus modified test project are primary scope. Six supporting documentation/execution paths match bootstrap. No code edits, build, git mutation, or delegation performed.

Independence: no inherited implementation chat; no author packet or prior report read. Narrow CCE queries incidentally returned neutral review2/review3 bootstrap headings and baseline metadata, but no findings or author rationale. Session recall omitted because historical author decisions would compromise requested blind pass. Known-file excerpts followed narrow CCE retrieval because results only supplied truncated symbol excerpts.

Preliminary result: no actionable defect identified. Replay reconstructs actual F1 through lifecycle Replay with empty participant history. Command verifies inactive one-opportunity authority, version 13, exact actor/public capability/action/creation/cycle/position. Single event resumes retained phasing route at version14; exact authenticated retry returns same event. Replay and readback compare canonical emitted bytes, rejecting forged event fields or caches even with refreshed receipt/prefix. Tests exercise six forks, golden trigger/input/event/two cuts, authority and actor negatives, leaf mutations, re-signed event/cache attacks, malformed/capacity buffers, whole-World forgery, buffer detachment, and F2 rejection.

Remaining checks: reconcile retained oracle/source pins and author evidence; distinguish observed test execution from static test coverage; confirm no widened parent/public/clock activation claim. Potential limitation: overflow guard is structurally unreachable on exact version13 authority; do not claim an executed synthetic receipt-capacity test.

## Findings

No actionable findings in frozen F3 scope. Source review covers all three new Core files and entire new test file, fixture registration, F1-to-F2 empty-suffix reconstruction, public-window helper, and expected-World serialization guard. Canonical spec, ordered schema, oracle source-pin/golden verification, retained fixture, implementation-plan F3 gate, and supporting documentation inspected.

## Plan Review

F3 matches `008F3` dependency and acceptance gate: causally F1, sequentially after F2 delivery, both owners × three mutually exclusive direct exits. One close3 transition at 13→14 resumes exact phasing Moving route, retaining World/RNG/resources/tracks/progress and prior receipts. F2 participant events remain excluded by distinct replay suffix and exact event family/input checks. Five-primary-file boundary holds. Supporting README, naming, tech design, roadmap and execution metadata keep F3 in progress and F4–F6/H, parent Task008, runtime/public activation and publication gates open. No rollout or scheduler work belongs in this dormant codec slice. Lead should reconcile final acceptance metadata after required review/integration rounds.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Full F1 history via F2 zero-suffix wrapper | Closure.Replay; Lifecycle.Replay/Initial; Trigger predecessor | Confirmed | No caller cache replaces predecessor; F2 terminal cannot enter |
| Exact actor/action/public handle and competing fork rejection | Closure.Command/Authorize/Apply; ReasonForksPublicAuthorityAndActorsRejectBeforeAndAfterRetry | Confirmed | Actor checked before accepted-input lookup; alternate reason cannot execute sequentially |
| Compatible 24-field close3 and irl receipt | ClosureCodec.SerializeEvent; canonical schema; golden test; Receipt helper | Confirmed | Ordered legacy/suffix bytes and identity matched by frozen golden hashes |
| World/RNG/resource/progress preservation and route resume | Closure.Emit/State; codec; golden before/after field comparison; lifecycle WriteWorld guard | Confirmed | No reactor stop or resource/RNG mutation introduced |
| Six forks, 12 cuts, 30 artifacts | Test case counts and loops; fixture inventory; retained oracle log | Confirmed | Both owner theories each exercise three kinds; five serialized artifacts per fork |
| Canonical/re-signed/cache/buffer defense | CanonicalAndResignedEffectsAndCachesReject; WholeWorldForgeryRejectsAndAllBuffersDetach; strict byte equality | Confirmed | Fresh receipt/prefix cannot authorize altered event effects or history |
| Focused25/build/full format completed | Retained focused/build logs; format empty logs and lead evidence exit0 | Confirmed with provenance limits | Focused25 and build visible as passed; format exit code attested, not independently rerun |
| Full solution test completed | `/tmp/f3-suite.log` at review time | Unverified/pending | Only Contracts project completion visible; lead must retain final outcome |
| Source pins stable | Independent Python SHA256 comparison, all14 paths | Confirmed | Frozen oracle dependencies unchanged |

## Verification Performed

Reviewer executed only read-only checks and wrote this report:

- `git status --short`, `git branch --show-current`, `git rev-parse HEAD`, `git diff --stat c83d7c4`: exact branch/base and manifest/documentation scope confirmed.
- `shasum -a 256 -c .planning/combat-task008-delivery/f3-source.sha256`: all five OK, including repeated end-of-review check.
- `git diff --check`: exit0, no whitespace errors.
- Inline `python3` read fixture and independently SHA256-hashed every `sourceHashes` path: all14 match; six cases and 30 golden entries.
- Inspected `/tmp/f3-reaction-baseline.log`: `CMB-IRC PASS`, 6 traces/events/retries, 12 cuts, 372 mutations, 180 raw, 126 boundaries, 14 pins. Did not rerun oracle.
- Inspected `/tmp/f3-focused-final.log`: 25 succeeded, zero failed/skipped, 35s069ms. Recorded producer command: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatReactionClosureTests' '*CombatReactionTriggerTests' '*CombatReactionLifecycleTests' '/bl:/tmp/f3-focused-final-{}.binlog'`. Did not rerun tests.
- Inspected `/tmp/f3-build.log`: Build succeeded, zero warnings/errors, 3.91s. Recorded command: `dotnet build Sandtable.slnx --no-restore '-bl:/tmp/f3-build-{}.binlog'`.
- `/tmp/f3-format-full.log` and `/tmp/f3-format-verify.log` empty; producer evidence states exit0 for full `dotnet format Sandtable.slnx --verify-no-changes --no-restore` and scoped verification. Reviewer did not independently execute format.
- Full `dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/f3-suite-{}.binlog'` still active when inspected; no full-suite passing claim made.

## Open Questions And Residual Risks

Full integration result remains lead-owned and pending at review cutoff. Exact bounded authority is version13 with small retained history; explicit overflow guard exists but tests do not establish general-envelope capacity/performance. Canonical state layout duplicates predecessor layout intentionally; golden bytes constrain drift, future shared changes must preserve them. No conclusion covers F4–F6, general Snapshot restore, clocks/schedulers, public activation, or publication. No heavy pivot needed.

## Verdict

Ready — frozen F3 implementation and bounded plan gate supported by code, contract, focused tests and retained oracle evidence. This verdict is not full-suite completion or permission to skip lead integration and remaining configured review rounds.

## Recommended Next Actions

Lead retain full-suite result; preserve frozen manifest through remaining required review rounds; then update acceptance metadata. No code correction requested.
