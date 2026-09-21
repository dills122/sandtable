# Task017A2 independent review

Review instance: 2 of 3.

Base: `32a038c322c561f642af2aedafe37165147f4939`.
Candidate: `a4b4f630d1250fd00d61ebae486f18813ffedd01`.
Reviewer clone: `/tmp/sandtable-review2-a4b4f6`, detached at candidate, clean after checks.

## Findings

No actionable findings. Review covers implementation and bounded execution plan.

Blind pass read neutral bootstrap, repository guidance, canonical Task017 refinement, dispatch, frozen spec/schema/oracle, tests, four source/test changes and surrounding cycle/configuration contracts. Preliminary ledger persisted before author explanation. No prior review/check reports, memory, CCE, implementation conversation, extra agents or workstreams used. No source changes or commits made.

`CampaignCombatReserveRelease.Replay` validates copied event bytes before trusted context, validates initial base/request, reexecutes admitted inputs and compares complete event bytes. `Apply` cannot receive caller-supplied state as authority. `ReadState` compares complete canonical replay state, so a matching superficial hash cannot replace replay. A1 base implementation remains unchanged; current converted-II/pending-exception states receive replay validation instead of invalid initial-base rules.

`Transition` preserves frozen oracle ordering and semantics: authenticated actor/command checks before retry, original receipt return before stale/terminal rejection, fixed opening queue/deadline, equality rejected for owner input, unavailable/regressed clock fallback, canonical first-I conversion, later-II bulk retention, accepted-choice preservation and explicit completion. Original input actor persists when fallback event author is System. Event receipt is derived before receipt-linked history; prefix includes final event. Result bytes/state collections are owned. CP, retained World/RNG and attack history remain unchanged; no cycle/World mutation added.

## Plan Review

Exact four-file material manifest respected; remaining seven changed paths are stated administrative/documentation scope. No frozen fixture/schema, public registration, transport or unrelated source changes. Implementation matches 017A2 acceptance targets and dependency ordering after accepted A1. README, design, naming and roadmap distinguish implementation from acceptance and isolated evidence from actual campaign lineage.

Native retained Result2-to-empty Release adapter remains 017B. Positive predecessor/Release provenance, later-II authentic lineage, World projection, Movement, cycle control, Snapshot/durable publication and public admission remain explicit future gates. Parent017 remains open. No rollout/migration gate is missing for this dormant private kernel; activation/recovery obligations remain in later plan tasks. Replay-on-apply cost is bounded by 34 events and 32 members; no production performance claim made.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Four source/test paths; no fixture/schema edits | Frozen Git diff and dispatch | Confirmed | Scope stays bounded |
| 44 isolated traces,132 event hashes,176 state hashes/lengths,two terminal literals; four historical rows excluded | `FortyFourTracesMatchEveryFrozenEventStateAndTerminalLiteral` and executed focused suite | Confirmed | Native parity demonstrated within stated isolated scope |
| Independently retained base/request and admitted inputs authorize replay; no imported state authority | `Replay`, `Apply`, `ReadState`, tampering tests | Confirmed | Trust boundary preserved |
| One budget, deterministic locked fallback, original actor, explicit completion | Transition branches, frozen oracle, clock/deadline tests | Confirmed | Lifecycle matches contract |
| Atomic capacity/overflow and immutable outputs | Post-transition serialization, version/ordinal guards, owned collection/result accessors, boundary test | Confirmed | No partial immutable result published on rejection |
| Meaningful earlier RED and intermediate numeric-effect fix | Current regression test covers numeric effect; historical runs deliberately not inspected | Historical sequence unverified | Not counted as reviewer evidence; current regression passes |
| Full solution gates and exact CI pending at author freeze | Packet explicitly scopes these as pending | Not independently rerun here | Root/reviewer3 retain full-suite/CI ownership; no claim of passing gates here |

## Verification Performed

All commands run with `login:false` in detached candidate clone.

- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatReserveRelease*'`: first sandbox run terminated exit134 because .NET MTP local named-pipe bind failed with `System.Net.Sockets.SocketException (13): Permission denied`. Environment failure, not test failure.
- Same exact command retried with approved escalation: exit0, **16 passed,0 failed,0 skipped**, duration3.676s. This includes ten lifecycle tests and six unchanged A1 tests; 44/132/176/2 assertions passed.
- `python3 docs/specs/verify-combat-reserve-release-v1.py`: exit0, **13 literal cases/48 side-slot traces;188 cuts,2368 mutations,840 raw rejects,20 timing and27 boundary checks**. Python includes historical contract traces; it does not expand native 44-trace provenance claim.
- `git diff --check 32a038c322c561f642af2aedafe37165147f4939 HEAD`: exit0, no output.
- `git rev-parse HEAD`: exact candidate above. `git status --porcelain=v1`: no output after checks.

Both reviewer-owned processes completed. No full-solution build/test/format or CI result claimed by this review.

## Open Questions And Residual Risks

No blocking questions. Independent expected base and trusted input admission are caller responsibilities; deriving either from untrusted persisted candidate defeats trust model. Closed in-code shape inventory duplicates frozen wire inventory, with parity/mutation tests mitigating drift. Isolated synthetic receipt/history seeds do not prove authentic campaign lineage. Broader delivery gates remain root-owned.

## Verdict

**Ready** for bounded Task017A2 isolated lifecycle. This verdict is an independent review result, not parent017 completion, public gameplay approval or a substitute for remaining full-solution/CI gates.

## Recommended Next Actions

Complete remaining prescribed full-solution/Boundary/format, third fresh review and exact-candidate CI gates before marking A2 accepted. Retain explicit 017B and positive-lineage exclusions in acceptance evidence.
