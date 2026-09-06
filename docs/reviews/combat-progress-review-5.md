# Combat progress independent review5

**Review instance:5 of7. Verdict: Ready with non-blocking follow-ups.**
Date2026-09-06. Fresh-context read-only reviewer `combat_progress_review_5`, no inherited author
conversation. Preliminary ledger delivered before reading author explanation. No files modified.

Target: branch `codex/cmb-des-004-resolution-order`, base `063ca2e8ed40614fce6e22120adde8e0acbb725d`,
HEAD `e3d1a267bac9b3ef6711c9ca6a147b7c5bf3e1f5` plus explicit working-tree additions.
Exact28-file path/hash boundary verified before and after review. See retained
[bootstrap](combat-progress-review-5-bootstrap.md), [manifest](combat-progress-review-5-manifest.json)
and separately delivered [author explanation](combat-progress-review-5-author.md).

## Findings

**P3 — Refresh current navigation and review-budget status.**

Frozen `docs/README.md:83` still describes TASK-001 as in progress and contract freeze as future
work. Line90 says review budget remains exhausted at four passes. Frozen
`docs/research/combat-cycle-source-inventory.md:203` similarly says no fifth review is implied,
while its opening status announces the requested progress review. Readers entering through these
documents receive conflicting completion and authorization information. Smallest correction:
update current summaries to completed001/002/003A, reviewed003B status, open003C/D/004 gates and
cumulative five-of-seven review accounting. Preserve historical reports at original counts.

**No substantive contract, policy or plan blocker found within the stated contract-only boundary.**

## Plan Review

Task ordering remains coherent: accepted policies and source normalization precede exact contracts;
combined checkpoint B precedes runtime consumers; public projection/admission precedes Exercise/
Runner adoption. The earlier ordinary break-off gap has explicit source, contract, implementation
and continuation-witness owners. Source research covers highest applicable Contact/Engaged cost
plus terrain, ordinary150%-CPA limits and immediate excess DP.003D/018/019 retain atomic membership
changes and reuse the same movement rule for continuation.

TASK-003 remains properly open. Plan lines106–112 assign Rules/config bytes, creation/snapshot/
sealed envelopes, cycle/history/movement reconciliation and checkpoint acceptance before
implementation. World settlement values preserve one current resource ledger, historical pre-loss
evidence, capture as a casualty subset, separate guard transfer, original participant identities
and future obligations. World contract line162 prohibits unsupported upkeep/maturity execution
beyond the bounded terminal. No heavy architectural pivot is warranted.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status / consequence |
| --- | --- | --- |
| Docs/fixtures/Python only; no runtime changes | Git range, dirty state, exact manifest | Confirmed |
| Amendment changes only defender+2/34,35,36 to10% | Raw/optical fixture, normalization and checks | Confirmed against retained evidence; historical intent unproved |
| Content7/Setup7 bind explicit seeds, provenance and initial ledgers | Specs, literal fixtures, validators/negatives | Confirmed |
| No duplicate current TOE/CP/Cohesion ledger | Schema and materialized World states | Confirmed; before-state records are historical evidence |
| Retreat/capture/guard/escape order and conservation | DES004/005, policies, oracle and expanded sweep | Confirmed within selected fixture surface |
| Six goldens/57 negatives/112 cuts/8,840 arithmetic cases pass | Independent execution | Confirmed |
| Cut checks prove production restart | Explicitly disclaimed; trusted-input reconstruction | Not claimed; actual restart remains future gate |
| Current navigation tracks progress | README/roadmap/plan versus docs index/inventory footer | Partially contradicted; P3 |
| Rules bindings, public privacy and runtime remain pending |003C/D/004 and runtime gates | Confirmed |

## Verification Performed

- `python3 docs/research/verify-combat-source-freeze.py`:360 normalized coordinates,357 unchanged
  source values,three amendments,6,480 joint coordinates,8,840 settlements,12 seeded checks,
  five calendar and eight movement vectors passed.
- `python3 docs/specs/verify-combat-content-v7.py`:10,339 canonical bytes,70 negatives,eight geometry
  probes and hash/order checks passed.
- `python3 docs/specs/verify-combat-creation-ledger-v1.py`:1,655 Setup bytes,2,685 initial-element
  bytes,63 negatives,alternate holder,provenance propagation and stage2 refusal passed.
- `python3 docs/specs/verify-combat-world-settlement-v1.py`:six goldens,57 negatives,112 isolated
  cuts/20scenarios,8,840 arithmetic cases and eight calendar boundaries passed.
