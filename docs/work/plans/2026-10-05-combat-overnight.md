# Seven-hour orchestrated Combat session

Status: approved plan retained as history; N0, R1, R2, N1, N2 and N3 accepted. N4 documentation and integrated evidence await fresh independent review and publication. Prepared 2026-10-04 America/Toronto.
Coordinator: 01a0c9dc-00bc-78a3-800d-3cb36859e422 (local).
User authorizes orchestrator-managed child chats, GPT-6.1 medium (high for research/high-effort),
TDD, fresh independent review at every milestone, bounded research/review recovery, and merging
after independent review plus required CI pass. The allocation below is the approved launch plan, not current execution status.

## Execution reconciliation — 2026-10-05

Product work is frozen at merged `e90eef556bde6bbde4fd6b3e17064ba613868ee6`.
R1/PR152 and R2/PR153 delivered reviewed research; N1/PR154 delivered019D3;
N2/PR155 delivered019E0; N3/PR156 delivered019E1. Exact heads, merge commits,
review counts and evidence are in the [session handoff](../handoffs/2026-10-05-overnight-session.md).
The original conditional N2 wording below anticipated selection predecessor replay. Accepted R1
narrowed execution to actual Movement/Breakdown and a supported candidate **before selection**;
019E0/019E1 implement that bounded prerequisite. No C3a/Result2 bridge was delivered.
R2 found no proven cause and made no runtime fix. N2 retains the failed original Breakdown
source-pin oracle, accepted as a pre-existing limitation with separate reviewed maintenance.
N3 pass1 returned Not ready for route ordering; corrected code passed fresh pass2 with
non-blocking follow-ups. No recovery research spikes occurred. N4 may claim completion only
after its own independent Ready verdict and final publication gates. Parent017–019 remain open.

## Verified baseline and evidence

Fetched origin/main is 907f41403ed159d65024f193f3e1f730a23b9bbb, merged PR151.
PR150 merged b908465; PR151 final hosted checks all SUCCESS. Pre-launch primary was f33c78c with user config edits and untracked local tooling/handoff.
Launch successfully fast-forwarded to907f414 with tracked user diff byte-identical; main index refreshed.

Canonical sources (read from fetched origin/main for this plan):
- docs/roadmap/pre-alpha-roadmap.md
- docs/design/combat-cycle-implementation-plan.md (019D0–019D2)
- docs/design/combat-cycle-post-movement-dispatch.md (Following runtime and actual-history sequence)
- docs/specs/combat-settled-control-v1.md (native five-file manifest)
- docs/work/handoffs/2026-10-04-settled-control-contract.md

Git confirms actual delivery sequence, contracts preceding native consumers. Commit/merge intervals
include human waiting, and squash commits hide development start. Therefore estimates combine Git/PR
history with recorded child first-turn duration through handoff, not lines changed or merge spacing.

| Slice | PR / merged commit | Observed task wall time | Scope |
| --- | --- | --- | --- |
| Released-I Movement completion |148 / 1d8de59 |29.5 min |Native adapter + gates/review/PR |
| Settled proof contract |149 / f33c78c |24.4 min |Executable contract + gates/review/PR |
| Native settled proof |150 / b908465 |41.4 min |Native adapter including failed gate/reruns |
| Settled-control contract |151 / 907f414 |28.0 min |Executable contract + gates/review/PR |

Median29 min; sample only four, all reused established patterns and passed first independent review.
PR151 ancestry repair was another2.2 min. These are observations, not promises for novel integration.
Local full-suite observations7.5–9.8min; recent hosted verify roughly5–10min, plus queue variability.
Allow45–80min for next native control and60–110min for less-established contract/native bridges,
including first review/gates. Additional review sets consume the explicit reserve and displace scope.

## Outcome and priority

Target: reviewed, CI-green native settled control; a reviewed decision on smallest genuine positive
campaign entry; and, if that decision establishes a bounded path, its executable contract and native
adapter. Deliver retained evidence and honest remaining gates. This is not a seven-hour promise of
complete Combat, public gameplay, later-II/consumed lineage or hosting.

