# Independent review: CMB-019D0

Review instance: **1 of 3**. Reviewer `/root/independent_review`, GPT-6.1-sol medium,
fresh `fork_turns=none`, independent-review skill. Neutral bootstrap preceded author explanation;
preliminary ledger was recorded before the separate author packet was read. Read-only review.

## Findings

**No actionable findings.** Branch `codex/settled-continuation-contract`, base/HEAD
`ec0db5319d9598313372ab239253ac1bde351594`, explicit dirty working-tree boundary reviewed.
Five primary paths match bootstrap; other retained files are administrative and `.serena/` excluded.
No production source or old frozen fixture changed.

`replay_source` regenerates native upstream state, then compares complete source to unchanged
Result2 catalogue (oracle lines107–131). Replayable forged clock variants cannot acquire admission.
`commitment_progress` binds actual event receipt/hash to committed history, attacker and scope
(lines157–166). IDs and empty Release receipts cannot substitute. `_bridge` requires exactly two
untimed System events, retains World/RNG/history and returns immutable canonical bytes without
repeat authority or movement charges (lines168–209). All32 contexts have explicit supported
outcomes, source-specific literal costs/counts/duties. Original-distance/exclusion probes are
synthetic. Exact readers and byte-keyed cache preserve ownership and reject altered output.

## Plan Review

Dispatch acceptance, Checkpoint H, Task019D0, movement dependencies and roadmap are consistent.
Five-file scope is respected. Earlier synthetic provenance remains labelled. Parent017–019 closure,
positive campaign admission, settled control/repeat, later-II/consumed, Snapshot and public activation
remain gated. Following native proof/control are separate. No heavy pivot or new workstream needed.

## Author-Claim Reconciliation

| Claim | Evidence | Status |
| --- | --- | --- |
| Replay plus independent catalogue trust | Source reader and valid re-signed clock rejection | Confirmed |
| Native017B basis/null high-water | Python basis and existing C# `CampaignCombatResultRelease.Derive` | Confirmed |
|32 contexts preserve state/costs/duties | Literal tests and independent oracle run | Confirmed |
| Signed receipt/hash and no empty Release progress | Extraction and source/proof rejection tests | Confirmed |
| Immutable/canonical/closed bounds | Readers/cache and tests | Confirmed |
| Old artifacts unchanged | Git scope,29 pins and six oracle runs | Confirmed |
| Semantic RED preceded implementation | Retained two-failure RED log | Corroborated by retained evidence; historical ordering not independently reproduced |
| Full repository gate passed | Author-run log and exit0 | Confirmed as observed author-run evidence |

## Verification Performed

Independently executed in exact review worktree; all exit0:

```text
python3 -B docs/specs/verify-combat-settled-continuation-v1.py
python3 -B docs/specs/verify-combat-result-settlement-v2.py
python3 -B docs/specs/verify-combat-sealed-round-v2.py
python3 -B docs/specs/verify-combat-reserve-release-v1.py
python3 -B docs/specs/verify-combat-ordinary-movement-v1.py
python3 -B docs/specs/verify-combat-cycle-control-v1.py
python3 -B docs/specs/verify-combat-inherited-reserve-movement-completion-v1.py
git diff --check
```

New oracle:9 semantic groups,32 canonical proofs,4 retained descriptors,29 pins. Python AST,
both JSON files and local contract/plan links pass. Observed author `just check`: full2,475 and
Boundary81 passed, zero failed/skipped, build0 warnings/errors and format success. Reviewer did
not rerun .NET checks.

Reviewed primary SHA-256:

```text
afb315fa82f31b5f6300399d5244642f0aacda1e5331e4f73b050d75d199e5e6  docs/specs/combat-settled-continuation-v1.md
b84878bef914b8cb9ea17e31d77d0a49d81cf40a0d23364ad2f06f2db416d076  docs/specs/combat-settled-continuation-v1.schema.json
aa7b752a7ed809a60e841402aa0e4d3cb138fe42f4ecb8bc217aa3d9f331649f  docs/specs/fixtures/combat-settled-continuation-v1.json
a4740c236288389f76b66c645f086ede84821a9d2b3fa494ac535f95f14fa41a  docs/specs/verify-combat-settled-continuation-v1.py
cca15db9b522bbabc5ca8e0a392e2f80e3045c3c3bcf8cdcd2c4cb93e8376db9  docs/design/combat-cycle-implementation-plan.md
```

## Open Questions And Residual Risks

No blocking questions. Graph `sandtable-cmb019d0` correct: new paths not_tracked and plan
metadata_changed; source fallback covered these gaps. Existing cited paths metadata-matched.
Serena manual read and exact worktree activated; no reindex/review edits. Closed catalogue and
synthetic earlier history remain limits. No native consumer or reachable creation-to-settled proof.

## Verdict

**Ready.** Retain report/gates, reconcile administrative status, proceed with authorized draft PR.
No further instance needed unless executable behavior or scope materially changes.

Author response: Accept. No fixes required. Only final status/evidence/publication metadata changes
follow this report; reviewed contract/schema/fixture/oracle hashes remain unchanged.
