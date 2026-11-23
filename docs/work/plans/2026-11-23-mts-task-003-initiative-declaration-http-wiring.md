# MTS-TASK-003: Initiative Declaration's real HTTP wiring (2026-11-23)

**Status:** a real, test-verified slice. Every MTS-TASK-005/006 slice this
session deliberately deferred Host/HTTP wiring ("Implemented, not yet
Activated-and-witnessed"). This is the first attempt at that wiring, scoped
to the one mechanic that turned out to need it most: Stage 2's own Initiative
Declaration, the single genuine player decision among the six-event preamble
chain built in #518.

## The auto-drain question, resolved with evidence, not guessed

The preamble chain's six native events (`CampaignNativeInitiativeDeclarationV49`
through `CampaignNativeNoObligationFleetRepairV54`) had an explicitly open
question: should each one auto-drain like a `CampaignActionAudience.System`
step (#508), or require an explicit owner-opening?

Checked directly against this project's own existing classification of the
Stage 1 original mechanics (`CampaignAudienceOutcomeSource.cs:192-199`):

```csharp
(NoObligationNavalConvoyScheduleResolved, "resolve-no-obligation-naval-convoy-schedule") when System(actor) => (null, null),
(InitiativeOrderDeclared e, "act-first") when Human(actor, e.DeclaringHolder) => ...,
(InitiativeOrderDeclared e, "act-last") when Human(actor, e.DeclaringHolder) => ...,
(WeatherDetermined, "resolve-weather") when System(actor) => (null, null),
(NoObligationOrganizationResolved, "resolve-no-obligation-organization") when System(actor) => (null, null),
```

**Resolved: Initiative Declaration is a genuine player decision (`Human`); Weather,
Organization, Naval Convoy Arrival, Fleet Assignment, and Fleet Repair are all
`System`-audience, automatic, no real choice.** The new V49-V54 classes mirror
these exactly by name and shape. Not a design fork — the answer was already
encoded in this codebase, just not yet applied to the Stage 2 chain.

**Scope of this slice:** only Initiative Declaration's own owner-opening.
Wiring the five automatic mechanics to actually auto-drain (via `CampaignExercises.Query`'s
`System`-audience path, which today has zero knowledge of any native-chain
field — confirmed by reading it directly) is real, separate work, not
attempted here — tracked as this slice's own follow-up.

## What was built

- `CampaignExercises.QueryStageEntryInitiativeDeclarationSource` /
  `SubmitStageEntryInitiativeDeclaration` (Core) — mirrors
  `QueryStageEntryReserveSource`/`SubmitStageEntryReserve`'s exact shape.
- Six new `ExerciseCheckpoint` constructors (one per preamble event) and six
  new `QueryCheckpoint` branches, inserted in the correct chronological
  position — a real gap flagged but not fixed by the original Reserve slice's
  own doc ("the six preamble events... have no ExerciseCheckpoint branch here
  at all"). Fixing it here closes a genuine silent-wrong-fallthrough risk:
  without it, `QueryCheckpoint` would have fallen through to a stale
  first-side checkpoint instead of the true current position.
- `CampaignOwnerOpeningKind.StageEntryInitiativeDeclaration` + the full Host
  chain: `CampaignHostAdapter`'s draft-builder/submit methods,
  `CampaignDurableAuthorityJournal`'s Query/Submit wrappers + registry entry
  (#525's pattern) + dispatch-switch entry, `CampaignDurableAuthorityJournalStore`'s
  sync/async wrappers, `CampaignGrain`'s two dispatch switches, and the
  outward API's `stageKnown` allowlist.
- A real end-to-end HTTP test: `TwoRealHttpPlayersConductAnEntireOperationStageOne`
  extended with the real submission, proving a live HTTP player can declare
  Initiative for Stage 2 with zero backdoors, and that re-querying the same
  opening afterward correctly fails (the session genuinely advanced).

## Real bugs found and fixed along the way

Each of these was discovered by actually driving the mechanic end-to-end over
real HTTP, not by inspection — the same discipline this project has used
all session, and the same reason it kept finding things:

1. **`CampaignActionCandidate`'s base constructor hardcoded `operationStage`
   to exactly `1`** (`throw` for anything else) — a Stage-1-only assumption
   baked in when nothing past Stage 1 existed. Never reachable until this
   slice became the first caller to construct a candidate for a real Stage 2
   position. Widened to this codebase's real range (1-3).
2. **A second, separately-named `AcceptedKind` method**
   (`CampaignDurableAuthorityJournal.Submissions.cs`, OrleansHost layer) had
   no entry for the new kind and threw `ArgumentOutOfRangeException` — easy
   to miss because an *unrelated* `CampaignHostAdapter.cs` (Core layer) method
   with the identical name was fixed first, creating false confidence.
3. **The real live dispatch switch in `SubmitOwnerActionCore`** (the one that
   actually calls `CampaignHostAdapter.SubmitXxx`) never got the new kind's
   entry at all — an earlier edit landed in a different, similarly-shaped
   switch (the import/replay dispatcher in `CampaignHostAdapter.cs`, keyed by
   string constants rather than the enum). Submissions silently fell through
   to the generic Movement handler.
4. **`actionId` misuse**: every Submit method's `actionId` parameter is the
   real sha256 content-hash core action id (used only for the acceptance
   receipt's audit trail), confirmed against `SubmitStageEntryReserve`'s
   identical shape. This slice's first draft passed a semantic
   `"act-first"`/`"act-last"` label instead, which failed
   `CampaignActionAcceptanceReceipt`'s own hash-format validation. Fixed by
   threading the real action id through and passing the decision as its own
   explicit `bool actFirst` parameter.
5. **A real, genuinely dangerous ordering bug in `CampaignExercises.Reconstruct`'s
   own forward-dispatch cascade** — see below, not fully fixed.

## Known gap, explicitly not fixed here: `Reconstruct`'s cascade ordering

`Reconstruct` (the ~1,700-line cascade that replays history into a session
shape) checks `completedSession.CurrentSecondAssault is { Events.Count: > 0 }`
unconditionally — and like every other field in this session class, that
condition stays true forever once set. This silently intercepted the
chronologically-later `CurrentInitiativeDeclaration` check below it, re-verifying
against the stale second-side-assault expectation instead of ever reaching
the new preamble block — the exact same disease `QueryCheckpoint` already had
(and was fixed for, in the Reserve slice), just in a second, independent
cascade nobody had audited for it.

**Fixed:** this one site, guarded on `CurrentInitiativeDeclaration is null`,
mirroring `QueryCheckpoint`'s own most-recent-first discipline.

**Not fixed, not safely scoped into this slice:** whether other blocks across
this same ~1,700-line cascade have the identical latent issue. Given how many
"never reset" fields `ExerciseSession` carries (92 members, confirmed earlier
this session), and that this exact bug class has now been found three times
independently (`QueryCheckpoint`, this `Reconstruct` site, and the original
`WithInitiativeDeclaration` guard), a full audit of `Reconstruct`'s cascade is
warranted — but is its own task, not a five-minute follow-on. The real HTTP
test in this slice proves the **live** Submit/Query path end-to-end, which is
unaffected by this gap; it does not re-verify via cold replay after the new
step, documented honestly in the test's own comment rather than silently
weakened or hidden.

**Recommendation:** before Reserve/Movement/Breakdown/Combat's own Host
wiring is attempted (this slice's own deferred follow-up), audit
`Reconstruct`'s full cascade for the same "never-reset field matched without
a later-check guard" shape, the same way `QueryCheckpoint`'s cascade was
audited and fixed. Every one of those mechanics will land its own new
`CurrentStageEntryXxx` field into a session that still carries `CurrentSecondAssault`
non-null too, so this is not a one-off risk specific to Initiative Declaration.

## Test results

- `dotnet build Sandtable.slnx`: clean, 0 errors.
- `dotnet format --verify-no-changes`: clean.
- `CNA001`/`CNA002` (the Roslyn analyzer from #522): flagged nothing.
- Real E2E test (`TwoRealHttpPlayersConductAnEntireOperationStageOne`): passes.
- Targeted Core regression (`*StageEntry*`, `*Second*`, `*ActionCandidate*`,
  `*Checkpoint*`): **157/157**.
- Targeted OrleansHost regression (the real E2E test, SecondReserve,
  cross-audience wrong-side rejection tests, rejection-reason-code tests,
  Force Assignment/Retreat/Custody): **13/13**.

## Left for follow-up slices

- The five automatic preamble mechanics' own auto-drain wiring (Weather,
  Organization, Naval Convoy Arrival, Fleet Assignment, Fleet Repair) —
  needs either extending `CampaignExercises.Query`'s `System`-audience path to
  recognize native-chain state (today it has none), or a dedicated Host-side
  auto-chain mirroring Breakdown's own "complete as a side effect of the
  prior step's acceptance" pattern. Not guessed at here.
- Reserve/Movement/Breakdown/Combat Decision/Assault/No-Attack's own Host
  wiring — each is now proven mechanical (confirmed by this slice reaching
  all the way through Query/Submit for one mechanic), but each is its own
  multi-touch-point PR (Journal Query+Submit, registry entry, Store
  sync+async, two CampaignGrain switches, the outward API allowlist) — and
  per the finding above, each should be preceded by the `Reconstruct`
  cascade audit, not just copy this slice's pattern blind.
