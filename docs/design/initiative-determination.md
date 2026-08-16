# Initiative Determination Technical Design

**Status:** Implemented and independently reviewed

**Date:** 2026-08-15

**Specification:** [Initiative Determination](../specs/initiative-determination.md)

## Design summary

Add one pure authoritative path:

```text
recognized campaign setup + current snapshot + ResolveInitiative
                              |
                              v
                  validate state and command
                              |
                 +------------+-------------+
                 |                          |
                 v                          v
       predetermined holder       contested rating lookup
                                            |
                                            v
                               versioned SHA-256 d6 stream
                                            |
                                            v
                                  reroll complete ties
                 |                          |
                 +------------+-------------+
                              v
                  InitiativeDetermined event
                              |
                              v
          exact-event validation and pure projection
                              |
                              v
                 Naval Convoy campaign position
```

The engine, rating table, random stream, event projector, and serializer remain in `Cna.Core` and
perform no I/O. The only campaign mutation is the accepted event.

## Required correction to the merged foundation

The current `CreateTurn(gameTurn, firstPlayer)` catalog binds a single side to a fixed
first/last/first actor pattern. Land Rules 7.14 instead grants the initiative holder a separate
first-or-last choice in each Operation Stage.

Before adding initiative adjudication:

- replace the catalog's `firstPlayer` concept with an actor role;
- represent `None`, `Commonwealth`, `InitiativeHolder`, `FirstActingSide`, and
  `SecondActingSide` as distinct roles in the normalized sequence definition;
- include actor role and its source references in the sequence artifact hash;
- resolve a concrete `ActiveSide` only from campaign state; and
- reject entry into a first/second acting-side position until the corresponding stage declaration
  exists.

The current slice reaches only Naval Convoy, whose actor role is `None`. Initiative Declaration
later has role `InitiativeHolder`; player-execution roles remain unresolved until that future
mechanic. This preserves the complete sequence outline without inventing a stage choice.

Because the existing internal contract encoded incorrect semantics and no persistence boundary is
deployed, bump its contract version and reject the obsolete shape rather than maintaining a
misleading compatibility alias. Protobuf contracts are unaffected.

The canonical version-2 `LandSequencePosition` property order is:

```text
contractVersion, positionId, gameTurn, operationStage, stageId, phaseId,
segmentId, stepId, actorRole, activeSide, sources
```

`actorRole` is exactly one of `none`, `commonwealth`, `initiative-holder`,
`first-acting-side`, or `second-acting-side`. `activeSide` is null until a nonrelative or declared
role resolves it, and must agree with the role/campaign state when non-null. `segmentId` and
`stepId` are explicit nulls when absent. Sources are sorted by `sourceId`, then `locator`; each item
orders `sourceId`, `locator`. The sequence artifact hashes this canonical shape, including actor
role and sources, in catalog order.

## Component ownership

| Component | Responsibility |
| --- | --- |
| `Cna.Core.Rules` | normalized Initiative Ratings table, source references, sequence actor roles, ruleset artifacts/hash |
| `Cna.Core.Randomness` | repository-owned deterministic byte stream and unbiased d6 operation |
| `Cna.Core.Setups` | recognized setup definitions and canonical setup hashes; two clearly synthetic lab setups initially |
| `Cna.Core.Campaigns` | versioned command/event/snapshot contracts, validation, decision, projection, canonical serialization, replay |
| `Cna.OrleansHost` | future scheduling/activation only; not involved in this slice |
| Intelligence projects | no responsibility and no access to random seed/cursor |

These namespaces remain domain modules inside `Cna.Core`; no new service or project is introduced.

## Normalized rules data

### Initiative Ratings

Use typed rows rather than resolver constants:

```csharp
public sealed record GameTurnRange(int First, int Last);

public sealed record CommonwealthInitiativeRating(
    int SchemaVersion,
    GameTurnRange Turns,
    int Rating,
    IReadOnlyList<RuleReference> Sources);

public enum AxisInitiativePresence
{
    RommelOnQualifyingGameMap,
    GermanLandCombatUnitOnQualifyingGameMap,
    NeitherOnQualifyingGameMap,
}

public sealed record AxisInitiativeRating(
    int SchemaVersion,
    AxisInitiativePresence Presence,
    int Rating,
    IReadOnlyList<RuleReference> Sources);
```

`Cna1979InitiativeRatings` owns immutable canonical rows and pure lookup functions. It rejects Game
Turns outside 1-111 and undefined enum values. The Axis situation resolver applies precedence in
the listed order: Rommel, then any German land combat unit, then neither. A Holding Box fact is not
a qualifying-map fact and therefore never enters this enum as a qualifying presence.

The manifest adds a `cna-1979.1.initiative-ratings` artifact whose content hash covers schema
version, range bounds, presence identifiers, ratings, and sorted source references.

