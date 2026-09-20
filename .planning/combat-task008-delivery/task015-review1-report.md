# Task015 independent review

Review instance: 1 of 3

## Findings

No actionable findings. Reviewed bounded dormant custody implementation, not public or durable campaign activation.

Target verified: `d00a230..00a8b68c580a7efc38070571750c9a973e0fa579`; detached isolated clone `/tmp/sandtable-task015-review1-1813`. Source checkout was `codex/combat-custody-closure-session` at `ba54efd720b38627b473cfd5d2e2ba4ee638d6b2`. All seven pinned source/test paths identical between candidate and source HEAD. Source dirty checks/evidence artifacts excluded. Candidate diff also contains README, roadmap, technical-design changes and six delivery artifacts; only author explanation and worker evidence read after blind ledger. No CCE, memory, older review, aggregate evidence or developer notes accessed.

Blind preliminary ledger saved before author packet: `task015-review1-preliminary.md`. Tests and acceptance criteria inspected before implementation. No source edits made; final clone remains clean at candidate.

## Plan Review

Task015/checkpoint G requirements covered within existing certified synthetic C3a/Round2 boundary:

- Positive capture alone opens captor-owned custody decision. Noncapture retains retreat state without invented custody receipt.
- `CampaignCombatResolution.cs` extends existing checked transition/replay path. Own-window timing governs custody; accepted audit maximum cannot constrain opening. Actor/choice/context checks precede fallback. Exact accepted retry recovers retained receipt; later callback cannot transfer twice.
- `CampaignCombatLossRetreat.cs:110` reconstructs custody from post-retreat projection and compares full expected settlement. Guard path transfers exactly one donor TOE while retaining resources and original victim source; escape path stores entitlement without victim TOE restoration. Lines 123–135 retain upkeep/training gates.
- `CampaignCombatObligations.cs:281` prevents out-of-order/duplicate custody append; existing constructor binds predecessor identities. Codec validates full typed projection before canonical guard provenance serialization.
- Six new facts cover 176 literal events, 208 state hash cuts, eight guarded/eight escaped outcomes, four CP11 guards, 200 same-owner clock comparisons, replay retry/discard and malformed/rehashed data.

Seven physical paths exceed plan's illustrative 3–5-file estimate, but only five materially change functionality; two existing tests adjust future-family cutoff. Scope remains cohesive. Documentation accurately says implementation under verification and retains later gates. Existing naming-overview needs no name/architecture change.

Checkpoint G as a whole remains open pending Task016 relationships/closure. Positive campaign provenance, successor Snapshot, public activation and HOST-PUB-001 remain excluded. No production migration, provider decision or rollout introduced.

## Author-Claim Reconciliation

| Claim | Inspected evidence | Status | Consequence |
| --- | --- | --- | --- |
| Same trusted predecessor replay remains sole authority | Resolution Replay/ApplyTrustedBoundary; codec ReadState; rehashed-forgery tests | Confirmed | Caller World/hash cannot replace causal replay. |
| Independent captor window, retry before clock/stale rejection | Resolution Transition; 200-pair custody test | Confirmed | Same-owner prior timestamp isolation holds in bounded corpus. |
| Guard transfers one TOE and preserves post-retreat CP11/provenance | LossRetreat Project lines 116–124; conservation tests | Confirmed | Resources retained without double loss or reset. |
| Escape retains source, costs, twelve-stage gate and no immediate TOE | Custody lines 141–170, Project lines 128–135; fixture/state tests | Confirmed | Maturity obligation retained rather than executed. |
| 176 events/208 hashes and 23 focused facts | Independent focused test execution | Confirmed | Exact event/cut agreement proven. |
| Literal paths cover one/two-edge guards and zero-edge escape only | Independent fixture inventory: guard1=4, guard2=4, escape0=8 | Confirmed | Do not promote literal corpus into route-maximum coverage. |
| Certified profile supports route/resources | Certification CertifyInitialProfileFacts/ProveSupport; SealedRound commit requires ammo10 and records ammo0; adjacent tests | Confirmed | Fixed two-unit Clear initial geometry supports implementation assumptions. |
| Earlier worker red/green and 100 shared tests passed | Worker report read; original historical logs not audited | Unverified historical claim | Not needed for verdict; own 23 focused plus 91 adjacent checks passed. |
| No host transaction/public activation claim | Diff scope and entry points | Confirmed | Readiness limited to dormant mechanism. |

