# Task010B author explanation

## Intent and plan
Base6e6dd6f. Implement frozen C3a timed selection, explicit defender decline and cancellation as dormant trusted-facts mechanisms. Canonical010A/B/C refinement governs scope:010A actual empty histories accepted;010C cumulative actual routing remains next. Positive history authentication, Prepared011, public actions, extended Snapshot12 and publication remain excluded. Parent010 cannot close from this child.

## Approach and flow
Five primary files: immutable models; replay/state machine; strict typed codec; narrow existing IdentityCodec external-syntax bridge; CombatStepsTests. Replay captures owned Created before caller list access, bounds paired input/event lists, validates accepted event grammar, then validates independently trusted Boundary against retained Created/request and009B profile certification. Every accepted event must equal private Transition output for the separately supplied trusted Input. Untrusted event.input cannot supply authority. Apply first replays complete local history, then applies a fresh input; no caller Control authorizes a transition.

Full frozen C2 request is pinned by derived creation binding, while Created is independently read against that request. Current World is exactly certified initial profile except permitted integral current CP. World writer serializes retained initial baseline and maps only validated current CP; no initial-world reader accepts arbitrary current JSON. Completed Breakdown/Weather/prefix inputs remain independently trusted facts, not self-authenticating hashes.

Clock and lifecycle follow historical C3a policy exactly. Command hash identifies retry; recorded actor is checked separately. Changed valid timing/availability cannot alter retained reply. Primitive validation precedes retry; stale System callbacks become no-ops. Selection/RBA windows retain deadlines and accepted high-water. Player expiry/unavailability rejects; valid System fallback closes/cancels. Cancellation retains selection audit evidence without manufacturing a decline or spending resources. Selected progression stops at Force Assignment; empty/cancelled paths structurally traverse six steps.

## Codec and invariants
Every mandatory field, nullable value, effect tag and schema property order stays frozen. Whole-value bounds1MiB/depth32/arrays512 and paired accepted-history cap16 fail closed. Readers validate raw syntax/canonical bytes before trusted semantics and whole-value comparison. C3a preserves all arrays; named bridge reuses existing external descriptors with preserve-order mode while default inherited ordering remains unchanged. No schema/oracle/fixture change or generic serialization framework added.

Models/control receipts and returned event bytes own storage. Replay retains current state on exact retries/no-ops and returns original owned event bytes for retries. Every accepted event advances version/prefix once. World/RNG/CP/TOE/ammunition remain unchanged. Task009B result certification is reused rather than reimplemented.

## Evidence and limits
Exact final commands, counts, failures, source hashes and root gates belong in task010b-checks.md after freeze. Five original synthetic traces contain41 literal events and five literal final Controls; intermediate Controls are derived and replay-checked, not fixture-stored literals. Both-side mirrors are supplemental oracle-derived evidence, not extra original literals. No synthetic trace is represented as complete actual campaign history.

## Tradeoffs and review challenges
Repeated bounded replay is deliberate authority work. Closed descriptors duplicate frozen schema text and need parity checks on future changes. Fixed creation pin intentionally limits the C3a mechanism; it is not a configurable live profile. Focus review on independent input provenance, deep canonical ordering, command-versus-input hash, actor/duplicate ordering, regressed/unavailable/early/equality clocks, typed World current-CP mapping, owned Created before adversarial lists, and honest distinction between synthetic and actual histories. No inference from author claims or preceding slices substitutes for current candidate verification.

Lead dev review found a C3a-only grammar mismatch in shared Route handling. A sentry Boundary regression reproduced expected JsonException versus actual trusted-context ArgumentNullException. Fix rejects Route/LegacyBrokenVehicleLot only in C3a mode, preserving inherited parsing. Dedicated array-order tests ensure legal unsorted syntax reaches trusted context before semantic rejection. Final verification facts will identify the corrected freeze; earlier positive runs do not prove the final candidate.
