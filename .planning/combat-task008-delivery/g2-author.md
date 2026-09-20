# Author Explanation — G2 Breakdown completion

## Intent and plan
Task008 G2 consumes actual creation-rooted E2/G1 history and publishes the one System completion
into first Combat Position Determination. Five primary files: new models, projector, codec, tests
and fixture project link. F Reaction and H progressive restore remain open; no Combat action or
positive vehicle authority. Canonical wire/schema/fixture unchanged.

## Flow and choices
Replay requires all three lifecycle records, reconstructs actual predecessor, and re-emits zero or
one G2 event for full canonical byte comparison. Apply checks shape, System actor, action, creation
and cycle before exact retry lookup. Consumed changed input and second fresh completion reject.
Fresh input must equal current Command, binding version and position. ReadState compares full
replayed state bytes; wrapper retains immutable lifecycle rather than trusting supplied caches.

Codec uses inherited v1 action hash, v2 ibc receipt domain, actual predecessor sources, existing
catalog successor, actual Reserve and Movement proof receipt IDs. State preserves all 26 lifecycle
fields except head, position, prefix and ledger; appends nullable Breakdown receipt field. World
writer retains E1 history guard. No synthetic legacy Snapshot11 and no weakened initial reader.
Typed flow stays Idle, null interrupt, symbolic side null; actor is explicitly System.
Long version/512 receipt limits checked before append; input/event/state bytes bounded at1MiB.

## Verification and dev review
Frozen eight traces cover both owners after1/5/6/7 moves, eight events, sixteen cuts, forty artifacts.
TDD red missing implementation then focused green24 (12 G2+12 lifecycle). Independent source/action/
receipt/prefix checks, all preserved fields, exact retry, wrong actor/action/version/creation/cycle,
missing/reordered history, same-owner different-move and cross-owner transplants, coherent re-signed
source/proof/cache forgeries, raw bytes, caller/result buffers, legacy reader rejection tested.
Dev source/test review found no blocker; requested same-owner history transplant before freeze.
Exact checks and final independent verdicts are in g2-evidence.md; integration initially pending.

## Costs, limits and challenge points
Bounded duplicated codec layout constrained by frozen hashes; full history replay trades bounded
cost for strong admission. No general Snapshot12, arbitrary history capacity, Reaction, positive
vehicle, later cycle, public activation or durable publication. HOST-PUB-001 remains open.
Review actual lifecycle requirement, predecessor sources, receipt separation, preserved proof and
lack of material progress; reaching Combat position alone must not claim playable Combat.
