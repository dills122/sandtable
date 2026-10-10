# Handoff: actual prepared-round entry contract REVIEW_READY

Date:2026-10-10 America/Toronto, continuing the preserved2026-10-09 WIP.
Author status only, not independently Ready. No commits, staging, publication or merge.

## Objective And Boundary

REL-AUD-02B implements the accepted exact private contract at selection20 through Prepared CA25,
or cancelled no-attack Reserve Release. Both original owners and positive seal orders; separate
original selection and round ledgers; full provenance/ownership/canonical/capacity/error/privacy,
conservation and dependency closure. Five primary files only. Native and paid/result/public
activation remain subsequent gates, not a continuation authorized from this author packet.

## Canonical Sources

- `docs/research/combat-actual-round-entry-feasibility.md`, all eight acceptance groups.
- New `docs/specs/combat-actual-round-entry-v1.md`, schema, literal fixture and oracle.
- Actual-selection-v1, original positive-entry, sealed-round-v2 compatibility and step design.
- `docs/work/plans/2026-10-05-actual-selection-dependency-disposition.md` and canonical Combat plan.
- Preserved `docs/work/handoffs/2026-10-09-actual-round-entry-contract-wip.md`.

## Current Repository State

Exact checkout `/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable`; branch `codex/actual-round-entry-contract`.
Base and HEAD `0db4745a5ec631269c7751a244e1f49d84a81d6f`. Review target is the frozen five-path working tree below,
not a nonexistent contract commit. Existing `.serena` metadata was preserved. Other workers'
edits were not reverted; coordinator's internal worker had stopped before this continuation.
Research PR166 is separate and its merge is not a prerequisite for this accepted exact base.
Only plan is tracked-modified; four new primary paths and dated admin packets are untracked.
Verify all SHA256s/status before review. No old native branch switch, no new worktree/automation.

## Completed Work And Evidence

Preserved WIP hash205086c65e97d1d39385d91a1006285718c44d12aedb309967e214f930ed5597;
initial schema677b1c671bebe5f23cf4b80f03078c55000c60e45416701757e2552cbd05edb8 and
oracle17bd314dd122ef1f3a962ea4ceb8d6c943ed20079e0bce89be242c62152175b0 matched before edits.
Initial two-owner semantic RED and minimal GREEN4positive/30cancel remain historical evidence;
original GREEN wall time was not measured. No restart or fabricated RED.

New retained RED/GREEN: numeric wrong-type002→001; missing lookup KeyError→009; admission flag
and actor/role wrong-type002→001; cancellation firstSeal attacker→null/defender labels. A late
public warm AA completion RED showed Content changed after both preflights could be accepted,
and disappearance leaked KeyError. Exact consumed bytes now recheck the pinned digest at use;
both new rejection cases GREEN. Normal run was interrupted130, not called passing. Frozen
literal fixture was NOT regenerated; valid canonical acceptance bytes remain unchanged.

Earlier acceptance failures were harness construction defects: re-signing malformed Base via
canonicalization, consumed-slot expectation, and cancellation callbacks that are original retries.
Logs are retained separately, never counted as semantic RED or waived implementation failures.
The final harness locally binds even malformed ordered Base JSON before the reader rejects shape.

Pre-literal full adversarial check passed all34 traces/cuts/mutations before generation in
1940.484s. That run preceded later type/label/consumption acceptance edits; final normal below
covers the exact amended verifier, fixed original fixture, and all acceptance groups. One-time
external generation20.48s froze34cases/1100artifacts; max source83051B/max proof32364B, under1MiB.
The normal verifier has no writing/regeneration path. Original12+7+up to6 events remain separate.

### Exact final normal gate