Its UTF-8 canonical hash payload has property order `schemaVersion`, `commonwealthRows`,
`axisRows`. Commonwealth rows order by `firstTurn` and contain `firstTurn`, `lastTurn`, `rating`,
`sources`. Axis rows use stable presence order `rommel-on-qualifying-game-map`,
`german-land-combat-unit-on-qualifying-game-map`, `neither-on-qualifying-game-map` and contain
`presence`, `rating`, `sources`. Source items use `sourceId`, `locator`. Artifact content hash is
`sha256:` plus 64 lowercase hexadecimal digits over those exact bytes.

### Source references

Use stable locators rather than one broad range:

- `spi-1979-land-rules:7.11` for initiative-side/first-player semantics;
- `spi-1979-land-rules:7.12` for beginning-of-turn timing and holder duration;
- `spi-1979-land-rules:7.13` for the date-dependent rating concept;
- `spi-1979-land-rules:7.14` for opposed dice, rating addition, tie rerolls, and the per-stage
  first-or-last election;
- `spi-1979-land-rules:7.15` for predetermined first-turn behavior;
- `spi-1979-common-charts:initiative-ratings` for rating rows; and
- `spi-1979-common-charts:initiative-ratings-note` for the Holding Box exclusion.

The repository stores facts and locators, not scanned pages or copied prose.

The actor-contract migration removes the current `OperationStageOrderSourceReference` pointing at
`7.12`. Actor-role semantics instead cite `7.11` and `7.14`; `7.12` remains attached only to the
timing and holder-duration facts it actually supports.

## Campaign setup seam

Initiative needs scenario-owned facts before full scenario content exists. Introduce a small,
versioned setup definition rather than adding free-form fields to `CreateCampaign`:

```csharp
public abstract record InitiativePolicy;

public sealed record PredeterminedInitiative(LandSide Holder) : InitiativePolicy;

public enum AxisInitiativeLocation
{
    QualifyingGameMap,
    TripoliTunisiaHoldingBox,
    OffMapOrUnavailable,
}

public sealed record AxisInitiativeSourceFacts(
    AxisInitiativeLocation RommelLocation,
    IReadOnlyList<AxisInitiativeLocation> GermanLandCombatUnitLocations);

public sealed record ContestedInitiative(
    AxisInitiativeSourceFacts AxisFacts) : InitiativePolicy;

public sealed record CampaignSetupDefinition(
    int SchemaVersion,
    string SetupId,
    string DisplayName,
    bool IsSynthetic,
    int InitialGameTurn,
    InitiativePolicy InitialInitiative,
    IReadOnlyList<RuleReference> Sources,
    string Hash);
```

`Cna1979InitiativeRatings.ClassifyAxisPresence` maps source facts to the three normalized rating
cases. Rommel qualifies only at `QualifyingGameMap`; otherwise any German land combat unit at that
location selects the middle case; otherwise the result is `NeitherOnQualifyingGameMap`.
`TripoliTunisiaHoldingBox` is intentionally distinct from absence so the chart note has a real
input/output test. These three location values are the entire location model for this slice, not a
map or placement schema. `GermanLandCombatUnitLocations` is a duplicate-free list in stable enum
order, representing occupied location categories rather than unit identities. Future scenario
ingestion derives these facts from authoritative placements.

`CreateCampaign` carries `SetupId` and `SetupHash`. The engine resolves them through a fixed setup
catalog and copies the canonical definition into the creation event. An unknown identifier, hash
mismatch, unrecognized schema, or invalid policy rejects before campaign creation.

The first implementation contains two synthetic entries and must not claim either is a published
scenario:

- `rules-lab.initiative.predetermined`: Game Turn 1 with a fixed Axis holder and source
  `sandtable-rules-lab:initiative.predetermined-axis.v1`; and
- `rules-lab.initiative.contested`: Game Turn 43 with Rommel `OffMapOrUnavailable` and one German
  land combat unit at `QualifyingGameMap`, sourced as
  `sandtable-rules-lab:initiative.contested-turn-43.v1`.

The particular fixture facts are original test content. They exercise the predetermined path and
a lower-Axis-rating-versus-higher-Commonwealth-rating contested path through the public campaign
integration without encoding arbitrary ratings.

The setup hash covers schema version, identifier, synthetic marker, initial turn, complete policy
facts, and sorted setup sources. `CampaignCreated` records the canonical setup identity and source
set. `InitiativeDetermined.Sources` is the sorted union of the applicable rule/table references and
the setup sources that supplied the initial holder or presence facts. Consequently, the
predetermined event cites both Land Rule 7.15 and its synthetic setup locator; the contested event
cites its synthetic setup locator, the applicable separate Land Rule 7.12, 7.13, and 7.14
references, and the chart rows/notes it used.

