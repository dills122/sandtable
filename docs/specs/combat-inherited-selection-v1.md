# Creation-rooted Combat entry and empty selection v1

Status: CMB-TASK-003D2c.3d contract checkpoint. This packet consumes the accepted
[inherited Breakdown completion](combat-inherited-breakdown-completion-v1.md), admits its actual
moved state at first Combat Position Determination, and freezes the two-event no-candidate selection
opening/closure. [Inventory](combat-inherited-selection-v1.schema.json), retained vectors and the
executable oracle form one prospective private contract. No C# type, registered schema, runtime
Combat action, public observation or full Snapshot12 is activated.

The owner-approved [combined plan](../design/combat-cycle-implementation-plan.md) supplies scope and
acceptance. Existing project commands, structure and conventions remain those in `AGENTS.md`.

## Objective and accepted boundary

Every public apply/replay/read entry reconstructs the full creation-rooted history through actual
`breakdown-segment-completed`2. Accepted cases are both acting sides after six or seven ordinary
Clear moves: CP12/Cohesion-2 and CP14/Cohesion-4. The entry remains turn1, stage1, first acting-side
slot, ordinal1, first Combat Position Determination, idle Breakdown and no Reaction window.

AdmissionBoundary1 retains the complete canonical CombatEntryState from3c without replacing moved
World locations, cumulative CP, Cohesion, Weather/RNG, Movement-end proof, material progress,
receipts, prefix or symbolic sequence5 position. It appends a derived CandidateAssessment. The
assessment names the acting and opposing elements, their current locations and CP, and records each
closed predicate used by the selected voluntary-adjacent profile: Normal Weather, adjacency, acting
CP≤5 and defending CP≤7. `candidateIds` is derived, never caller supplied. All accepted vectors have
an empty list because the moved units are nonadjacent and the acting unit exceeds its CP ceiling.

An unsupported or unauthenticated history rejects before assessment. Missing candidate support is
not an empty result: only a successfully reconstructed supported AdmissionBoundary may carry zero
candidates. The initial C3a boundary is reference behavior, not accepted predecessor history; its
original locations, CP0..10 assumptions, fixed request seed and synthetic receipt hashes cannot be
substituted here.

## Commands, events and control

Command2 is a closed record in inventory order. `open-segment` requires exact segment, prior version
and symbolic Position Determination ID, with `openingReceiptId=null`. `close-empty-selection`
requires the resulting version/position and exact opening receipt. Both are trusted System actions;
players, model output and self-asserted command fields cannot authorize either.

Input2 is Command plus authenticated actor. No time or clock is accepted: an empty candidate set
opens no decision window and has no deadline. Command kind/version/field combinations and authority
are checked before retry lookup. Exact authenticated retries return retained bytes/state; changed
payload, actor or history rejects.

Event2 binds campaign/rules/config/cycle/segment, adjacent version, prior prefix, accepted Input,
closed Effect and receipt. `segment-opened` contains boundary and assessment hashes, candidateCount0
and null decisionId; it retains Position Determination and sets outcome `system-no-selection`.
`selection-closed` requires that exact opening receipt, records outcome `no-selection`, candidate
null, and stays at Position Determination. Neither event changes World, RNG, Weather, CP, Cohesion,
proof/progress, inherited receipts or symbolic sequence identity. Neither advances to Gun Position,
creates a player/model decision, commits an attack, or claims segment closure.

Control1 retains AdmissionBoundary bytes and the current authority head. Initial outcome is
`unopened`; opening records one receipt and `system-no-selection`; closure records its distinct
receipt, `no-selection`, and `selectionClosed=true`. `stepIndex` remains0 and `segmentClosed=false`.
The inherited entry state remains byte-identical inside all cuts; only Control version/prefix,
selection fields and appended local receipts change.

## Canonical bytes, identities and replay

Objects are closed, ordered and canonically encoded as compact ASCII UTF8. Unknown, duplicate,
missing or mistyped properties reject. Arrays keep semantic order and may not exceed512 items;
depth≤32, whole value≤1MiB, local events/receipts≤2, Int64 versions adjacent and nonoverflowing.
Hashes are lowercase `sha256:` values. No normalization, repair, alternate numeric spelling,
whitespace, BOM or escaped-key alias is accepted.

```text
boundaryHash = SHA256(canonical AdmissionBoundary)
segmentId = "seg." + hex(SHA256(ASCII("sandtable.combat.inherited-selection-segment.v1") || 0 || canonical AdmissionBoundary))
receiptId = "cis." + hex(SHA256(ASCII("sandtable.combat.inherited-selection-receipt.v2") || 0 || canonical Event without receiptId))
```

After complete event bytes exist, prefix uses the frozen sequence5 prefix-event algorithm. Receipt
ledger entries bind canonical Input hash, full event hash, authenticated System actor and resulting
version. Replay reconstructs the complete3c predecessor, re-derives assessment, input, effect,
receipt and prefix, and compares exact bytes. Cached Boundary/Control readback must equal replay;
self-consistent forged caches or re-signed effects cannot establish authority.

Private diagnostics use `CMB-CIS-001` codec/type/bounds, `003` version/tag/field combination, `004`
identity/provenance/assessment, `005` actor/capability, `006` lifecycle/order/retry/cache and `007`
capacity; `008` is noncanonical bytes. Rejection has no effect.

## Testing strategy and success criteria

The oracle first performs semantic RED against an unimplemented admission/transition. GREEN must
cover four literal traces: both owners at CP12 and CP14; two events/eight total accepted records;
every cut; exact retries; canonical Boundary/Event/Control bytes; and direct3c predecessor replay.
Deep mutation coverage changes every event leaf with recomputed receipt, selected inherited/control
state leaves, malformed bytes, altered assessment predicates, synthetic C3a substitution, stale or
foreign actor/history, wrong opening receipt, reordered/duplicate events and capacity edges.

Completion requires:

- full3c history is the only admission path and retained entry bytes never change;
- assessment independently derives zero candidates from supported facts;
- exactly two System events close selection without a decision, clock, RNG or structural advance;
- exact retry/readback and all negative vectors pass without mutating accepted inputs;
- direct3c oracle, JSON/Python structure, documentation navigation and local link checks pass;
- source/runtime/test/scenario and all predecessor contract bytes remain unchanged.

## Boundaries and remaining work

Always preserve deterministic authority and strict unsupported-history rejection. Ask owner before
new gameplay policy, positive capability expansion, registered schema/runtime work, dependencies or
CI changes. Never reinterpret rejection as empty selection, expose hidden state, edit historical
contract bytes, or claim full segment/Checkpoint-B completion.

Full no-attack traversal is a separate bounded child. Positive Reaction/vehicle/Reserve movement,
armed continuation, release-exception expiry, D2c.4 full World/Snapshot composition, Task004 public
and Exercise evidence, checkpoint B and runtime005–025 remain open.
