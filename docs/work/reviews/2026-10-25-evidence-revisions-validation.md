# Evidence revision validation handoff

Branch: `codex/rc-reserve-crash-recovery`. Base: `3e2d4336b1505d9f308529841361e5413cf70f74`. Bounded implementation validated; parent/base recorded above. This handoff is committed with implementation.

## Delivered behavior

Exact legacy and refreshed dependency bundles authenticate separately. Retained legacy evidence reproduces original proof/event/command/retry identities; refreshed provenance binds refreshed manifests. Mixed/unknown bundles reject. Legacy145-file archive and two genuine paid/armed R03 vectors retained. No public admission, source-history rewrite or automatic external-store migration.

## Native validation

All rows exit0. Core method rows use `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj -c Release --no-build --no-restore -- --filter-method '*<method>'`; snapshot uses `--filter-class '*CombatDependencySnapshotTests'`. Boundary command is `just test-suite boundary`. Exact command arrays and durations retained locally in `artifacts/rc-source-pins/final-native-summary.json`.

| Selection | Passing cases | Seconds |
| --- | ---: | ---: |
| `Legacy*` | 33 | 12.193 |
| `EveryGenuineParentHasExactThreeCutSourceControlProofAndEventParity` | 56 | 142.765 |
| `AllParentsBothDispositionsMatchEveryReferenceCutAndClaim` | 112 | 339.844 |
| `AllParentsEveryCutMatchesCanonicalEventsControlAndProof` | 56 | 255.128 |
| `CompleteTraceEveryCutAndAllPriorRetry` | 300 | 294.315 |
| `AuthenticTerminalMatchesFrozenReference` | 4 | 10.751 |
| `GenuineInheritedClockVariant*` | 2 | 20.057 |
| `AllPhysicalPinsRemainRequiredAfterWarmReplay` | 1 | 6.64 |
| `WarmDependencyPinsAndEveryHandlerCapacityRemainMandatory` | 1 | 7.065 |
| `OwnedDeliveryBytesAreRevalidatedOnWarmReplay` | 2 | 1.926 |
| `snapshot` | 11 | 1.53 |
| `boundary` | 81 | 8.398 |

Total659 selected cases, not a complete Core run. First561 preceded isolated internal RMC Derive capture fix; next98 rebuilt final source and tested both affected direct-entry flows, physical drift and authority boundaries. Prior moved-assault56, non-moved continuation4/movement4/completion20/breakdown20 and two physical drift tests passed. Initial breakdown20 failures (missing capture) and their successful rerun remain recorded. Final Core Release build and changed-file format verification passed; `git diff --check` passed.

## Oracle validation and remaining gates

Final original Cycle and inherited Breakdown scripts pass, exit0 (0.794s and58.120s). Original Selection, Armed, NoAttack, CycleControl, ReserveMovement, ReserveMovementCompletion and AuthorityComposition scripts passed. Exercise Occurrence full8 groups passed before final digest-only metadata correction. Side Projection full run found three preserved subset hash failures; all non-pin fixture data equal base, three digest constants corrected, exact failed groups and both pin-check functions pass. Python AST checks confirm no oracle logic changes.

Exercise Child pre-final-metadata registry checked134 sources (126 succeeded,8 expected failures,28 historical),2442 steps,44 owner fallbacks; admission, failure/forgery and identity checks passed. Final metadata representative7/failure22/identity/exact fixture checks passed (134 summaries,12 goldens). Final Exercise Parent original main passes all7 groups, including exact fixture and invalid-child/cache/binding/report rejection checks; final pin-closure pass changes0 slots.

Unrun on final state: complete Core solution gate, all complete original native-family Python scripts, full Side/Exercise Occurrence/Exercise Child reruns after final digest-only correction, all300 R03 native hashes (selected4 and two legacy vectors run), hosted exact-head CI. Owning generation functions and selected original groups are not substitutes for these release gates.

Final Snapshot and Outward scripts fail, exit1 (1.559s and6.844s), not waived successes. Outward currently fails requirement anchors:24 offsets in two design docs were already wrong at base; source-pin reconciliation also remains open. Snapshot retains independent `CampaignCurrentEventRuntime.cs` and `CampaignElementMovedV3Factory.cs` pins. No unsupported ancestry promoted:44 provisional positive,256 absent,zero admitted. Public certificates, Runner/studies, dependency remediation, Maproom and product/scenario gates remain open.

## Review and next bounded task

Independent review6/6: **Ready with non-blocking follow-ups** after final artifact delta; [report](2026-10-25-evidence-revisions-independent-review.md). One accepted internal Derive capture finding fixed and focused2 regression cases pass. Accepted P2 corrected and verified. Deferred complete final-head owning-oracle/CI coverage remains release-maintainer responsibility, with required pass criteria in RC checklist. No further fresh-context review instance authorized by this packet.

Next bounded task: independently assess both Snapshot runtime-source changes against snapshot contract, retain RED evidence and refresh only justified pins/derived artifacts. Then separate Outward closure. Exact-head CI required before any merge; no push/PR/merge performed for this packet.

All implementation, native test and provenance processes completed. Reviewer finished. No active worker or test process remains for this packet; no push, PR or merge performed.