`python3 -B docs/specs/verify-combat-actual-round-entry-v1.py`: PASS exit0, 1865.639s.
Verifier SHA256 before/after: `875de826f4fc2fa8c5410e3d0b8be4e5dcb688e0ad814819a8cdca68bfbd3e05`.
Full stdout SHA256: `89dae8908626a0c523e598e7c24a2926b1e8f98426aca7359a43f5af7ebb0547`.
Fixture before/after SHA256: `fc17cda07618bdd539a56b8c296037bbf36f37c6eb2ce6258c2b789b765f640b` (unchanged from generation).

Coverage counts (not inferred from code):

```json
{
  "capacity": 15,
  "causal-array": 96,
  "configuration": 4,
  "control-leaf": 5604,
  "cuts": 224,
  "dependency": 399,
  "dependency-consumption": 2,
  "dependency-missing": 57,
  "entry": 4,
  "entry-leaf": 1512,
  "entry-order": 26,
  "event-leaf": 8016,
  "family": 4,
  "future": 50,
  "history": 570,
  "input-leaf": 2510,
  "integer-arm": 20,
  "ledger": 102,
  "lifecycle-matrix": 5904,
  "lifecycle-public": 328,
  "opening": 8,
  "owner": 2,
  "ownership": 204,
  "precedence": 16,
  "precedence-primitive": 80,
  "prepared-noop": 84,
  "preselection": 28,
  "primitive": 17,
  "privacy-clock": 192,
  "privacy-invalid": 768,
  "privacy-witness": 4,
  "proof-leaf": 23522,
  "proposal": 16,
  "raw": 1530,
  "raw-event": 2850,
  "raw-input": 150,
  "raw-limits": 40,
  "retries": 2520,
  "retry-actor": 630,
  "retry-primitive": 630,
  "selection-leaf": 516,
  "selection-ledger": 34,
  "selection-order": 6,
  "stale-noop": 4,
  "suffix": 630,
  "unsupported": 92
}
```

### Original oracle observations

Measured with600seconds per command, never treating timeout as pass. No original oracle bytes
were changed; none needed repeating after private new-family fixes.

- `python3 -B docs/specs/verify-combat-actual-selection-v1.py`: PASS exit0, 231.291s, stdout SHA256 `0ec2ecc01734e319a6abd590a7bf1484cf5569d657874981e893a6c93d8cc874`.
- `python3 -B docs/specs/verify-combat-positive-entry-v1.py`: PASS exit0, 121.706s, stdout SHA256 `37988e248972aed56c4b2f9112a3cb8b55e94c023add34a1635e4e31b4359199`.
- `python3 -B docs/specs/verify-combat-selection-steps-v1.py`: PASS exit0, 5.434s, stdout SHA256 `a6d1fb28ec2d5487bfd23e5cd0c4dc208c4bda1147d027ad9ded9c0639777691`.
- `python3 -B docs/specs/verify-combat-sealed-round-v2.py`: PASS exit0, 18.188s, stdout SHA256 `7497cdcac2a85a116e5b2eaaf43be2313d90522fddc1da852312b388eedd527c`.
- `python3 -B docs/specs/verify-combat-result-settlement-v2.py`: PASS exit0, 94.335s, stdout SHA256 `2a034604581df3937100289a18e23f5b3270ba57d6ff345cb0f5c2e9f3b400bb`.
- `python3 -B docs/specs/verify-combat-inherited-breakdown-completion-v1.py`: FAIL exit1, 0.448s, stdout SHA256 `53763fd8940f5a72d343f67ae7ef1c41175d0df64abfa414ecd532309674f748`.
- `python3 -B docs/specs/verify-combat-cycle-sequence-v1.py`: FAIL exit1, 0.077s, stdout SHA256 `2b37c9479b23cf9201a18ce0fae319c542b09c20f23e85a7105be3cc474dbdcf`.
- `python3 -B docs/specs/verify-combat-inherited-snapshot-v1.py`: FAIL exit1, 1.662s, stdout SHA256 `3dbdcdc29d0a06b51cb07e39075f7934ea519e393f4f301bf4293905c27c8952`.
- `python3 -B docs/specs/verify-combat-outward-composition-v1.py`: FAIL exit1, 5.165s, stdout SHA256 `5e02eac0416e1a906b8da65ad298bfdaa0bfbc5e4ae57d0627346748a799af36`.
- `python3 -B docs/specs/verify-combat-snapshot-composition-v1.py`: PASS exit0, 90.848s, stdout SHA256 `8b4f91220c348c27947067df340969ac204dc3a3183eb8c924283e51ecd5067a`.

