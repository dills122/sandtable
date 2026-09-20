# PR metadata

Branch: codex/combat-task008-movement-lifecycle
Base: codex/combat-task008-inherited-movement
Title: Replay actual Movement stop, resolution and completion

Joint Task008 E2/G1 now replays the actual owner stop, System empty-cohort resolution and owner Movement completion after retained Move4 history. Captured cycle/position restores through the real stop event; completion derives both sides’ final locations and a distinct receipt-bound Movement-end proof.

World, CP/cohesion causes, Weather/RNG, member history, tracks and material-progress references remain unchanged. Audience/version capabilities differ from persisted route/stop IDs. Exact authorized retries return original events with current state; forged context, progress, proofs and caches reject.

Validation:20 focused tests (12 new lifecycle+8 Movement), eighttraces/24events/32cuts/88 frozen artifacts; full solution2,032 passed,0failed/skipped; build0warnings/errors, format/diff/local links pass. Dev review and three sequential independent reviews passed with no findings.

Stacked on #132. G2 remains separate Breakdown-to-Combat transition; Reaction, general restore, public activation and HOST-PUB-001 publication proof remain open.
