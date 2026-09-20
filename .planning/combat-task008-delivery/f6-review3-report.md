# F6 independent review

Review instance: 3 of 3. Base and HEAD: `6dd60cf565f51020cafaf1b07abca3bb6703f759`.
Branch: `codex/combat-task008-reaction-lifecycle`. Five frozen primary files plus six
supporting documentation changes reviewed. No implementation conversation or earlier reports read.
CCE returned incidental earlier contract handoff and G2 metadata snippets, not earlier F6 opinions.
CCE expansion failed with `Chunk not found`; exact retrieved paths read directly afterward.

## Blind preliminary ledger

Recorded before author explanation, retained gate evidence, or session recall.

- No actionable implementation defect found in initial static pass. Three explicit transitions
  authenticate owner/System roles, compare exact derived commands, replay predecessor authority,
  and retain event bytes for terminal retries. Events and caches are compared against recomputation.
- Golden test covers both owners, 22 artifacts and eight cuts; independently reconstructed public
  capability/action/stop hashes supplement fixture comparisons. Mutation tests cover event and
  command leaves, raw cache leaves, resigned events, history, capacities, typed World, and buffers.
- Need retained full gate/baseline evidence and source binding before final readiness judgment.
- Plan accurately limits F6 to dormant exact-profile completion; full restore, activation, vehicles,
  broader opportunities, publication and Task008 parent remain open. Pending review status is proper
  before lead acceptance. No plan dependency defect found.
- Residual inspection target: inherited F5 World validation and canonical input parser must justify
  F6 reuse, especially typed guard and strict JSON behavior; inspect before final verdict.

## Findings

No actionable code or plan findings. Inherited F2 parser checks depth32, 1MiB, array512,
variant tags and exact input reserialization; F6 replay then compares whole event bytes against
newly emitted bytes. F5 World writer recomputes `ExpectedWorld` before writing, and F6 additionally
checks its World equals predecessor World. This resolves both blind-ledger inspection targets.

## Plan Review

Ready for bounded `008F6` acceptance. Canonical `3Q-AC-01`–`06` map to actual predecessor replay,
public capability and independent identity checks, three mandatory event successors, all four
state cuts per owner, material retention, exact route restoration, rejection/retry/capacity tests,
and synchronized scope documentation. F5 is correct causal dependency; H restore and broader
Tasks009–025 remain explicitly pending. No new transport, activation, persistence provider,
migration, or rollout obligation is introduced by internal dormant codecs. Existing publication
obligation remains under HOST-PUB-001. Lead should reconcile pending status only after acceptance.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Consequence |
| --- | --- | --- | --- |
| Actual F5 terminal15, no caller cache authority | Completion.Replay/ReadState; F5.Replay; history/cache tests | Confirmed | No synthetic predecessor accepted through operational API |
| Three distinct successors16/17/18 | Completion.Emit; golden theory and canonical schema/oracle | Confirmed | Empty stop cannot be skipped; only close resumes phasing |
| Public window13/opportunity15/stop16 distinct from authority | Codec helpers; independent preimage theory | Confirmed | Frozen capability and action domains match |
| Owner/System authorization before retry lookup | Completion.Apply/Authorize; command actor and terminal retry tests | Confirmed | All accepted inputs retry safely at terminal |
| World/tracks/progress/RNG unchanged; typed World guarded | Codec.WriteWorld/F5 guard; every-cut golden and ownership tests | Confirmed | Bounded writer cannot silently discard altered typed World |
| Two traces, six events, eight cuts,22 artifacts | Golden inventory assertions; independently rerun oracle | Confirmed | Canonical evidence reproduced |
| focused24 and full gate | Retained raw focused/build/full/boundary logs; lead evidence | Confirmed with provenance limitation below | Existing execution supports readiness; reviewer did not run .NET |
| Full gate/reviews pending in author packet | Later evidence and raw logs | Superseded | Packet is deliberately preacceptance; no implementation discrepancy |

## Verification Performed

Reviewer executed:

- `shasum -a 256 -c .planning/combat-task008-delivery/f6-source.sha256` twice: all five OK.
- `git status --short`, `git diff --stat 6dd60cf`, supporting-document diff,
  `git branch --show-current`, `git rev-parse HEAD`: scope and base matched bootstrap.
- `git diff --check`: exit0, no output.
- `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-inherited-reaction-movement-completion-v1.py`:
  exit0; 2 traces,6 events,8 cuts,6 retries,506 mutations,54 raw cases,72 boundaries,18 source pins.

Reviewer inspected retained execution output, did not rerun these commands:

- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatReactionCompletionTests' '*CombatReactionSecondMoveTests' '-bl:/tmp/f6-final-{}.binlog'`:
  `/tmp/f6-final.log` reports24 passed,0 failed/skipped,2m22s661ms (11 F6 +13 F5 from test inventory).
- `dotnet build Sandtable.slnx --no-restore '-bl:/tmp/f6-build-{}.binlog'`:
  `/tmp/f6-build.log` reports success,0 warnings/errors,4.10s.
- `dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/f6-suite-{}.binlog'`:
  `/tmp/f6-suite.log` reports2,104 passed,0 failed/skipped,4m51s848ms.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace' '-bl:/tmp/f6-boundary-{}.binlog'`:
  `/tmp/f6-boundary.log` reports81 passed,0 failed/skipped,10s140ms.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`:
  `/tmp/f6-format-full.log` empty; exit0 attested by lead retained evidence and direct message.
  `/tmp/f6-format-verify.log` likewise empty. Empty output alone does not prove process exit status.

## Open Questions And Residual Risks

- No blocker or unresolved correctness question within exact frozen profile.
- Reviewer did not rebuild, run .NET tests, or independently reproduce format process exit. Retained
  logs plus source manifest/lead execution chain provide evidence, not independent binary provenance.
- Canonical exact-profile assumptions (one ordinary infantry reactor, no remaining move, empty
  vehicle cohorts) must not be generalized without new contracts/tests. Internal Command/state
  constructors are not public arbitrary-state admission interfaces; Apply and ReadState replay.
- Repeated creation-rooted replay and packet-local canonical writer duplication remain bounded
  maintenance costs; this review establishes no runtime throughput or durable recovery claim.
- Required recall happened after blind ledger. It returned earlier contract/history decisions.
  `f6-evidence.md` later incidentally exposed earlier rounds' Ready summaries; no earlier review
  reports read, and preliminary judgment was already recorded before those summaries.

## Verdict

**Ready** — code and plan. Review instance3of3 complete. Lead owns acceptance and publication.
No blocker justifies conditional deep experiment or fourth review. No further review spawned.

## Recommended Next Actions

Lead reconcile F6 accepted status and exact gate counts, preserve frozen review evidence, then
continue existing authorized delivery workflow. No implementation correction requested.
