# 004A2 source preparation

Read-only agent task004_evidence; no oracle or full gate. Result2 implementation pending.
Canonical sources: docs/design/combat-settlement-disclosure-v1.md:187 and74;
docs/specs/combat-world-settlement-v1.schema.json:13;
docs/specs/combat-world-settlement-v1.md:71 and134;
docs/specs/verify-combat-result-settlement-v1.py:168;
docs/specs/fixtures/combat-result-settlement-v1.json:4.

## Retained branch matrix

Values attacker/defender; mirror both acting sides and use both seal orders.

| Branch | Mandatory choice | TOE | Cohesion | CP | Relation/asset |
| --- | --- | --- | --- | --- | --- |
| ordinary | none |8/9|0/0|5/3|Contact|
| zero-retreat | defender retreat |10/10|3/0|5/4|none|
| refusal-loss-dp | defender refusal |9/7|0/-3|5/3|Contact|
| zero-engaged | none |10/10|0/0|5/3|Engaged|
| defender-capture-guard | attacker guards |8/8|0/0|5/3|Contact;guard|
| defender-capture-escape | attacker unguarded |9/8|0/0|5/3|Contact;defender entitlement|
| attacker-capture-guard-cp-limit | defender retreats/guards |7/9|0/-1|10/11|none;guard|
| attacker-capture-escape | defender retreats/unguarded |7/10|0/0|5/4|none;attacker entitlement|

Synthetic cursor probes, not creation-rooted gameplay. Branch names identify captured side,
not captor. Paired retreat/custody branches have same owner.

## Disclosure and choices

- Result resolution: structural progress only; never dice/differential/result ID/cursor.
- Retreat owner: distance, certified route in path order, own CP cost, refusal, own deadline/bindings.
  Nonowner generic waiting, no private settlement substage/cause.
- Disposition: own receipt/intent; no premature movement/loss or opposing choice disclosure.
- Joint losses: own loss/captured/other loss/remaining TOE and separate loss-DP cause.
- Retreat settlement: own location/CP/Cohesion causes, attacker's proved victory RP; approved
  apparent movement and eventual relation facts only for opponent.
- Custody owner: count, authorized origin/class/location, own bounded choices; victim knows own
  captured loss, never enemy route/guard choice.
- Guard: captor's guard/donor/location/TOE/inherited CP,Cohesion,readiness,ammo/upkeep; no raw asset IDs.
- Escape: victim quantity/reunion recipient/location/earned and eligible scope/entitlement;
  captor's own disposition. No immediate TOE credit or enemy route history.
- Relations/closure: original-pair Contact/Engaged, own retained assets/obligations; no internal counts.
- Candidate order: retreat then refuse-retreat; relocate-and-guard then leave-unguarded.
  Sole live owner only, no zero-distance retreat window or zero-positive-lot custody window.
- Inherit exact A1 history/receipts. Changed reuse rejects; exact retry recovers. System fallback
  never creates player receipt. Revision changes only with authorized facts or own receipt.
- Separate before/after loss-DP and victory-RP causes; no anonymous net delta.

## Bounds / timing / negative pairs

Retained A2 needs CP11, Cohesion-3..+3, four own receipts (RBA/seal/retreat/custody), two actions,
lot1..3, guardTOE1, singleton assets/obligations, retreat route2nodes, guard route<=4nodes.
Replacement delay12 Operation Stages, eligible turn up to115; preserve future obligations.
Measure complete composed history/bytes; never truncate. No positive-coverage claim for Cohesion-4.
New Result2 policy must enter A2 config reference from first frame while accepted A1 goldens stay exact.
Fresh mandatory opening independent of previous private accepted timestamps; live own-window
floor/deadline governs choices.10001/11000 prior retreat then10500 custody is SAME-owner isolation.

Negatives: victim equal histories across private custody choice before authorized entitlement;
nonowner equal histories across private opening/disposition; fallback no player receipt; altered
route order/foreign donor/raw opponent ID/wrong lot/changed reuse/cross-profile config reject;
guard transfer not lossDP; escape not TOE credit; zero-lot no custody; retreat/refusal no Engaged;
guard no inherited relation; closure retains obligations without A2 release/repeat candidates.
