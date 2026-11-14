# Graziani autonomous source and release work packet

**Status:** in progress on `codex/rc-status-reconciliation`; baseline `0f8a828a`.
This packet is scoped for **at least 30 minutes of active work**, not an estimate
of overall release completion. Preserve the existing uncommitted RC documentation.
No scenario content or authoritative runtime is admitted by this packet.

| Work | Planned allocation | Output and exit evidence |
| --- | ---: | --- |
| Original-source crosswalk | 10 minutes | Reconcile selected §32 supply, movement, transport and timing rules with scenario §§59.6/60.92 and errata; distinguish direct source reading, inference and unexamined cases. |
| Setup accounting pilot | 8 minutes | Derive explicit, reviewable source-row counts and arithmetic for bounded initial supply; retain ambiguous values, optional loads and placement choices rather than claiming a complete scenario. |
| Ruling and release dependencies | 7 minutes | Compare Matruh, Tobruk and victory-reference alternatives; document working decisions, negative cases, authority limits, and a dependency-ordered next packet. |
| Evidence and quality review | 5 minutes | Check arithmetic, source locators, document links, row counts and Git diff; record performed/unrun tests and unresolved independent review. |

Allocations are planning targets, not measured per-lane time; work overlaps and
no lane stopwatch was kept. Time and findings must be reported honestly. If source
access or a required decision blocks one lane,
continue the other lanes and state the exact remaining gap. Source scans remain
outside Git; no third-party art or extended rule text is incorporated.

## Guardrails and acceptance

- The source register is a research ledger: only source-backed claims, bounded
  applicability and explicitly qualified inference. A row count does not measure
  total unsupported mechanics.
- RUL-GRA-01/02 remain working interpretations until reviewed and versioned;
  independent review is not satisfied by another copy read by the same researcher.
- Keep actual setup choices distinct from fixed totals and source contradictions.
  Record version/hash and conservation tests before any scenario implementation.
- Complete source/contract and existing skeleton gates in roadmap order. Maintain
  deterministic replay, privacy and unsupported rejection in the proposed path.
- No requirement to rerun full solution tests for research-only edits. Validate
  document links, scoped arithmetic and `git diff --check`; disclose unrun gates.

## Retained work and verification

- Expanded the [source register](2026-11-12-graziani-source-register.md),
  [logistics crosswalk](2026-11-12-graziani-abstract-logistics-crosswalk.md),
  [supply pilot](2026-11-12-graziani-supply-accounting-pilot.md),
  [arrival/OA comparison](2026-11-12-graziani-arrival-scope.md), and targeted
  weather, map, formation and [victory](2026-11-12-graziani-victory-matrix.md)
  analyses. [Next work order](2026-11-12-graziani-rc-work-order.md) lists concrete
  independent review and later release dependencies.
- Corrected the earlier claim that the errata PDF omitted the three formation
  scans: PDF p. 4 includes them. Two extracted image objects were pixel-equal to
  the separately hosted GIFs; those copies are not independent scan evidence.
- `python3 -B docs/work/plans/verify-graziani-supply-pilot.py` passed; five
  deliberately mutated temporary copies failed as expected. Row accounting,
  weather outcome and truck subtotals were independently recomputed in a small
  local script; a link/anchor and whitespace check covered current research
  documents. These checks do not independently verify source transcription.
- `git diff --check` passed for tracked changes; untracked research Markdown
  was also checked for trailing whitespace and missing final newlines. No
  production runtime behavior changed. Full .NET suites, exact-head CI, source
  double entry, map/rights review and end-to-end games were not run or claimed.

Prior focused initiative test evidence belongs to the earlier work session and
does not certify this working tree or a playable scenario. Working decisions are
not independent approval, and all source scans remain outside Git.
