# Combat Cost and Settlement Independent Review 3

Review instance: **3 of 3**. Date: 2026-09-06.

- Base: `063ca2e8ed40614fce6e22120adde8e0acbb725d`.
- Reviewed head: `6228119f2c5a292442d6aebf21f5e0709df83169`.
- Branch: `codex/cmb-des-004-resolution-order`.
- Scope: two commits, six documentation files, +498/-14; clean review worktree.
- Reviewer: fresh-context `combat_settlement_independent_3`, read-only.

## Findings

**No actionable findings.** Blind preliminary review completed before author explanation was
provided. Reviewer used repository/source evidence without author session-memory lookup; no
additional agents or review instances were created.

## Plan Review

[DES-004](../design/combat-cost-resolution-order-v1.md) and
[DES-005](../design/combat-settlement-disclosure-v1.md) satisfy their bounded design-input scope:

- Atomic commitment preserves costs/history before RNG; both results use frozen strength and one
  shared differential.
- Retreat intent precedes final loss arithmetic; actual movement follows losses. Capture is a loss
  subset, guard formation a separate transfer.
- Loss DP, retreat CP, actual-victory RP, Retreat/Engaged priority and delayed escape replacements
  have explicit boundaries supported by source rules.
- Mandatory settlement choices have finite persisted fallback; immediate obligations close before
  existing round/step completion.
- Disclosure separates authority from approved side projections and requires equivalent submission
  outcomes.
- Roadmap/inventory retain policy, cycle, schema and implementation-plan gates; proposed acceptance
  cases are not represented as implemented tests.

DES-005's integer-CP and geometry restrictions are acknowledged by DES-004. Broader simultaneous
combat, repeated assaults and ongoing custody remain explicit extension/composition work.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Documentation only, six files +498/-14 | Frozen Git range/diff | Confirmed | Runtime regression tests unnecessary for this patch. |
| Fifteen decisions, 24 ACs, 189 local links | Independent structural check | Confirmed | Traceability/navigation consistent. |
| Atomic costs/results and ordered settlement | DES-004/005 against DES-001/002/003 | Confirmed | No contradictory owner or duplicate completion found. |
| Refusal, capture, DP/RP and escape arithmetic reflect sources | Land pp13/24/25/40, Logistics PDF67/68, errata | Confirmed | Rules facts support proposed arithmetic. |
| Prisoner origin/rendezvous are proposals requiring approval | DES-005 custody section; Land28.12/28.17/28.22 | Confirmed | Source does not independently establish digital timing policy. |
| Existing CP/TOE/RNG/disclosure supply patterns only | WorldV5/V6, movement, SandtableRandom, aliases/MapSubmission | Confirmed | Successor contracts remain necessary. |
| Research checks prove implemented settlement/replay/privacy | Author disclaims this; scripts report limits | Not claimed | Runtime correctness remains unverified. |
| All future custody/cycle branches can be implemented under profile | Requirements exist; geometry/cycle runtime absent | Unverified | Retain admission and combined-freeze gates. |

## Verification Performed

- `git status --short --branch`: clean, expected branch.
- `git rev-parse HEAD`: exact frozen head.
- `git log --oneline 063ca2e..6228119`: expected two commits.
- `git diff 063ca2e..6228119 --stat`: expected six-file scope.
- `git diff 063ca2e..6228119 --check`: passed.
- `python3 docs/research/verify-combat-mutable-state.py`: passed eight boundary vectors,
  108 conditional combinations and three guard probes.
- `python3 docs/research/verify-combat-rng.py`: passed 12 seeded vectors, 6,480-coordinate domain,
  1,900,656 expanded leaves.
- Read-only Python Markdown check: 189 local path targets, 15 ordered decision IDs and 24 ordered
  acceptance IDs; no errors.

Visually inspected Land renders13/19/24/25/40 and Logistics PDF67/68; checked errata28.17/50.12/50.2.
PDF text extraction returned no usable text; source assessment used renders. No full loss-table
transcription audit or .NET build/tests performed. No files changed by reviewer.

## Open Questions And Residual Risks

- Owner approval: bounded profile, custody rendezvous, fallback and disclosure policies.
- Current World6 rejects CP above CPA and cannot represent proposed guard/custody/replacement state;
  contract freeze must address these limits.
- Full chart transcription/independent table validation remains outstanding; sampled goldens cannot
  certify every loss coordinate.
- CYCLE-DES-001 must preserve future feeding/replacement obligations and prevent unsupported upkeep,
  guard-action or maturity transitions.
- Replay recovery, calendar semantics and hosted timing/traffic privacy remain unproved until
  contracts and executable checks exist.

These are disclosed downstream gates, not hidden completion claims or actionable patch defects.

## Verdict

**Ready** as bounded DES-004/005 inputs to subsequent cycle and contract design. No production or
runtime activation approval implied.

## Recommended Next Actions

Proceed through the stated policy and CYCLE-DES-001 gates. Author accepted the report with no fixes
required; closeout adds this report and navigation links only.

**Maximum reached: 3 of 3.** No further independent instance starts under this review budget.
Materially revised scope or remaining approval decisions return to the initiating task and human
owner. The earlier [review2](combat-design-review-2.md) remains dated evidence for DES-001/002/003.
