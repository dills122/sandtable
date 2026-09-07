# Combat Rules10 and creation authority envelopes

**Status:** `CMB-TASK-003C2`, input `d59446e` (merged PR92), 2026-09-07. Contract-only
Rules10 manifest and Created11/Snapshot12 **creation cut**. No registry, runtime, migration, later
snapshot reader, event handler or simulator activation. [Combined plan](../design/combat-cycle-implementation-plan.md)
keeps003C3/003D2 and checkpoint B open. [Schema inventory](combat-authority-envelope-v1.schema.json),
[literal goldens/vectors](fixtures/combat-authority-envelope-v1.json) and
[oracle](verify-combat-authority-envelope-v1.py) accompany this text.

## Frozen dependencies and Rules10

Use [003A Setup7/initial ledger](combat-creation-ledger-v1.md),
[003B World7](combat-world-settlement-v1.md), [003C1 selected inputs/config](combat-rules-inputs-v1.md)
and [003D1 catalog5/cycle codec1](combat-cycle-sequence-v1.md). No predecessor bytes change.
Ruleset identity remains `cna-1979.1`, contract10. Canonical manifest uses existing
[RulesetManifest](../../src/Cna.Core/Rules/RulesetManifest.cs) order and sorting:
`rulesetId,contractVersion,artifacts,rulings`. Its hash is64 **raw lowercase hex**, unlike
`sha256:`-prefixed artifact, Setup, Content, configuration, event and prefix digests.

The9,168-byte Rules9 predecessor is captured from actual
[Cna1979Ruleset.Manifest](../../src/Cna.Core/Rules/Cna1979Ruleset.cs), using its public artifacts and
rulings. A temporary C# writer reproduces `CalculateHash()` exactly; literal bytes and source-file
hashes remain in fixture. Predecessor hash:
`17f3e6047f34b5bf6f5f809055863b664a4bd82a8481db83ee83e0ae5cad3a2a`.

Rules10 preserves all nine artifact IDs and all ten rulings from Rules9, replaces only
`cna-1979.1.land-sequence` content with catalog5, and adds two artifacts:

| Artifact | Hash / source rule |
| --- | --- |
| `cna-1979.1.land-sequence` | `sha256:9f0828f10511fa49bd650c51eb5d5909637aa663b0593c712a2740a0b670ceb1`; sorted distinct union of all catalog position/interrupt references |
| `cna-1979.1.combat-selected-inputs.v1` | `sha256:fafb24792c9e3f774c368f85c02d1d068f84c9c78e67bf8324723257d0f13029`; exact003C1 artifact sources |
| `cna-1979.1.cycle-identity.v1` | `sha256:6b442b9a9b6d49ea8629bb47b048e8bebcda14dbd6a6cf84596aef2d5629e9dc`; exact003D1 codec sources |

Add ruling `cna-1979.1.ruling.combat-source-gap`, new manifest conflict ID `CMB-SRC-GAP-001`,
selecting `fill-defender-plus2-34-35-36-with10` over `defer-positive-assault`. It encodes already
accepted [CMB-SRC-RUL-001](../research/combat-source-freeze-v1.md), not a new policy decision.
Sources: `sandtable-rules-lab/CMB-SRC-RUL-001` and `spi-1979-common-charts/15.79`.
Protecting check ID `combat-envelope.source-ruling` names this oracle's exact manifest/ruling and
source-derived selected-input comparison; it does not claim an existing production C# test.
All eight accepted gameplay policies remain bound by selected-input artifact; do not duplicate
configuration budgets or mutable campaign state in Rules. Config stays a separately bound artifact.

Result:11 artifacts,11 rulings,10,495 canonical bytes. Full Rules10 hash:
`8af256c6c2bbf71cb72ea7e29922db9c4c897c2535121a68a6849bbd06e0bd18`.
No missing member, source substitution, rehashed unsupported rule, duplicate ID or reordered bytes
is accepted. Existing movement/Breakdown/vocabulary/rulings remain exact historical dependencies;
retention is not cross-profile admission.003D2 must reconcile break-off/history semantics before
checkpoint B. Any later changed rule bytes require explicit manifest and dependent golden refreeze.

## Creation request and nonrecursive binding

The schema inventory lists mandatory property order and types. It is an oracle descriptor, not
JSON Schema. All fields are mandatory; no opaque object payloads. Canonical JSON uses ASCII IDs,
exact UTF-8 bytes, fixed object order and ordinally sorted keyed arrays. No BOM, whitespace,
trailing newline, duplicate/missing/unknown keys, alternate escaping, floats, exponent integers or
repairs. IDs follow `[A-Za-z0-9][A-Za-z0-9._:-]{0,127}`; references retain their prior source grammar.
Max complete envelope1,048,576 bytes, depth32, arrays512; creation inventory constraints are tighter.
Embedded Setup/Content still obey their own65,536-byte limits. Snapshot later-state capacities
must be reconciled with World7's4,096-cause capacity in003C3/003D2 before any runtime consumer;
this creation-only reader's empty history is not evidence that all later Worlds fit an envelope.

