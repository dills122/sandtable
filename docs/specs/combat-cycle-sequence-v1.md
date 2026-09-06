# Combat sequence and cycle identity contracts

**Status:** `CMB-TASK-003D1`, contract-only slice at input `4c2bd03`, 2026-09-06.
Freezes sequence/catalog5, cycle identity codec1 and occurrence/prefix framing. No new runtime
registration, cycle event handler, Rules10 manifest, snapshot, migration or playable-cycle claim.
[Plan](../design/combat-cycle-implementation-plan.md) orders003C2 full Rules/creation/snapshot next;
003C3 event envelopes,003D2 movement/history and004 outward contracts remain required.

Canonical requirements: [accepted cycle decisions](../research/continual-cycle-identity-and-history-decision.md),
[cycle composition](../design/continual-cycle-reserve-composition-v1.md),
[Combat step design](../design/combat-step-transitions-v1.md),
[accepted policies](../design/combat-cycle-policy-reconciliation.md) and
[003C1 rules inputs/config](combat-rules-inputs-v1.md).
[Schema inventory](combat-cycle-sequence-v1.schema.json) gives exact value field order/types;
[fixtures](fixtures/combat-cycle-sequence-v1.json) retain literal artifact bytes, binary preimages,
hashes, source anchors and mutations. The inventory is a local typed descriptor, not JSON Schema.

## Registry, catalog and canonical representation

Reserve `Cna1979LandSequenceV5`: position contract5/catalog schema5. Keep every existing sequence4
reader, source file and canonical byte unchanged. The successor copies all112 turn1 positions and
the one Breakdown interrupt, changing only their `contractVersion` to5, then adds six cycle-edge
rows. No linear position ID is renamed, removed, duplicated or silently promoted to executable.
The successor catalog order is exactly `schemaVersion, positions, interruptPositions, cycles`.

`Position` preserves predecessor fields/order: `contractVersion, positionId, gameTurn,
operationStage, stageId, phaseId, segmentId, stepId, actorRole, activeSide, sources`. Full-turn
positions retain prelude/stage/phase/segment/step order. Source arrays sort ordinally by source ID,
then locator, without duplicates. All other catalog arrays preserve semantic order. Root catalog
is the turn1 template; materialization changes `gameTurn` only within1–111 and resolves actor from
trusted retained Initiative/stage-order evidence, never from the faction-name spelling of a slot.

| Template actor role | Materialized actor |
| --- | --- |
| `none` | null |
| `commonwealth` | Commonwealth |
| `initiative-holder` | Retained Initiative holder |
| `first-acting-side` | Retained stage first side |
| `second-acting-side` | Opposite of retained stage first side |

Turn preamble positions legitimately have operationStage0. Cycle scopes require stages1–3. The
Breakdown interrupt remains `land.position.breakdown-stop`, actor `none`, null activeSide, operation
stage1 in its static template and unchanged source references. Runtime interruption context must
carry the actual movement/cycle scope; the generic interrupt ID alone cannot identify a repeated
or later-stage occurrence. Interrupt materialization/return validation belongs to003C2/003C3; the
static interrupt template does not grant access to arbitrary stages or other capability profiles.

Each CycleEdge has exact fields `operationStage, playerPhaseSlot, entryFromPositionId,
movementPositionId, breakdownPositionId, combatPositionIds, releasePositionId, finishPositionId`.
Rows order by stage1–3, then first-acting-side, second-acting-side. Each row references the same
stage/relative slot in the predecessor catalog. `combatPositionIds` contains exactly six ordered
steps: Position Determination, Barrage, Retreat Before Assault, Force Assignment, Anti-Armor,
Close Assault. Movement precedes Breakdown and those six steps; Reserve Release follows them.

- First entry is the existing Reserve Designation completion→Movement boundary. The successor
  completion event must create ordinal1 atomically; do not invent a second authority event.
- Repeat goes from completed Reserve Release back to that row's Movement entry, with ordinalk+1,
  same turn/stage/relative slot/resolved actor. Checked ordinal overflow is a fault, not a finish.
- Finish goes from that row's Reserve Release to its immediately following Truck Convoy Movement
  phase. It never jumps to the opposing side or performs stage housekeeping.

These are structural edges, not generic advance commands.003C3/003D2 must validate all intermediate
step/selection/settlement/release receipts and the continuation/progress guards before traversing
an edge. The linear catalog successor at Reserve Release describes finish, while repeat is the
explicit cycle edge. A generic linear `GetNext` cannot choose between them. End-of-turn continuation
outside turn111 is unsupported in this bounded contract; no wrap or clamp is allowed.

## Exact Rules artifact contributions

