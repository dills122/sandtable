# Custody and Retreat entry-clock failure

Base `69175f16fefa3da50171ab9758dbfb4c428b656c`; branch
`codex/rc-overnight-plan-20261023`; PR329. Ruflo task
`task-1792895685734-7va0ib`, namespace `sandtable-rc-20261023`.

## Pass criteria

Fail trusted-clock read exactly when a real accepted combat history enters
Custody or required Retreat. Preceding Force must complete and actual paid result
must exist; cancelling Force before entry does not prove this gate. Polling an
already-open round also does not prove entry behavior.

Use deterministic test clock with bounded trace/injection and source-backed call
order. Verify target through actual event/round bindings and persisted evidence,
not a read count alone. Prefer genuine repeated seed50 Custody and seed4 Retreat
fixtures; cover initial cuts if bounded. No fabricated authority, snapshot, lot,
result or RNG state. Avoid public diagnostic APIs/reflection/stack inspection.

Assert explicit existing-contract fallback reason/timing evidence, no invented
observed timestamp, System leave-unguarded/refuse-retreat effects, no owner receipt,
no extra paid roll and verified replay. Restore clock, reopen, and prove idempotent
continuation; use focused rollback evidence where warranted. Fix only demonstrated
runtime gaps, preserving failure evidence.

## Injection mechanism

Save actual authority immediately before final defender Force seal. Healthy twin
uses clock returning unique increasing milliseconds; persisted target round
`opened_ms` identifies its exact entry read in the trace. Fault twin starts from
same saved authority and identical initial clock, throws only on that read, then
recovers. This derives target from persisted authority rather than a magic count.
Compare paid result/Force completion bytes, require target unavailable fallback
with sentinel−1 timing/no observed timestamp, no target collecting row, and no
Force cancellation. Initial and repeated counterparts may use same bounded design.

## Execution and limits

Host worker owns new proof and necessary minimal implementation fix; coordinator
owns docs/review/git/PR. Serialize builds; focused new and adjacent Custody/Retreat
checks plus build/format. No full Core iteration. Independent release review and
exact-head CI remain required; no merge or public admission. Remaining process/
crash cuts, source pins, certificates, dependencies and playable RC gates stay open.
CCE unavailable; decisions retained in Ruflo/repository. Graph Tier2 existing
repeated/initial Custody/Retreat tests have matching metadata; further paths need
coverage checks and source fallback on gaps.
