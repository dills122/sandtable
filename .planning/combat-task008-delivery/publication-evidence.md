# Publication evidence recommendation for A2

Read-only research complete; proposal awaiting A2 implementation/review, not a storage selection.

A2 decision: choose the preservation alternative below. Actual-persistence obligation remains
open as HOST-PUB-001; Core D/H report their in-process scope separately and Task025 carries the
pending obligation. Canonical envelope and plan now pin owner, seam and exact provider matrix.
No requirement is relabeled satisfied by codecs or memory CAS. Three independent A2 reviews accepted
this allocation without finding; actual provider evidence remains open.

Sources: [creation contract](../../docs/specs/combat-authority-envelope-v1.md#creation-publication-and-recovery-contract),
[Task008 index](../../docs/design/combat-cycle-implementation-plan.md#task008-execution-index),
[HOST-RSH-001](../../docs/research/orleans-publication-feasibility.md).

## Proposed seam

Versioned per-campaign commit batch, campaign ID as uniqueness key, conditional on expected prior
journal head. Creation expects absent record and retains canonical request/binding, exact Created11,
creation receipt and initial head P0 atomically. Snapshot12 is a verified reconstructable checkpoint.
Later batches retain command identity, exact events/receipt and resulting head. Provider undecided.

Authenticate ingress before retained receipt lookup. Existing identity is checked before fresh
admission: exact retry returns retained bytes; changed immutable input conflicts. Only absent creation
consults admission. Unknown commit outcome requires authoritative reread; unavailable truth remains
unavailable, never assumed uncommitted or silently regenerated.

## Evidence ownership to pin by A2

| Gate | Evidence |
| --- | --- |
| A1c | Exact codecs and pure retry/conflict decision; no stored uniqueness claim |
| A2 | Exact Snapshot12 and receipt/P0; trusted independent regeneration; disabled-admission creation readback; missing/altered/noninitial negatives; explicitly assign actual-persistence obligation |
| Parent008/H | Full Core causal replay and retained ledger through inherited families and019A; tamper/reorder/omission/unsupported histories; disabled-admission restore |
| Production host020–021 plus verified023 | Actual provider concurrent same/changed creation, before-commit failure, commit/ack loss, reply loss, ambiguous-outcome read failure, process crash/restart, fencing, corrupt evidence, checkpoint lag, retention and record bounds |

## Conflict requiring explicit documentation

Envelope currently says Task008 must prove atomic uniqueness/response loss using actual persistence.
Plan checkpoint D excludes atomic Chronicle/durable persistence and assigns hosting separately.
Recommended correction: retain behavioral requirement and assign actual-persistence proof to named
host gate in both docs by A2 review. Alternative: leave parent008 publication acceptance pending
until host proof. Neither pure codecs nor memory CAS satisfy original requirement.

Existing probe uses Rules9 Exercise reconstruction, memory CAS and fresh Begin during restore.
Command failure injections and same-process reactivation do not prove Combat creation crash recovery,
disabled-admission recovery or process durability. No external research or new experiment needed now.