Breakdown/cycle-sequence/inherited Snapshot retain original LandSequence source-pin failures;
outward retains CMB-OUTWARD-COMPOSITION-REJECTED at its original gate (the disposition records
independent Content-document drift). Synthetic Snapshot passing is separate. Historical S1a12/60s
and research60s timeouts remain unverified observations; completed runs supplement, not overwrite.
No .NET restore/build/test/format or public/native result-readiness claim for this contract.

### Frozen primary file manifest

| Path | SHA256 |
| --- | --- |
| `docs/specs/combat-actual-round-entry-v1.md` | `ed4fd69b4f599acae7c4850270557363a546df36d0b984107c50c08c29304548` |
| `docs/specs/combat-actual-round-entry-v1.schema.json` | `677b1c671bebe5f23cf4b80f03078c55000c60e45416701757e2552cbd05edb8` |
| `docs/specs/fixtures/combat-actual-round-entry-v1.json` | `fc17cda07618bdd539a56b8c296037bbf36f37c6eb2ce6258c2b789b765f640b` |
| `docs/specs/verify-combat-actual-round-entry-v1.py` | `875de826f4fc2fa8c5410e3d0b8be4e5dcb688e0ad814819a8cdca68bfbd3e05` |
| `docs/design/combat-cycle-implementation-plan.md` | `75134d9e895e1321f4569dd3be0aa3d483798af071c21e28cd2e2e14f2ecef91` |

### Retained evidence manifest

Evidence `/private/tmp/actual-round-entry-gates`; original RED script/log remain at the WIP's
exact `/private/tmp/actual-round-red.py` and `.log` paths. Preserve them for fresh review.

