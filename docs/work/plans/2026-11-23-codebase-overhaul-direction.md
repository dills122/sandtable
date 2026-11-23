# Codebase overhaul direction: dispatch, duplication, and structural future-proofing (2026-11-23)

**Status:** direction document for owner review and sync — no code in this pass. Written
after two read-only audits (2026-11-22/23) found the real quality problems in this
codebase. This doc goes one level deeper than those audits: for each problem, it names
the concrete pattern a mature event-sourced / data-driven engine would use instead, shows
what that looks like against this codebase's actual types, and gives a sequencing plan
that doesn't collide with the in-flight 6-turn expansion (MTS-TASK-005/006 and beyond).

**What this doc is not:** a request to stop feature work. The owner was explicit — finish
what's in flight, plan this carefully, sync before touching anything. Section 6 is the
part that answers "what can start now vs. what must wait."

## The diagnosis in one sentence

The domain model (one class per native event, e.g. `CampaignNativeSecondReserveV43`) is
well-isolated and genuinely easy to extend — every slice landed this session proves that.
The problem is entirely in three **orchestration hubs** that every new mechanic must
touch by hand, in the same repetitive shape, because nothing in this codebase treats
"which handler does this event/kind/opening map to" as *data*. It's encoded as Boolean
control flow instead — a sequence of `if`/`else if` on an enum, or a sequence of
`if (session.CurrentX is {} x) return ...` on "whichever optional field happens to be
set." Both shapes share the same failure mode: correctness depends on **manually
maintained order**, enforced by nothing but a reviewer's attention and a comment.

## 0. Deeper direction: orchestration/event-handling architecture (owner-requested follow-up)

The owner asked for more than dispatch-table swaps: a genuine rethink of how this engine
orchestrates and handles events, holistically, not a patch on the existing shape. This
section is that rethink. Sections 1-6 below (the original pass) stay valid as the
*tactical* fixes; this section is the *structural* one underneath them — read it first.

### 0.1 The real shape of `ExerciseSession`, confirmed by direct count

`ExerciseSession.cs` is 1,341 lines and currently runs **two different mutation patterns
side by side**:

- **The original pattern** (lines 39-475, the one constructor): ~30 optional parameters
  threaded through ~440 lines of dense cross-field invariant checks — e.g. the Combat
  chain alone (lines 99-246) checks that `combatBarrage` binds `combatSelection`'s state
  version and decision-action-id, that `combatRetreatMove` binds `combatBarrage`'s state
  version *and* the moved World *and* the prior contacts, and so on through eleven more
  fields. This is real, load-bearing validation — not boilerplate to delete.
