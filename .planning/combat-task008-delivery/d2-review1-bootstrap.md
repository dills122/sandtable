# Fresh Review Bootstrap

Review instance 1 of3 for Task008 D2 Reserve completion codec. Use independent-review reviewer mode.
Repository /Users/dsteele/repos/sandtable, branch codex/combat-task008-reserve-completion,
base/HEAD073423f (D1 PR129), explicit working-tree delta including untracked files.
Read AGENTS/skill, canonical requirements, code/tests and record preliminary ledger BEFORE author.
No prior reports. Do not delegate or start reviews.

## Scope and requirements
Five primary files in d2-source.sha256; README.md, tech-design.md, naming-overview.md,
docs/design/combat-cycle-implementation-plan.md, docs/roadmap/pre-alpha-roadmap.md and execution
status/evidence. Canonical docs/specs/combat-reserve-designation-v1.md/schema/fixture/verifier;
combat-cycle-sequence-v1.md/schema/verifier AUTH_FIELDS and combat-inherited-successors-v1.md.
Review implementation AND D1→D2→019A plan refinement against original requirements.

## Exclusions
D2 derives exact completion2 event including ordinal1 cycle from full genuine predecessor history.
019A still owns applying SAME event atomically, terminal readback/retry and Movement handoff.
No generic restore, public ingress, durable publication. Preserve open HOST-PUB-001.

## Verification
Verify source hashes. Focused no-build allowed:
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatReserveCompletionTests' --filter-class '*CombatReserveDesignationTests' '-bl:/tmp/d2-review1-{}.binlog'
No builds while lead integration runs. Unchanged oracle baseline /tmp/d-reserve-baseline.log;
inspect canonical source+baseline, rerun only if concern warrants.
Only write .planning/combat-task008-delivery/d2-review1-report.md; no code/docs edits/staging/commits.
After preliminary ledger read d2-author.md and d2-evidence.md. Return actionable findings, plan
assessment, claim reconciliation, exact checks/residual risks and evidence-based readiness verdict.