Request contract1 order:
`contractVersion,campaignId,rulesetHash,setupId,setupHash,content,configurationHash,randomState`.
Content selection copies all six Setup selection fields; no second independently chosen scenario.
Rules hash must equal exact supported manifest; Setup and Content must pass003A/002 validation and
agree on profile, initial stage1 and all immutable provenance. Configuration must pass003C1 and
bind selected-input artifact. Trusted registry/archive supplies these inputs independently of
untrusted event/snapshot bytes. All embedded duplicates are checked against those trusted inputs.

Random state preserves existing shape/order:
`contractVersion,algorithmId,seed,nextByteCursor`. Version1, algorithm
`sandtable.sha256-counter.v1`, unsigned64 seed0…18446744073709551615 and cursor exactly0 at creation.
No signed narrowing, generated seed on retry, draw, reroll or precomputed Weather outcome.
Campaign ID and seed are explicit immutable request inputs. Future later cursor validation remains
causal replay work. This packet exposes no seed or authority digest to an opposing audience.

Let J be canonical Request bytes. Derive:

```text
requestDigest = SHA256(ASCII("sandtable.combat.creation-request.v1") || 0x00 || J)
creationBinding = "creation." || lowercaseHex(requestDigest)
```

No length prefix follows the domain separator: remaining bytes are one bounded canonical JSON
record. This is a new creation-request domain, distinct from cycle's binary tuple and prefix
framing. Binding is73 ASCII characters, a valid World7 stable ID. World, event bytes, receipt,
snapshot and Chronicle prefix are absent from J. Derive binding first, then World, then Created11;
never solve a self-referential event hash. Exact field/property order is part of this version.

A same campaign ID with any changed immutable request input is a conflicting retry, even if all
embedded hashes are recomputed. A different campaign ID denotes a distinct creation namespace.
Use campaign ID as atomic publication uniqueness key; binding distinguishes exact inputs. Never
accept multiple creation events merely because their request digests differ.

## Created11 and initial Snapshot12

Created11 preserves [Created10](../../src/Cna.Core/Campaigns/CampaignCreatedV10Serializer.cs) event
token `campaign-created` and authoritative `stateVersion=1`. Exact order/types are in inventory:
Setup7, full Config, Request/binding, initial World7, unchanged RNG shape, catalog5 first position,
and idle Breakdown flow are carried explicitly. No creation timestamp/choice receipt is invented.
Config's hash is over canonical003C1 bytes; full config retained for recovery and future windows.

Initial World is derived from certified Content/Setup, with binding above.003A owns initial elements
and origins;003B owns World order. Canonically sorted elements receive existing initial independent
representations `map-representation.0001` and `.0002`; all cause/relationship/loss/guard/future/settlement
and broken-lot arrays are empty. CP0/1, Cohesion0, ammo10, current TOE, readiness and parent provenance
match exact selected seeds. Creation is the only place this initialization runs.

Initial sequence is catalog5's first position, `gameTurn` copied from validated Setup (1…111),
operationStage0 as in predecessor preamble. The profile still starts stage1; preamble position0
is not an extra playable stage. Actor remains system/null. Initiative holder is not yet resolved in
snapshot even though Setup supplies its predetermined policy; Weather and stage-order arrays stay
empty until actual events. Breakdown is `{ "kind": "idle" }`; Reaction/Cycle/Combat do not exist yet.

Created bytes C must validate completely before computing:

```text
creationEventHash = "sha256:" || lowercaseHex(SHA256(C))
P0 = D(sandtable.cycle.prefix.creation.v1, U64(byteLength(C)) || C)
```

D/U64 are exactly003D1. The creation event contains neither digest nor P0. Snapshot12 carries
`creationReceipt` contract1 with Request/binding/event hash and `chroniclePrefix=P0`, then current
World/position/RNG equal projected creation. The receipt is an authority value, not a new Chronicle
event. No independent snapshot self-hash is introduced. Trusted archive identity and exact Created11
bytes are prerequisites; a matching checksum alone does not authenticate imported state.

Snapshot12 root order preserves prior Snapshot11 fields through Breakdown, then adds Config,
creation receipt, Chronicle prefix and reserved Cycle/Combat/command-receipt slots. **Only creation
cut is frozen as an admissible value here**: stateVersion1, cycleState/combatState/reactionWindow null,
commandReceipts/stage-order/weather arrays empty, initiativeHolder null. Later tagged state arms,
accepted command receipts, clocks, event suffixes, active-stage histories and actual replay remain
003C3/003D2. They must complete the same prospective Snapshot12 before checkpoint B; null/empty
creation slots are not optional fields, opaque maps, lost-history defaults or a final universal
Snapshot12 reader. This bounds the C2 dependency slice without claiming parent003C completion.

## Creation publication and recovery contract

