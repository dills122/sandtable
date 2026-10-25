# Persisted Selection deadline plan

Base `38c17170`, branch `codex/rc-overnight-plan-20261023`, PR329.
Ruflo task `task-1792886193779-aw4muf`; namespace `sandtable-rc-20261023`.

## Behavior and contract

Initial and repeated Combat Selection currently lack persisted rounds. Start an
immutable round at actual decision entry, not first owner query. Initial entry
may follow generic legacy Breakdown or owner Movement submission; repeated entry
follows accepted native repeated Breakdown. Cover both ingress paths and avoid
allowing generic submission to bypass a retained deadline.

Add native V38 explicit System `no-selection` authority, bound to initial snapshot
or repeated Breakdown, actual World/ordinal and trusted timing configuration and
clock evidence. No owner action, acting element, contact or target is fabricated.
For native adapter compatibility, V38 combines selection closure and native
Position Determination progression; existing V13/V32 completes the five remaining
no-work steps using System event identity. This differs from isolated C3a's
separate closure and structural-step events. Preserve owner V12/V31 canonical
bytes. Do not claim original C3a/P01 certificate equivalence.

No-selection reaches same-slot Reserve Release without RNG draw, resource debit,
attack history or target use. A clock-unavailable entry carries explicit absent
clock evidence rather than a new invented deadline. Invalid/stale/foreign owner
proposals reject before clock mutation. Owner acceptance and System fallback
consume exactly once, including retries and cold restart.

Host schema14 adds a seventh independently bounded scheduler lane, strict
entry/event/row binding and atomic fallback/continuation. Preserve historical
schema thresholds and read-only recovery; no writable timestamp migration.

## Ownership and checks

Core worker owns Core/runtime/adapter/tests and native spec. Host worker owns
journal/scheduler/Host tests and journal spec. Coordinator owns architecture docs,
CI new-row weights, checklist, final validation, commit/push and PR329 update.
Preserve other edits and serialize builds. No full Core iteration.

Prove initial and genuine repeated accepted histories, entry deadlines before any
query, exact boundary, owner wins/retry, invalid proposal non-mutation, clock
faults, changed/missing rows, transaction rollback and cold replay. Check legacy
owner bytes, System audience, unchanged World/RNG and correct successor. Report
synthetic-only evidence separately if any path cannot be exercised genuinely.
Run focused Core/Host and affected consumers, Release build, format, boundary and
contract checks. Reconcile actual discovery for new Core rows. Run relevant
Python oracle independently and preserve source-pin failures without refreshing
pins. Exact-head CI and independent release review remain required before merge.

Parent graph generation2026-10-24T23:46:20Z: Host Submissions/Openings clean
best-effort metadata. Core Adapter partial across multiple ranges including
423–1535; Exercises partial line147. Read relevant source directly; no exhaustive
coverage claim from graph alone. CCE tools unavailable; Ruflo retains decisions.