Setup hash format is `sha256:` plus 64 lowercase hexadecimal digits over UTF-8 canonical JSON with
property order `schemaVersion`, `setupId`, `isSynthetic`, `initialGameTurn`,
`initialInitiative`, `sources`. The hash field itself and `DisplayName` are excluded. Policy and
source encoding use the canonical shapes defined below.

Future scenario ingestion implements the same immutable setup contract and canonical hash. It does
not change initiative commands or resolution. Setup hash is campaign configuration identity and is
stored beside, not folded into, the `cna-1979.1` ruleset hash.

`DisplayName` is catalog-only presentation metadata. It is excluded from the setup hash,
`CampaignSetupSnapshot`, authoritative event bytes, and campaign snapshots; changing it cannot
change replay. All other setup fields shown above are authoritative and hashed.

## Random-stream contract

### State

```csharp
public sealed record RandomStreamState(
    int ContractVersion,
    string AlgorithmId,
    ulong Seed,
    ulong NextByteCursor);
```

The only initial algorithm ID is `sandtable.sha256-counter.v1`. Algorithm ID is immutable for the
campaign. `Seed` and `NextByteCursor` are internal authoritative state and checkpoint data, not
player observations.

### Byte generation

For byte cursor `n`:

```text
blockIndex = n / 32
byteIndex  = n % 32
input      = ASCII("sandtable.random.v1")
             || 0x00
             || UInt64BigEndian(seed)
             || UInt64BigEndian(blockIndex)
block      = SHA256(input)
result     = block[byteIndex]
```

The domain is 19 ASCII bytes, making the complete hash input 36 bytes. Encoding, byte order, hash,
and indexing are part of contract version 1 and require golden vectors. A checked increment guards
cursor overflow.

### Unbiased d6

Consume candidate bytes until one is less than 252. Return `(candidate % 6) + 1`. Advance the
cursor for every candidate, including rejected candidates. This produces six equally sized byte
buckets and prevents modulo bias.

Initiative always requests Axis first and Commonwealth second within each opposed round. This
ordering is a software normalization needed for reproducibility; it does not change simultaneous
table behavior and is recorded in the algorithm artifact. A tie starts a new complete pair.

The ruleset manifest adds a `cna-1979.1.random-procedure` artifact whose content hash covers the
algorithm ID, domain bytes, endian convention, d6 rejection threshold/mapping, and initiative draw
order. The artifact cites the Land Rules for the required dice and identifies its deterministic
encoding as a Sandtable normalization.

Its UTF-8 canonical hash payload uses this exact property order and values:

```text
schemaVersion: 1
algorithmId: "sandtable.sha256-counter.v1"
domainAscii: "sandtable.random.v1"
separatorByte: 0
integerEncoding: "unsigned-64-big-endian"
blockBytes: 32
d6AcceptBelow: 252
d6Modulo: 6
d6Offset: 1
initiativeDrawOrder: ["axis", "commonwealth"]
sources: [{ sourceId, locator }, ...]
```

Sources are canonically sorted and include `spi-1979-land-rules:7.14` plus
`sandtable-random-procedure:sha256-counter.v1` for the deterministic normalization. Artifact
content hash uses the same `sha256:` lowercase format.

## Contracts

Names below define responsibilities; minor C# shape changes are acceptable if serialized semantics
and requirement IDs remain unchanged.

### Creation command and replay payload

```csharp
public sealed record CreateCampaign(
    string CampaignId,
    string RulesetHash,
    ulong Seed,
    string SetupId,
    string SetupHash) : CampaignCommand(2, 0);

public sealed record CampaignSetupSnapshot(
    int SchemaVersion,
    string SetupId,
    string SetupHash,
    bool IsSynthetic,
    int InitialGameTurn,
    InitiativePolicy InitialInitiative,
    IReadOnlyList<RuleReference> Sources);

public sealed record CampaignCreated(
    string CampaignId,
    long StateVersion,
    string RulesetHash,
    CampaignSetupSnapshot Setup,
    RandomStreamState RandomState,
    LandSequencePosition SequencePosition)
    : CampaignEvent(2, CampaignId, StateVersion);
```

`CreateCampaign` is a request to use a catalog entry, not an authoritative copy supplied by the
caller. Decision resolves `SetupId`/`SetupHash` through the fixed catalog and rejects a mismatch.
It then emits an immutable `CampaignSetupSnapshot` containing every replay-required setup fact and
source. `DisplayName` is deliberately absent.

Creation preserves existing rejection precedence: the dispatcher first returns `InvalidState` for
a supplied noncanonical snapshot; otherwise any existing campaign returns
`CampaignAlreadyCreated` before create-command fields are inspected. Only a null snapshot reaches
contract/ruleset/setup validation and possible creation.