Committed product truth belongs to specs/roadmap/implementation plan. This ignored .planning file is
an execution ledger, not product truth. Each child commits its review/handoff and relevant canonical
updates on its PR. Coordinator reconciles current states and merge SHAs here.

## Relative seven-hour allocation

| Window | Primary lane | Parallel lane |
| --- | --- | --- |
|00:00–00:20 |N0 baseline, current plan review, dispatch |No implementation before plan pass |
|00:20–01:40 |N1 native settled control, review, CI/merge |R1 positive-entry feasibility,45–60min, high |
|01:40–03:10 |N2 smallest positive-entry executable contract, conditional on R1 pass |R2 Runner aggregation diagnosis,30–45min; reviewed result |
|03:10–05:00 |N3 native positive-entry adapter, conditional on N2 pass/merge |R2 bounded fix only if justified; otherwise evidence handoff |
|05:00–06:00 |Recovery/review/CI reserve; no speculative extra gameplay |Use reserve first for blocking evidence |
|06:00–07:00 |N4 integrated acceptance, reconciliation, final review and morning handoff |Finish/stop remaining lanes cleanly |

Windows are budgets, not forced completion deadlines. Do not wait idle after passing a milestone.
Do not start new behavior after hour6; finish evidence/handoffs rather than publish unverified work.
If review/research exceeds reserve, drop N3 first, then N2. Return a complete reviewed smaller outcome.
Hard end at launch+7h: preserve exact state, stop new dispatch, report incomplete work honestly.
Launch: 2026-10-05T02:47:28Z (Oct4 22:47:28 America/Toronto).
No new behavior after 2026-10-05T08:47:28Z (Oct5 04:47:28 local).
Hard end 2026-10-05T09:47:28Z (Oct5 05:47:28 local).

## N0 — baseline and plan checkpoint (15–20min)

Owner coordinator; reviewer fresh GPT-6.1 high for the integration plan.
Verify current main/PRs, preserve local edits, refresh Codebase Memory, confirm exact-worktree Serena,
record deadline, gate/merge policy and active children. Review this plan against current canonical
requirements before dispatching implementation. Ready gate required. Check credentials through requested
Keychain skill only when performing authorized GitHub operations. No credential extraction.

## N1 — native settled-control adapter (45–80min, medium)

Dependency merged151 satisfied. Freeze task019D3 or next available canonical ID before behavior.
Five primary owned paths from accepted151 manifest:
1. src/Cna.Core/Campaigns/CampaignCombatSettledControl.cs
2. src/Cna.Core/Campaigns/CampaignCombatSettledControlCodec.cs
3. tests/Cna.Core.Tests/Campaigns/CombatSettledControlTests.cs
4. tests/Cna.Core.Tests/Cna.Core.Tests.csproj
5. docs/design/combat-cycle-implementation-plan.md

Acceptance:
- Complete packet through019D1, exact32+4 sources/80 control traces; supported owner/forced/fallback
  outcomes, receipt/hash/source identity, authority/ordinal/prefix and original-byte retry parity.
- Every retained cut, stale/foreign/deadline/clock/canonical/forgery rejection, owned immutable bytes;
  World/resources/RNG/member/Reserve history/future duties preserved, repeat resets only defined fields.
- Repeat enters Movement without executing it; finish enters Truck Convoy without executing it;
  synthetic earlier trust remains explicit. No public/transport/Snapshot or parent closure.

Semantic RED then GREEN. Focused tests, unchanged control/dependency oracles, full required gate,
independent review, final-head CI then merge. Native code never loads fixtures/runs Python for authority.

## R1 — genuine positive-entry decision (45–60min, high; parallel N1)

Decision: which smallest actual creation-rooted Movement/Breakdown/selection history can legally enter
supported positive Combat and eventually Result2? Existing empty inherited selection is insufficient;
synthetic Result2 catalogue cannot be relabelled reachable. Do not assume all32 cases are reachable.
Own docs/research/combat-positive-entry-feasibility.md and a dated docs/work/handoffs artifact only.
Temporary controlled probes allowed in isolated worktree; no retained production behavior changes.

