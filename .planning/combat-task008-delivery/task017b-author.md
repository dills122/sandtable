# Author Explanation — Task017B

## Intent And Success Criteria
Connect already settled native Combat to native empty Reserve Release, without manufacturing
positive campaign history. Same32 supported Result2 sources must derive32 exact native bases and
emit64 exact native events. All96 zero/one/two-event native cuts must recover independently.

## Plan-To-Implementation Traceability
Task017B only, rooted in accepted Task016 and017A2. Primary manifest: adapter, narrow Release codec
extension, adapter tests, existing fixture csproj link and root plan. README/tech-design/naming/
roadmap and delivery records are administrative. Parent017 remains open. No fixture, schema or
oracle modification. No CycleControl, Movement, public API, extended Snapshot or publication.

## Technical Approach And Flow
Owned CombatResultReleaseSource retains typed request, Created bytes, independently trusted
synthetic boundary, Selection/Round2/Result2 inputs and canonical event bytes. Each operation fully
replays native Result2; no source cache or imported state object grants authority. Committed-state
hash pins bind32 named canonical upstream histories (28 distinct hashes), including Selection and
Round2 prefixes and input/event receipt chains. A separate kind/choice/actor signature binds native
Result2 command sequence. Typed envelopes close metadata that raw Python predecessor envelopes
could otherwise ignore. This proves canonical authority equivalence, not arbitrary predecessor JSON
byte equivalence. Accepted Result2 effect reasons and exact owner-choice authors/payloads reject
valid fallback history even when final World matches. Reliable Result2 owner retiming is accepted.
Closed Result2 supplies cycle, actual CA receipt, version/prefix, canonical settled World hash, RNG,
selected own unit, spent CP, attack history; pinned profile has CPA10 and singleton none member with
empty scoped history. Release high-water starts null; Result2 audit is retained separately.
Existing native Release kernel performs System open/complete; max2 events, null admitted time,
reliable clock. Completion stays at Release. ReadBase and ReadState validate raw syntax before source
replay and compare entire independently derived bytes. Retry preserves A2 receipt/state behavior.

## Changed-Component Walkthrough
CampaignCombatResultRelease.cs owns bounded source authentication, derivation and adapter APIs.
CampaignCombatReserveReleaseCodec.cs accepts strict singleton settled-empty-release structure and
exposes raw base syntax validation; isolated-ledger validation otherwise unchanged.
CombatResultReleaseTests.cs joins existing frozen native fixtures, verifies literals/cuts/World,
retiming and fallback distinctions, source forgery, raw-first rejection, input ownership and retries.
Cna.Core.Tests.csproj copies existing bridge fixture; tests never invoke Python or regenerate output.
Root plan and project maps distinguish this bounded adapter from later actual campaign admission.

## Decisions And Rejected Alternatives
Reuse A2 native lifecycle rather than duplicate event logic. Pin canonical upstream committed hashes
instead of copying huge fixture payloads into runtime; production loads no fixture. Keep complete
settled World unchanged rather than rebuild partial World from Release fields. Do not cache source
replay before correctness is accepted; repeated replay costs CPU but avoids mutable cached authority.
CPA10 remains profile-pinned literal as canonical bridge specification requires.

## Invariants And Boundary Conditions
No hidden-state external I/O, model calls or new service. All operations deterministic. Owned arrays
protect source bytes. Shared request/boundary values use existing immutable domain types. Upstream
native readers enforce closed/canonical frames and authority bounds. Raw Release limits1MiB/depth32
and tighter native arrays remain. Imported source snapshots, fabricated receipts, changed lineage,
wrong actors, stale commands and nonnative suffixes cannot authorize progress. Every World field,
RNG, resource, guard, entitlement, obligation and attack history remains unchanged across Release.

## Verification Performed And Results
Behavioral RED: compiled skeleton Apply threw Adapter not implemented on exact first event test;
/tmp/017b-red.log. Expanded test compile initially hit CA1869; corrected test serialization, not
behavioral evidence. Initial focused run6/7passed; retimed Round2 test helper used old RoundId.
Rebound round/slot identities to actual replay; focused adapter7 + A1six + A2ten passed23/0/0 in
21.730s, /tmp/017b-focused-2.log. Full gates and review evidence are recorded in task017b-checks.md
as completed; pending checks are not claimed.32bases/64events are frozen exact literals,96cuts are
native invariant/recovery checks, not wrapper-domain expected state hashes.24owner retiming cases,
16custody fallback sources plus8opening fallback sources rejected;8fallback Worlds exactly match.

## Risks, Tradeoffs, And Maintenance Costs
Compatibility catalogue intentionally restricts synthetic sources. Future positive campaign lineage
requires a separate policy/API rather than adding arbitrary sources to this catalogue. Hash pins and
signature strings are reviewable compatibility constants but must move with explicit contract change.
Repeated full source replay has measurable cost; no runtime/public performance claim made. World
hash extraction uses canonical Result2 serializer to keep authority bytes identical to native contract.

## Deviations, Deferrals, And Known Gaps
No change to accepted plan. Native bridge prefix only; synthetic Movement certificate and native
cycle events are outside this slice. Actual held-I no-move precursor/3h then3i and later-II lineage
remain prerequisite work. Parent017,018/019 and020–025 gates remain open. No playable six-turn or
UI-ready milestone is claimed by this adapter alone.

## Challenge Points For The Reviewer
Verify hash-pinned canonical authority reaches every typed predecessor input/event; validate fallback
reason/author checks, genuine retiming acceptance, complete member coverage under pinned sources,
strict codec profile extension and raw-before-source failure order. Review tests for false positives,
wrapper/native hash confusion and actual immutable World preservation. Review plan limits equally.
