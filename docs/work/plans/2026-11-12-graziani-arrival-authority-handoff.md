# Graziani arrivals: bounded authority handoff

**Status:** research and proposed contract slices, not an implemented scenario,
accepted rule, or change to existing save formats. This packet connects the
[source arrival analysis](2026-11-12-graziani-arrival-scope.md) and
[working 6th NZ decision](2026-11-12-graziani-ruling-decisions.md#rul-gra-04-6th-nz-brigade-arrival-working-interpretation)
to current Core. An independent agent verified a bounded 11-row source pilot;
full source review, scenario content rights and formal roadmap predecessors remain open.
See the [review disposition](graziani-source-review/review-disposition.md),
[setup draft](../../specs/graziani-setup-draft.md) and implemented synthetic
[index contract](../../specs/scheduled-arrival-index-v1.md). The index is not
wired to campaign authority and does not satisfy the following slices. A separate
[synthetic transition laboratory](../../specs/scheduled-arrival-lab-v1.md) exercises
multiple obligations, replay and retry without changing actual campaign state.
A [content draft analyzer](../../specs/campaign-scheduled-arrival-content-draft-v1.md)
now checks deferred groups against real Core content definitions. It retains the
legacy missing-placement errors and does not create an admitted artifact.

## Existing bounded authority

- `CampaignStageEntryPolicy` currently accepts operation stage 1 only.
  `Cna1979SetupCatalog.IsAdmittedStageEntryPolicy` admits a specific explicit-none
  policy for Organization, Naval Convoy Arrival and fleet obligations at the
  initial turn/stage; its source identity is part of the test profile.
- `CampaignEngine.StageEntryEventFactory.CreateArrival` checks that admitted
  policy, exact turn/stage and position before emitting a
  `NoObligationNavalConvoyArrivalResolved` event and moving to Fleet Assignment.
  `CampaignProjector.ApplyArrival` reuses the factory through stage-entry replay.
  This is useful rejection/recomputation scaffolding for the bounded synthetic
  profile, not a scheduled reinforcement implementation.
- Source §60.47 and the Commonwealth track identify six schedule positions during
  turns 1–6. The pilot's 21 OA row candidates and the 6th NZ cross-reference
  conflict are not yet an admitted force. Convoy/supply arrivals and other
  obligations are separate, so absence from this track does not prove an empty
  Naval Convoy Arrival phase.

Directly inspected code: [policy](../../../src/Cna.Core/Setups/CampaignStageEntryPolicy.cs),
[catalog](../../../src/Cna.Core/Setups/Cna1979SetupCatalog.cs),
[decision/factory](../../../src/Cna.Core/Campaigns/CampaignEngine.cs) and
[projector](../../../src/Cna.Core/Campaigns/CampaignProjector.cs). Graph coverage
reported no recorded issue for these paths, a best-effort signal rather than proof
of completeness. Do not change historical `ExplicitNone` meaning or admit new
states by treating unmodeled arrivals as empty.

## Contract-first slices

1. **Source and content identity.** After double-entry and rights review, define
   a versioned schedule record for each force or arrival group: stable owner,
   arrival `(turn, stage)`, source and ruling identities, constituent references,
   training metadata, optional load/attachment choices, and destination rules.
   Preserve scenario exceptions without duplicating OA/track entries. The setup
   and content/rules hashes must bind the records and working rulings actually
   accepted for that version.
2. **Arrival obligation and choice.** Define pending/eligible/completed identities
   and exact ordering relative to Organization, naval convoy arrivals, other
   cargo, and each player decision. Side-specific legal destinations/choices must
   be derived from authoritative state. Scheduled presence must not be bypassed
   by an `ExplicitNone` event; absence is admitted only when all applicable
   obligations, not just land units, have been accounted for.
3. **Atomic result and replay.** Specify versioned command, event, snapshot and
   receipt contracts before implementing them. Record exactly which group and
   destination were accepted, created units, parent/attachment and resource
   effects, and stage boundary. Projection must revalidate source/content/ruling
   identity and reconstruct the same state; duplicate requests and failure after
   commit must not deliver a second arrival. Unknown or invalid obligations
   reject before mutation or randomness.
4. **Following actions and information.** Preserve source-eligible movement in
   the arrival stage, but apply actual attachment, training, stacking, weather and
   action-order restrictions. Morale training is optional under §17.36; do not
   infer a blanket training lock. Define event and observation redaction so the
   opposing side cannot infer private future entries or hidden choices from legal
   actions, errors, logs or public snapshots.
5. **Activation.** Extend policy, serializers, creation admission, authority,
   projector, snapshots and public adapters as one compatible vertical slice;
   retain historical no-obligation reads and fail closed for non-admitted profiles.
   Existing public Combat/Runner gates and scenario admission remain roadmap
   predecessors for a playable release, not implied by a local arrival test.

## Minimum failure-sensitive evidence after the source decision

- Correct group and owner at each accepted `(turn, stage)`; no early/late or
  duplicate appearance. In particular, exercise 6th NZ artillery at turn 2 and,
  if RUL-GRA-04 is adopted, the four brigade OA entries at turn 6.
- Permitted destination and player choice, wrong-side/stale/invalid choice,
  capacity/stacking boundaries and legitimate unresolved training/attachment.
  Unit movement in its arrival stage requires a separate actual legal-action test.
- Simultaneous obligations must not erase one another; if only one category is
  implemented, unsupported obligations block phase completion rather than being
  projected as empty.
- Reordered or missing source/content/ruling hash, forged event, wrong phase,
  repeated command, checkpoint restore, cold replay, lost response and attempted
  cross-side disclosure. Preserve all old admitted profiles and exact event bytes
  under their original identities.

**Current validation:** Release build and four focused
`StageEntryArrivalTests` passed on this working tree using
`dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --no-build --no-restore --filter-class '*StageEntryArrivalTests*'`.
The preceding `just test-class core '*StageEntryArrivalTests*'` built successfully
but its test runner failed inside the sandbox because local named-pipe binding was
blocked; the direct focused command above passed with that restriction lifted.
Those four tests cover existing synthetic no-obligation behavior only. The
new laboratory is isolated, with no campaign scheduled-arrival authority.
Full Core and release-head CI evidence must be reported for the exact build used.
