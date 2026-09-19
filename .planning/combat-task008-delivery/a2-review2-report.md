# A2 independent review

Review instance: 2 of 3.

## Preliminary ledger (before author explanation)

Scope verified: branch codex/combat-task008-creation-snapshot, HEAD/base a8eed35bbf22ba69959530a7f0f703ca3a8f6361, unstaged documentation edits and three untracked C# files. No prior reviewer reports read. CCE search exposed brief author/evidence opening snippets inadvertently; full author rationale not read before this ledger. Session recall omitted to avoid prior-review contamination.

- Creation model validates exact Created11 against separately trusted request before event and framed-prefix hashes. Evidence copied before validation/hashing; caller-byte mutation cannot create divergent receipt. No defect observed.
- Snapshot codec reconstructs every creation field, checks exact canonical bytes, and rejects any later state. Fixed null/empty fields agree with canonical C2 requirements and frozen fixture tests.
- Tests cover golden bytes, independent digest/preimage, unsigned seed boundaries, disabled admission, foreign evidence, noncanonical bytes, and frozen snapshot negative vectors. Need execute focused tests and C2/D1 oracles.
- Publication documentation retains original actual-persistence requirement and introduces explicitly open HOST-PUB-001 with owner, seam, failure matrix and Task025 tracking. Need reconcile author claims against this limited scope; no provider proof inferred.
- Existing C2 outcome text says Created11/Snapshot12 serializers absent; historical oracle packet context and pre-existing Created11 wording make this documentation follow-up, not demonstrated runtime defect.

## Findings

No actionable findings in reviewed A2 delta. No prior review reports opened; author evidence file contains an earlier-round verdict line, encountered only after preliminary ledger and not used as evidence.

## Plan Review

A2 meets bounded creation-root/readback row in docs/design/combat-cycle-implementation-plan.md: exact Snapshot12 golden, projection from trusted request/Created11/World, retained readback after fresh admission disabled, and rejection of noninitial state. Frozen bytes, authority dispatch and historical serializers remain unchanged. Three new internal C# files form appropriately narrow implementation slice.

Publication evidence allocation in docs/specs/combat-authority-envelope-v1.md:158–197 preserves original actual-persistence requirement. HOST-PUB-001 names joint Core/OrleansHost ownership, expected-head commit batch, concrete failure matrix and retained configuration/results. Dependency gates D/H expressly cover in-process Core evidence only. Provider proof remains required before production hosting/durable save acceptance and tracked through Task025. This fits existing HOST-RSH-001 timing rather than asserting codecs prove atomicity. No architecture pivot needed.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Validation and private evidence copy precede receipt/P0 digests | CampaignCreationSnapshotV12.Create and private constructor; caller-mutation test | Confirmed | Hashes bind same validated event bytes |
| Prefix uses domain/NUL/BE64 length/exact event | Constructor, literal preimage test, D1 verifier | Confirmed | Matches retained framing without self-reference |
| Entire creation snapshot reconstructed against trusted request | Codec Deserialize, Created11 Deserialize, C2 inventory/fixture | Confirmed | Rehashed or noninitial import cannot supply authoritative state |
| New code has no provider, registry or public surface | All three new files and tracked diff | Confirmed | Creation-only scope preserved |
| Disabled-admission result is pure Core evidence | RetainedSnapshotReadsAfterFreshAdmissionCloses | Confirmed | Does not demonstrate host recovery |
| Golden, mutations and seed tests pass | Independently executed focused test run | Confirmed | 34 passed, zero failed/skipped |
| Publication guarantee stays open | C2 ownership block, plan D/H, roadmap, existing HOST-RSH-001 | Confirmed | A2 readiness does not close parent008 or hosting |
| Full integration passed | Lead reports 1,874 passed; build/format pass in author evidence | Reported, not independently rerun | Avoided concurrent full suite/build; focused independent checks below |

## Verification Performed

- `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-authority-envelope-v1.py`: exit0; four canonical goldens,67 mutations,36 raw-byte rejections,nine recovery boundaries,12 identity/context forks,two turn boundaries,693 nested type rejections.
- `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-cycle-sequence-v1.py`: exit0;112 positions,one interrupt,six cycle edges,two artifact/two identity goldens,38 mutations,3,996 scope/ordinal identities,896 actor materializations,prefix/occurrence negatives.
- `git diff --check`: exit0.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatCreationSnapshotTests' '-bl:/tmp/a2-review2-{}.binlog'`: initial sandbox attempt failed before tests, local named-pipe socket permission denied (exit134). Identical command outside sandbox: exit0,34 passed,zero failed/skipped. Successful binlog `/tmp/a2-review2-20260919-212235--46796--mZPIbh-dotnet-test.binlog`.
- Read-only inspection of Setup7 and World7 ownership confirms immutable/read-only projection shape; no caller event-byte storage retained by new snapshot.

## Open Questions And Residual Risks

Provider selection, authentication-backed retained identity lookup, atomic uniqueness, ambiguous commits, process restart and bounded deduplication remain unimplemented HOST-PUB-001 obligations. Later causal replay and noninitial Snapshot12 remain B–H/019A. Tests deliberately cannot establish those guarantees. No unresolved A2 blocker.

## Verdict

**Ready** for bounded A2 creation Snapshot12 slice. Verdict excludes full parent Task008/publication acceptance.

## Recommended Next Actions

Record A2 evidence and continue initiating task's bounded review/delivery workflow. Preserve HOST-PUB-001 as open until exact provider matrix passes. No additional review dispatched by this reviewer.