| Artifact ID | Canonical content / manifest source references |
| --- | --- |
| `cna-1979.1.land-sequence` | Entire catalog5 bytes. Sources are the distinct sorted union of all position and interrupt references: Land21.24–21.26,5.2,7.11,7.14. Replaces only this artifact's version4 content in the future manifest. |
| `cna-1979.1.cycle-identity.v1` | Entire codec artifact1 bytes from the fixture. Sources: rules-lab `CYCLE-DES-001:CYCLE-COMP-001-002` plus Land5.2/7.11/7.14. Digital framing is labeled rules-lab policy, not a claim about tabletop serialization. |

The codec artifact's exact property order is `schemaVersion, artifactId, identityVersion, domains,
authorityFields, publicFields, occurrenceFields, creationPrefixFields, eventPrefixFields, bounds,
openingPrefix, sources`. Its descriptors and bounds are closed, literal normative values. No
unknown descriptor/default extension is admitted. Artifact content hashes are `sha256:` plus SHA-256
of complete canonical JSON. Full Rules10 still uses the unchanged manifest's raw64-hex hash;003C2
must assemble both contributions with003C1 and retained predecessor artifacts/rulings before producing
new creation goldens. Neither catalog artifact hashes nor codec probes substitute for full Rules10.

Artifact JSON is ASCII UTF-8, fixed property order, compact standard JSON quoting, no BOM/newline,
whitespace, alternate escape, duplicate/missing/unknown field or noncanonical number. Nullable fields
must be present. Integer fields reject bool, float, exponent and numeric strings. IDs match
`[A-Za-z0-9][A-Za-z0-9._:-]{0,127}`; locators are1–512 printable ASCII characters. Limits:1MiB artifact
bytes, depth32 and512 array items, narrowed by exact membership. This new closed codec preserves
all predecessor bytes and does not change historical Unicode handling. Readers reject reordered
positions, changed source provenance and cross-slot edges even if the caller recomputes a hash.

## Cycle and occurrence tuple bytes

`U32`/`U64` are unsigned big-endian. `S` is U32 byte length then exact UTF-8. `H` is raw32-byte SHA-256,
parsed from a validated lowercase `sha256:` display value. `D(domain,payload)` hashes ASCII domain,
one zero byte, then payload; domain bytes are included in retained preimage goldens. No locale,
case-folding, normalized spelling, truncation or nullable/implicit field enters a tuple.

The exact authority tuple order is:

```text
U32(contractVersion=1)
S(campaignId), S(rulesetHash), S(setupId), S(setupHash)
S(contentPackId), S(contentHash), S(scenarioId)
U64(gameTurn), U64(operationStage), S(playerPhaseSlot), S(actingSide)
U64(ordinal), U64(openedAuthorityVersion)
H(openingPrefix), H(admittedPolicyBundleDigest)
```

Authority domain is `sandtable.cycle.authority.v1`. `rulesetHash` is64 raw lowercase hex; Setup and
Content hashes are71-character prefixed display strings and are encoded as `S`, exactly as accepted
in CYCLE-DES-001. Prefix and policy bundle use raw `H`. Do not silently substitute H for a string
hash field or include the resulting cycle digest inside its own tuple.

Public domain is `sandtable.cycle.public.v1`, with only:

```text
U32(contractVersion=1), S(campaignId), S(rulesetHash)
U64(gameTurn), U64(operationStage), S(playerPhaseSlot), S(actingSide), U64(ordinal)
```

These are audience-invariant sequence facts. Private Setup/Content/config, opening version and
Chronicle prefix are absent. Equal public tuples remain equal under hidden-only forks; this says
nothing by itself about equality of side actions, observations, timing or submission outcomes.
004 must prove those properties and freeze candidate/action-set codecs separately. Do not expose
an authority cycle ID as a public reference. Hash equality never grants authorization.

Both tuples require gameTurn1–111, stage1–3, ordinal1–2147483647. `openedAuthorityVersion` is positive
signed64-bit (maximum9223372036854775807) despite its U64 wire encoding. The first ordinal is1; its
actual opening result version is supplied by accepted history, not inferred from ordinal or position.
Valid slots are the two relative roles; actual side must agree with the externally supplied trusted
stage-order resolution. The binary reader checks complete consumption, string lengths, domain,
version and primitive grammar before returning a value. It rejects overflow/truncation/trailing bytes.
A different valid prefix/version produces a different ID; rejecting a forged-but-well-formed tuple
requires comparison against trusted creation/history in003C2/003C3, not just this codec.

`admittedPolicyBundleDigest` binds the exact admitted canonical decision configuration (003C1),
whose own hash includes the selected policy/rules-input digest. Full Rules, Setup and Content are
separately retained in this authority tuple.003C2 must pin all these inputs coherently; a caller
cannot supply a same-ID replacement configuration or mix legacy/current creation contracts.

Authority occurrence value has exact order `U32(1), H(cycleId), S(positionId)`. It is a tuple value,
not another digest or public action ID. The bound cycle plus position identifies a repeated step.
Allowed references are that cycle row's Movement, Breakdown, six Combat steps, Reserve Release,
Truck Convoy finish target, or the generic Breakdown interrupt with context supplied by the cycle.
Reserve Designation and other-slot/stage positions reject. A finish-target occurrence is only a
structural reference; it requires a validated closed-cycle/finish receipt before being a terminal.
No generic occurrence value proves interruption legality, current position or successful completion.

## Chronicle prefix and nonrecursive opening

```text
P0 = D(sandtable.cycle.prefix.creation.v1,
       U64(length(canonicalCreationBytes)) || canonicalCreationBytes)
