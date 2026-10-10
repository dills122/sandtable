# Independent contract review 1: accepted finding and correction

Date:2026-10-10 America/Toronto. Review instance1of9, set1/pass1. Verdict: **Not ready**.
Coordinator Brain accepted one bounded P2 at published75b308d7c4ceb9c84894e356f7cb9e41837046be.
No heavy pivot. This record retains the coordinator-delivered finding and verification testimony;
it is an author reconciliation, not a new independent verdict. Counter1of9, research recoveries0of2.

## Complete finding

New `typed(..., 'RoundEffect')` bundled dictionary shape, present string `kind`, and known tag
membership into CMB-ARE-003. Through public `read_source` of frozen fixturecase0, independently
supplied original ledgers, a first event effect of null, [], {}, {kind:true}, or {kind:1}
returned003 at /effect. Spec lines45 and155 require malformed shape/wrong primitive001 before
version/tag/arms003. An unknown string tag properly remains003. Inherited Effect mapping is
historical and must not be altered. The smallest correction separates shape/type001 from tag003
only in the new RoundEffect arm. Severity P2: observable rejection-code contract mismatch.

## Reconciliation: Accept

Retained regression `python3 -B /private/tmp/actual-round-effect-regression.py` before correction
exited1, with null effect returning003 when001 was required. After the two-check correction,
the exact regression exited0 in4.444s and passed54 public readbacks. Six effects (five malformed
shape/type values plus unknown string tag) × no-conflict/version/receipt conflict × source,
control and proof readers prove the new error precedence with original separate ledgers.
The same checks are embedded in `round_effect_checks` and run by normal acceptance.
Inherited Effect branch was compared byte-for-byte to75b308d and is unchanged. The fixture
SHA256 staysfc17cda07618bdd539a56b8c296037bbf36f37c6eb2ce6258c2b789b765f640b; no regeneration.
Spec/schema/dependencies remain untouched. Tests reject malformed source before trusting retained
control/proof objects; no direct internal validator substitutes for the public regression.

## Reviewer's other verification (coordinator testimony)

Independent reviewer confirmed all primary manifests/evidence;34semantic cases/1100literals,
192privacy comparisons plus768invalid proposals,16precedence vectors plus80primitive overlays,
399dependency tamper plus57missing plus2consumption, and92unsupported checks passed.
Reviewer did not repeat the31-minute normal run; that author gate remains separately recorded.
No other findings or pivot were reported. This author correction does not claim independent Ready.

## Corrected author gate

`python3 -B docs/specs/verify-combat-actual-round-entry-v1.py`: PASS exit0 in1897.769s.
Exact verifier before/after SHA256 `2596531cb50ddbae7f1e606284f3d845b224d4f1fc10f478b717843d1062990c`;
stdout SHA256 `8e584c8ef48b7284e5385f6e0fe82462cae724235fa45d82df9cc3f11b8f2b60`. Full counts:

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
  "round-effect": 54,
  "selection-leaf": 516,
  "selection-ledger": 34,
  "selection-order": 6,
  "stale-noop": 4,
  "suffix": 630,
  "unsupported": 92
}
```

RED log SHA256 `21091568d96e25eae9ee6ea7dc8268db8c163fb2ae07ae3c4e7ce4f6be9b0195`;
GREEN log SHA256 `4553c46ed40c57b8a9ea519e1441f1087f127a2ce1d72577b28373430ce7b2fa`.
Exact run metadata `/private/tmp/actual-round-entry-gates/review1-corrected-normal.json`;
full stdout at matching.log. Historical earlier gates remain in the original handoff.
Original four failures (Breakdown,cycle-sequence,inherited Snapshot,outward) remain failures;
no unchanged predecessor sweep or .NET gate was repeated. No native work or merge.

## Frozen correction and next review

Research base0db4745a5ec631269c7751a244e1f49d84a81d6f. Previous contract head75b308d.
Corrected implementation commit `6ac709bcdca1f01e4f894bc372bb1f85ec5e81c6` contains only verifier and canonical plan.
Subsequent administrative commit refreshes this record, author, neutral bootstrap and handoff.
Five-primary-file manifest:

| Path | SHA256 |
| --- | --- |
| `docs/specs/combat-actual-round-entry-v1.md` | `ed4fd69b4f599acae7c4850270557363a546df36d0b984107c50c08c29304548` |
| `docs/specs/combat-actual-round-entry-v1.schema.json` | `677b1c671bebe5f23cf4b80f03078c55000c60e45416701757e2552cbd05edb8` |
| `docs/specs/fixtures/combat-actual-round-entry-v1.json` | `fc17cda07618bdd539a56b8c296037bbf36f37c6eb2ce6258c2b789b765f640b` |
| `docs/specs/verify-combat-actual-round-entry-v1.py` | `2596531cb50ddbae7f1e606284f3d845b224d4f1fc10f478b717843d1062990c` |
| `docs/design/combat-cycle-implementation-plan.md` | `d7aa53239cc1b330356e136b140656239a8700b9e15b16b9b607372bcd730265` |

Status: correction author REVIEW_READY, fresh independent review instance2of9/set1pass2 pending.
Brain owns dispatch/reconciliation. No reviewer was dispatched here; no budget reset.
User publication authorization persists for draftPR167. Keep review read-only; no native/merge.


Publication reconciliation: live GitHub verification after correction push found researchPR166
merged atd0cc7344399e38c6d1a89992199bf629307f8f51, mergedAt2026-10-10T15:17:24Z.
PR167 is open againstmain and no longer draft; earlier stacked/draft statements are historical.
Corrected implementation6ac709b remains the frozen primary target; administrative commit836f4a7
packaged evidence, followed by this live-status reconciliation only. Fresh review2 remains pending.
