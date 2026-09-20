# PR metadata

Branch: codex/combat-task008-reaction-lifecycle
Base: main
PR: https://github.com/dills122/sandtable/pull/136
Title: Replay Reaction lifecycle and closure paths

Task008 now covers three creation-rooted Reaction paths: F2 participant movement/completion with
mandatory stop resolution and noeligible closure; F3 direct owner decline or System fallback; and
F4 active unavailable/timeout fallback. Active fallback records an exact reactor stop when closing
the window, then requires a separate System resolution before the original phasing route resumes.
Direct closure creates no stop. No fallback changes World resources, RNG, tracks or material progress.

Strict canonical commands/events/caches bind trusted creation history, actual predecessor state,
reason-specific public capabilities and authenticated actors before retry. Re-signed effects,
competing forks, skipped/reordered events and caller-buffer mutation cannot replace that authority.
Historical readers and public activation remain unchanged.

F2, F3 and F4 each passed dev review and three sequential fresh-context independent reviews.
F4: four forks/eight events/12cuts/32artifacts; focused29/full2,080/boundary81 passed, zero failures
or skips. Build zero warnings/errors; full format passed. Exact logs/manifests/reports retained.

One main-based delivery PR; F5 second move, F6 completion and initial H full Snapshot12 restore
remain open. Later gameplay/public activation and HOST-PUB-001 durable publication retain their gates.