Creation initializes `RandomState` with contract version 1, algorithm
`sandtable.sha256-counter.v1`, the command seed, and cursor 0. `SequencePosition` is the version-2
Initiative Determination position for `Setup.InitialGameTurn`, with actor role `None` and no active
side. `CampaignCreated.StateVersion` is exactly 1.

Replay validates the embedded setup snapshot and recomputes `SetupHash` from its fields; it does
not look up the current setup catalog. This makes accepted history independent of later catalog
display changes or entry removal. The ruleset hash still selects the versioned rules resolver; only
the current canonical hash is supported in this pre-persistence slice.

Canonical `CampaignCreated` JSON has this exact property order:

```text
contractVersion, eventType, campaignId, stateVersion, rulesetHash,
setup, randomState, sequencePosition
```

The nested `setup` order is `schemaVersion`, `setupId`, `setupHash`, `isSynthetic`,
`initialGameTurn`, `initialInitiative`, `sources`. The nested `randomState` order is
`contractVersion`, `algorithmId`, `seed`, `nextByteCursor`. Policy discriminators and fields are
versioned, lower-kebab-case values. Sources use canonical source/locator order. Unknown or extra
semantic values are rejected. The in-process command has no transport serializer in this slice;
any future transport must preserve the command version and five fields above.

`initialInitiative` uses one of these exact canonical shapes:

```text
{ kind: "predetermined", holder: "axis" | "commonwealth" }

{ kind: "contested", axisFacts: {
    rommelLocation: "qualifying-game-map" |
                    "tripoli-tunisia-holding-box" |
                    "off-map-or-unavailable",
    germanLandCombatUnitLocations: [same location values, unique and sorted]
  }
}
```

The event type discriminator is `campaign-created`; the later result uses
`initiative-determined`.

### Command

```csharp
public sealed record ResolveInitiative(
    long ExpectedStateVersion,
    string ExpectedPositionId) : CampaignCommand(2, ExpectedStateVersion);
```

It contains no outcome-bearing data. `CompleteCurrentSequenceStep` remains unsupported at
Initiative Determination and Naval Convoy.

### Resolution facts

```csharp
public sealed record InitiativeRollRound(
    int Round,
    int AxisDie,
    int AxisRating,
    int AxisTotal,
    int CommonwealthDie,
    int CommonwealthRating,
    int CommonwealthTotal);

public abstract record InitiativeOutcome;

public sealed record PredeterminedInitiativeOutcome(
    LandSide Holder) : InitiativeOutcome;

public sealed record ContestedInitiativeOutcome(
    AxisInitiativeSourceFacts AxisFacts,
    AxisInitiativePresence AxisPresence,
    IReadOnlyList<InitiativeRollRound> Rounds,
    LandSide Holder) : InitiativeOutcome;
```

Rounds are immutable and ordered from 1. The event does not store caller-controlled derived totals;
the resolver constructs them, and history validation recomputes them.

All collection-bearing contracts are value objects, not default positional records over
`IReadOnlyList`. Constructors defensively copy, validate, and canonically order unordered source
sets. `Equals`/`GetHashCode` compare collection elements structurally; ordered roll rounds use
`SequenceEqual`. Tests mutate the caller's original arrays and compare separately allocated equal
collections. Projector validation may compare canonical bytes or explicit semantic fields, but
must never depend on interface reference equality.

Canonical round order is `round`, `axisDie`, `axisRating`, `axisTotal`, `commonwealthDie`,
`commonwealthRating`, `commonwealthTotal`. Outcome shapes are exact:

```text
{ kind: "predetermined", holder }

{ kind: "contested", axisFacts, axisPresence, rounds, holder }
```

`axisFacts` uses the creation-policy shape defined above; `axisPresence` uses the stable presence
strings defined by the ratings artifact. The event therefore explains its Axis input without
requiring a separate creation-event lookup, while replay still verifies it equals the embedded
setup policy.

### Event

```csharp
public sealed record InitiativeDetermined(
    string CampaignId,
    long StateVersion,
    string FromPositionId,
    InitiativeOutcome Outcome,
    string RandomAlgorithmId,
    ulong RandomCursorBefore,
    ulong RandomCursorAfter,
    LandSequencePosition SequencePosition,
    IReadOnlyList<RuleReference> Sources)
    : CampaignEvent(2, CampaignId, StateVersion);
```

The target position must be the first Naval Convoy position. Predetermined outcomes use equal
cursor boundaries. The applicable procedure/table/setup source union is sorted and deduplicated
canonically.

