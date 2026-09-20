# Task012 independent review

Review instance: 1 of 3. Reviewer mode; source/Git read-only. CCE unused throughout; no agents or additional review instances.

## Findings

No actionable findings. Frozen implementation satisfies bounded dormant Task012 commitment requirements. Independent focused17/shared55 checks and frozen Python oracle pass. Preliminary ledger below remains intact as evidence of blind-first ordering; its pending-check statements describe that earlier stage, superseded by final verification below.

## Preliminary blind ledger — persisted before author/checks packets

Target: branch `codex/combat-task008-reaction-lifecycle`, base and initial HEAD `6f9b5013ed5ab5f9fdfc3c5b7e6cac064a809405`; explicit frozen working tree. All five entries in `task012-source.sha256` match. Three additional doc changes: README, roadmap, tech-design. New `CombatCommitTests.cs` is untracked at initial inspection; included by manifest. “Four new commits” means four new commitment events, not four Git commits.

Blind sources inspected: three SealedRound files, CombatCommitTests, CombatSealsTests, selection-boundary authentication, initial-profile certification, CombatSpending; canonical Task012/Checkpoint E/F and publication limits; sealed-round-v2 spec/schema/commit oracle; costs and opportunity designs. No author/checks packet or prior review read.

No actionable defect established in blind source pass. Evidence and questions carried forward:

| Area | Independent observation | Remaining check |
| --- | --- | --- |
| Eligibility and provenance | Every operation reconstructs original authenticated Boundary and separate predecessor/round inputs. Event input never supplies its own authority. Initial certification allows only original typed profile with integral cumulative CP; selected limits 5/7. | Execute focused and shared regression tests after clearance. |
| Atomic debit | Prepared plus five step receipts and two sealed role-ordered slots required. Both costs derived before immutable paid World construction; attacker5/defender3, ammo10→0, CP ceiling10, no Cohesion change. One event/receipt/head update. | Check oracle and literal assertions. |
| Paid recovery | Base preserves pre-use World; typed current World holds payment. Exact retries recovered before committed/status/clock gates, with command/actor/primitive validation first. No second charge or draw. | Runtime checks pending. |
| History and lifecycle | Directional attacker→defender edge and segment target use derive from original selected candidate. Commit remains step5/open for later result; postcommit fresh structural continuation rejected. | No result/settlement/public activation claim. |
| Codec and ownership | Syntax validated before trusted-input indexing; closed schema and canonical byte comparison retained. Paid serialization derives CP/ammo from current typed World and rejects unrelated typed changes. Lists copied/read-only; event bytes cloned. | Shared hostile grammar/ownership tests pending. |
| Coverage | Nine new tests include four paid event/state literals, all58 events/68 cuts, cumulativeCP5/7, discard/lost-reply, stale/forged history, paid-world mutation, ownership. Original54/64 assertions remain in CombatSeals. | Python oracle started; not yet recorded as passed. |
| Plan limits | Fits five primary paths and dormant Task012. Candidate discard proves Core candidate atomicity, not host transaction or durable publication. Actual positive provenance, Snapshot12, result/settlement and HOST-PUB-001 remain open. | Reconcile author claims only after this ledger exists. |

.NET clearance marker absent at preliminary stage; no .NET command run. Review remains in progress, without verdict pending verification and claim reconciliation.

## Author-claim reconciliation

Author/checks packets read only after preliminary ledger persisted. No prior review, aggregate evidence, worker notes, developer notes or execution history consumed.

| Claim | Independent evidence | Status / consequence |
| --- | --- | --- |
| Four paid events/states complete58/68 | Fixture inventory independently counted10 traces/58 events/68 states; four committed cases cover both sides and both seal orders. New tests assert exact frozen bytes, not regenerated expectations. | Confirmed structurally; runtime pending clearance. |
| CP5/3 and ammo10/role, ceiling10 | Transition derives role-indexed costs from frozen rules; explicit after<=10 guard precedes ordinary-spending helper. Certification authenticates both starting ledgers and resources. | Confirmed. |
| Typed paid World preserves original eligibility | Separate World property; Base unchanged; WriteWorld reflects current typed CP/ammo and compares all other typed fields against original. | Confirmed; no stale paid-byte cache. |
| Atomic result and exact retries | Immutable construction, event/prefix/receipt return only after state validation; duplicate path recovers retained event bytes. | Confirmed for Core candidate/replay only. No host transaction proof. |
| No RNG draw or step5 closure | Commit carries original RandomState; no RNG API used; Closed/StepIndex unchanged. | Confirmed. |
| Historical test counts and root build | Packet reports17 focused and55 shared plus root build success. | Testimony only; logs intentionally not consumed. Independent checks below govern reviewer evidence. |
| Public/history/Snapshot/result/publication excluded | No changes in those adapters/hosts/contracts; docs explicitly preserve later gates. | Confirmed; no scope expansion. |

## Verification performed so far

