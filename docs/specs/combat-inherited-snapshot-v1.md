# Combat inherited Snapshot12 v1

Status: Task008 H0 contract evidence; acceptance is tracked in the implementation plan.
This additive packet defines literal roots for retained cuts of implemented B–G/019A and
F1–F6 families. It supplies no C# history router, runtime restore, storage provider, admission switch,
publication proof, or later Combat handler. [Initial H boundary](../design/combat-cycle-implementation-plan.md#task008-execution-index)
owns scope. [Schema](combat-inherited-snapshot-v1.schema.json),
[fixture](fixtures/combat-inherited-snapshot-v1.json), and
[oracle](verify-combat-inherited-snapshot-v1.py) jointly define this contract.

## Literal root and predecessor compatibility

Contract version remains 12. Preserve these **19** fields in exact existing order:

`contractVersion,campaignId,stateVersion,rulesetHash,setup,world,initiativeHolder,operationStageOrders,operationStageWeather,randomState,currentPosition,reactionWindow,breakdownFlow,configuration,creationReceipt,chroniclePrefix,cycleState,combatState,commandReceipts`.

The earlier audit's “twenty” count was a counting error, not permission to add a field. Creation roots
must equal [C2](combat-authority-envelope-v1.md) canonical bytes exactly for every selected request;
its creation-only reader stays unchanged. Historical [first-Combat composition](combat-snapshot-composition-v1.md)
keeps its synthetic authority context and frozen bytes. Those roots are not reinterpreted as reachable
inherited campaign roots. Imported schemas and module globals are never widened in place.

JSON is ASCII canonical encoding with existing property order, exact number spelling, no whitespace,
no duplicate keys, no BOM, and no alternate escapes. Arrays preserve predecessor canonical order;
Movement tracks preserve route order. Encoding is `json.dumps(ensure_ascii=True,separators=(',',':'))`
after typed canonical field validation. Strict whole-byte comparison remains mandatory even when
JSON values compare equal. Unknown fields, wrong union tags, different order, omitted/null fields,
noncanonical escapes and recomputed but untrusted bindings reject.

Qualified schema names refer to exact predecessor types in `externalContracts`. `C2.Receipt` is
creation receipt. `InheritedCommandReceipt` explicitly aliases `Pre.PreambleReceipt`: commandHash,
eventHash, receiptId, actor and stateVersion in that order. Actor values include `system`; this is
not C3's later command receipt grammar. Root command receipts contain complete increasing-version
ledger from state2 through current head, with no gaps, duplicates or family-local truncation.

## Typed state slots

Before actual Reserve entry, cycleState is null. At state10 Reserve entry it is already
`reserve-designation`, even when read through stage-entry B2. Membership and first owner derive from
actual `Reserve.initial(request,Created11,preamble,Weather,stageEvents)`; source-family labels cannot
choose whether this authority exists. Optional designation retains this same arm.

`reserve-designation` fields: kind, firstActingSide, members (`ReserveMember[]`). After atomic
completion2/019A, use `inherited-cycle` with these fields in order:

1. kind, firstActingSide, members;
2. cycle (`Authority`), cycleId, openingBaseHash, completionReceiptId, sequencePosition;
3. tracks (`InheritedTrack[]`), actualProgressRefs (`ActualProgressRef[]`);
4. interruptContext (`InterruptContext?`), movementEnd (`MovementEndProof?`),
   breakdownCompletionReceiptId (`id?`).

No family State blob and no duplicate current World are allowed. Existing predecessor value types,
nullable values, route ordering, membership history, progress identities and proofs remain intact.
The new arm's sequencePosition is exact predecessor sequencePosition: in Reaction this preserves
suspended Movement while root currentPosition describes Reaction or its stop. Phasing stop preserves
actual interrupt sequence position plus original Movement in interruptContext. When root position is
sequence, its sequencePosition equals the arm's sequencePosition.

Root position is closed union `sequence`, `reaction`, `breakdown-stop`. Existing Reaction positions
are copied verbatim. Ordinary phasing stop maps actual stop sequence position to `breakdown-stop`.
Root flow is closed union `idle`, `moving`, `phasing-stop`, `reacting`, `reactor-stop-open`, and
`reactor-stop-closed`, with exact typed predecessor payloads. Window retains full trigger authority,
apparent trigger, opportunities, representations, adjacency evidence, resolved and active identities.
F2/F6 inactive resolved windows survive until close. F4 closed-stop flow retains continuation after
window closure. These cuts are deliberately distinct.

Missing/null flow is normalized to `{ "kind": "idle" }` only at replay-proven pre-route cuts: no
tracks, progress, window or interrupt, and no accepted move/stop/participant event. This covers
creation, preamble/Weather/stage/Reserve and no-route E1 entry. Existing non-null flow always survives;
actual resolved idle is copied. No general caller-default rule exists. Earlier absent tracks/progress
are empty only at atomic first-opening head. Absent interrupt/end/completion slots require absence of
corresponding accepted event authority; later families retain actual evidence. Null reactionWindow
before trigger is similarly a causal absence, not a supplied cache value.

Root Setup, configuration and creationReceipt derive from trusted creation. All current World, RNG,
headers, positions, stateVersion, receipts and prefix derive from full causal replay. Existing
configurationHash, creationBinding and creationEventHash must match root configuration/creationReceipt;
these remain checks, not additional root fields. Every event hash, state version and accumulated
Chronicle prefix must match full ordered history.

combatState is null throughout H0, including actual G2 Combat entry. Tasks009–019 must add or version
reachable Combat composition that retains inherited membership, route/progress, opening, interrupt,
Movement-end and Breakdown-completion evidence. Converting to historical synthetic CycleFrame would
lose authority and is forbidden. Historical first-Combat byte vectors stay frozen; future actual
preRoundSnapshotHash derives from actual complete predecessor root.

## Trust and restore requirement

Authoritative input is caller-owned trusted request plus fixed pinned registry/context, independently
retained exact Created11, and independently retained ordered accepted event stream/head. Snapshot
contains no event-history copy. Replay, then compare entire literal root; recomputable event hashes,
prefixes and receipt IDs do not authenticate commitment. A different legal branch with internally
consistent hashes is still wrong for selected trusted committed history.

H0 Python `read_root(data, trusted_context)` is a **contract evidence adapter**, not generic restore
router. Its context names a fixture replay family plus concrete complete predecessor segments and
local events. Family/case names describe which accepted oracle interface is exercised; they are not
persisted authority fields. All segments pass actual predecessor replay. B2 Reserve entry normalizes
from actual history; equal complete trusted histories must yield equal roots across interfaces.
F6's case-based interface additionally replays supplied complete F5 transcript and compares it with
F6's internally reconstructed predecessor before admitting any F6 event. No caller family state or
expected-state cache is consumed by root construction.

The immutable, bounded request/Created/history/case tuple may memoize replay-derived root bytes in
one verifier invocation. Any changed request, creation or event bytes make a different key and run
actual replay; changing a diagnostic label cannot alter a root. No mutable state escapes memoization.
This optimization serves repeated falsification against the same trusted context; it is not a
snapshot-history trust mechanism.

H1–H3 must implement ordered-history routing by actual causal state and exact next event, rejecting
unsupported tails and ambiguous legal projections rather than selecting by caller family tag.
H4 must expose actual fresh-admission-disabled restore independent of publication. Missing retained
Created11 must reject even when request can regenerate identical creation bytes. Trusted request,
registry, history/head mismatch, mutated caller buffers, incomplete predecessor histories and forged
self-consistent snapshots must reject. The evidence adapter accepts concrete tuple/list event buffers: collection counts are checked before
event iteration, aggregate bytes before ownership copy. Request grammar (including bounded fields)
and serialized byte size are checked before request copy or memoization. Future runtime restore must
apply equivalent bounds before accepting unbounded enumerables or copying retained buffers.
H0 does not claim this runtime seam or whole-host restore has been implemented. HOST-PUB-001 still
owns archive authentication, durable restart, atomic uniqueness and lost-response publication proof.

## Limits and evidence

Root and each accepted event: at most 1,048,576 bytes. Root structural depth: at most32, root at depth0.
Ordinary arrays: at most512 items. World `cohesionCauses`: at most4096 typed causes; this exception
applies to that World field only. History: at most512 accepted post-Created events and16,777,216
aggregate event bytes. Independently retained Created11 has its own one-MiB bound and is not counted
as post-creation event. The verifier measures every selected root and complete retained history;
fixture `measured` records actual maxima and `familyCuts` records exhaustive selected case/cut counts.
Measured fixture-state maxima:20,242 root bytes, depth9,21 array items,21 retained events and57,445 retained event bytes. These bounds accommodate this implemented profile, not unrestricted campaign-long retention.
Later capacity policy must be explicit before unsupported larger histories become reachable.

Fixture roots retain full inspectable `canonicalJson`, byte length and SHA-256, creation hash and
ordered event hashes. Every case/cut from predecessor fixtures participates: preamble seeds/choices,
Weather seeds/choices, stage variants, both Reserve owners/designations, ordinary move counts,
route-lifecycle counts, Breakdown entry, both Reaction owners and selected exits. Shared-cut groups
assert exact root equality, including B2/D1, F1/F2/F3 and F2-first-move/F4/F5. Every prefix of every selected complete transcript is also root-vectorized. Supplemental prefixes
are deduplicated by exact Created11 plus complete exact prefix bytes; fixture-interface cuts remain
distinct. Weather seeds add creation/preamble prefixes through actual B1 replay. An uncovered later
prefix fails inventory rather than selecting a guessed adapter. `fixtureStateCuts`,
`supplementalPrefixCuts` and unique history counts remain separate.

Falsification precedes vector freeze. Coverage includes per-root-field edits, nested bindings,
missing/duplicate/reordered/raw/changed accepted events in every causal segment, all root-field
omissions, nested new arm/union fields across both owners and distinct shapes, absent creation,
wrong trusted request, a forged event with receipt/hash/progress/prefix bindings genuinely
recomputed, a lawful alternate tail from the same trusted predecessor, canonical spelling and capacity
boundaries. Direct predecessor oracles independently retain their original World/event inner-field
and behavioral coverage. Transitive imported oracle/schema/fixture and declared normative source
pins are checked before inventory generation. Normal verifier only compares frozen evidence and
never rewrites fixtures.

Run `python3 -B docs/specs/verify-combat-inherited-snapshot-v1.py`. Runtime C# integration, disabled
admission at every actual restored cut, full later28-trace runtime closure and durable persistence
are separate gates. Contract success alone does not close Task008.