Source-set construction is exact. Predetermined outcomes include all setup sources plus Land Rules
7.12 and 7.15. Contested outcomes include all setup sources, Land Rules 7.12, 7.13, and 7.14, and
`spi-1979-common-charts:initiative-ratings`. They additionally include
`spi-1979-common-charts:initiative-ratings-note` if and only if Rommel or any German land combat
unit has location `TripoliTunisiaHoldingBox`. No other source is inferred.

Canonical `InitiativeDetermined` JSON order is `contractVersion`, `eventType`, `campaignId`,
`stateVersion`, `fromPositionId`, `outcome`, `randomAlgorithmId`, `randomCursorBefore`,
`randomCursorAfter`, `sequencePosition`, `sources`. The outcome discriminator precedes its fields;
contested rounds retain chronological order. This event contains no seed or setup display text.

### Snapshot

```csharp
public sealed record CampaignSnapshot(
    int ContractVersion,
    string CampaignId,
    long StateVersion,
    string RulesetHash,
    CampaignSetupSnapshot Setup,
    LandSide? InitiativeHolder,
    RandomStreamState RandomState,
    LandSequencePosition SequencePosition);
```

This version-2 shape replaces `FirstPlayer`. Before resolution, the embedded setup policy remains
available and `InitiativeHolder` is null. After resolution, the holder is set and the unchanged
setup remains as replay/audit context. Canonical snapshot order is `contractVersion`, `campaignId`,
`stateVersion`, `rulesetHash`, `setup`, `initiativeHolder`, `randomState`, `sequencePosition`.
`DisplayName` is absent.

No Operation Stage first actor is stored in this slice. Later, add a three-entry stage-order
declaration collection rather than overloading `InitiativeHolder`.

## Decision flow

`CampaignEngine.Decide` remains a pure dispatcher.

For `ResolveInitiative`:

1. After the dispatcher has validated any supplied non-null snapshot, require a snapshot; null
   returns `CampaignNotCreated` before command fields are inspected.
2. Validate command contract and required fields.
3. Apply stale-version and expected-position checks in the specification's order.
4. Require Initiative Determination and a null holder.
5. Resolve the setup-bound policy.
6. For predetermined policy, create the outcome without calling the random stream.
7. For contested policy, derive both ratings from normalized data, consume opposed rounds until a
   non-tie, and select its higher side.
8. Resolve the immediate next sequence position as Naval Convoy.
9. Emit exactly one event with state version incremented by one.

No step mutates the input snapshot. The random stream returns values plus a new state; it never
keeps ambient mutable state.

## Projection and trusted-history validation

`CampaignProjector.ApplyInitiativeDetermined` must not merely trust event fields. Given the prior
snapshot, it reruns the pure resolver with the prior setup, seed, cursor, and ruleset, then compares
the complete expected event with the supplied event. Any changed roll, rating, total, round count,
source, cursor, holder, state version, campaign ID, from-position, or target position throws
`InvalidCampaignHistoryException`.

After an exact match, projection:

- increments state version;
- stores the holder;
- stores the event's ending cursor;
- retains the immutable embedded setup unchanged; and
- sets the Naval Convoy position.

This closes the existing provisional projector seam for the first implemented mechanic. Future
untrusted Chronicle ingestion still requires origin authentication, but structurally forged
initiative history is rejected even at the trusted boundary.

Contract-v2 projection removes `CampaignSequenceAdvanced` from the supported path. In particular,
`[CampaignCreated, CampaignSequenceAdvanced]` cannot cross Initiative Determination to Naval
Convoy; the obsolete version-1 event throws `InvalidCampaignHistoryException`. No compatibility
adapter may translate it into `InitiativeDetermined`, because it contains neither an outcome nor
random/provenance facts.

## Canonical serialization

Extend explicit JSON writers/readers; do not rely on reflection property order.

- Snapshot serializer includes contract, campaign/setup/ruleset identities, state version,
  initiative policy/holder, random state, and sequence position in fixed order.
- Add a canonical campaign-event serializer so repeated decisions and golden history can be
  compared byte-for-byte.
- Enum values serialize as stable lower-kebab-case identifiers, not C# member names.
- Source collections sort by source ID then locator; table rows sort by semantic key; roll rounds
  preserve chronological order.
- Standalone readers reject unknown contract versions, missing/extra semantic values, invalid
  enums, bad hashes, structurally inconsistent outcomes, and positions that are not shaped as
  Naval Convoy. Cursor continuity, exact source unions, and exact catalog positions depend on the
  prior campaign snapshot and are therefore rejected by history-aware projection rather than by
  context-free deserialization.

Internal canonical snapshots contain seed/cursor. Player observations and future transport DTOs
must be projections that omit them; the snapshot serializer must never be reused as a player API.

## Failure paths

