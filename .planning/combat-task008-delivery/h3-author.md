# Author Explanation — H3 Reaction retained-history routing

## Intent and plan
Extend H2 single owned retained stream through accepted F1–F6 branches. Base fdd2a4f; four primary
paths will be frozen in h3-source.sha256. H4 literal roots/disabled-admission restore remain open.
No public registration, host publication, later gameplay or unrestricted-history claim.

## Approach and components
HistoryReplay's existing Move loop uses openedReactionWindow only as candidate hint. Non-null hints
reach actual strict F1 reader, which requires one exact ordinary predecessor and recomputes canonical
trigger effects. Null hints stay with strict E1; malformed or forged selection never grants authority.
ReplayReaction validates actual trigger first. Inactive window can enter direct closure or one actual
participant move. That participant can enter normal lifecycle completion, System fallback or second
move. Actual second move precedes explicit completion/resolution/closure. Every terminal reader takes
whole remaining suffix, so excess tails fail. No exception-swallowing or caller family discriminator.
Models add six closed arms around actual predecessor states. Existing H2 actualF1 test now admits
valid trigger while retaining strict E1 rejection. New tests own all Reaction branches and negatives.

## Verification and invariants
Both owners and seven forks each produce fourteen traces. Every selected intermediate cut compares
actual family state/event goldens and H0 root fields: world, RNG, headers, full receipt ledger, prefix,
window, flow, actual currentPosition, suspended sequencePosition, members, cycle, tracks and progress.
Exact identity-set coverage targets50vectors/34histories,2sharedH2/32new. Combined with H1/H2 this
covers all368selected vectors/286histories, but does not yet serialize full literal runtime roots.
Missing/duplicate/reordered/foreign-fork/unknown tails and mandatory explicit stop/resolution/closure
are tested. Buffer mutation cannot affect projections or retained replay. Typed effect/actor forgeries
reserialize with changed receipts and readable input before router rejection. Null/object/missing/
duplicate window hints and wrong event tags test dispatch resistance separately from causal probes.
Actual commands/results and construction corrections belong in h3-checks.md after worker freeze.

## Choices, costs and limits
Reuse strict readers rather than reimplementing domain transitions. Candidate tags select one exact
reader; no trial-and-ignore fallback. Small private routing method contains branch graph without new
registry/service. Repeated bounded predecessor replay costs remain; no scalable arbitrary campaign
claim. Every root currentPosition must remain distinct from suspended Movement in Reaction; typed
case preserves both for H4. Trusted history commitment remains caller responsibility; recomputable
hashes establish canonical consistency, not archive authenticity. Legal shorter prefixes valid.
No accepted predecessor production/schema/oracle/fixture change. Challenge selector ambiguity, fork
separation, remaining-tail consumption, actual/inactive window retention and false full-restore claims.