Evidence: exact source graph with coverage/source fallback, canonical rules/contracts and reproducible
probe commands at recorded SHA. Primary authoritative sources if new external rules evidence is needed.
Compare a minimal legal source against rejected candidates; record facts/observations/inferences/unknowns.
Deliver exact source and causal receipts, field/byte mapping, negative tests, <=5-primary-file contract
and native manifests, known missing prerequisites, go/no-go and review verdict.

This is a prerequisite decision, not authority to invent a new gameplay profile. If blocked, retain
reviewed gap/next-step decision and drop dependent N2/N3. Heavy pivots require user sync.
Research review high; no fabricated TDD for prose, but any executable retained probe has semantic tests.

## N2 — smallest positive-entry contract (60–90min, medium; high if evidence warrants)

Depends on accepted R1 and N1 reconciliation. Conditional task, not a pre-certified reachable path.
Freeze exact five-path manifest from R1 before dispatch. Expected shape: new positive-entry spec,
ordered schema, retained fixture, executable oracle, canonical implementation plan. Names/source IDs
are provisional until research proves compatible input/output and coordinator records the decision.

Acceptance: one genuinely reachable minimal profile, both owners when rules symmetric; full actual
Movement/Breakdown/entry/selection predecessor replay and ownership; no forged completed-history or
synthetic-to-actual promotion; literal canonical transitions/all cuts/retries/rejections. Stop at a
bounded accepted positive entry—do not fold the entire result/repeat lifecycle into this task.
Semantic RED/GREEN, unchanged predecessor oracles, appropriate regression gate, independent review,
CI/merge before native consumer. A no-go R1 is a legitimate stop, not permission to invent inputs.

## N3 — native positive-entry adapter (75–110min, medium)

Depends on reviewed/merged N2. Freeze engine/models, codec, focused tests, fixture project link,
canonical plan (five primary files) from accepted contract. Exact source/byte parity, actual causal
lineage, every cut/retry, adversarial history/clock/owner/prefix and immutable ownership tests.
No later-II/consumed or public activation. TDD, focused/oracle/full/Boundary/build/format, independent
review, final-head CI/merge. If contract reveals a separate prerequisite, return bounded gate; do not
rename the task to hide extra scope. Explicitly report which later positive-round/result bridge remains.

## R2 — intermittent Runner failure (30–45min diagnosis, high; optional20–30min fix medium)

Parallel and secondary to critical path. Investigate CertifiedMovementCostPairRetainsRepeatableRouteDivergence
(Succeeded vs AggregationFailed) from019D1; isolated/full reruns passed, root cause unknown. Retain exact
runtime/build/artifact inputs and repeat counts; neither failure to reproduce nor a rerun proves a fix.
Own docs/research/runner-aggregation-failure.md plus dated handoff initially. Bounded experiments,
no simultaneous full-suite load while N1/N3 gate is running. After reviewed evidence identifies cause,
coordinator may assign a <=3-file fix within existing Runner aggregation/test scope, naming exact files
before edits and avoiding N1/N3 ownership. Regression RED must fail for diagnosed cause before fix.
If no reproducible cause, return reviewed evidence/diagnostic next step; no speculative retries/timeouts,
skips or assertion weakening. This task does not block product work absent an actual recurring failure.

## N4 — integration and morning handoff (45–60min, medium)

Verify all intended merged SHAs, combined state, source/fixture integrity, canonical status and remaining
milestones. Run integration-level checks after last merge; reuse unchanged evidence only with explicit
byte/base equivalence and no interacting changes. Fresh final independent review checks the integrated
plan/requirements/handoff, not merely individual PR counts. Review pass needed for completion claim.
Commit any canonical reconciliation on a small docs PR with review/CI; no main-direct commits.
Output merged/open/blocked PRs, exact gates, review ledger, researched decisions, failures/limits,
next task and current main SHA. Keep parent017–019 open unless actual acceptance is independently proven.

## Review/research state machine — user override of default limit

Apply independently to each bounded delivery milestone; not three mandatory approvals and not three
passes for the entire night. Each session/set contains at most3 fresh reviewer passes. Stop early on
Ready (or Ready with genuinely non-blocking follow-ups, explicitly accepted with owner/rationale).
Not ready/Unable to verify blocks dependents/publication until resolved. All gate evidence must be final.