| Condition | Result |
| --- | --- |
| Unknown setup or setup-hash mismatch on create | `InvalidCommand`, no event |
| Noncanonical ruleset hash | `InvalidCommand`, no event |
| Invalid snapshot/setup/random/sequence invariant | `InvalidState`, no event |
| Malformed initiative command | `InvalidCommand`, no event |
| Wrong state version | `StaleState`, no event |
| Wrong expected position | `UnexpectedSequenceStep`, no event |
| Resolve outside Initiative Determination or after holder exists | `UnsupportedTransition`, no event |
| Random cursor overflow | `InvalidState`, no partial event |
| Generic completion at Initiative or Naval Convoy | `UnsupportedTransition`, no event |
| Legacy `CampaignSequenceAdvanced` attempts Initiative-to-Naval replay | `InvalidCampaignHistoryException` |
| Inconsistent replay event | `InvalidCampaignHistoryException` |

## Testing strategy

Use xUnit v3 through Microsoft.Testing.Platform. Tests are deterministic and require no clock,
files, network, host, or model.

### Rule/table tests

- Commonwealth boundary turns 1/42/43/90/91/111 and invalid 0/112.
- All three Axis presence cases and undefined enum rejection.
- Holding Box-only typed facts normalize to neither qualifying presence and remain distinguishable
  from absence at the classifier input.
- Table row/source/actor-role changes alter artifact and manifest hashes.

### Random tests

- Golden stream blocks and cursor-spanning bytes from an independent vector generator.
- Golden d6 sequences, including candidate rejection at 252-255.
- Checked cursor overflow.
- Same seed/cursor yields same dice; selected different seeds differ.
- Statistical smoke tests are not acceptance evidence and should not replace exact vectors.

### Resolver tests

- predetermined path consumes no bytes;
- contested no-tie and multi-tie paths;
- exact draw order, ratings, totals, holder, and source set;
- no input object mutation.

### Campaign tests

- every acceptance scenario in the specification;
- validation precedence and zero-event rejection;
- duplicate and generic advance rejection;
- canonical event/snapshot byte equality;
- catalog-independent replay of embedded `CampaignCreated` setup/random facts and `DisplayName`
  exclusion;
- replay recomputation and forged-field matrix;
- explicit rejection of legacy generic Initiative-to-Naval history;
- defensive-copy and structural-equality tests for every collection-bearing contract;
- holder exists at Naval Convoy while stage actor remains absent.

Use chosen golden seeds checked into test data as numbers and expected results, not source scans.

## Implementation plan

| Checkpoint | Status | Evidence |
| --- | --- | --- |
| `INIT-IMP-001` | Complete | ratings boundaries, provenance, structural equality, and manifest mutation tests |
| `INIT-IMP-002` | Complete | published random golden vectors, rejection sampling, cursor, and overflow tests |
| `INIT-IMP-003` | Complete | recognized synthetic setup catalog and canonical hash tests |
| `INIT-IMP-004` | Complete | version-2 campaign/sequence migration, strict snapshot round trip, and legacy rejection tests |
| `INIT-IMP-005` | Complete | predetermined/contested resolution, tie reroll, command, projection, and forgery tests |
| `INIT-IMP-006` | Complete | canonical event golden bytes, strict reader, replay, and seed-divergence tests |
| `INIT-IMP-007` | Complete | repository reconciliation, 122-test quality gate, and independent `Ready` readback |

Every numbered task is an independently compiling, green checkpoint. Each begins with a focused
red test, implements the minimum behavior, refactors while green, and ends with the full Core test
project. File counts are estimates, not a correctness constraint: the version-2 campaign migration
is intentionally atomic across every direct consumer so no checkpoint contains incompatible
contracts. If a task is split during execution, the compatibility bridge must keep both halves
buildable and must be removed before the task is complete.

### `INIT-IMP-001` — Normalize Initiative Ratings and provenance

**Depends on:** approved specification.

- Add immutable Commonwealth/Axis rows, exact 7.13/chart sources, lookup functions, and artifact
  hashing without changing campaign contracts.
- Add the minimal Axis location/source-fact value and classifier. Cover all table boundaries, Axis
  cases, Holding Box-only normalization, defensive copies, and hash mutation.

**Ownership:** Axis location/source-fact values, rating rows/catalog, standalone artifact hash
builder, and their focused tests. It does not edit `Cna1979Ruleset.cs` or the canonical manifest.

**Green evidence:** focused rating/manifest tests, full Core tests, build.

### `INIT-IMP-002` — Implement deterministic random stream

**Depends on:** approved algorithm contract; parallel-safe with `INIT-IMP-001`.

- Add the SHA-256 counter byte source, immutable random state, unbiased d6, overflow handling, and
  a normalized random-procedure artifact.
- Generate exact expected vectors independently of production code and cover rejected candidates.

**Ownership:** `Cna.Core.Randomness`, standalone random-artifact hash builder, and their focused
tests. It does not edit `Cna1979Ruleset.cs` or the canonical manifest.

