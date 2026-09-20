# PR metadata

Branch: `codex/combat-task008-creation-snapshot`; base `codex/combat-task008-creation-binding` (PR124).

Title: `Add dormant Combat Snapshot12 creation readback`

Creation-only Snapshot12 now derives its complete canonical state, receipt and framed Chronicle
prefix from independently trusted request and exact validated Created11 bytes. Missing, altered,
rehashed foreign or noninitial evidence rejects; retained readback survives disabled fresh creation.
Caller buffers are copied before validation/hashing and never retained.

Publication evidence allocation preserves Task008's actual-persistence obligation as open
HOST-PUB-001 with Core/Host owner, existing later-host timing and explicit provider failure matrix.
No storage provider chosen, publication mechanism implemented or parent completion claimed.

Validation:34 focused tests; full solution1,874 passed, zero failed/skipped; build/format/diff/link
checks pass. C2/D1 oracles pass. Dev review complete; all three independent rounds Ready.
