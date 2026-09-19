# A2 independent review

Review instance: 1 of 3.

## Preliminary ledger — before author explanation

Reviewed working tree on codex/combat-task008-creation-snapshot, HEAD/base a8eed35bbf22ba69959530a7f0f703ca3a8f6361. Three untracked C# files and documented navigation/spec/plan deltas match bootstrap. Execution ledger is tracked modified auxiliary evidence. No staged changes.

- No concrete implementation defect found: construction validates exact Created11 against trusted request, hashes private copied bytes, writes BE64 creation prefix, and readback compares every reconstructed canonical Snapshot12 byte. Later states reject rather than reset.
- Tests cover golden/prefix parity, caller mutation, disabled admission, ulong seed boundaries, missing/foreign evidence, rehashed foreign history and noninitial/noncanonical negatives.
- Base C2 explicitly requires actual persistence for Task008 atomic uniqueness and response loss. Added HOST-PUB-001 preserves obligation as open, assigns Core/OrleansHost and failure matrix, and distinguishes child Core completion from parent acceptance. Base checkpoint D already defines in-process recovery; HOST-RSH-001 already schedules production after020–021/023. No waiver found.
- Top-of-plan progress prose still describes A1c current branch/A2 pending. Low-impact progress staleness; not runtime/plan correctness defect, can update during final evidence reconciliation.
- Verify tests/oracles and inspect author claims next. No author explanation or prior report read before this ledger.

## Findings

No actionable findings in reviewed A2 implementation or publication-evidence allocation.

## Plan Review

A2 acceptance row is satisfied: exact 7,903-byte Snapshot12 golden; trusted request/Created11/World reconstruction; independent creation event hash and D1 prefix framing; readback with fresh admission disabled; rejection of later state and unsupported bytes. New types remain internal and disconnected from public registration. No existing codec, protobuf or runtime dispatch changes.

Compared publication language with `git show a8eed35:docs/specs/combat-authority-envelope-v1.md`, current implementation plan checkpoint D, and HOST-RSH-001. Original actual-persistence requirement remains verbatim and explicitly open under HOST-PUB-001. Assigned owner, timing, expected-head batch target and failure matrix make evidence boundary concrete without selecting storage. D/H may close only Core codec/replay scope, not Task008 publication obligation. Production timing already follows public020–021 plus verified023 in HOST-RSH-001. This is compatible clarification of child scope, not evidence that parent008 or durable save is complete.

Plan's top progress summary still needs normal post-review status reconciliation; detailed A2 row and publication gate are accurate. No heavy pivot indicated.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Validate private copy before event/prefix hashing | CampaignCreationSnapshotV12.Create and constructor; caller mutation test | Confirmed | No retained caller byte-array dependency |
| D1 domain/NUL/BE64 framing | Constructor; literal golden and independent prefix test; D1 oracle | Confirmed | Prefix exactness covered independently |
| Complete reconstruction rejects noninitial state | Codec.Deserialize exact SequenceEqual of Serialize(expected); mandatory null/empty writes; negative tests | Confirmed | Safe creation-only scope; no silent reset |
| Disabled admission preserves retained recovery | RetainedSnapshotReadsAfterFreshAdmissionCloses and CampaignCombatCreationCut.Decide | Confirmed | Pure Core proof only; no running host/registry assertion |
| Golden, foreign evidence and unsigned seed tests pass | Independent focused34 execution | Confirmed | Runtime parity demonstrated for bounded scope |
| Publication requirement preserved open | Base C2 comparison; new runtime ownership section; plan/roadmap | Confirmed | Cannot claim parent008 publication completion |
| Lead full build/format passed; full suite pending | Author evidence and initiating task statement | Reported, not independently rerun | Final delivery must retain lead's completed full-suite evidence |
| Worker RED failed before implementation | Author evidence only | Unverified | Does not affect direct inspection and passing independent checks |

## Verification Performed

- `git branch --show-current`, `git rev-parse HEAD`, `git status --short`, actual tracked diff plus untracked source/test inspection: scope matches bootstrap at base a8eed35bbf22ba69959530a7f0f703ca3a8f6361.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatCreationSnapshotTests' '-bl:/tmp/a2-review1-{}.binlog'`: initial sandbox attempt failed before test execution because runner IPC socket creation was denied. Same command outside sandbox passed34, failed0, skipped0. Verified `/tmp/a2-review1-20260919-211958--46238--b6uE1V-dotnet-test.binlog` exists.
- `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-authority-envelope-v1.py`: pass;4 goldens,67 mutations,36 raw-byte negatives,9 recovery checks,12 identity forks,2 turn boundaries,693 nested type rejections.
- `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-cycle-sequence-v1.py`: pass;112 positions,6 cycle edges,4 goldens,38 mutations,3996 scope/ordinal identities,896 actor materializations, prefix/framing negatives.
- `git diff --check`: pass.
- CCE context search used for implementation and surrounding dependencies. Its expansion request returned `Chunk not found`; exact known-file inspection used fallback. Required session recall returned historical metadata including prior checkpoint verdict summaries incidentally; no prior review report or author explanation read before preliminary ledger.

## Open Questions And Residual Risks

Full suite/build/format not rerun by reviewer; lead owns integration checks. No provider proof, process recovery, public registration, inherited-event replay, later Snapshot12 or complete Task008 acceptance established. These are explicit retained gates. Focused tests reuse trusted fixture Setup/Config and predecessor validated code; they do not establish independent artifact custody or host authentication.

## Verdict

**Ready** for bounded A2 creation-only Snapshot12 and publication-evidence documentation. No finding requires fix or heavier plan change. Verdict does not extend to parent Task008, durable publication or gameplay activation.

## Recommended Next Actions

Retain completed lead integration evidence, reconcile progress prose when recording A2 review completion, and continue existing bounded review/delivery sequence. Keep HOST-PUB-001 open until actual provider matrix passes.
