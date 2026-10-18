# P01 AC-006 private boundary evidence

**Status:** partial, no admission. The canonical
[P01 acceptance matrix](../specs/combat-actual-moved-repeat-position-v1.md) remains open.

The pinned Land Rules and September errata PDFs are checked by the inherited R03 ancestry replay.
September errata §15.88 distinguishes assault at Cohesion −17 from enemy movement adjacent at
−26; §50.12 requires actual assault for ammunitionless surrender. The diagnostic reads the
pre-event ammunition and refuses a typed automatic-surrender settlement receipt because no
ordinal-two full-TOE/custody transition is yet defined. `CMB-POL-009` excludes voluntary
surrender only; it does not waive automatic surrender.

The v0 input/output inventory and source/cancellation semantics are in
[`combat-p01-surrender-rba-boundary-v0.md`](../specs/combat-p01-surrender-rba-boundary-v0.md).
Focused tests include both thresholds, post-cost zero ammunition, absent trigger, mutually
exclusive decline/cancellation, invalid source fields and fake settlement receipt. The all-row
verifier reconstructs CreationRequest plus eleven independent R03 ledgers and checks both units
at each authentic pre-assault ordinal-two terminal. It also repeats the eight existing
ordinal-one selection/RBA traces, labelled as predecessor control regression only.

The remaining AC-006 work needs an authenticated ordinal-two trigger event and target identity,
pre-trigger resource/receipt binding, full-TOE surrender World and custody settlement, and
versioned decline/cancellation replay with trusted deadline/actor/prefix. Until then every
diagnostic has `automaticSurrenderSettlementProved=false`,
`ordinalTwoRbaProofProved=false`, `admitted=false` and no public actions.

## Local evidence at this branch

```text
python3 -B docs/specs/test-combat-p01-surrender-rba-boundary-v0.py
Ran 5 tests — OK

python3 -B docs/specs/verify-combat-p01-surrender-rba-boundary-v0.py \
  --evidence /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence
verifiedRows=300; provisionalPositive=44; provisionalEmpty=256; admittedRows=0
inspectedUnits=600; priorSelectionRbaTraces=8
automaticSurrenderSettlementProved=false; ordinalTwoRbaProofProved=false; publicActions=[]
```
