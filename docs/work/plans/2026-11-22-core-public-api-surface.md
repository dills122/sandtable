# Cna.Core public API surface (internal reference)

> **2026-11-26 status pointer:** The original scope/design/evidence below is unchanged. The
> real synthetic HTTP checkpoint now reaches open Turn 2 Reserve, not six-turn completion,
> and native Reaction/executor migration remain open. See the [current roadmap](../../roadmap/pre-alpha-roadmap.md#current-checkpoint-and-next-gates).

This is a map of the genuinely public (not `internal`) surface of `src/Cna.Core/`
that a caller outside the `Cna.Core` assembly can use, for engine developers
working on the Exercise Harness, the Orleans Host, or Core itself. It is not
exhaustive and it is not the player-facing contract — for the HTTP API a UI
actually talks to, see
[`2026-11-22-outward-http-api-reference.md`](2026-11-22-outward-http-api-reference.md).

Every claim below was checked directly against the current source in this
worktree (branch `worktree-agent-a5fbd11f77786b3ac`, same commits as
`main` as of 2026-11-22: `06a2141a`).

## The two layers, and who calls which

Core exposes two public static classes that matter for this survey, and they
are easy to conflate because they look similar in shape:

- **`CampaignExercises`** (`src/Cna.Core/Exercises/CampaignExercises.cs`,
  namespace `Cna.Core.Exercises`) — the low-level engine surface. It operates
  directly on `ExerciseSession` (Core's full in-memory campaign state) and
  exposes generic `Query`/`Submit` plus a long tail of mechanic-specific
  methods (roughly 218 `public static` members in this one file). This is
  **not** a thin facade; it contains most of Core's actual rule logic.
- **`CampaignHostAdapter`** (`src/Cna.Core/Hosting/CampaignHostAdapter.cs`,
  namespace `Cna.Core.Hosting`) — also public (267 `public static` members),
  and despite the name it lives inside `Cna.Core`, not inside
  `Cna.OrleansHost`. Its own doc comment calls it a "trusted, in-process
  boundary." It wraps `CampaignExercises` with per-mechanic, audience-aware
  `Query*OwnerDraft`/`Submit*` methods that shape output for a specific owner
  seat (visible bytes, private action IDs scoped to one side) rather than
  exposing the raw session.

**Correction to the task framing:** the task description for this survey
refers to "`Cna.OrleansHost`'s `CampaignHostAdapter`." That class is actually
part of `Cna.Core` (`src/Cna.Core/Hosting/CampaignHostAdapter.cs`), not
`Cna.OrleansHost`. `Cna.OrleansHost`'s real wrapper around it is
`CampaignDurableAuthorityJournal` (split across
`CampaignDurableAuthorityJournal.Openings.cs` and
`.Submissions.cs`), which calls `CampaignHostAdapter`'s methods directly. The
distinction matters for this doc's purpose: `CampaignHostAdapter` is already
inside Core, and is the thing both the real Host path and (indirectly) the
Exercise Harness sit on top of.

Who actually calls what, confirmed by grep against the two consumer projects:

- **`Cna.ExerciseRunner`** (the test-automation harness) calls
  `CampaignExercises` directly — `QueryCheckpoint`, `QueryWithCombat`,
  `SubmitWithCombat` (see `src/Cna.ExerciseRunner/Execution/ExerciseExecutionRuntime.cs:85-100`,
  `src/Cna.ExerciseRunner/Artifacts/ExerciseBundleSemanticValidator.cs:261-379`).
  It does not go through `CampaignHostAdapter`.
- **`Cna.OrleansHost`**'s `CampaignDurableAuthorityJournal` calls
  `CampaignHostAdapter`'s `Query*OwnerDraft`/`Submit*` methods (see the
  dispatch tables in `CampaignDurableAuthorityJournal.Openings.cs:227-525`
  and `.Submissions.cs:650-730`), which in turn call into `CampaignExercises`
  and other Core internals.

So "Core is public" does not mean "Core is what a real player's client talks
to." A real player's HTTP request never reaches `CampaignExercises` or
`CampaignHostAdapter` directly — it goes through the Orleans Host's HTTP
layer and `CampaignDurableAuthorityJournal`, which is Part 2 of this
documentation pass.

## `CampaignExercises`: the five methods named in scope

- **`Query(ExerciseSession session, CampaignActionAudience audience)`**
  (`CampaignExercises.cs:3877`) — returns a `CampaignLegalActionQueryResult`
  wrapping a `CampaignLegalActionSet` (the current legal actions for that
  audience at the current cut) or a rejection
  (`CampaignLegalActionQueryRejectionReason`). This is the generic
  entry point used for everything that isn't Combat-in-progress or
  pre-game/source setup; it explicitly rejects
  (`InvalidState`) while `session.CurrentCombatDecision is not null` — the
  gate documented as "#394 Gap 1" in
  `docs/work/plans/2026-11-18-combat-continuation-second-side-design-proposal.md`
  and discussed in the scope/readiness docs for this release.
- **`Submit(ExerciseSession session, CampaignActionSubmission submission)`**
  (`CampaignExercises.cs:4314`) — the generic counterpart to `Query`. Returns
  an `ExerciseStepResult` (accepted successor session + evidence, or a
  rejection reason). Same `CurrentCombatDecision` gate applies.
- **`QueryCheckpoint(ExerciseSession session)`** (`CampaignExercises.cs:4043`)
  — projects an `ExerciseCheckpoint` describing where the session currently
  sits in the sequence catalogue (position ID, contract version, side), by
  walking an explicit cascade of `session.Current*` fields from latest to
  earliest. Used by both the Harness and the Host (indirectly, via
  `CampaignHostAdapter`/tests) to know "what step are we at."
  the Harness directly calls `CampaignExercises.QueryCheckpoint`.
- **`QueryWithCombat(ExerciseSession session, CampaignActionAudience audience)`**
  (`CampaignExercises.cs:3907`) — `Query` plus the subset of Combat steps the
  Harness is allowed to drive (first-side Selection and its immediate System
  follow-up). Its own doc comment is explicit: **"Test-harness capability
  only: the Host does not call it, and it is not Combat activation or
  certification."**
- **`SubmitWithCombat(ExerciseSession session, CampaignActionSubmission submission)`**
  (`CampaignExercises.cs:3919`) — the `Submit` counterpart to
  `QueryWithCombat`, same test-harness-only scope.

`QueryWithCombat`/`SubmitWithCombat` are the two methods a reader should be
most careful not to mistake for production reach: they exist specifically so
the Exercise Harness can push combat forward in isolated regression runs,
and the doc comment on them says plainly that the Host does not use them.
This is exactly the kind of distinction the scope/readiness docs for this
release found had been conflated before.

## Action candidate types

`src/Cna.Core/Actions/CampaignActionCandidate.cs` defines the abstract base
`CampaignActionCandidate` (public, contract-versioned, with a
content-addressed `ActionId` computed from the candidate's own kind/subject
semantics — see `CalculateId`/`WriteSemantics`/`WriteSubjectSemantics`) and a
handful of concrete preamble candidates: `ResolveInitiativeAction`,
`ResolveNoObligationNavalConvoyScheduleAction`,
`ResolveNoObligationTacticalShippingAction`, `ResolveWeatherAction`,
`ResolveNoObligationOrganizationAction`,
`ResolveNoObligationNavalConvoyArrivalAction`,
`ResolveNoObligationFleetAssignmentAction`,
`ResolveNoObligationFleetRepairAction`, `ActFirstAction`, `ActLastAction`,
`DesignateReserveAction`, `CompleteReserveDesignationAction`.

These are only the preamble-phase candidates. Movement, Combat, and the
other mechanics each have their own candidate types in sibling files under
`src/Cna.Core/Actions/` (e.g. `CampaignMovementActionCandidate.cs`,
`SelectContactAssaultAction.cs`, `SealCombatForceAssignmentAction.cs`,
`CampaignRetreatBeforeAssaultActions.cs`,
`CampaignRetreatAfterAssaultActions.cs`,
`FinishCombatWithoutAttackAction.cs`, `CombatExerciseSystemActions.cs`,
`BreakdownActionCandidates.cs`) — not surveyed exhaustively here, since the
point of this section is the shape of what a caller gets back, not a
catalogue of every mechanic's candidate type.

A caller receives these wrapped in a `CampaignLegalActionSet`
(`src/Cna.Core/Actions/CampaignLegalActionSet.cs:7`, a `public sealed record`
carrying campaign ID, state version, ruleset hash, position ID, audience,
the candidate list, and a policy ID — note the explicit
`CombatExercisePolicyId = "sandtable.legal-actions.combat-exercise.v1"`
constant whose own doc comment again flags it as Harness-only, not served
by the Host) or, for submission, acts through a `CampaignActionSubmission`
(`CampaignActionSubmission.cs:7`) and gets back a `CampaignActionSubmissionResult`/
`ExerciseStepResult` whose rejection reasons include the FID-002-style
`RuleNotImplemented` (`CampaignActionSubmission.cs:18-29`) — "a source-legal
rule edge the engine has not built yet; it stays visible in the legal set
and is refused explicitly" per its own comment. This is the same mechanism
behind the Reaction and second-side Cycle-Control-Repeat rejections
documented in Part 2.

## What this means for a reader of the outward API doc

Core's public surface (`CampaignExercises` and `CampaignHostAdapter`) is
large, genuinely public at the C# access-modifier level, and almost entirely
consumed today by two callers: the Exercise Harness (`Cna.ExerciseRunner`,
via `CampaignExercises` directly) and the Orleans Host's durable authority
journal (`Cna.OrleansHost`, via `CampaignHostAdapter`). Neither of those
consumers is itself the thing a real player's browser/UI talks to — that is
the outward HTTP API documented in Part 2, which wraps `CampaignHostAdapter`
one layer further out. A method being `public` in Core says nothing about
whether a real player can reach the mechanic it implements; only the HTTP
surface answers that question.
