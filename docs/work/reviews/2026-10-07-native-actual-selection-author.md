# Author Explanation

## Intent And Success Criteria

Implement REL-AUD-01 / S4 as the dormant native consumer of the frozen actual-selection
contract. Both original seed1 Normal/NONE opening owners must match every selection event,
Control and complete proof byte. Positive selection stops at Force Assignment20 after defender
decline19; seven fallback variants per owner reach Reserve Release. This packet is author
explanation, not an independent readiness verdict.

## Plan-To-Implementation Traceability

The five accepted primary files were used. Task019F1 in the canonical Combat plan records the
implementation and preserves parent/public/round/result gates. No contract fixture, schema,
oracle, predecessor runtime file or dependency pin was changed. Administrative review/handoff
files were added separately. The original spec/research/dependency disposition remain binding.

## Technical Approach And Flow

ReplayTrustedSource receives full source bytes, the retained creation context, separately
supplied trusted input bytes and a dependency-byte lookup. All16 frozen digests are verified
before admission; no proof/source cache exists. The caller must supply independently retained
bytes and perform any acquisition I/O outside authoritative turns before future activation.
Tests deliberately provide a repository-file lookup. No production caller, seat/controller,
clock-confidence or retained-store authenticator is implemented.

The codec copies/strictly parses the source. Replay owns each input/event, admits the complete
original history via unchanged CampaignCombatPositiveEntryCodec.ReadSource plus replay, rejects
premature11/12 cuts, and derives the actual boundary from the admitted proof and full original
Weather/Breakdown event bytes. It derives initial Control from complete boundary bytes. Each
retained event is regenerated from its independently supplied input and must match exactly.
Embedded event inputs cannot be reconstructed as the trusted ledger.

Apply first owns/validates the current input, fully replays history, and runs the private
transition. Exact command+actor retries return the original owned event and current Control;
valid changed clock/availability cannot renew windows. Accepted apply returns a proof bound to
the resulting source with its new event appended. Duplicate/no-op proofs bind the unchanged
source. Stale timers produce no event/receipt. Positive FA completion rejects007 after clock005.

Framing is the actual-selection source/segment/receipt domain family, with full-event hashing
and existing Sequence prefix-event folding. Route comes from the actual catalog without
materializing activeSide. Owner comes from retained cycle actingSide; defender is the original
opposing participant. Semantic histories and step receipts preserve order. External original
world/history grammar uses the unchanged positive-entry canonical helper; selection grammar
and mechanics remain separate from C3a admission.

## Changed-Component Walkthrough

- CampaignCombatActualSelection.cs owns replay/apply, actual boundary derivation, private
  mechanics, fixed dependency manifest and result objects that return fresh byte/projection copies.
- CampaignCombatActualSelectionCodec.cs owns the closed ordered new grammar, explicit nulls,
  bounds, primitive/closed syntax errors and source/proof/control readers requiring full source
  plus the separate ledger. ReadInput is a syntax decoder only. Internal Transition is a mechanics
  helper over derived facts, never a standalone admission API.
- CombatActualSelectionTests.cs owns independent semantic expectations, all16 frozen traces,
  every cut/next suffix/retry/readback, complete original-history and re-signed leaf mutations,
  owner/clock/combined-error/capacity/grammar/privacy/family/ownership/dependency regressions.
- Cna.Core.Tests.csproj adds only the frozen actual-selection fixture link.
- Combat plan appends Task019F1; prior reconciliation and all exclusions remain intact.

## Decisions And Rejected Alternatives

Use explicit full replay and owned byte/JSON representations consistent with native positive
entry. Avoid a cache entirely: pin-before-cache and key-ownership hazards disappear, at the cost
of repeated bounded replay. Retain the frozen selection mechanism privately instead of widening
legacy seed0/owner-materialized admission or refactoring frozen C3a into a shared kernel. No
new service or public contract is introduced. No fixture is loaded by Core as authority; tests
load the immutable fixture, and dependency bytes are supplied only to verify fixed digests.

## Invariants And Boundary Conditions