| Cut / condition | Required result |
| --- | --- |
| No stored creation, admission enabled | Derive exact request/binding/initial event without RNG draws; publish one event and uniqueness record atomically. No partial state or consumed command on failed publication. |
| No stored creation, admission disabled | Reject before publication. Admission never silently selects historical Rules9 or generates substitute seed. |
| Publication succeeds, response lost | Retry exact request returns retained bytes/receipt, with no new event, state increment, initialization, or RNG. Check retained identity before consulting fresh-admission switch. |
| Same campaign, changed request | Conflict; preserve original bytes even if changed request is otherwise supported. No overwrite or second identity. |
| Stored creation, new admission disabled | Compatible recovery reader plus retained Rules/Setup/Content/Config remain available. Read exact stored creation/snapshot. Missing reader or trusted artifact is an error, not fallback. |
| Snapshot with missing creation evidence or altered current values | Reject; recomputed snapshot/World hashes cannot replace separately trusted creation binding. |
| Noninitial snapshot | This reader rejects. Dispatch only to completed compatible C3/D2 causal reader; never reinitialize depleted ammo, losses, RNG, relationships, or future work. |
| Historical save | Preserve existing version dispatch/reader and bytes; no inferred upgrade or reset. |

`creation_cut` in oracle is a pure decision table: it returns candidate bytes plus whether a first
publication is required. It is not a database, compare-and-swap implementation, retry coordinator,
concurrency proof or later replay engine. TASK008 must prove atomic uniqueness and response-loss
cuts using actual persistence. Later readers must validate trusted whole history even when both
World and embedded context are changed. Same-ID registry substitutions are not trusted inputs;
registry/archive must pin exact digests instead of resolving latest-by-name.

## Diagnostics and verification

Private codes: `CMB-ENV-001` decode/shape/type/limit, `002` primitive grammar/range, `003` unsupported
top-level version, `004` manifest/artifact/ruling mismatch, `005` request/context binding, `006`
creation/snapshot mismatch against trusted input, `007` unavailable recovery/admission evidence,
`008` noncanonical bytes. Decode failures use empty JSON Pointer; other errors identify first field.
Nested context validation uses predecessor's diagnostics before envelope validation. Shape/bounds
precede semantics, then canonical comparison. Version-invalid envelopes cannot become historical
values by changing only their version number. Outward error projection remains004.

The verifier checks four literal goldens (manifest, request, Created11, initial Snapshot12), retained
request preimage, independent D1 prefix framing,67 single-edit negatives,36 raw-byte rejection cases,
nine recovery boundary checks,12 seed/Setup-holder/config/Content-provenance identity variants and
turn1/111 boundaries.
Full manifest comparison includes source-ruling membership and all predecessor identities; selected
rule content is reconstructed through003C1's source oracle. Fixture generator copied retained value
fragments; reader reconstructs elements from Content/Setup and independently builds envelope order.
This shares predecessor research lineage and does not claim a fresh source or independent review.

Creation request golden741 bytes; Created11 is7,520 bytes; initial Snapshot12 is7,903 bytes. Complete
literal bytes, SHA-256 values and67 negative mutations are retained in fixture. .NET capture verifies
actual Rules9 and Rules10 manifest hashing only; Created11/Snapshot12 C# serializers do not yet exist.
No full solution test or production replay claim. Independent-review flow remains exhausted7of7;
this checkpoint receives author verification, with no new Ready verdict attributed to prior reviews.


Executed commands at this checkpoint:

```text
/opt/homebrew/bin/python3 docs/specs/verify-combat-authority-envelope-v1.py
/opt/homebrew/bin/python3 docs/specs/verify-combat-rules-inputs-v1.py
/opt/homebrew/bin/python3 docs/specs/verify-combat-cycle-sequence-v1.py
/opt/homebrew/bin/python3 docs/specs/verify-combat-world-settlement-v1.py
/opt/homebrew/bin/python3 docs/specs/verify-combat-creation-ledger-v1.py
/opt/homebrew/bin/python3 docs/specs/verify-combat-content-v7.py
/opt/homebrew/bin/python3 docs/research/verify-combat-source-freeze.py
/opt/homebrew/bin/dotnet build /tmp/cmb-c2-capture/Capture.csproj '-bl:/tmp/cmb-c2-capture/build-{}.binlog' --nologo
/opt/homebrew/bin/dotnet build /tmp/cmb-c2-capture/Capture.csproj --no-restore '-bl:/tmp/cmb-c2-capture/parity-{}.binlog' --nologo
/opt/homebrew/bin/dotnet /tmp/cmb-c2-capture/bin/Debug/net10.0/Capture.dll
git diff --check
```

All pass. Capture project references unchanged `src/Cna.Core/Cna.Core.csproj`; initial writer uses
public Rules9 members and compares resulting hash with `Manifest.Hash`. Second capture step builds
public `RulesetArtifact`/`Ruling` values from retained manifest and compares new `RulesetManifest.Hash`
with retained Rules10 hash. Temporary project/binlogs are local aids, not production source changes.
New oracle also rejects693 nested scalar/empty-array type substitutions and noninitial lot variants.
Author RED reader stub preceded implementation. First golden exposed Setup/Rules `Policy` name
collision; restricted dependency imports fix it without changing predecessor schemas or goldens.