Pnext = D(sandtable.cycle.prefix.event.v1,
          H(Pprior) || U64(length(canonicalEventBytes)) || canonicalEventBytes)
```

Length counts bytes, excluding any JSONL LF separator or diagnostic framing. Each complete canonical
creation/event record must contain1…1,048,576 bytes. Inputs must first pass their exact versioned
semantic/canonical reader; the prefix helper only frames already validated bytes.003C2/003C3 own
those readers and must reserve enough record capacity before admitting a transition. World-only
bounds are not proof that an enclosing record fits. No truncation, pruning, synthetic empty record
or step-limit finish substitutes for valid evidence.

A cycle opening uses the prefix **before** the event that opens it. Compute cycle ID from Pprior,
embed it in the new opening event, then extend the prefix to Pnext. Repeat similarly binds its new
ordinal to the prefix before that single close/open event. Opening authority version is the event's
result version; it is distinct from the pre-event history-prefix cut. No recursive hash includes its
own opening record. Replay retains creation and the complete ordered event chain; a digest supplied
by an untrusted import is not sufficient evidence for arbitrary restored World or cycle state.

## Evidence, compatibility and remaining gates

The predecessor fixture was captured from an isolated package-free .NET10 project linking only the
nine source paths whose SHA-256 values are retained in the manifest. Its Program calls:

```csharp
File.WriteAllBytes("catalog-v4.json", Cna1979LandSequenceV4.SerializeCanonicalCatalog());
Console.WriteLine(Cna1979LandSequenceV4.CreateArtifact().ContentHash);
```

The capture build passed with0 warnings/errors and a retained temporary binlog; it is not a solution
build or runtime test claim. Current C# catalog4 hash is
`sha256:42b4e721252ddf5f1107927d12b3a7f0c7ed956b08c766a3b9e2d28e3601ab9c`.
[Verifier](verify-combat-cycle-sequence-v1.py) checks that source anchoring and literal predecessor,
derives successor endpoints from its actual catalog membership, and compares against separately
constructed golden endpoint strings. The codec writer uses integer byte conversion; retained
binary preimages were generated with explicit big-endian `struct.pack` calls. Fixtures are never
regenerated by normal verification.

`framingOnly` probes use opaque byte records labeled `codecProbe` and real predecessor identity
strings. They are not production canonical creation/events, admitted new campaigns or placeholder
Rules10 manifests. Probe configuration comes from003C1; mixing it with predecessor identity is
allowed only in a byte-codec test, not campaign admission.003C2 must replace these probes with
actual compatible creation/snapshot/history evidence when that complete contract exists.

Errors: `CMB-SEQ-001` JSON/binary shape/type/truncation/duplicate/size; `002` primitive grammar/range
and ordinal overflow; `003` unsupported version/domain family/slot/actor resolution; `004` artifact
semantics/provenance/edge mismatch; `005` occurrence/cycle binding mismatch; `008` noncanonical
artifact bytes. Decoder errors use empty JSON Pointer; structured value failures retain a pointer.
A wire domain-prefix mismatch is malformed binary (`001`); an unsupported requested codec family
is `003`. Shape/ranges precede version/semantic checks, then canonical spelling.

Canonical identity/edge evidence advances CYCLE-COMP-AC-001/002 and the occurrence part of011 only.
First/repeat/finish atomicity, exact opening prefix cuts against real event bytes, active-stage
history and omitted-history rejection remain003C3/003D2/008/017–019. Public equality/admission remains004;
real Exercise terminal/replay and paired runs remain022–024. All parent003/004 and checkpoint B gates
remain open. Source/catalog presence never activates later stages, broader Breakdown, actual RBA,
released-Reserve offensive Combat, future prisoner upkeep or a playable campaign.
