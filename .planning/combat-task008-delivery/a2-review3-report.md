# A2 independent review

Review instance: 3 of 3.

## Preliminary ledger — recorded before reading author explanation

Scope verified: branch `codex/combat-task008-creation-snapshot`, HEAD/base `a8eed35bbf22ba69959530a7f0f703ca3a8f6361`; three untracked C# files and eight unstaged documentation/planning changes. No staged changes. Read root instructions, bootstrap, independent-review skill, original C2 contract from base, complete new C# diffs and proposed specification/plan/navigation changes.

Independence disclosure: no implementation history inherited. Required CCE recall/search incidentally returned short author intent, evidence/PR headers, publication-evidence recommendation and opening scope paragraph from review2 report. No prior finding or verdict appeared. Did not open prior reports or author packet before this ledger; incidental snippets limited perfect blindness.

- Creation Snapshot12 construction copies supplied event bytes before validation/digests, reconstructs through trusted Created11 readback, computes framed BE64 prefix and raw event digest. No defect identified.
- Serializer emits complete fixed creation slots; bytewise comparison rejects alternate syntax, missing fields and all noninitial changes. Exact frozen fixture plus independent hash/prefix assertions cover parity.
- Tests cover seed extrema, caller-owned buffers, fresh admission disabled, foreign self-consistent evidence, canonical/raw mutations and frozen negatives. Need focused execution/oracle confirmation.
- Publication requirement remains explicitly open under HOST-PUB-001, jointly owned and gated before production/durable acceptance; Task008/H codec completion cannot discharge it. Existing parent requirement retained. No plan blocker identified.
- Residual check: inspect retained author assertions against observed source and executed tests; no full build or broad suite rerun justified.

## Findings

No actionable findings. Source and plan support bounded A2 acceptance. No optional nits promoted to blockers.

## Plan Review

A2 row's exact creation golden, trusted request/Created11/World reconstruction, disabled-admission readback and noninitial rejection are implemented and directly tested. B–H/019A remain pending; no generic Snapshot12 registration, historical reader modification or host behavior enters scope. New runtime-evidence allocation retains original actual-persistence requirement verbatim, adds joint Core/OrleansHost ownership and explicit provider failure matrix, and prohibits interpreting in-process D/H completion as full Task008 publication acceptance. Provider proof follows020–021 plus verified023 and precedes production hosting/durable save acceptance, consistent with retained HOST-RSH-001 direction. No heavy pivot or scope expansion needed.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Created bytes bounded, copied, validated before hashes | `CampaignCreationSnapshotV12.Create`, constructor; existing `CampaignCreatedV11Serializer.Deserialize` | Confirmed | Receipt and prefix bind one private validated byte copy |
| Complete frozen creation projection, later values reject | Codec `Serialize`/`Deserialize`; base C2 specification; oracle `make_snapshot`; literal7903-byte test | Confirmed | Exact comparison sound for fully derived creation cut |
| D1 domain/NUL/BE64 framing | Constructor; base D1 lines93–95 and152–159; independent frozen/variable-length prefix tests | Confirmed | No prefix-domain or length discrepancy |
| Mutable input/output buffers cannot alter completed value | `CallerByteMutationCannotChangeValidatedSnapshotOrReceipt`; private event model and no retained caller arrays | Confirmed | No newly exposed mutable byte storage |
| Closed admission does not disable retained recovery | `RetainedSnapshotReadsAfterFreshAdmissionCloses` and pure cut implementation | Confirmed within Core scope | Does not establish host/registry/provider behavior |
| Publication proof remains open | Spec `Runtime evidence ownership`, plan gate audit/A2 boundary, roadmap and navigation changes | Confirmed | Parent Task008 cannot receive whole-contract completion claim |
| Focused34 and full1874, build and format pass | Independently reran focused34; author evidence records broader checks and lead reports completion | Focused independently confirmed; broad checks reported, not rerun | No redundant broad suite required for three internal files |

Read author packet only after preliminary ledger. Author evidence document itself contains brief earlier-round status claims; those claims were not used as evidence or readiness input.

## Verification Performed

- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatCreationSnapshotTests' '-bl:/tmp/a2-review3-{}.binlog'`: initial sandbox attempt exited134 because native MTP local named-pipe bind was denied. Same command with approved environment access passed34, failed0, skipped0 in1.068s. Successful binlog: `/tmp/a2-review3-20260919-212539--48190--kaiyn3-dotnet-test.binlog`.
- `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-authority-envelope-v1.py`: passed4 goldens,67 mutations,36 raw-byte negatives,9 recovery checks,12 identity/context forks,2 turn boundaries,693 nested type negatives.
- `git diff --check`: passed.
- Reviewed original C2 requirements, D1 framing, HOST-RSH-001 evidence limits, proposed plan/spec/navigation diffs, all three new source/test files, and smallest relevant Created11 reader/oracle context. CCE expansion by returned location failed (`Chunk not found`); read known locations directly for that context.
- Source SHA-256: model `8fbd296e1b820cf20a43db59fe566750db61530abbd95017a8b32805b8f230ab`; codec `148f458b818d769c579f4ee299ae56687e2a666b247d74736cf7b04ede118d3b`; tests `ab5d7babc7caa7962ae8133e06357dbaffe21ac60092e287b885d5113211b19a`.

## Open Questions And Residual Risks

No unanswered question blocks A2. Actual provider atomicity, crash/restart, fencing, ambiguous acknowledgments and retained-identity lookup remain unproved and explicitly owned by HOST-PUB-001. Later causal readers, public registration and parent Task008 acceptance remain outside A2. Focused runtime execution used existing lead-built artifacts; no independent rebuild or full-suite rerun claimed. Limited incidental CCE exposure described above remains independence caveat.

## Verdict

**Ready** for bounded Task008 A2 creation-only Snapshot12 and publication-evidence allocation. This verdict does not complete parent Task008 or publication/runtime recovery gates.

## Recommended Next Actions

Lead may accept A2, update review-pending evidence/status and retain scoped completion wording. Preserve HOST-PUB-001 on subsequent Task008/Task025 and hosting ledgers. Three scheduled review instances exhausted; no further review spawned or requested, because no blockers remain.
