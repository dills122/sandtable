# H3 independent review

Review instance: 2 of 3.

## Blind preliminary ledger

Recorded before author/checks packets and session recall. No prior review reports read.
Frozen primary hashes pass; HEAD fdd2a4fa348db658af55fbd4d347206beef193f3. Four primary paths include untracked new Reaction test. Supporting documentation matches stated H3 scope.

- Router derives trigger from non-null window hint, but existing trigger reader verifies exact emitted bytes and actual one-move predecessor. Hints cannot establish authority.
- Trigger plus zero/one participant forks map onto F1–F6 existing readers. Readers validate counts and exact emissions; whole remaining tails reach bounded readers. No skipped or ignored tail found.
- New tests reconstruct actual traces for both owners and seven forks, check each intermediate cut, compare event/state hashes against frozen predecessor fixtures, compare root fields against H0, and test omissions/duplicates/reordering/foreign/sibling tails, canonical mutation, signed effects/actors and buffer ownership.
- Independent fixture inventory confirms 50 F1–F6 rows / 34 histories; each root has complete receipts and matching version. Shared earlier Movement cuts intentionally included.
- H4 literal root codec, disabled admission and publication remain explicitly deferred in canonical plan and synchronized docs. No new service/public API or model I/O.
- Concern to resolve with checks packet: test execution evidence for frozen sources. No actionable code defect identified in blind pass.

## Findings

No actionable findings. Existing strict reader boundaries align with new routing graph. Direct closure consumes entire suffix with count <=1; normal lifecycle <=4; active fallback <=2 after exactly one participant; second move exactly one; completion <=3 after exactly one second move. Every path either returns at accepted cut or subjects all remaining bytes to strict replay.

## Plan Review

H3 fulfills bounded F1–F6 routing requirement in Task008 execution index and H execution refinement. Exact authoritative transitions remain in Core reader families; router only partitions owned retained stream. Tests retain actual currentPosition separately from suspended sequencePosition, full receipts, World, window, flow, tracks and progress. H1/H2 compatibility remains exercised by focused HistoryReplay suite; earlier valid Reaction rejection intentionally becomes admission while strict E1 rejection remains.

README, tech-design and naming-overview changes accurately describe ongoing H3 and defer H4. Full literal nineteen-field root serialization, fresh-admission-disabled restore, later gameplay and HOST-PUB-001 remain open; no plan completion overclaim found. No rollout/migration needed for internal dormant routing addition.

## Author-Claim Reconciliation

| Claim | Evidence | Status / consequence |
| --- | --- | --- |
| Hints/tags select candidates only | Replay loop and ReplayReaction; F1–F6 Replay exact canonical emission comparisons | Confirmed; forged hints cannot authorize transitions. |
| Whole tails rejected by strict readers | All ReplayReaction terminal calls pass Count-cursor; family maximum counts and per-event replay | Confirmed; no unvalidated retained suffix. |
| 50 vectors / 34 histories, 2 shared / 32 new | Independent Python fixture identity sets; FrozenReactionInventoryContainsExactlyFiftyVectorsAndThirtyFourHistories | Confirmed. |
| Both owners/seven forks/every selected cut | Cases, Trace, EveryReactionForkAndIntermediateCutMatchesStrictReadersAndFrozenRoots; exact fixture hashes and CheckH0 comparisons | Confirmed. |
| Canonically resigned actor/effect negatives | Forgeries changes typed effects/actors, serializes event, checks readable input plus changed receipt, then Reject | Confirmed; goes beyond invalid raw JSON. |
| Buffer ownership preserved | Unchanged bounded Capture plus RetainedAndReturnedBuffersCannotChangeAnyReactionCut | Confirmed by code and focused test evidence. |
| 129 focused pass; final inventory-only addition passes | /tmp/h3-focused-final.log and /tmp/h3-inventory.log | Confirmed retained execution; not independently rerun here. |
| Build and format pass | /tmp/h3-build.log zero warnings/errors; retained format facts and root confirmation | Build log confirmed; format exit reported by root, empty log alone cannot prove exit. |
| Full cumulative gate | Root explicitly reports Core suite still running | Pending, not counted as pass. |
| No full-root/runtime restore claim | Canonical plan, spec and changed docs | Confirmed; H4 remains required. |

## Verification Performed

- `shasum -a 256 -c .planning/combat-task008-delivery/h3-source.sha256`: four paths OK before/after review.
- `git rev-parse HEAD`, scoped diffs/status: expected fdd2a4fa348db658af55fbd4d347206beef193f3 and declared working-tree scope, including new untracked test.
- `git diff --check`: exit 0.
- Independent `python3 -B` fixture inspection: F1–F6 50 rows, 34 distinct history identities, 2 ordinary-shared / 32 new; every row has full command receipt count and stateVersion equal to event count +1. Event cuts span 11–17 post-created events.
- Compared runtime test fixture bytes against canonical docs fixtures: H0 plus all six Reaction fixture files match exactly.
- Inspected retained exact focused command result: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*HistoryReplayTests' --no-restore '-bl:/tmp/h3-focused-{}.binlog'` produced 129 passed / 0 failed / 0 skipped, 1m58.066s. Subsequent inventory-only targeted run passed 1/1 after two assertions added.
- Inspected build log: success, 0 warnings, 0 errors, 4.21s.
- Required session recall performed only after persisted blind ledger. Recalled context treated as testimony; no prior reports opened or verdicts used.
- No .NET process launched by reviewer; no concurrent suite, no pending reviewer processes. No fixes, Git mutations or delegation.

## Open Questions And Residual Risks

Root cumulative Core/boundary gates pending at review close. Retained focused logs provide execution evidence; this review independently verified source/fixture scope and routing but did not rerun .NET. Repeated predecessor replay is bounded current-profile cost; broader campaign history remains unsupported. Request/history authenticity remains upstream trust obligation; canonical hashes alone do not authenticate commitment.

## Verdict

Ready for bounded H3 scope. Final integration remains subject to root-owned cumulative gates and prescribed remaining review round. Verdict does not close Task008/H4, enable gameplay or establish durable publication.

## Recommended Next Actions

Lead completes existing cumulative gate and remaining ordinary review; retain H4 and HOST-PUB-001 as open obligations. No code correction requested from this review.