**Green evidence:** byte/d6 vectors, manifest mutation tests, full Core tests, build.

### `INIT-IMP-003` — Add recognized setup values

**Depends on:** `INIT-IMP-001`.

- Add setup/policy value objects, structural collection semantics, canonical setup hashing, the two
  synthetic entries, and stable `sandtable-rules-lab` sources.
- Reuse the Task 001 Axis location/source-fact value; do not redefine or wrap it.
- Keep setup definitions additive; do not change `CreateCampaign` yet.

**Ownership:** `Cna.Core.Setups` and focused setup tests only.

**Green evidence:** setup validation/hash/provenance/mutation tests, full Core tests, build.

### `INIT-IMP-004` — Atomically migrate sequence and campaign authority to version 2

**Depends on:** `INIT-IMP-001`, `INIT-IMP-002`, `INIT-IMP-003`.

- Replace fixed side assignments in the normalized sequence with sourced actor roles and migrate
  the manifest hash from stale 7.12 actor provenance to 7.11/7.14.
- Replace `FirstPlayer` in creation/event/snapshot contracts with canonical setup identity/policy,
  holder state, and random state; bind creation to the setup catalog.
- Update every direct consumer in the same checkpoint: engine, projector, snapshot validator,
  snapshot serializer, replay harness, canonical manifest construction, and all affected existing
  tests.
- Make the sole canonical-manifest cutover: add the rating/random artifacts, replace the sequence
  artifact with actor-role bytes, update `Cna1979Ruleset.cs`, and reconcile manifest tests.
- Implement the complete canonical version-2 snapshot writer/reader and round-trip every field.
- Bump internal contract versions and reject obsolete shapes. Keep the legacy
  `CampaignSequenceAdvanced` type constructible only as negative-history input, but reject it in
  the version-2 projector from this checkpoint onward. The campaign still stops at Initiative
  Determination after this task.

**Ownership:** sole owner of `Cna1979Ruleset.cs`, canonical manifest tests, version-2 campaign
contracts/consumers, and `CampaignSnapshotSerializer`. This is deliberately larger than five files
because a partial schema migration cannot be a green checkpoint.

**Green evidence:** creation/replay and canonical snapshot round-trip remain byte-equivalent under
version 2; actor-role/hash/setup tests and `INIT-AC-014` pass; all legacy foundation tests are
migrated; full Core tests and build pass.

### `INIT-IMP-005` — Resolve and project initiative

**Depends on:** `INIT-IMP-004`.

- Add collection-safe outcome/event values, `ResolveInitiative`, the pure resolver, validation
  order, deterministic rolls, and exact event emission.
- Recompute the expected initiative event in projection and advance only to Naval Convoy.
- Preserve the Task 004 rejection of legacy generic events; this task adds only the new
  `InitiativeDetermined` projection path.

**Ownership:** initiative contracts/resolver, campaign engine/projector, and focused resolver,
campaign, collection-equality, and negative-history tests.

**Green evidence:** `INIT-AC-001` through `INIT-AC-006`, `INIT-AC-010`, `INIT-AC-011`, and
`INIT-AC-015`; full Core tests and build.

### `INIT-IMP-006` — Canonical event serialization and replay proof

**Depends on:** `INIT-IMP-005`.

- Add canonical serialization for `CampaignCreated` and `InitiativeDetermined`, golden event bytes,
  and event-reader hardening. Validate source unions, structural collections, cursor continuity,
  and enum/version values without introducing any snapshot field left out of Task 004.
- Prove repeated decision/event bytes, multi-tie replay, seed divergence, and the full forged-field
  matrix.

**Ownership:** `CampaignEventSerializer`, event-reader validation, replay matrices, and event golden
tests. It does not add or change snapshot fields; Task 004 owns snapshot serialization.

**Green evidence:** `INIT-AC-007`, `008`, `009`, and `017`, canonical golden history, full Core
tests, build.

### `INIT-IMP-007` — Demonstration and repository reconciliation

**Depends on:** `INIT-IMP-006`.

- Update README, technical design, naming, roadmap/design status, and developer demonstration to
  describe implemented behavior rather than the proposal.
- Run focused tests, `just check`, diff check, traceability audit, and independent code review.

**Ownership:** documentation and verification evidence only.

## Requirement-to-checkpoint traceability

Every functional requirement reaches one implementation checkpoint through explicit acceptance
evidence:

