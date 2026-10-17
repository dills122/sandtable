# Author Explanation

Review instance: **10 of 10**.

## Intent And Success Criteria

The frozen Breakdown Source cap is 256 MiB; prior structural arithmetic permits 257,781,248 bytes but says nothing about native memory. The change tries to establish a conservative tighter **byte** model by replacing the moved Assault event cap of 65,536 with a schema-shaped event maximum, then propagating through unchanged successor wrappers. Success here means a reviewer can validate every reachable event and array count. It does **not** mean capacity closure, public release, or native resource safety.

## Plan-To-Implementation Traceability

The capacity packet called for a grammar and transition proof before treating observed fixture sizes as bounds. The calculator is a first auditable implementation of that proof, and the packet identifies the remaining ancestry and native profiling work. No frozen contract or runtime behavior changed.

## Technical Approach And Flow

The calculator reads the moved Assault schema's `objects` and `effectTags`, recursively constructs the largest encoded event shape for Selection, Round and Paid, prices scalar characters at six escaped bytes, includes optional fields as populated, and requires an explicit cardinality for every reachable array. It verifies the existing moved Assault generic recurrence, substitutes the computed maximum event bytes, then applies five downstream `2 × parent + wrapper addition` recurrences from frozen schemas. The resulting provisional Breakdown Source model is 207,807,680 bytes, 60,627,776 under the frozen 268,435,456-byte cap.

## Changed-Component Walkthrough

`docs/specs/verify-combat-actual-moved-repeat-breakdown-event-bound-v1.py` owns the estimator and fail-closed array registry. `docs/specs/test_combat_actual_moved_repeat_breakdown_event_bound_v1.py` pins the result, detects new unbounded array fields and effect arms, checks the pinned content's one-component participants, and checks six-byte ID escaping. The capacity packet records calculations and limitations.

## Decisions And Rejected Alternatives

An earlier ASCII-only estimator yielded 151,226,560 bytes but could undercount escapes in inherited codecs; it was rejected. A hand-maintained effect-arm list was replaced with `effectTags`. The generic 512-item `Participant.componentIds` grammar is not used for a *reachable* event because reaction-entry certification binds the candidate to the pinned content's two one-component elements; this ancestry is the most important independent-review claim. If it fails, the same calculator yields a 4,506,960,064-byte model.

## Invariants And Boundary Conditions

All emitted moved events must be covered, including unobserved paid choice/custody variants. Optional fields must be priced at maximum non-null size. Canonical JSON cannot exceed the six-byte-per-character allowance. Every nontrivial array cardinality must follow from a codec bound and authoritative transition, not an observed fixture. The parent Source is still bounded by 1 MiB, and each successor's wrapper addition must retain its frozen schema semantics.

## Verification Performed And Results

Genuine TDD: both initial tests failed because the verifier did not exist; after implementation, a deliberately more conservative escape model caused the initial 40,000-byte target to fail; the test was corrected to the actual frozen-cap criterion and exact calculated values. Four current Python tests pass. The calculator emits event maxima 33,645 / 28,259 / 47,377 bytes. The existing structural capacity verifier passes. `git diff --check` passed. The focused native command `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class Cna.Core.Tests.Campaigns.CombatActualMovedRepeatBreakdownCompletionTests` passed 38/38 locally in 5m20s after a first sandboxed attempt failed to write ordinary build artifacts. This is authentic fixture replay, not near-bound or hosted resource evidence.

## Risks, Tradeoffs, And Maintenance Costs

The array registry is explicit and maintainable but hand-audited. `Participant.componentIds=1` is reachable-state reasoning, not a standalone grammar limit. The model may overestimate drastically, so it does not identify a near-limit authentic input. It does not measure full replay peak RSS, latency, or hosted four-CPU/16-GiB behavior. Those remain required gates.

## Deviations, Deferrals, And Known Gaps

The earlier 40,000-byte event target was superseded by 47,377 after safe string escaping. No new native near-bound replay, hosted CI, contract freeze, publication, or merge occurred. The exact proof that every listed array bound follows all accepted transition branches is still pending independent review.

## Challenge Points For The Reviewer

First trace `Participant.componentIds` from the reaction-entry certificate through `BindParticipant`, moved selection, round opening and paid admission. Then inspect all event unions and array fields against runtime codecs and transition branches; verify the five wrapper recurrences and whether arbitrary IDs/strings could escape more than six bytes per UTF-16 code unit. Distinguish an all-valid-Source byte bound from a native resource certificate.
