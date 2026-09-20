# PR metadata

Branch: codex/combat-task008-reaction-lifecycle
Base: main
PR: https://github.com/dills122/sandtable/pull/136
Title: Replay Reaction participant lifecycle and direct closure

Task008 F2 replays actual reacting movement, participant completion, mandatory empty stop resolution
and no-eligible closure. F3 adds mutually exclusive reacting-owner decline and System unavailable/
timeout directly from the inactive F1 window. Both paths preserve exact original phasing route;
direct closure creates no reactor stop and consumes no World resources, progress or RNG.

Commands bind reason-specific public capabilities and authenticated actors before retries. Full
creation-rooted replay and canonical byte comparison reject changed history/effects/caches even
when receipts and prefixes are re-signed. Existing historical readers remain unchanged.

F2 validation: focused16/full2060; dev plus three independent reviews passed.
F3 validation: six forks/six events/12cuts/30artifacts, focused25/full2069 passed, build zero warnings/
errors and full format passed. Dev review and three sequential independent rounds passed without remaining findings.
Boundary81 passed. Review reports and source manifests retained in planning evidence.

Direct-to-main cumulative delivery replaces previous manually stacked targets. F4 active fallback,
F5 second move, F6 completion, initial H Snapshot12 restore and later gameplay/public activation
remain open; HOST-PUB-001 durable publication proof remains a later gate.
