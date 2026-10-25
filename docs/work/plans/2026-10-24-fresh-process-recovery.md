# Native fresh-process recovery proof

Base `72775390f6cf2014dccf4713834141fe55d3acca`; branch
`codex/rc-overnight-plan-20261023`; PR329.
Ruflo task `task-1792887938731-phxmmz`, namespace `sandtable-rc-20261023`.

## Pass criteria

Start from genuine accepted campaign history with a pending native decision.
Preserve SQLite WAL state and credentials when branching an identical baseline.
Launch a separate OS process which receives only journal path and controlled
clock input, opens the persisted journal, verifies it and runs the background
scheduler without an owner query. Prove process identity differs from the parent.

Before expiry, retain original deadline and accepted history unchanged; normal
monotonic clock high-water evidence may advance. At expiry,
compare exact logical journal records and reconstructed authority against the
in-process branch. Reopen in another fresh process and prove no duplicate timeout
or owner action. Retain precise initial/repeated decision cuts tested; coverage of
one cut must not stand in for another. Bound child execution and drain output so
failed children cannot hang the focused suite.

Preserve existing private P01 child protocol. Use internal test access already
present; do not add production public APIs for the harness. Fix only demonstrated
runtime defects and extend affected focused checks if production changes result.

## Ownership and validation

Host worker owns native child mode and fresh-process tests, with exclusive build
lease. Coordinator owns plan, checklist, handoff, review, commit and PR update.
Retain focused new tests, legacy child regression, adjacent integrated proof and
build/format results. No full Core iteration. Prior independent review allowance
is exhausted; coordinator review does not claim independent release acceptance.

## Limits

This is native journal process recovery, not process-kill atomicity, public
admission or a full playable campaign. Third-cycle Reserve, repeated captured-lot
Custody/required Retreat, exact entry clock faults, source-pin reconciliation,
dependencies and release-head validation remain separate gates unless directly
proved here. No original P01 ancestry promotion. PR remains draft.

Graph Tier2 generation2026-10-25T00:24:00Z: existing child Program, P01 helper and
integrated campaign test have matching metadata and no recorded coverage issue;
coverage is best-effort. Check any further relied-on source. CCE tools unavailable;
repository and Ruflo retain decisions and evidence.