| Evidence | SHA256 |
| --- | --- |
| `adversarial-2.json` | `5b1cd9bf69fce8cf58f0abd21dcfec5e02acd80a28144b31761ee0d47e9fa770` |
| `adversarial-2.log` | `ff13fb8d920cf9fde0b4952acecbd6811e7644a61b647e999ae897c057ed946d` |
| `adversarial-3.json` | `638ca04719d3ba84388429adf3832bf7759e96c4ea78f1c4a96667b49037d211` |
| `adversarial-3.log` | `5b736af12953c7480df9fd992eb7fcfc9649a9c0ed7c2bd3bdea4595269230cc` |
| `adversarial.json` | `0eee66809de2064a8127804b1067535d0d1e1056dfe772e176cbdbbee71a3552` |
| `adversarial.log` | `a53d8cfab019c30189aa6059fbe34e7ab001041b4ee464f854135377c2f9481b` |
| `bounded-acceptance.log` | `a5b36f910d8f05bfadf6880dfe0d6dbe6cff889756d1d7e21273e526c5fc0bfe` |
| `closure-green.log` | `1a62e550392d39a3ec653ebd8fb8176333a0ccf80d3590a08c9337d7f7a86155` |
| `closure.log` | `aa5814e44b5aafb69cd774543506791ced8efbc5ac3b69f149aa09444b3cb19e` |
| `consumed-closure.json` | `21e0ec7b43a1c2d958e96f9d9e59cf559ba25f492a250962cadb5fac6273385f` |
| `consumption-green.log` | `37517e5f3dc66819f61f5a7bb8ace1921282415f10551d2defa5c3eb0985b570` |
| `consumption-red-1.log` | `77f90be410d80de976a42e8bce0249563e479b910b4747bd6103bd04618dd50a` |
| `consumption-red.log` | `fb4fa6278c57ed42682cbecd2864b4fa4ed8889b586049336d118f11011dde93` |
| `enum-green.log` | `37517e5f3dc66819f61f5a7bb8ace1921282415f10551d2defa5c3eb0985b570` |
| `enum-red.log` | `971ab3ede854d94c5c6c7d714814e68400884cc3a438b9d3865ca4a362f24566` |
| `final-normal.json` | `9559a2b3cbff35d82f2e73cb2314564a00610f768503983fa42adc4c05c8326e` |
| `final-normal.log` | `89dae8908626a0c523e598e7c24a2926b1e8f98426aca7359a43f5af7ebb0547` |
| `flag-green.log` | `37517e5f3dc66819f61f5a7bb8ace1921282415f10551d2defa5c3eb0985b570` |
| `flag-red.log` | `67ef39262f52338a309463db34c6c6d95b85e5127c006716d78510b82de65c1f` |
| `golden-generation.json` | `0716e55cd7877cf3c8117ab5b1c0b85e0e5ffec5801559c3a811aeaad65efe5d` |
| `interrupted-normal-1.json` | `85b5a5c5df2bd25debbb37bc0af497494ce3e77cff8c94a2e1c42caa14b4aa20` |
| `interrupted-normal-1.log` | `b42ff85dc99576985f6b8316df4c3d3a12d56e6b49e4f0482566030370920426` |
| `label-green.log` | `37517e5f3dc66819f61f5a7bb8ace1921282415f10551d2defa5c3eb0985b570` |
| `label-red.log` | `1401a41ddf87821ce3d69e62363c23b2d51849866ed3325aeb33c7e508083b70` |
| `matrix.log` | `0753c55bfa623c388e4d3b5d180bb2673bce9dd0ace7c29b12159f4b1897bfcb` |
| `original-actual-selection-v1.log` | `0ec2ecc01734e319a6abd590a7bf1484cf5569d657874981e893a6c93d8cc874` |
| `original-cycle-sequence-v1.log` | `2b37c9479b23cf9201a18ce0fae319c542b09c20f23e85a7105be3cc474dbdcf` |
| `original-inherited-breakdown-completion-v1.log` | `53763fd8940f5a72d343f67ae7ef1c41175d0df64abfa414ecd532309674f748` |
| `original-inherited-snapshot-v1.log` | `3dbdcdc29d0a06b51cb07e39075f7934ea519e393f4f301bf4293905c27c8952` |
| `original-outward-composition-v1.log` | `5e02eac0416e1a906b8da65ad298bfdaa0bfbc5e4ae57d0627346748a799af36` |
| `original-positive-entry-v1.log` | `37988e248972aed56c4b2f9112a3cb8b55e94c023add34a1635e4e31b4359199` |
| `original-result-settlement-v2.log` | `2a034604581df3937100289a18e23f5b3270ba57d6ff345cb0f5c2e9f3b400bb` |
| `original-sealed-round-v2.log` | `7497cdcac2a85a116e5b2eaaf43be2313d90522fddc1da852312b388eedd527c` |
| `original-selection-steps-v1.log` | `a6d1fb28ec2d5487bfd23e5cd0c4dc208c4bda1147d027ad9ded9c0639777691` |
| `original-snapshot-composition-v1.log` | `8b4f91220c348c27947067df340969ac204dc3a3183eb8c924283e51ecd5067a` |
| `originals.json` | `42565d6caf0cc866478c7e58ae0c2be589cb258043d0e7467dcfc394bedadcc2` |
| `regression-green.log` | `37517e5f3dc66819f61f5a7bb8ace1921282415f10551d2defa5c3eb0985b570` |
| `regression-red.log` | `ffd4ba1435397b99483fb3a32ed75b547221e18589914c477e215299dfc8352e` |

## Decisions And Limits

