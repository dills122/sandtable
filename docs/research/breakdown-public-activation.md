# BRK-TASK-006 — Public activation and privacy

Task 006 activates the certified Breakdown implementation through first-side Combat entry.
Task 007 retains checked Runner scenario migration and clean-run artifacts.
[Final review 4](../reviews/brk-final-review-4.md) leaves BRK-AC-009 acceptance open: isolated
Observation 7 and boundary tests need full current-version audience transcript/progress comparisons.
No runtime disclosure defect was demonstrated; public activation is implemented.

## Current contract set

| Boundary | Current identity |
| --- | --- |
| Rules | Ruleset 9; prior Ruleset 8 retained explicitly as historical |
| Content and setup | Schema 6; content JSON format v5; certified Truck/battalion profile |
| Authority | World 6, Snapshot 11, CampaignCreated 10, sequence/catalog 4 |
| Decisions | Observation 7, Breakdown side-safe policy v1, projected history 2 |
| Actions | Legal set 2, policy `sandtable.legal-actions.v3` |
| Disclosure | `user-space-disclosure-manifest.v2.json` |
| Events | ElementMoved 3; ReactingElementMoved, ReactionParticipantCompleted, ReactionWindowClosed and MovementSegmentCompleted 2; stop/resolution/Breakdown completion 1 |

Public creation resolves exact registered content/setup identities before constructing initial
authority. Truck and bounded contact inputs are independent certified catalog entries. Original
Truck fixture bytes and all historical checked Runner fixtures remain unchanged. The contact input
supports current Reaction regression tests; it does not close the thirteen checked successor
scenario obligations in the fixture migration inventory.

Current query/submission, event codecs, exercise reconstruction and checkpoint admission use this
set together. Historical serializers and test drivers retain explicit predecessor contracts; no
current path backfills BP, accepts an old authority handle, or creates a legacy shadow snapshot.
Unchanged preamble event schemas bind sequence 4 through a strict canonical adapter; predecessor
readers retain sequence 3. Full replay recalculates every accepted transition.

## Admission and disclosure

Snapshot admission first validates local World/flow certification, then recomputes retained
Initiative and Weather from the seed, validates opening checkpoint progression and Reserve state,
and checks untouched world/RNG state at Movement entry. This certifies a current checkpoint;
it does not prove its entire history. Exercise reconstruction separately starts from creation,
replays canonical events and compares the complete final snapshot byte for byte.

Open ordinary routes publish a state-scoped stop capability and prevent another element or segment
from starting. Pending stops publish one System resolution action. Both player views use generic
waiting; reactor waiting suppresses owner rows and lots that could reveal identity bindings.
Owner lot summaries expose only cohort/location counts, with totals required to match own broken
counts. Public route hashes derive from approved observation facts. Full dice, BP evidence,
checks, lot identities and continuation authority remain in trusted history.

Runner current event admission delegates nested validation to Core's strict decoder, recognizes
all successor event tuples and preserves pending-stop positions in receipts and checkpoints.
Bounded movement policies select the explicit route stop before continuing their existing
one-move-per-element policy. Checked Rules 8 manifests now fail current admission; current tests
use certified inputs rather than silently upgrading those historical bytes.

## Verification

Final verification on 2026-09-05:

- `dotnet restore Sandtable.slnx`: passed; all projects up to date.
- `dotnet build Sandtable.slnx --no-restore`: passed, zero warnings/errors. Binlog:
  `artifacts/binlogs/brk-006-final-build-20260905-193342--97551--Sp_P6X.binlog`.
- `dotnet test --solution Sandtable.slnx --no-build`: **1,590 passed, zero failed/skipped**.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace'`:
  **66 passed**; historical and current disclosure manifests and transcript checks included.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: passed.
- `python3 docs/research/verify-breakdown-contract-freeze.py`: passed; fourteen historical fixture
  hashes, fifteen Reaction children (thirteen successor obligations, two deferred), fifteen inherited
  schema hashes and all requirement/acceptance links retained.
- `python3 docs/research/verify-breakdown-outcomes.py`: passed; 324 cells and 606 bounded loss combinations.
- Local links in changed Markdown and `git diff --check`: passed.

After that full solution run, nine creation tests were added for all Rules/Setup/Content identity
combinations and cross-bound certified catalogs. Final Core build passed with zero warnings/errors
(`artifacts/binlogs/brk-006-identity-matrix-build-20260905-193743--98192--A6nqFq.binlog`);
`dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build` then passed
**1,156 tests, zero failed/skipped**, and format verification passed again. Production code and
Runner/transport tests were unchanged.

Observed RED cases covered public activation, catalog identities, Observation 7, preamble replay,
inconsistent own lot totals, Runner version/flow admission and route-stop policy.
Regression coverage includes complete preamble/Reserve replay, full Truck movement-stop-resolution
history to Combat, deleted/reordered/changed history rejection, forged retained preamble evidence,
exact action bindings, zero-survivor movement exclusion, destination stack certification and
retained player transcript privacy. Historical Rules 8/policy 2 tests retain their original
behavior through explicit test-only historical drivers and unchanged canonical readers.

## Limits and next task

Combat actions, later-stage reset, positive ZOC under the certified size bound, grouped losses and
motorized-infantry losses remain unsupported. Current public tests do not establish historical
artifact reproducibility for the old Runner matrix. Task 007 must add the inventoried certified
checked scenarios, reconcile research claims, run two clean artifact generations, and perform its
independent review. Rollback reverts activation as a whole; there is no dual-current downgrade mode.