- **The newer pattern** (introduced at `#394`, e.g. `WithSecondBreakdown` at line 827,
  `WithStageEntryReserve` at line 1264, 18 `WithXxx` methods in total): `MemberwiseClone()`
  plus direct field assignment, with a handful of guard clauses (2-4 checks, not the
  original's dense invariant graph) and a dedicated `XxxPredecessor` back-pointer property
  per mechanic — confirmed by direct count: **18 `*Predecessor` properties, 62 `Current*`
  properties, 25 `WithXxx` methods** in the file today.

Nobody decided "the old mechanics get heavyweight validation, the new ones get a shallow
clone" as a design choice — it's what happened because touching the original
constructor's invariant graph for every new side-scoped field was (correctly) judged too
risky, so new work routes around it entirely. The result is two different answers to "how
much can I trust this session's internal consistency," depending on *when* the field was
added, undocumented anywhere except by reading which code path constructed it.

**The forward-dispatch cascade** (`CampaignExercises.QueryCheckpoint`, cited in Section 1
below) is one symptom of "which optional field is set tells you the state" as an implicit
state machine. It is not the only one. The identical pattern recurs **a second time**, in
reverse, at `CampaignExercises.cs:7583-7600+` — a predecessor-walking cascade that pattern-
matches the same ~18 `Current*`/`*Predecessor` pairs in the same hand-maintained order to
walk a session backward to its prior state for replay verification:

```csharp
var (isNativeSuccessor, predecessor) = cursor switch
{
    { CurrentStageEntryMovementCompletion: not null } => (true, cursor.StageEntryMovementCompletionPredecessor),
    { CurrentStageEntryMove: not null } => (true, cursor.StageEntryMovePredecessor),
    { CurrentStageEntryReserve: not null } => (true, cursor.StageEntryReservePredecessor),
    { CurrentSecondBreakdown: not null } => (true, cursor.SecondBreakdownPredecessor),
    // ... 12 more arms, same shape
```

The comment directly above this switch (`CampaignExercises.cs:7584-7588`) says "Checked
most-recent-first, same ordering discipline as the #394 second-side arms below" — the
*third* place in this codebase where a comment, not a type, is what keeps a hand-ordered
cascade correct. This is the concrete evidence for why a structural fix (Section 0.2),
not just a lookup-table swap at one call site, is the right scope for "orchestrate events
properly": the anti-pattern is replicated at the data-model level (`ExerciseSession`), not
just at its two call sites.

### 0.2 Target: `IPendingDecision` as a discriminated union, one property, not 18 (or 62)

Replace the `Current*`/`*Predecessor` property pairs for "whichever decision point is
currently open" with a single sealed, pattern-matchable union:

```csharp
internal abstract class PendingDecision
{
    internal abstract ExerciseSession? Predecessor { get; }
}

internal sealed class StageEntryReservePending(
    CampaignNativeSecondReserveV43 Reserve,
    CampaignWorldSnapshotV6 World,
    ExerciseSession predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}
// one sealed type per mechanic, replacing one Current*/World/*Predecessor triple each
```

`ExerciseSession` gets **one** property, `internal PendingDecision? Pending { get; }`.
`QueryCheckpoint` becomes a single `switch (session.Pending)` pattern-match (or a registry
lookup keyed by `Pending.GetType()`, see 0.3) instead of a 25-branch ordered null-check
cascade — and the backward predecessor-walk at `CampaignExercises.cs:7583+` becomes
`cursor.Pending?.Predecessor`, collapsing 18 hand-written `switch` arms into one property
read. **The ordering-discipline bug class becomes structurally impossible on both sides**:
there is no order to get wrong when there's only one field to check.

This directly targets what the owner asked for ("how a proper engine handles events"):
this is exactly how ECS engines represent "entity's current state" (one component slot,
tagged by type, not N optional component fields) and how typed event-sourcing frameworks
represent "aggregate's pending command" (a discriminated command/decision type, not parallel
nullable fields on the aggregate).

**Scope discipline — what this does *not* touch.** The *original* pattern's 30-parameter
constructor and its dense cross-field invariant graph (0.1, first bullet) is a different,
harder problem: those checks encode real domain rules (e.g. "a Force completion requires
both retained owner seals"), not just "which field is set." Collapsing that into
`PendingDecision` subtypes is possible in principle (each subtype's constructor could run
its own slice of those invariants) but is a much larger, riskier rewrite than the newer
mechanics, which have almost no invariant logic to preserve. **Recommendation: scope
`PendingDecision` to the newer (`#394`-onward) mechanics first** — that's where the pain
actually is (every MTS slice this session has extended this family, never the original
Combat chain) — and leave the original constructor's validated path alone until there's
a concrete reason to touch it.

### 0.3 Self-registering handler-per-mechanic: the actual extension point

Combine 0.2 with Section 1's registry idea, but aim it at the real unit of repetition.
Every MTS slice this session (`#519`, `#520`) has added the **identical triple**: an
`ExerciseSession` field + predecessor property + `WithXxx` method; a `CampaignExercises`
dispatch block (Query/Submit/checkpoint-contribution/reconstruct); a timing-manifest row.
Two of those three are mechanical consequences of the first — once `Pending` exists (0.2),
and once there's a handler object per mechanic, both the forward dispatch and the
backward predecessor-walk read it, and no per-mechanic code needs to be added to either
`CampaignExercises.QueryCheckpoint` or the `#7583` cascade ever again:

```csharp
internal interface IStageDecisionHandler<TPending> where TPending : PendingDecision
{
    OwnerDraftResult QueryOpening(TPending pending, CampaignActionAudience audience);
    ExerciseStepResult Submit(TPending pending, ExerciseSession session,
        CampaignActionAudience audience, string actionId);
    CheckpointContribution Checkpoint(TPending pending);
}
```

A registry (`FrozenDictionary<Type, object>`, resolved by `Pending.GetType()`, cast once
per handler via a small generic dispatch helper — a known, common pattern for
type-keyed polymorphic registries in C#) replaces the manual `QueryCheckpoint`
cascade entirely. **This is the real future-proofing target the owner is asking for**:
adding a 7th MTS slice, or a Stage 3 mechanic, becomes "write one
`PendingDecision` subtype + one handler class implementing this interface + register it,"
not "find the right insertion point across three files by hand," which is the literal,
confirmed cost of every slice landed so far.

### 0.4 Command/Event/Projection separation (CQRS framing)

Today, `CampaignExercises` is one ~9,657-line static class doing all three of: **answer
"what's the next legal decision"** (the Query/`QueryCheckpoint` side), **validate and
apply a submitted decision, producing an event** (the Submit side), and **project history
into the current session shape** (the Reconstruct side) — for every mechanic, in the same
file, with no formal boundary between them. Event-sourcing frameworks (MartenDB's event
projections run as a formally separate pipeline stage from command handling; EventStore's
typed subscriptions are registered independently per event type) keep these three
concerns as separate interfaces/pipelines precisely because they have different
contracts and different failure modes: a Query is read-only and side-effect-free, a
Submit is the only place that produces a new canonical event, and a Reconstruct runs
during replay and must be byte-for-byte deterministic.

**Concrete recommendation:** split `IStageDecisionHandler<TPending>` (0.3) into three
narrower interfaces — `IOpeningQuery<TPending>`, `IDecisionSubmitter<TPending>`,
`ICheckpointProjector<TPending>` — implemented together by one handler class per
mechanic (so there's still exactly one file per mechanic to write), but callable
independently. This buys two concrete things this codebase doesn't have today: (a) a
Submit-path unit test can exercise `IDecisionSubmitter` directly without needing a real
`ExerciseSession` wired up for the Query side too; (b) it makes the replay/determinism
boundary (`ICheckpointProjector`, which must never branch on wall-clock time or anything
`DET-001`/`DET-002` forbid) a *type signature*, not a convention someone has to remember
while editing a 9,657-line file that also contains non-replay code.

### 0.5 Host/API layer: the same disease, one layer up

`CampaignDurableAuthorityJournal`'s `SubmitOwnerActionCore`/`QueryOwnerOpeningCore` (Section
1) and its ~30 `AdmitSourceXxx` triads (Section 4) are this exact anti-pattern reproduced
over HTTP, one layer above `CampaignExercises`. The fix is the same shape, scaled up:
`IOwnerOpeningHandler`/`ISourceAdmissionHandler` (Sections 1 and 4's proposed interfaces)
should be thought of as the Host-layer *instances* of `IStageDecisionHandler` — ideally the
Host handler for a mechanic directly wraps/depends on that mechanic's Core handler rather
than independently re-implementing "look up by kind, call the right thing." Once both
layers dispatch through a registry keyed by the same kind/type, adding a new
HTTP-exposed decision becomes: write one Core handler (0.3), write one thin Host handler
that adapts it to the HTTP request/response shape, register both. No switch statement at
either layer grows.

### 0.6 Migration path — what's safe to change and what must never move

This is a deterministic-replay, event-sourced system, so the honest safety line matters
more than the elegance of the target shape:

- **Must never change:** event wire formats/types (`EVT-001`) — every `CampaignNative*`
  record's shape and serialization is permanent once shipped. Nothing in 0.1-0.5 touches
  this; `PendingDecision`/the handler registry are purely **in-process, in-memory**
  constructs that exist between events, never serialized, never replayed. A historical
  event replays into whatever shape `ExerciseSession`/`PendingDecision` happens to have
  *today* — the replay-correctness tests (which replay real history and check canonical
  hashes) are the proof this holds, and they don't change meaning or go away.
- **Safe to redesign freely:** `ExerciseSession`'s in-memory field shape, `CampaignExercises`'
  dispatch structure, the Host's switch statements. All of this is pure orchestration —
  code that decides *which* validated, already-correct domain logic to call, not the
  domain logic or the wire format itself.
- **Concrete incremental sequence**, each step shippable and independently safe:
  1. Introduce `PendingDecision` as a **computed, derived property** alongside the
     existing 18 `Current*`/`*Predecessor` fields first — `Pending` is computed from
     whichever field is non-null (literally the current cascade, moved into one place and
     run once instead of twice). Zero behavior change, zero risk, and it immediately lets
     `CampaignExercises.cs:7583+`'s predecessor-walk collapse to `cursor.Pending?.Predecessor`
     without touching `QueryCheckpoint` yet.
  2. Migrate `QueryCheckpoint`'s cascade to dispatch off `session.Pending`'s runtime type
     instead of the 25-branch null-check order. Still computed from the same underlying
     fields — this step is "stop reading the fields directly, read the derived value,"
     not "change what the fields mean."
  3. Build the handler registry (0.3) for the mechanics added since `#394`, since those
     are both the newest (lowest-risk-to-touch-again, nobody else has built on their exact
     shape yet) and the ones actually growing with every MTS slice.
  4. **New mechanics going forward** (the still-upcoming Breakdown/Combat slices for this
     stage's first side, then the whole thing again for the second acting side, then
     Stage 3) implement `PendingDecision` + the handler interfaces **directly**, skipping
     steps 1-2's "derive it from legacy fields" shim entirely — they have no legacy field
     to derive from, so they're free.
  5. The original (`pre-#394`) mechanics (Combat/Reserve-Release/Cycle-Control/Repeat,
     Section 0.1's "original pattern") migrate opportunistically, if ever — they're
     working, heavily validated, and not the thing growing every sprint. Don't schedule
     this; revisit only if a future change needs to touch that invariant graph anyway.

No step requires a big-bang rewrite, a replay-format change, or pausing feature work.
Steps 1-2 are pure refactor (provably behavior-preserving, verified by existing tests).
Step 3 is additive for new code and non-destructive for old. Step 4 just means new slices
are written in the target shape from here on, which is strictly less work per slice than
the current triple-touch pattern, not more.

## 1. Dispatch-on-kind-enum god files

### The current shape, confirmed against live code

`CampaignOwnerOpeningKind` (`src/Cna.OrleansHost/Api/CampaignDurableAuthorityJournal.Openings.cs:767-830`)
is a flat 60+-member enum. `QueryOwnerOpeningCore`
(`CampaignDurableAuthorityJournal.Openings.cs:223-719`, ~500 lines) and
`SubmitOwnerActionCore` (`CampaignDurableAuthorityJournal.Submissions.cs:237-1035`, ~800
lines) are each one giant `if (kind == X) {...} else if (kind == Y) {...}` chain — 40 and
46 branches respectively, confirmed by direct count. A representative branch
(`Openings.cs:245-251`):

```csharp
if (kind == CampaignOwnerOpeningKind.PregameAxisSupply)
{
    var draft = CampaignHostAdapter.QueryPregameAxisSupplyOwnerDraft(authority.Session, seat.Audience);
    visible = draft.VisibleBytes;
    authorityVersion = draft.AuthorityVersion;
    actionIds = draft.PrivateActionIds.ToArray();
}
else if (kind == CampaignOwnerOpeningKind.PregameAxisSupplyFinish)
{ /* identical shape, different method name */ }
```

Every branch is mechanically identical: call one `CampaignHostAdapter.QueryXxxDraft`
method, destructure the same three fields. The only thing that varies is *which* method
to call — which is exactly the shape a lookup table exists to solve.

`CampaignExercises.QueryCheckpoint` (`src/Cna.Core/Exercises/CampaignExercises.cs:4044-4200+`)
has the same disease in a worse form: it's not keyed by an enum at all, it's an **ordered
cascade of nullable-field checks** on `ExerciseSession`, where position in the campaign's
timeline is inferred from which of ~25 optional fields happens to be non-null, and the
order of the checks is itself load-bearing:

```csharp
if (session.CurrentSecondAssault is { Events.Count: > 0 } secondAssault) ...
if (session.CurrentSecondNoAttackCompletion is { } secondNoAttack) ...
// ...
if (session.CurrentStageEntryMovementCompletion is { } stageEntryMovementCompleted) ...
if (session.CurrentStageEntryMove is { } stageEntryMove) ...
if (session.CurrentStageEntryReserve is { } stageEntryReserve) ...
if (session.CurrentPostCombatPass is { } phasePassed) ...
```

The comments at `CampaignExercises.cs:4050-4056` and `:4071-4084` **explicitly document
why the ordering matters** ("chronologically later... must be checked first") — this is
tribal knowledge encoded as prose next to code whose correctness depends on prose being
read and respected. Every one of the six MTS slices landed so far has had to find the
right insertion point in this cascade by hand. This is the single most expensive
recurring tax in the whole codebase: it's not just long, it's an **implicit state
machine with no explicit states**.

### What a proper engine does here

Event-sourcing frameworks (Marten's `Inline`/`Async` projections, EventStore's typed
subscriptions) and ECS game engines solve "dispatch the right handler for this
event/component type" the same way: **a registration table keyed by type/kind, built
once at startup, with each handler self-contained and independently testable.** The
kind/kind-enum stays — it's a fine wire format — but it becomes a dictionary key instead
of a chain of `==` comparisons.

For `CampaignExercises.QueryCheckpoint`'s specific disease (ordering inferred from field
nullability), the fix is different and more fundamental: **the campaign's position in its
own timeline should be an explicit, named value** — it already almost is, via
`LandSequencePosition` / `CampaignOperationStageOrder`. `ExerciseSession` should expose
one authoritative "what phase am I in" value (even if it's just "the last-populated field,
tagged"), and `QueryCheckpoint` should look it up, not re-derive it by testing 25 fields
in a specific order every single call.

### Recommended shape for this codebase

**Registry/handler-object pattern for the Host layer's `kind`-dispatch** (addresses
`QueryOwnerOpeningCore`/`SubmitOwnerActionCore` directly, and doubles as the foundation
for Section 4):

```csharp
internal interface IOwnerOpeningHandler
{
    CampaignOwnerOpeningKind Kind { get; }
    OwnerDraftResult Query(ExerciseSession session, CampaignActionAudience audience);
}

internal sealed class PregameAxisSupplyOpeningHandler : IOwnerOpeningHandler
{
    public CampaignOwnerOpeningKind Kind => CampaignOwnerOpeningKind.PregameAxisSupply;
    public OwnerDraftResult Query(ExerciseSession session, CampaignActionAudience audience) =>
        CampaignHostAdapter.QueryPregameAxisSupplyOwnerDraft(session, audience);
}
```

with a registry built once (`FrozenDictionary<CampaignOwnerOpeningKind, IOwnerOpeningHandler>`
— .NET 8+'s `FrozenDictionary` is the right container here: built once, read on every
request, no mutation). `QueryOwnerOpeningCore` shrinks to:

```csharp
if (!_openingHandlers.TryGetValue(kind, out var handler))
    throw new InvalidOperationException($"No opening handler registered for {kind}.");
var draft = handler.Query(authority.Session, seat.Audience);
```

**Why a handler-object registry over a plain `Dictionary<Kind, Func<...>>`:** a delegate
dictionary would shrink the code just as much, but loses two things this codebase
actually needs: (a) a place to put the "what else do I need, like `ForceAssignment`'s
`authority.Timing is null` guard at `Openings.cs:238-239`" per-kind special case without
reintroducing an `if` chain around the registry lookup — give the interface an optional
`Validate(authority)` hook instead; (b) a natural unit-test seam — one handler class is
one thing to unit-test in isolation, instead of needing to drive the entire 500-line
method to reach branch #31.

**Source generator, considered and not recommended (yet).** C# source generators could
emit the registry automatically from an attribute on each handler class
(`[OwnerOpening(CampaignOwnerOpeningKind.PregameAxisSupply)]`), eliminating even the
registration boilerplate. This is the "proper" compiler-driven answer and worth doing
*eventually* — but it's new build-pipeline machinery this team hasn't used before, and
the manual registry (one `AddHandlers()` method, ~60 lines, that must list every handler)
is already self-checking: a missing registration throws immediately on the first request
for that kind, loudly, in every test that exercises it. Recommend starting with the plain
registry; revisit the source generator only if the manual registration list itself starts
causing missed-registration bugs in practice.

**`QueryCheckpoint`'s cascade — different fix, not a registry.** Introduce an explicit
`ExercisePhase` marker (an enum or small tagged union) that gets set exactly once, at the
same place each `WithXxx` method already sets its own field. `QueryCheckpoint` becomes
`_checkpointBuilders[session.Phase](session)` — a lookup, not a 25-branch ordered cascade
— and **the ordering bug class (forgetting to insert a new check in the chronologically
correct position) becomes structurally impossible**, because phase transitions are
assigned explicitly by the code that causes them, not inferred after the fact by testing
fields in a guessed order.

## 2. The recurring hardcoded-assumption bug class

Found and fixed 7+ times this session (`#518`'s seven classes), one more found-and-refuted
today (`CampaignNativeReserveReleaseV27.cs:168`'s `.Single()` — confirmed safe under the
*current* architecture, but only because nothing currently appends a second
`CampaignOperationStageOrder`; it's a landmine for the next person who doesn't know that).
The shape is always one of: hardcoded `SecondActingSide` instead of a derived role,
literal `OperationStage==1`/`GameTurn==1` comparisons, or a bare `.Single()`/`.First()` on
a collection whose cardinality is an *assumption*, not a type-level guarantee.

### Recommendation: a Roslyn analyzer, not an architecture-test library

Considered three options:

- **NetArchTest / ArchUnitNET-style architecture tests** — good for *structural*
  assertions ("namespace X must not depend on namespace Y") but these are *expression-shape*
  rules ("this specific LINQ call, on this specific type, outside this specific file").
  Architecture-test libraries work on assembly/type metadata, not syntax trees, so they
  can't see "a bare `.Single()` call with no predicate" at all. Wrong tool for this job.
- **Regex-based CI lint step** (grep for `OperationStageOrders.Single()` /
  `OperationStage == 1` in CI, fail if found outside an allowlist) — cheap to write today,
  and genuinely fine as a stopgap, but it has no understanding of context: it can't tell
  `OperationStageOrders.Single()` (dangerous) from `SomeUnrelatedList.Single()` (fine)
  without increasingly fragile pattern-matching, and false positives erode trust in the
  check fast.
- **A small custom Roslyn analyzer** — this is a C#/.NET codebase already building
  against modern SDKs (`net10.0`, confirmed from CI logs); a `DiagnosticAnalyzer` project
  is a normal, well-documented thing to add (`Microsoft.CodeAnalysis.CSharp.Workspaces`,
  referenced as an analyzer package from `Directory.Build.props` so it runs for every
  project without per-csproj wiring). It operates on the actual syntax/semantic model, so
  it can precisely target "a call to `.Single()`/`.First()`/`.SingleOrDefault()` with no
  predicate, on an expression whose static type is
  `IReadOnlyList<CampaignOperationStageOrder>` (or similarly-shaped known-dangerous
  types), outside a file on an explicit allowlist (`FirstActingSideResolver.cs`,
  `CampaignSnapshotV11Validator.cs`, and nowhere else)" — and the same analyzer can carry
  a second rule for literal `OperationStage == 1` / `GameTurn == 1` comparisons outside
  the same allowlist.

**Recommendation: build the Roslyn analyzer.** It's the only option precise enough to
avoid both false negatives (regex missing a reformatted call) and false positives (regex
flagging an unrelated `.Single()`), it runs at build time (not a separate CI step that can
be skipped), and it directly encodes the exact lesson learned 7+ times this session: "this
specific shape, on this specific type, is dangerous outside these specific files." Size it
small — two diagnostic rules to start (bare-`.Single()`-on-stage-orders,
literal-stage/turn-comparison), `severity=error` in `.editorconfig`, fails the build.
Expand the rule set only as new instances of the same bug class get found, rather than
trying to anticipate every variant up front.

## 3. Duplicated codec schema literals

~20 `CampaignCombatActual*Codec.cs` files each define their own ~150-400 character inline
schema string for the same recurring shapes (`Random`, `World`, `Control`,
`AssaultResult`) independently. A missed sync (editing one copy of the `World` shape
without finding all 20) fails **silently at runtime** — wrong canonical bytes, not a
compile error — which is the worst possible failure mode for a codebase whose entire
correctness model (`DET-001`/`DET-002`, replay-exact canonical hashing) depends on byte-
for-byte schema agreement.

**Recommendation: shared fragment constants, composed, not a new DSL.** Don't build a
schema-composition mini-language — that's new machinery to maintain for marginal benefit
over the simplest fix: extract each recurring shape (`Random`, `World`, `Control`,
`AssaultResult`, etc.) into one `internal const string` per concept in a single file
(e.g. `CampaignCodecSchemaFragments.cs`), and have every codec's schema string become
**string interpolation/concatenation of those fragments** instead of a hand-retyped
literal:

```csharp
internal static class CampaignCodecSchemaFragments
{
    internal const string Random = "randomState:Random";
    internal const string World = "world:World";
    // ...
}
```

```csharp
// before: "contractVersion:int ... world:World randomState:Random attackHistory:..."
// after:
private const string Schema = $"contractVersion:int ... {CampaignCodecSchemaFragments.World} " +
    $"{CampaignCodecSchemaFragments.Random} attackHistory:AttackHistory[] ...";
```

This is a pure mechanical extraction (no behavior change — the resulting literal strings
must be byte-identical, verified by the existing canonical-hash tests, which already exist
and already catch exactly this kind of drift). **Enforcement:** no new architecture-test
needed here — the existing replay/canonical-hash test suite already fails loudly if a
codec's assembled schema string doesn't match what was previously canonical. The win is
entirely in *authoring*: the next new codec composes from the same fragments instead of
retyping a 300-character literal by hand, so drift becomes structurally harder to
introduce, not just easier to catch after the fact.

> **Correction (2026-11-23, after attempting this):** this section's premise — "pure
> mechanical extraction, low risk, start now" — is **wrong**, found by actually attempting
> the refactor. The static check (every fragment resolved back to its literal and diffed
> against the original: 1,664 dict entries, 0 mismatches, 507 replacements across 19
> files) passed clean, but the real `dotnet test` run then failed 996/1,595 tests with
> `JsonException: CMB-MRC-009`. Root cause, confirmed by bisecting against the original
> file: this codebase pins the **SHA256 hash of the actual `.cs` source files themselves**
> inside each codec's `Pins`/`Delivery` dict (`CombatEvidenceRevisions.cs`), as a
> deliberate cross-file provenance/integrity gate, independent of whether the assembled
> schema string is byte-identical. Editing *any* file that another file pins by hash
> breaks that other file's dependency check — 5 of the 19 files this section's example
> touches are pinned by other codecs this way. `CombatEvidenceRevisions.cs`'s own
> comments are explicit: *"registration is code-owned, never supplied by a proposal"* and
> *"Refreshed entries are frozen only after provenance regeneration and semantic
> comparison."* The one prior real instance of refreshing these pins
> (`docs/work/reviews/2026-10-25-evidence-revisions-author.md`) was a multi-day,
> human-reviewed effort with dedicated archival tooling — not something to push through
> unilaterally, agent or human.
>
> **Revised status: moved to "needs owner decision," not "start now"** (see Section 6).
> The attempt was fully reverted — no commits, no PR, working tree clean. A narrower
> version (scoping to only the files that aren't referenced as a pin target elsewhere)
> would dodge the gate but delivers a smaller, asymmetric win and leaves the
> most-duplicated, most-cross-referenced files untouched — whether that's worth doing on
> its own is the owner's call, not a thing to decide by default.

## 4. `CampaignDurableAuthorityJournal` god object / ~30 near-duplicate admission triads

14 partial files, ~9,000 lines combined. The ~30 `AdmitSourceXxx`/`QuerySourceXxxOpening`/
`SubmitSourceXxx` triads (e.g. `CampaignDurableAuthorityJournal.cs:866-1263`) are each the
same 10-line shape (`RequireOperator` → `Load` → already-applied check → `Accept` →
`PersistAccepted`), differing only in which Core method/kind constant they call — this is
the exact same disease as Section 1, just at the Host/pregame-admission layer instead of
the owner-opening layer, and it's *already* mid-evolution toward the right shape: the
existing `CampaignOwnerOpeningKind` enum is the right idea, just implemented as a switch
instead of a registry (Section 1's fix applies here directly, same registry pattern,
different handler interface):

```csharp
internal interface ISourceAdmissionHandler
{
    CampaignOwnerOpeningKind Kind { get; }
    bool AlreadyApplied(ExerciseSession session);
    ExerciseStepResult Accept(ExerciseSession session, CampaignActionAudience audience, string actionId);
}
```

Each of the ~30 existing `AdmitSourceXxx` methods becomes one handler class implementing
this interface; `AdmitSourceXxx`'s current body (already mostly boilerplate) collapses
into one shared `AdmitCore(kind, ...)` that looks the handler up and calls it. **This is
the highest-value single refactor in this whole document by volume** — ~30 near-identical
10-line blocks collapsing into ~30 small handler classes plus one shared driver is a
larger line-count win than anything else here, and it's the lowest-risk one: each handler
is a mechanical extraction of an existing, already-tested code path, not new logic.

Same recommendation as Section 1: plain registry (`FrozenDictionary`), not a source
generator, for the same reasons (self-checking failure mode, no new build tooling to
learn, revisit later if manual registration itself becomes error-prone).

## 5. Architectural fitness functions — the actual future-proofing mechanism

Sections 1-4 fix specific instances. This section is the one that keeps them fixed. The
project's hard invariants (`DET-001`, `DET-002`, `EVT-001`, `REL-001`, `FOW-001`,
`FID-002`, `UX-001`) currently live only as prose — in `CLAUDE.md`, in design docs, in
this session's own memory. They are enforced by whoever happens to review the next PR
remembering them. That does not scale, and it is **the single reason the hardcoded-role
bug class recurred 7+ times** — nothing structural was stopping it.

Recommend a small, purpose-built architecture-test project
(`tests/Cna.ArchitectureTests`), using **NetArchTest.Rules** (lightweight, actively
maintained, designed for exactly "assert dependency/reference shape as a unit test" —
simpler and lower-footprint than ArchUnitNET for a codebase this size) for the rules that
are genuinely about *dependency shape*, plus the Roslyn analyzer from Section 2 for rules
that are about *expression shape*. Concrete starting rules, each one directly protecting
a ruling already made this session:

- **Protect the "don't widen `CampaignSnapshotV11`" ruling** (the Option 2/hybrid
  decision from the snapshot-representation fork): an architecture test asserting that
  `CampaignSnapshotV11` and `CampaignSnapshotV11Validator`'s current caller set doesn't
  grow — either a literal "known callers" allowlist that must be edited (and thus
  reviewed) to add a new one, or, more simply, that no file outside
  `src/Cna.Core/Campaigns/CampaignSnapshotV11*.cs` constructs a new
  `CampaignSnapshotV11` instance. This turns an architectural decision that currently
  lives only in a design doc and this session's memory into something CI enforces for
  every future PR, by any future contributor or agent, regardless of whether they've read
  that doc.
- **Protect `DET-001`/`DET-002` directly**: an architecture test (or the same Roslyn
  analyzer) asserting zero references to `DateTime.Now`, `DateTime.UtcNow`,
  `Random.Shared`, unseeded `new Random()`, or `Guid.NewGuid()` anywhere under
  `src/Cna.Core`, outside an explicit allowlist. Today's audit found zero violations — this
  rule's entire value is making sure that stays true as the codebase grows, not fixing
  anything that exists now.
- **Protect the Section 2 bug class structurally** — this *is* the Roslyn analyzer from
  Section 2; listing it here too because it's the same future-proofing goal wearing a
  different tool.
- **One rule per `UX-001`/`FOW-001` would be valuable but is genuinely harder to express
  mechanically** (both are about *information content*, not *dependency shape* or
  *expression shape* — no tool reads "did we expose the enemy's hidden state" from syntax
  alone). Don't force these into architecture tests; they stay owned by the existing
  discipline of deliberate audits (like today's) and targeted tests (like the #515 fix's
  own test class). Flagging this honestly rather than overclaiming architecture tests can
  cover everything.

This is the highest-leverage item in the whole document precisely because it's the
cheapest to build (a new test project plus ~5 rules, additive, touches zero existing
logic) and the only one that keeps paying off on every future PR without anyone having to
remember to apply it.

## 6. Sequencing — what starts now, what waits

**Can start now, in parallel, zero collision risk with the in-flight MTS slices:**
building `tests/Cna.ArchitectureTests` (Section 5) and the Roslyn analyzer project
(Section 2). Both are pure additions — a new test project and a new analyzer project
reference nothing in `CampaignExercises.cs`/`CampaignDurableAuthorityJournal`/
`ExerciseSession` that the MTS branches are touching, so there's no merge-conflict
surface at all. This is also the highest-value-per-hour item (Section 5's own argument),
so it's the right thing to front-load.

**Needs an owner decision, not "start now":** Section 3's codec-schema-fragment
extraction, downgraded after actually attempting it (see the correction in Section 3).
The static byte-identical check passed clean, but the real test run failed 996/1,595
tests — several of the touched codec files are pinned by SHA256 source-file hash in
`CombatEvidenceRevisions.cs`'s cross-file provenance gate, which this refactor trips
regardless of whether the assembled schema string changes. The prior real refresh of
those pins was a multi-day, human-reviewed effort with dedicated archival tooling, so
this isn't a "quiet moment" task — it's blocked on the owner deciding whether to scope it
down to the non-pinned files (smaller, asymmetric win) or treat it as its own
provenance-regeneration effort later.

**Should wait until the current MTS slices (005/006, then Stage 3) land:** Sections 1 and
4 — the `QueryOwnerOpeningCore`/`SubmitOwnerActionCore`/`QueryCheckpoint` registry
refactors. This is exactly where the in-flight feature work is also landing (every new
MTS slice adds a branch to `QueryCheckpoint`'s cascade and a case to the Submission/Query
dispatch chains) — refactoring the dispatch shape underneath a branch that's actively
growing invites exactly the kind of merge pain this whole effort is trying to eliminate.
Once Stage 2 Movement/Breakdown/Combat and Stage 3 are in and stable, the growth stops for
a while (Turns 2-6 should mostly reuse the same phases rather than add new ones), which is
the right moment to collapse the by-then-fully-grown cascade into a registry once,
instead of mid-growth.

**Rough ordering/sizing**, once the 6-turn push reaches a stable point:

1. (parallel, now) `tests/Cna.ArchitectureTests` + Roslyn analyzer scaffolding — small,
   additive, 1-2 PRs.
2. (owner decision needed, not scheduled) codec schema-fragment extraction — blocked on
   the `CombatEvidenceRevisions.cs` provenance gate; see the Section 3 correction and
   Section 6's note above before resuming this one.
3. (after MTS-TASK-005/006 + Stage 3 land) `CampaignDurableAuthorityJournal`'s ~30
   admission triads → registry (Section 4) — biggest line-count win, lowest behavioral
   risk, do it first of the two big ones.
4. (same window) `QueryOwnerOpeningCore`/`SubmitOwnerActionCore` → registry (Section 1,
   Host layer half).
5. (same window or just after) `CampaignExercises.QueryCheckpoint`'s cascade → explicit
   phase marker + lookup (Section 1, Core layer half) — do this last of the structural
   refactors since it's the one touching the most call sites and benefits most from the
   dispatch-table pattern already being proven elsewhere in the codebase by steps 3-4.

Nothing in this plan requires stopping or slowing the 6-turn expansion. The parallel-track
items (1-2) are pure net-new protection; the blocked items (3-5) are explicitly sequenced
to land in the lull after Stage 2/3, not fought for against actively-growing files today.
