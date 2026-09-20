# Task011 author explanation — frozen source

## Intent and scope
Base8108ccd, following accepted parent010. Add dormant Round2 precommit private seals and Prepared continuation. Five primary paths: SealedRoundModels, SealedRound, SealedRoundCodec, narrow SelectionStepsCodec external-syntax bridge and CombatSealsTests. Canonical Task011 and active frozen sealed-round-v2 govern;012 atomic debit and commit remain excluded. Frozen source pins in task011-source.sha256; verification facts in task011-checks.md.

## Authority and flow
Each operation must independently validate request/retained Created, trusted synthetic C3a Boundary and separately authenticated010B predecessor input/event history, then reconstruct Base2. Local RoundInput history remains separate from untrusted event.input; replay regenerates and compares complete event bytes. No caller Control/State authorizes a transition. Raw Base/state readers validate closed syntax/canonical bytes before consulting trusted context, then compare complete reconstructed bytes.

## Component choices
Immutable typed models own collections and buffers; get-only Base couples cached canonical World/Boundary to authenticated predecessor. Codec reuses existing external syntax through a narrow named bridge rather than duplicating World grammar. Seventeen local descriptors preserve active v2 fields, domains and role order. World remains unchanged in011; codec uses owned canonical World captured from authenticated typed Boundary. This serialization reuse must not introduce caller JSON authority. Private engine transition receives reconstructed state only.

## Invariants
Original Config1 remains predecessor evidence; supplemental ClockConfiguration2 binds original force-assignment budget and corrected public opening-floor policy. Opening floor/deadline are immutable and independent of previous private timestamps. First seal preserves other-side witness; second becomes Prepared. Exact Command+actor retries recover original event before status/time gates after primitive validation. Invalid own proposal rejects before clock cancellation. System callbacks cannot overtake Prepared.

Prepared continues FA then certified empty Anti-Armor to step5; cancelled finishes all remaining steps through6. No commit, costs, attack history, target-use or RNG mutation. Fresh admission disabled prevents opening while retained replay/retry/structural recovery remains available. No actual positive creation-rooted provenance, public action, publication or extended Snapshot12 claim.

## Verification and limits
Final observed checks belong in task011-checks.md after freeze. Required literal scope10Bases/54events/64states explicitly excludes4commit events from full58/68 fixture. Clock192freshoutcomes+96retries assert own declassified witness, not full private authority equality. Negative syntax, primitive, provenance, rehash, retry, clock, bounds and ownership tests must be failure-sensitive. Root frozen oracle pass establishes unchanged contract, not implementation correctness. Repeated replay is bounded; performance/host privacy is not benchmarked or claimed. Reviewer should challenge all authority handoffs, retry ordering, fixed-floor policy, typed bridge primitives and full-byte equality independently.
