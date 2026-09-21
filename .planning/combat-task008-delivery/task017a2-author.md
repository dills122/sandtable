# Author Explanation — Task017A2

## Intent And Success Criteria
Execute the dormant isolated Reserve Release lifecycle after accepted A1. Opening, canonical
choices, one deadline, conversion/retention fallback and explicit completion must match frozen
Release v1 bytes and reconstruct through separately trusted history. No public gameplay activation.

## Plan-To-Implementation Traceability
Exact four source/test paths plus root plan per task017a2-dispatch.md. No schema/fixture edits.
44isolated traces cover132event hashes/176state hashes and lengths/two terminal event literals.
Four historical Result1 rows explicitly excluded. Native empty bridge017B and actual positive
lineage, World projection, Movement/cycle control, Snapshot and publication remain separate.

## Technical Approach And Flow
Apply accepts independently retained typed base/request and separately admitted accepted inputs/events.
Replay validates owned event bytes before context, serializes typed inputs, validates initial base,
reconstructs initial state and reexecutes each input, comparing complete event bytes. Transition is
private; no caller-provided state authorizes actions. ReadState rejects raw syntax before context,
then compares all canonical bytes with replay-derived state. Retry is command+actor before stale
and terminal checks. Duplicate returns original receipt and no new event. Obsolete timers are NoOp.

## Changed-Component Walkthrough
Models add immutable command/input/disposition/state/result values, closed effects and owned state
collections/output bytes. Codec retains A1 base implementation, adds frozen command/input/event/state
writers and closed in-code shape inventory. Kernel owns deterministic transitions, pinned timing,
fallback and receipt/history projection. Tests use a bounded independent base recipe with existing
fixture links; expected golden hashes are outputs only. Status docs separate implemented from accepted.

## Decisions And Rejected Alternatives
No weakening A1's initial history checks for converted-II or pending next-Movement state. Lifecycle
state is validated by reconstruction instead. No generic state-machine framework, JSON-derived
authority, production fixture loader, World duplication or state mutation. The bounded in-code
shape inventory reuses existing primitive checks and permits raw-before-context recovery rejection.

## Invariants And Boundary Conditions
Own fixed queue; converted members cannot reenter. Config deadline never renews; equality rejects
owner input. Clock loss preserves authenticated input actor while event author becomes System.
Fallback locks first mandatory conversion or later bulk retention. Empty queue still needs opening
and explicit completion. Completion stays at Release. Event receipt precedes derived history; prefix
includes signed event bytes. Preserve retained World hash/RNG/CP/CPA/attack history. Pending next-
Movement exception grants no Movement or cost waiver. Event34/member32/byte1MiB/depth32 bounds and
version/ordinal overflow reject atomically. Owned result exposes copies; no remote I/O.

## Verification Performed And Results
Meaningful RED: compiling no-op skeleton failed first-I opening (expected open, got unopened).
Focused parity test passed all44/132/176/2. Negative test then found numeric effect tag raised
InvalidOperationException; explicit ID validation fixed this to JsonException before context.
Focused lifecycle10 plus existing A1six passed16/0/0 in3.366s before final formatting.
Scoped format passed after approved IPC execution; diff check passed. Full build/tests/Boundary/
format and exact CI pending at author freeze; check ledger owns eventual results.
Initial sandboxed dotnet test and format failed on local IPC permissions; approved retries used.
One intermediate test compile syntax error corrected before behavioral runs; not counted as RED.

## Risks, Tradeoffs, And Maintenance Costs
Isolated base is explicitly trusted; candidate-derived expected base defeats that trust boundary.
No actual campaign provenance from hashes. Replay-on-apply is bounded to34events and prioritizes
simple recovery correctness over caching; no runtime activation performance claim. Closed raw
shape inventory duplicates wire schema intentionally, protected by frozen fixtures and negatives.
Test-local base construction duplicates short A1 helper to avoid unrelated extraction.

## Deviations, Deferrals, And Known Gaps
Full A2 intended scope implemented; no A2 acceptance until full gates and three fresh sequential
reviews (third full rebuild). Native adapter and positive predecessor remain unimplemented. No
transport or protobuf change, no World7 weakening, no full six-turn or playable loop claim.

## Challenge Points For The Reviewer
Reconstruct scope before author packet. Check timing at equality/regression/overflow, retry ordering,
original actor vs fallback author, historical and converted-state validation separation, no caller
state authority, complete replay byte equality, collection ownership and capacity. Verify frozen
fixture parity counts and exclusions; isolated proof must not become parent017 acceptance.