New family uses full original source plus both complete ledgers; embedded input/actor/time is
consistency evidence, not ingress authentication. Derive Base and empty-AA from authenticated
actual provenance. Preserve independent supplement/parent Config1 hashes, immutable opening
floor/deadline and role-ordered slots. Cache stores only immutable full-source/full-ledger bytes;
57 pins precede even warm lookup and Content is revalidated at use. Returned objects are owned.
A test-only declassifier excludes opposing seal, time/count, receipt and authority hash/version.
It proves bounded mechanics privacy, not A2 outward/seat isolation or production availability.

Consumption trace verified all16 original plus41 additional physical hashes, no unpinned loaded
local modules or reads; new owned schema/oracle/fixture are review target, not self-referential pins.
No old16 values, old specs/fixtures/oracles or predecessor runtime changed. Dependency acquisition
for native remains outside authoritative turns. Native parity, paid/result/settlement and every
public/full Snapshot/Archives restart/Runner/repeat/later-II/consumed/parent017–019 gate remain open.
Graph ready generation2026-10-05 excluded docs; direct reads supplied evidence, not completeness.

## Immediate Next Actions

1. Brain verifies scope/hash/status and reconciles accepted research/contract boundary.
2. Dispatch fresh visible GPT-6.1 medium reviewer using neutral bootstrap first; counter0of9
   before dispatch, bounded3sets×3, high research recovery maximum2. Author did not spawn review.
3. Reconcile findings and heavy pivots within user-approved limits; do not reset review counts.
4. Commit/publish/create contract PR only after coordinator authorization/reconciliation. No
   staging/push/PR/merge here. REL-AUD-02C native starts only after independently Ready contract.

## Delivery Metadata

Proposed PR title: `Define private actual prepared-round entry contract`.
Neutral bootstrap: `docs/work/reviews/2026-10-10-actual-round-entry-contract-bootstrap.md`.
Separate author: `docs/work/reviews/2026-10-10-actual-round-entry-contract-author.md`.
This handoff: `docs/work/handoffs/2026-10-10-actual-round-entry-contract.md`.
No current blocker to author handoff. Full author gate evidence is retained; independent readiness,
publication, CI and native activation remain coordinator-controlled next actions.


## Publication authorization addendum

After the author freeze, the user explicitly requested a PR with the GitHub Keychain auth skill.
This author snapshot remains historical evidence. The contract will be committed and published
as a draft PR targeting `codex/actual-round-entry-research` while research PR166 is open.
Independent contract review is still pending; no Ready verdict or merge authorization is implied.
Brain retains the review counter and reconciliation responsibility. The five primary hashes above
remain the review target; only administrative packaging follows this snapshot.


## Review 1 correction supersedes the author freeze

Independent review1of9 at75b308d returned Not ready with one bounded P2; coordinator Accept.
New RoundEffect shape/non-string kind now rejects001 before unknown tag003. Retained RED/GREEN
and54 conflicting public readback cases prove the correction; inherited Effect and the literal
fixture stay byte-identical. Corrected implementation `6ac709bcdca1f01e4f894bc372bb1f85ec5e81c6` supersedes historical
working-tree/base/manifest claims above. Exact amended normal gate passed exit0 in1897.769s,
verifier before/after `2596531cb50ddbae7f1e606284f3d845b224d4f1fc10f478b717843d1062990c`, stdout `8e584c8ef48b7284e5385f6e0fe82462cae724235fa45d82df9cc3f11b8f2b60`.
The complete finding, reconciliation and current five-path manifest are in
`docs/work/reviews/2026-10-10-actual-round-entry-contract-review-1-response.md`.
Original predecessor observations remain unchanged; no redundant sweeps or .NET gates.
DraftPR167 publication follows user authorization; correction is author REVIEW_READY pending
fresh review2of9,set1pass2 by Brain. Review count1of9, recovery0of2; no heavy pivot or reset.
No native work or merge. Subsequent commit changes administrative packets only.