- Additional read-only sweep below:19,456 World materializations across every selected coordinate,
  applicable retreat/custody choices,both attack orientations and maximum pre-assault CP; checked
  loss arithmetic,conservation,guard transfers and mandatory retreat CP.
- `git diff --check` and `git diff 063ca2e8ed40614fce6e22120adde8e0acbb725d HEAD --check` passed.
- Exact manifest/path reconciliation and461 local Markdown path targets passed.

CCE confirmed reuse of existing component TOE/provenance. No .NET build/tests or runtime services
run; runtime source/tests unchanged. The reviewer supplied the exact previously executed sweep
and exit0 output after its report, without rerunning or starting another review instance:

```sh
python3 -B - <<'PY_SWEEP'
import importlib.util,json,itertools
from pathlib import Path
p=Path('docs/specs/verify-combat-world-settlement-v1.py').resolve(); s=importlib.util.spec_from_file_location('w',p); w=importlib.util.module_from_spec(s); s.loader.exec_module(w)
m=json.loads(Path('docs/specs/fixtures/combat-world-settlement-v1.json').read_text()); template=m['cutCases'][0]; count=0; branches=set()
for diff,ac,dc in itertools.product(w.source.DIFFS,w.source.COORDS,w.source.COORDS):
 trigger=(ac//10+ac%10 in w.DATA['attacker_capture_sums'][str(diff)] or dc//10+dc%10 in w.DATA['defender_capture_sums'][str(diff)])
 for die in range(1,7) if trigger else [None]:
  facts=w.result_facts(diff,ac,dc,die)
  for retreat in ['retreat','refuse-retreat'] if facts['requiredRetreat'] else ['not-required']:
   a=(facts['attackerPercent']+9)//10; d=(facts['defenderPercent']+(10 if retreat=='refuse-retreat' else 0))//10
   captured=(a if facts['capturedRole']=='attacker' else d if facts['capturedRole']=='defender' else 0)*facts['captureShare']
   for custody in ['relocate-and-guard','leave-unguarded'] if captured else [None]:
    for side in ['axis','commonwealth']:
     c=template|dict(differential=diff,attackerCoordinate=ac,defenderCoordinate=dc,captureDie=die,retreatChoice=retreat,custodyChoice=custody,attackerSide=side,preAssaultCp=[5,7])
     world=w.world_at(c,'relationships'); w.assert_conservation(world)
     assert world['settlements'][0]['losses']['roles'][0]['lossToe']==a
     assert world['settlements'][0]['losses']['roles'][1]['lossToe']==d
     assert world['settlements'][0]['retreat']['afterCp']['numerator']==10+(retreat=='retreat')
     count+=1; branches.add((facts['capturedRole'],retreat,custody))
print('PASS expanded World materialization:',count,'cases;',len(branches),'role/choice branches; max pre-assault CP both orientations. Fixture oracle only, no runtime replay.')
PY_SWEEP
```

Observed output: `PASS expanded World materialization: 19456 cases; 11 role/choice branches; max pre-assault CP both orientations. Fixture oracle only, no runtime replay.`

## Open Questions And Residual Risks

Source fidelity was checked against retained numeric/provenance evidence; this pass did not repeat
the original visual chart/OCR audit or establish designer intent. Python oracles share modeling
assumptions; expanded materialization does not prove C#, live geometry, atomic publication or replay.
World-only checks cannot authenticate changed context plus changed World;003C must bind creation/
Rules/Content/history.003D must reconcile later movement and predecessor non-null movement-ended/
Breakdown variants; empty fixture is not continuity evidence. Privacy,deadlines,real restart,
unsupported-boundary enforcement and full72-AC mapping remain downstream gates.

## Verdict

**Ready with non-blocking follow-ups** for the reviewed contract-only progress checkpoint and
continued003C work. Parent003,combined checkpoint B and runtime activation remain open.

## Recommended Next Actions

Correct current status summaries, retain report/author disposition, then continue the existing
ordered contract plan. No additional review pass justified solely by navigation corrections.
Preserve cumulative review count: **5 of7 used**.

## Author disposition and closeout

**Accept P3.** Current docs index, source inventory, README, roadmap and combined-plan status now
state completed001/002/003A/003B, open003C/D/004 and cumulative5of7. Historical reports retain their
original verdicts/counts. Follow-up is navigation/status only; schema, golden data and verifier
logic are unchanged from the reviewed hashes. No fresh Ready verdict is inferred from the author
correction. Report/packet retention and closeout commit metadata are outside the frozen code target.
Remaining actionable findings: none after the bounded author correction; residual gates above remain.
