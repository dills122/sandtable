# Actual repeated ordinary Movement v1

This private executable reference consumes the authenticated `repeat` outcome of
[actual post-assault continuation](combat-actual-post-assault-continuation-v1.md).
It publishes ordinary owner moves in the newly active ordinal 2 cycle. It does not complete
Movement or the selected branch. Native C#, Reaction, stop interrupts, Breakdown, Convoy,
subsequent Combat, further repeat/finish and public persistence remain separate contracts.

## Source and ancestry

[The closed grammar](combat-actual-repeated-movement-v1.schema.json) and
[Python reference](verify-combat-actual-repeated-movement-v1.py) define canonical ASCII JSON.
Objects have exactly the declared fields, in declared order; duplicate/unknown/missing keys,
noncanonical bytes, invalid primitives, unsupported versions and oversized input are rejected.
Nullable fields and inherited types retain the predecessor's semantics. Cohesion before/after
uses signed 32-bit integers. World DP causes use the unchanged inherited signed vocabulary.

Source contains `contractVersion:1`, the complete canonical PAC Source in
`continuationSourceCanonicalUtf8`, and zero through four canonical Movement event strings in
`movementEventCanonicalUtf8`. Five independently supplied ledgers authenticate selection,
round entry, paid assault, continuation and new Movement. Authenticate the entire PAC ancestry
before validating the new Movement ledger or any claimed Control/Proof. Reconstruct every new
event from its independent input; neither a caller's projection nor a re-signed effect supplies
authority. Public read methods return owned values and revalidate physical dependencies.
`transition` is an internal kernel on replay-derived state, not a persisted-state reader.

Only genuine repeat37 sources in the frozen ordinary first-stage/first-slot profile are admitted.
PAC `closed:true` closes the old Release/control fragment; its active ordinal 2 cycle remains
active. Derive the original ordinal 1 Authority, repeated Authority, Movement position, prefix,
closure, CA and repeat provenance, member, World, RNG, attack history and original MovementEndProof
from full PAC replay. Preserve the original proof's endpoint/proximity/exclusion evidence and
`iml.` completion receipt. Finish, fallback, pending, synthetic/released-I, positive/later-II or
consumed Reserve profiles cannot serve as this source. Four owner/seal literal roots are test
vectors; valid authenticated clock variants are accepted without a runtime fixture allowlist.

Base is `{contractVersion,dependencyManifestHash,continuationProof}`. The full continuation Proof
is reconstructed internally, with its original identities, Base and terminal Control. This family
has independent `source`, `base`, `receipt` and `dependency-manifest` hash domains under
`sandtable.combat.actual-repeated-movement.<kind>.v1`, with NUL-separated canonical payloads.
Source IDs use `armove.`, receipt IDs `armv.`, and hashes `sha256:`. The external manifest is sorted
by path before hashing. Campaign/rules/configuration hashes and active-cycle identity remain
bound to their authenticated ancestor. The Base does not renew CP or reset World state.

## Accepted move

Input is exactly `{command,actor}`; it carries no new clock or deadline. Command is exactly
`contractVersion,kind,baseHash,cycleId,positionId,expectedPriorVersion,unit,originLocationId,
destinationLocationId`. `kind` is `move`. Bind actor to original owning side, UnitKey creation
binding and original membership; bind Base, current cycle and Movement position before checking
retries. Validate primitives/family/context even for a duplicate. A previously accepted command
returns its original receipt against every later valid cut with unchanged Source/ledger and no
new event, before testing stale version or current location/legality.

For a new command, require current version and origin, a supported adjacent Clear destination,
original eligibility, no occupying element/guard, the current independent representation,
ordinary NONE member/history, positive TOE, ammunition-zero profile, current stage CP ledger,
no Breakdown or completed Movement. Reaction discovery precedes mutation: reject a destination
adjacent to any opposing combat representation, even if ammunition is zero. This guard does not
implement Reaction or claim that the opponent is unable to react. Destination names are never
used as a route allowlist.

