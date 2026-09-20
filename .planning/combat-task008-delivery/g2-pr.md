# PR metadata

Branch: codex/combat-task008-breakdown-completion
Base: codex/combat-task008-movement-lifecycle
Title: Replay System Breakdown completion into Combat entry

Task008 G2 now reconstructs full creation-to-Movement lifecycle history before accepting System
Breakdown completion. The event advances to first Combat Position Determination, retains predecessor
Breakdown sources and preserves actual Movement-end proof, exclusions, World, spending, RNG and progress.
Reserve, Movement and Breakdown completion receipts remain distinct. Exact authorized retries return
original event; changed reuse, missing or transplanted history and forged canonical state reject.

Validation: 24 focused tests (12 G2+12 lifecycle), eight traces/eight events/sixteen cuts/forty frozen
artifacts. Full solution 2,044 passed, zero failed/skipped; build zero warnings/errors; format, diff
and added-line local links pass. Dev review and three sequential independent reviews passed with no findings.

Stacked on #133. Reaction, general restore, Combat actions, public activation and HOST-PUB-001
publication evidence remain open. No canonical contract or fixture changes.