Set1 passes1–3 -> if no pass: research spikeA (high) -> reviewed in-scope revised decision ->
Set2 passes1–3 -> if no pass: research spikeB (high) -> reviewed in-scope revised decision ->
Set3 passes1–3 -> if still no pass: BLOCKED, user sync, no fourth set.

Maximum9 implementation reviewer instances and2 recovery spikes per milestone. This is the user's
explicit replacement for the skill's default3-instance stop; counts never reset on rebase, renamed task,
new child or model upgrade. Earlier milestones' passes do not approve later behavior.
Research spikes retain failing cases, root-cause evidence, alternatives, proposed correction and a
changed plan before another set. Spike result itself needs independent review (max3 passes, no nested
recursive recovery). Inconclusive/unreviewable spike blocks that workstream; no automatic reset.
Heavy architecture/gameplay/scope pivots still stop immediately for human approval per skill. The user
has authorized bounded re-alignment, not arbitrary architecture/profile expansion. Unaffected lanes continue.

Each reviewer is a brand-new chat/task with no inherited implementation history, read-only, neutral
bootstrap first, preliminary ledger before separately reading author explanation. Report both code and
plan findings, exact commands and claim ledger. Author responds Accept/Dispute/Defer with evidence.
Material changes require a fresh pass, counted in the current set. No pass shopping or rubber-stamp reviews.
Ledger per milestone: scope/base/head, set/pass/total, verdict/findings, fix SHAs, spike decision, next gate.
Routine child/reviewer GPT-6.1 medium; research, recovery spikes and high-effort cross-boundary reviews high.

## Coordination and publication

At most two active implementation/research workers plus one reviewer, with this coordinator managing
state. One host-wide full-suite gate at a time; focused checks/oracles overlap only when independent.
Separate managed worktrees, codex/ branches, unique Codebase Memory project, exact-worktree Serena and
coverage/source fallback. Only active primary lane owns canonical plan/roadmap; research writes its own
artifacts. Coordinator explicitly serializes documentation reconciliation to avoid conflicts.

Children report milestones/blockers and send committed handoff to this chat; coordinator may message
children/reviewers to dispatch, clarify, sequence and reconcile as explicitly authorized by human.
Reviewers do not spawn further reviewers or direct implementation. Child plans include scope/acceptance,
protected paths, verification, model, review budget and return packet. PRs attached to originating task
and coordinator. No speculative dependency implementation before prior milestone pass.

Merge authority explicitly granted by user: only after independent pass and required CI pass for exact
final head, no unresolved blocking finding/conflict, own diff/base verified. Remove draft status as needed;
use repository-compatible merge method, no administrator bypass. If protection requires unavailable human
review, mark blocked and continue independent work. Recheck after conflict/behavior changes; prior hashes
alone cannot approve changed code. Use requested github-keychain-auth outside sandbox, no token extraction.
Prefer main-based sequential PRs. If unavoidable stacked work, retain exact dependency and rebase/retarget
only own commits once ancestor merges, protected with exact-head lease; rerun affected gates.

At launch keep orchestrator active to receive authorized handoffs and wait on child tasks with bounded
waits; use app scheduling only if an explicit later wake is needed/authorized. No silent promise that work
continues after ending the session without active children or a scheduler. App/host availability, network,
credentials and account limits are operational prerequisites; record interruption rather than invent time.

## User-approved closeout requirement

All retained work must be committed/pushed and a durable session handoff created before stopping.
At hour6 send every active child closeout instruction. Incomplete work is checkpointed to its own
feature branch with explicit WIP/unverified status, no merge, no Ready claim; do not discard useful
work to make status appear clean. A WIP preservation push is not a passed publication milestone.
Exclude credentials, generated tooling and unrelated user changes. Handoff lists exact branches/SHAs,
remaining gates/failures and next command. Coordinator final handoff also committed/pushed even if
review or CI is incomplete; final handoff verdict accurately distinguishes preserved versus accepted.