Reuse only the already pinned pure `mov.affected`, `mov.terrain_cost` and `mov.cost` policies.
Each accepted event increments version once and binds the complete command/input, ancestor hash,
Base/cycle/position, prior prefix, cost/effect and receipt. The first safe departure ends Contact
once, with the new receipt and `ordinary-break-off`; historical settlement data stays unchanged.
CP is cumulative with the ordinary 15 CP ceiling. The four-move safe observation is:

| Move | CP before → after | Incremental DP | Cohesion after | Break-off CP |
| --- | --- | --- | --- | --- |
| 1 | 5 → 9 | 0 | 0 | 2 |
| 2 | 9 → 11 | 1 | −1 | 0 |
| 3 | 11 → 13 | 2 | −3 | 0 |
| 4 | 13 → 15 | 2 | −5 | 0 |

The owned projection atomically moves the original element and representation, updates World
CP/cohesion, member `spentCp`, DP causes, the one route track, prefix, receipts and actual move
receipt/hash progress. Three DP causes use `<receipt>.dp`, with checked ID/ordinal capacity.
New cycle progress starts empty and includes only the new accepted moves; earlier commitment,
Release, control and repeat receipts do not count as new Movement progress. Preserve original
components/origins/TOE/ammunition/readiness/parents, opponent, settlements, Reserve history,
future obligations, RNG, stage attack history, original proof/closure and active Authority.
A fifth safe move costs 17 CP and rejects atomically with the cost diagnostic.

Stopping/completing requires an explicit subsequent contract. A positive route cannot skip its
stop interrupt or manufacture a new MovementEndProof. No command in this family resolves or
completes Movement or advances the position.

## Bounds and delivery

Frozen limits are Source 32 MiB, Proof 2 MiB, Control 256 KiB, Event 32 KiB, Input 1 KiB,
Movement ledger 8 KiB and nesting 38. Independently enforce all inherited parser caps. Own shape
bounds are four events/receipts/progress references, one member/track, five route locations,
one ended Contact per event, three added DP causes and two original proof endpoints. Literal
freezing/public Proof acceptance requires measured structural domination and physical acquisition
closure; `closureFrozen:false` rejects public claims while those checks are provisional.

The measured external set is exactly PAC's 64 runtime paths plus its schema/oracle: 66 external
paths. Preserve PAC64/paid62/admission59 identities. New schema/oracle are owned delivery;
paid/PAC/new fixtures are test-only and never runtime authority. No self-code/schema hash cycle.
Cold and warm checks validate fixed hashes, missing/changed/unreadable/null and change-after-first-read
faults. Unknown material acquisition must be reconciled before freezing, not added as an arbitrary pin.
Capacity uses PAC's valid Source bound 8,651,264 rather than its looser 16 MiB syntax cap:
`2×8,651,264 + 4×2×32,768 + 512 = 17,565,184 < 32 MiB`.
The predecessor Proof bound plus World and metadata is
`1,598,020 + 192,965 + 131,072 = 1,922,057 < 2 MiB`.
Field/depth accounting and measurements are retained with author evidence before final freeze.

## Diagnostics and verification

`CMB-RMV-001`: shape/type/size/depth; `002`: numeric range; `003`: version/family/profile/Reaction
boundary; `004`: context/owner/ancestry-ledger binding; `005`: legality/CP/eligibility; `006`:
version/event/claim mismatch; `007`: capacity/provisional delivery; `008`: noncanonical bytes;
`009`: physical dependency delivery. Preserve inherited scalar diagnostics under this family.
Physical dependency validation precedes parsing; complete source ancestry precedes malformed
successor ledgers or claims. All rejection paths publish nothing.

The normal oracle verifies genuine semantic entry, signed/closed/preparse grammar, valid inherited
clock variation, finish rejection, all physical dependencies, four owner/seal routes, every cut,
every prior accepted command retry, owned readback, causal effect/claim forgeries, Reaction-adjacent
return, fifth-move ceiling and retained literal equality. Fixture generation is a separate explicit
`--write-fixture` operation after GREEN and measured closure, never part of the normal verifier.
The new oracle is an author gate; GitHub's .NET CI is separate and does not execute it. Parent
017–025/all72 and broader public/native/lifecycle completion are not inferred from these tests.