- `shasum -a 256 -c .planning/combat-task008-delivery/task012-source.sha256`: five matches initially and after root committed candidate as `8fcf8b9099909d970bfa942cf6dda869de8af38e`.
- `git diff --check`: passed, exit0.
- `python3 -B docs/specs/verify-combat-sealed-round-v2.py`: passed, exit0;12 semantic groups,10 traces,68 cuts,610 replay mutations,340 raw rejects,288 clock comparisons/retries,480 lifecycle retries,30 invalid proposals. Process completed.
- Read-only inline Python fixture inventory and schema comparison: all17 Round2 object descriptors match codec;10/58/68 inventory and both-side/both-order committed rows confirmed. First inventory attempt used nonexistent `events` field and failed with KeyError; corrected to canonical `eventCanonicalUtf8`, then passed. No files generated.
- Two guessed source filenames were absent during read-only inspection; corrected by scoped filename/symbol lookup. No product failure inferred.
- Doc SHA256 at inspection: README `5993debf8f77ecc2ada9964dff7966841e205e2fb0e888da76d3bc559860704a`; roadmap `7e2a77898a9b1fb80bdfdcd7e45d6f6d693e87ba90c2bea3fb8e9b9288d2dd57`; tech-design `6b619f787bd547f07e4c9c0a5b8acf2d9cc8364ad79c2a4c5d8d4b04f107f502`.

Source reread confirms World7 enforces exactly two elements, copy-owned sorted collections and structural equality; whole-world debit loop cannot accidentally charge unselected third units within admitted profile.

## Code and plan assessment

- `CampaignCombatSealedRound.cs:161` implements sole irreversible candidate transition after authenticated Prepared/step5 proof. Costs computed for both roles before fold; explicit CP ceiling10 prevents ordinary-spending helper's broader allowance. `Commit` constructs new typed World, history and target use; no mutable partial result escapes.
- `CampaignCombatSealedRound.cs:43` reconstructs all accepted authority from independent inputs and compares complete event bytes. Original eligibility remains authenticated even after current ammo becomes0; fresh paid World cannot be substituted as original Boundary. Duplicate receipt recovery preserves original bytes while changed/stale proposals reject.
- `CampaignCombatSealedRoundCodec.cs:128` retains canonical frozen World representation while deriving mutable cost fields from typed current World. Structural equality rejects unrelated field changes. ReadState requires full causal replay equality, so syntactically valid rehashed payment/history forgeries gain no authority.
- New tests exercise all four committed literals across side/order mirrors, complete58/68 replay/readback, cumulativeCP5/7→10/10, original Base retention, failed candidate discard, response loss, malformed-before-context, history direction/order, ownership and non-cost World mutation. Existing precommit54/64 and clock/provenance tests remain regression coverage.
- Canonical Task012 and Checkpoint F ordering respected: commit precedes RNG/result, does not close step5, creates no result/settlement/public action. Five primary paths respected; docs accurately label underway verification and deferred integration. Canonical plan's Task012-next wording is pre-acceptance status, not an erroneous assertion that later gates are complete.
- Root candidate commit also includes planning packets outside explicit frozen source/doc target. Only permitted author/checks/manifest contents consumed; excluded dev/worker/execution material was not read. No review claim extends to those packets.

## Final verification

Clearance marker `/tmp/task012-after-boundary-20260920` observed before first .NET invocation. Both commands used `login:false`, approved local IPC escalation, native MTP `--project`, existing build, unique binlogs, and no `--disable-build-servers`.

```text
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class Cna.Core.Tests.Campaigns.CombatCommitTests Cna.Core.Tests.Campaigns.CombatSealsTests '-bl:/tmp/task012-review1-focused-{}.binlog'
```

Exit0;17 passed,0 failed,0 skipped;8.497s. Binlog confirmed: `/tmp/task012-review1-focused-20260920-111939--29477--e30HvB-dotnet-test.binlog`.

```text
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class Cna.Core.Tests.Campaigns.CombatIdentityTests Cna.Core.Tests.Campaigns.CombatStepsTests Cna.Core.Tests.Campaigns.CombatWorldTests '-bl:/tmp/task012-review1-shared-{}.binlog'
```

Exit0;55 passed,0 failed,0 skipped;13.329s. Binlog confirmed: `/tmp/task012-review1-shared-20260920-111956--29489--cm0bX9-dotnet-test.binlog`.

Focused run independently confirms author-reported17-test behavior; shared run independently confirms55 regression cases. Author historical RED/build/format/full-suite timings remain testimony, not reviewer-executed checks. No separate build/full suite/format run: root owns full gates, existing build explicitly authorized. Final scoped `git diff --check 6f9b5013ed5ab5f9fdfc3c5b7e6cac064a809405 HEAD --` across five primary and three doc paths passed. Final HEAD `8fcf8b9099909d970bfa942cf6dda869de8af38e`; all five manifest hashes and all three doc hashes match initial review target.

All reviewer-launched Python/.NET processes exited successfully; no session remains running. Source, tests and Git untouched; only own report edited, plus authorized temporary binlogs from test execution.

## Open questions and residual risks

No unresolved question within frozen scope. Core discard/replay does not establish host transaction, durable storage, process/silo restart or atomic Chronicle publication. Positive campaign provenance, extended Snapshot12, result/settlement and public activation remain later obligations. No broader-profile guarantee inferred from bounded two-unit synthetic histories. Root full-gate/acceptance work remains separately owned.

## Verdict

Ready — bounded dormant Task012 implementation at verified frozen source/doc target.

## Recommended next actions

Root reconcile this report with remaining authorized review instances and full gates, then own acceptance. No fix, new workstream or additional reviewer requested by this instance.