## Verification Performed

All commands used `login:false`, clone cwd `/tmp/sandtable-task015-review1-1813`, unique `/tmp` outputs. Test commands use native MTP `--project`. Required local IPC escalation approved for successful test/format runs.

1. `git clone --shared --no-checkout /Users/dsteele/.codex/worktrees/8fcc/sandtable /tmp/sandtable-task015-review1-1813` then `git -C /tmp/sandtable-task015-review1-1813 checkout --detach 00a8b68c580a7efc38070571750c9a973e0fa579`: passed.
2. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatCustodyTests' --no-ansi -bl:/tmp/task015-review1-custody.binlog`: failed before tests, NU1900 vulnerability-index access failure. Log `/tmp/task015-review1-custody.log`.
3. `dotnet restore tests/Cna.Core.Tests/Cna.Core.Tests.csproj -p:NuGetAudit=false -bl:/tmp/task015-review1-restore.binlog`: exit0. Cached restore succeeded; vulnerability audit not performed. Log `/tmp/task015-review1-restore.log`.
4. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatCustodyTests' --no-ansi -bl:/tmp/task015-review1-custody-rerun.binlog`: exit134, sandbox denied native MTP named-pipe bind. Log `/tmp/task015-review1-custody-rerun.log`. No passing test claim from this attempt.
5. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatCustodyTests' --filter-class '*CombatLossRetreatTests' --filter-class '*CombatResolutionTests' --no-ansi -bl:/tmp/task015-review1-focused.binlog`: exit0, 23 passed, 0 failed/skipped, 33.250s. Log `/tmp/task015-review1-focused.log`. Includes compile/build of Core and tests.
6. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --no-restore --filter-class '*CombatSelectedRulesTests' --filter-class '*CombatWorldTests' --filter-class '*CombatIdentityTests' --filter-class '*CombatStepsTests' --filter-class '*CombatSealsTests' --filter-class '*CombatCommitTests' --no-ansi -bl:/tmp/task015-review1-shared.binlog`: exit0, 91 passed, 0 failed/skipped, 17.225s. Log `/tmp/task015-review1-shared.log`.
7. `python3 -B docs/specs/verify-combat-result-settlement-v2.py`: exit0; 10 semantic groups, 32 traces, 304 cuts, 3,728 mutations, 1,360 raw rejects, 384 timing checks, 200 prior-time comparisons. Log `/tmp/task015-review1-oracle.log`. Oracle includes later contract closure, not proof runtime Task016 implemented.
8. `dotnet format tests/Cna.Core.Tests/Cna.Core.Tests.csproj --verify-no-changes --no-restore --include src/Cna.Core/Campaigns/CampaignCombatResolution.cs src/Cna.Core/Campaigns/CampaignCombatResolutionCodec.cs src/Cna.Core/Campaigns/CampaignCombatObligations.cs src/Cna.Core/Campaigns/CampaignCombatLossRetreat.cs tests/Cna.Core.Tests/Campaigns/CombatCustodyTests.cs tests/Cna.Core.Tests/Campaigns/CombatLossRetreatTests.cs tests/Cna.Core.Tests/Campaigns/CombatResolutionTests.cs`: exit0, empty log `/tmp/task015-review1-format.log`.
9. `git diff --check d00a230 00a8b68`: exit0. Final `git status --short` empty; final HEAD unchanged.

All review-owned test/oracle/format sessions completed before return. No global build-server shutdown used because other review processes share host.

## Open Questions And Residual Risks

No unresolved blocker. Full solution gate and remote CI not executed in this review; parent integration must retain its separate evidence. No network vulnerability audit result claimed.

Existing certification admits turn1/stage1 initial two-unit profile only. Literal maturity test therefore covers turn5/stage1; checked calendar formula inspected, but no runtime late-horizon custody test claimed. Arbitrary map paths, nonzero donor ammo, multiple lots and repeated positive campaign histories require later profile/integration proof. Guard feeding and replacement training remain explicitly unsupported future execution gates.

## Verdict

**Ready** — bounded Task015 dormant custody implementation only.

## Recommended Next Actions

Retain separate full-gate/CI evidence and existing provenance/publication exclusions. Continue Task016 only through its own implementation/review gate; this verdict does not accept whole checkpoint G or future-obligation execution.
