# Task010C author explanation

## Intent and plan
Base fe5664d. Complete canonical Task010C actual cumulative history routing after accepted010A/B. Three primary paths: HistoryReplay, HistoryModels and CombatStepsHistoryReplayTests. Parent010 closes only after this slice passes development review, full gates and three independent reviews. Prepared011, actual positive-history authentication, new Snapshot12 families and publication remain excluded.

## Flow and choices
Generic replay owns Created before touching caller event lists, then uses existing bounded retained-history capture. Existing causal routing reaches Movement lifecycle unchanged. It reads exactly one strict G2 completion and returns immediately when history ends there. Only that exact prefix is passed to inherited selection admission, whose recursive generic replay therefore terminates at G2. Up to two selection events are strictly replayed; any remaining tail requires closed selection and all remaining records go to the strict six-step traversal reader. No events are ignored and no event tag alone establishes authority.

Two new closed projections derive head and position from accepted local states. They concatenate Entry, Selection and (where present) traversal receipts once into owned read-only cumulative arrays. Explicit get-only State prevents record-with replacement from desynchronizing cached ledger. Local wire Controls remain untouched. This repeats bounded predecessor validation rather than introducing an independently trusted state cache.

## Invariants and limits
Every retained event has exactly one cumulative receipt; current version, event-prefix fold and position match strict replay. World/resources/RNG and nested selection Control remain unchanged. Synthetic timed C3a traces cannot become actual history. Existing Snapshot12 closed writer switch rejects new projections; old G2 roots cannot restore a longer suffix. Arrival at Reserve Release performs no release.

Created-before-list ownership is a narrow integration correction exposed by a failure-sensitive new test, not a changed wire schema. Event count512, individual1MiB and total16MiB bounds remain. No fixture, schema, oracle, public API or Snapshot writer change. Three source/test files plus documentation and review evidence are the bounded scope.

## Verification and challenge points
Exact RED/GREEN, source hashes and root gates are in task010c-checks.md when frozen. New tests exercise four actual histories,32 new suffix cuts,36 including G2 and118 prefix visits (not118 distinct histories), frozen local commitments, cumulative ledger ordering, causal/forgery/foreign/synthetic/excess-tail rejection, ownership and explicit Snapshot12 rejection. Prior368 roots/286 histories remain regression requirements. Review the recursive cut, immutable cached ledger and failure paths independently; author rationale is not acceptance evidence.