| Requirement | Acceptance evidence | Completing checkpoint |
| --- | --- | --- |
| `INIT-001` | `INIT-AC-006` | `INIT-IMP-005` |
| `INIT-002` | `INIT-AC-001`-`003` | `INIT-IMP-005` |
| `INIT-003` | `INIT-AC-004` | `INIT-IMP-001` |
| `INIT-004` | `INIT-AC-005` | `INIT-IMP-001` |
| `INIT-005` | `INIT-AC-006` | `INIT-IMP-005` |
| `INIT-006` | `INIT-AC-001`, `002`, `006` | `INIT-IMP-005` |
| `INIT-007` | `INIT-AC-001`, `002`, `010` | `INIT-IMP-005` |
| `INIT-008` | `INIT-AC-006`, `011` | `INIT-IMP-005` |
| `INIT-009` | `INIT-AC-007`, `009` | `INIT-IMP-006` |
| `INIT-010` | `INIT-AC-011` | `INIT-IMP-005` |
| `INIT-011` | `INIT-AC-012` | `INIT-IMP-004` |
| `INIT-012` | `INIT-AC-013` | `INIT-IMP-004` |
| `INIT-013` | Internal snapshot-only contract test now; player-observation negative test is explicitly deferred to Sprint 2 | `INIT-IMP-004` plus deferred Sprint 2 gate |
| `INIT-014` | Pure resolver/replay fixtures in `INIT-AC-001`-`003`, `007`, `009` | `INIT-IMP-006` |
| `INIT-015` | `INIT-AC-014` | `INIT-IMP-004` |
| `INIT-016` | `INIT-AC-001`, `002`, `016` | `INIT-IMP-005` |
| `INIT-017` | `INIT-AC-009`, `017` | `INIT-IMP-006` |

Acceptance ownership is unique:

| Checkpoint | Acceptance scenarios completed |
| --- | --- |
| `INIT-IMP-001` | `INIT-AC-004`, `005` |
| `INIT-IMP-002` | Exact random vectors that support `INIT-AC-002`, `003`, `007`, `008` |
| `INIT-IMP-003` | `INIT-AC-016` |
| `INIT-IMP-004` | `INIT-AC-012`, `013`, `014` and v2 creation/snapshot prerequisites |
| `INIT-IMP-005` | `INIT-AC-001`, `002`, `003`, `006`, `010`, `011`, `015` |
| `INIT-IMP-006` | `INIT-AC-007`, `008`, `009`, `017` |
| `INIT-IMP-007` | Repository gate and documentation exit criteria; no new mechanic acceptance behavior |

Non-functional ownership: `INIT-NFR-001`, `003`, and `004` complete in `INIT-IMP-006` through
golden cross-contract bytes; `INIT-NFR-002` completes in `INIT-IMP-005` through pure resolver tests;
`INIT-NFR-005` completes across `INIT-IMP-001`, `003`, and `005` through provenance assertions;
`INIT-NFR-006` completes in `INIT-IMP-007` through `just check`; and `INIT-NFR-007` completes in
`INIT-IMP-005` through mutation/equality tests.

## Verification commands

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build
dotnet format Sandtable.slnx --verify-no-changes --no-restore
dotnet build Sandtable.slnx --no-restore
dotnet test --solution Sandtable.slnx --no-build
git diff --check
```

Use the repository's restore command before these if package state is not already current. The
final implementation handoff must report exact test totals and zero-warning build evidence.

## Decision log

| Decision | Reason |
| --- | --- |
| Separate initiative holder from stage first actor | Required by Land Rules 7.11 and 7.14 and preserves three player choices. |
| One outcome command for predetermined and contested policies | Keeps sequencing uniform while setup/state owns the policy. |
| Fixed setup ID/hash rather than free-form situation command fields | Prevents callers from authoring authoritative ratings or presence facts. |
| Repository-owned SHA-256 counter stream | Stable across runtime versions, explicit, dependency-free, and replay-verifiable. |
| Cursor counts candidate bytes | Rejection sampling remains exactly replayable. |
| Axis then Commonwealth draw order | Removes implementation ambiguity without changing opposed result semantics. |
| Projector recomputes exact expected event | Rejects structurally forged trusted history and proves seed-plus-command determinism. |
| Stop at Naval Convoy | It is the next mandatory unsupported phase. |
| No new service/project | All behavior is pure domain authority inside `Cna.Core`. |

## Deferred decisions

- The exact published `Graziani's Offensive` setup identifier, initial holder, map-presence facts,
  and content hash wait for source-controlled scenario ingestion.
- Initiative Declaration commands and the three stage-order entries wait for the Operation Stage
  slice, but the actor-role contract must support them now.
- Player observation, Chronicle redaction, and transport schemas will prove seed/cursor
  non-disclosure when those boundaries exist.
- Random algorithm migration policy beyond rejecting unknown IDs waits until a second algorithm or
  persisted production campaign exists.
- This slice replays only the current canonical ruleset hash. Before retained production campaigns
  can outlive a ruleset revision, projection must select a historical resolver/manifest by recorded
  hash rather than recomputing old history with new rules.