Separate trusted ledger is mandatory and count/order must match events. Coherent replacement
of both a source chain and its alleged trusted ledger crosses the caller authentication
precondition; this adapter does not claim to detect that ingress violation. Source digest is
binding, not independent authority. Both admitted sources retain seed1/cursor2, unchanged World,
twelve entry receipts and full original provenance at every selection cut.

Packet1MiB, depth32, ordinary arrays512, selection histories/inputs/receipts16; state version
is checked signed Int64. Strict ASCII carriers, closed fields/effect tags, explicit nullable
arms, canonical key order and spelling are enforced. Duplicate keys, unknown/missing fields,
whitespace/BOM, alternate escapes, float/exponent/bool integers and causal reorderings reject.
All returned projections are fresh and no caller buffer/mutable collection is retained.

Error precedence follows the S3 corrections: dependencies009; primitive/closed001/002; command
version/kind/arms003; segment and System/seat004; command+actor duplicate; stale timer no-op;
closed/capacity/structural/live-state006; owner/participant/candidate004 and choice003; clock005;
positive FA007. Deadlines are exclusive and overflow rejects. State/required decline precede
untimed clock rejection, while positive FA stop follows it.

## Verification Performed And Results

Exact commands, statuses, timings, log hashes, binlogs and RED/GREEN records are in the separate
handoff. The initial stub compile failed analyzer CA1822 and was corrected before the real
semantic RED; that compile failure is not counted as semantic TDD. Semantic RED2 showed13 instead
of14; opening GREEN2 passed. An intermediate GREEN attempt exposed JSON Int32-to-Int64 conversion,
fixed through the numeric helper. Parity21, precedence16 and exhaustive57 subsequently passed.

The self-check range/dependency regression RED ran3:2failed/1passed. Oversized UTC returned001
instead of002; missing dependency propagated KeyNotFoundException instead of009. GREEN3 passed
following bounded fixes. The initial full gate passed60 focused/81 Boundary/2,593 solution tests and format. Packet reconciliation then strengthened every original retry with all three frozen variants plus availability-only inversion (2,616 retry checks total). Native behavior stayed unchanged; final gates were repeated for the final test bytes. Tests compare
immutable literals and never regenerate fixture bytes. Full/Boundary/build/format gate evidence
must be read from the handoff; no unexecuted check is represented as passing here.

The S3 oracle passed with its full counts, including152cuts/1,962retries/4,456event and8,266proof
mutations/1,512entry leaves/6,080state-clock and276public probes. Original positive-entry, C3a,
Round2 and Result2 oracles passed. Original Breakdown, cycle-sequence, inherited Snapshot and
outward outcomes remain separate failures. Synthetic Snapshot composition also passed, which
is a separate observation and does not repair inherited Snapshot. No pin was waived/refreshed.

## Risks, Tradeoffs, And Maintenance Costs

Private mechanics and field inventory duplicate frozen C3a-compatible syntax rather than
extracting a cross-family kernel. Future changes must maintain explicit parity and review
compatibility; whole-source replay costs more than caching. Current bounded focused tests take
about4m06s, dominated by exhaustive leaf mutations. The dependency lookup is part of the dormant
caller contract; future production integration must design retained evidence acquisition and
proper seat/clock/store authentication outside authoritative turns.

## Deviations, Deferrals, And Known Gaps

No heavy pivot or expanded primary scope. README/naming/tech-design were not changed because
this implements the already documented dormant private admission; no architecture, product
names, setup or public workflow changed. No automation, new worktree, host/public/AI/UI adapter,
actual round/result/repeat/all32 reachability, full Snapshot/Archives restart/Runner closure,
publication, CI, merge or independent reviewer was supplied. Historical S1a timeouts remain
unverified observations, and the four historical failures remain failed.

## Challenge Points For The Reviewer

Verify both actual owners and catalog-null actor mapping; full original history and receipt
hash meanings; all16 preflight checks on every entry/retry/readback; separate ledger ownership;
raw grammar versus causal replay; every documented precedence boundary and positive FA stop;
proof identity after accepted apply versus unchanged duplicate/no-op; semantic array order;
mutable result/ledger ownership; and faithful canonical plan/exclusion status. Inspect native
implementation and tests before reading this rationale. No narrower review is requested.
