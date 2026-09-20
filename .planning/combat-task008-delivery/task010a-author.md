# Task010A author explanation

## Intent and plan
Base6f52d81. Implement actual inherited empty selection and six no-attack steps, first child of
canonical010A/B/C refinement. Four G2 histories retain both owners and six/seven moves. No positive
choice, clock, release action, generic history routing or extended Snapshot12 persistence here.

## Flow and files
CampaignCombatInheritedSelection owns Command/Input, immutable State/Result and two transitions.
Replay derives009A AdmissionBoundary from independently retained Created and complete predecessor;
local accepted events are parsed and compared to deterministic transition output. Apply replays
before private Transition. Exact accepted retries return owned retained bytes without another advance.

CampaignCombatInheritedNoAttack first reconstructs actual closed selection, then its six local
step events. Catalog determines successor; caller cannot supply it or proof/previous receipt.
Each disposition binds selection closure. First previous-step receipt is selection opening;
subsequent ones bind immediate predecessor. Frozen selection stays nested unchanged, including its
stepIndex0 and segmentClosedfalse. Only traversal closes at step6 and reaches same-slot ReserveRelease.
No World, RNG, CP, TOE, ammo, attacked history, target use, Weather or Breakdown proof changes.

IdentityCodec adds named local descriptors and strict input/event/Control writers/readers beside
existing Boundary grammar. Selection effect union and traversal tag checked; receipt/step arrays
preserve semantic order. Boundary's external ordering remains unchanged. Readers require closed
shape, bounds and canonical spelling before trusted replay and exact whole-byte equality.
Optional traversal cache is bounded before clone and never supplies authority. Results and stored
buffers own their bytes; retained creation is owned before caller-controlled list access.

Tests reconstruct all28 selection plus60 traversal hash/length commitments from typed runtime output.
These are frozen commitments, not fixture-stored literal event JSON. All four histories, control
cuts, prior retries, altered/rehashed events, forged caches, actor/position/disposition/version,
missing/reordered/duplicate/post-close tails, raw grammar, bounds and ownership are exercised.
Testproject links frozen no-attack fixture; selection fixture already linked. No fixture/schema/
oracle changes. Public API names remain internal Core implementation detail.

## Verification
InitialRED missingnewAPIs. First build hit two CA1826 indexed-collection warnings, then one actual
behavioral path passed. Expanded6tests passed32.437s with all88 commitments. Final ownership
regression, frozen focused run, format, fullgate and review facts belong in task010a-checks.md.
Earlier successful run does not substitute for final corrected candidate verification.

## Tradeoffs, limits and challenge points
Two small cohesive state modules avoid a universal mutable Control. Strict grammar descriptors
couple codec to frozen schemas; future contract revisions must preserve parity and array semantics.
Repeated bounded predecessor replay costs more than trusting a cache, but provides causal authority.
Every actual child operation already reconstructs creation-rooted history;010C still must integrate
concatenated suffixes into generic router. Full prefixes become28/29 events, authority29/30.
010B timed synthetic C3a and011 Prepared remain separate. Parent010 cannot close from this child.
Fragment readback is not full Snapshot12 or atomic durable publication evidence.

Challenge exact retry authorization before lookup, unsigned receipt domains, input-vs-command hash,
sequence prefix advancement once, malformed-byte validation order, nested immutable selection,
owned Created/event buffers and no material progress or reset at structural completion.

Final worker freeze: ownership regression reproduced failure before fix; capture now owns Created
before local Count/index and passes same owned bytes into predecessor replay. Final7tests pass36.320s,
Identity14tests pass12.104s, scoped format/verify clean. Root build clean4.16s; remaining gates pending.
