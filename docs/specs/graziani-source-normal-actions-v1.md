# Graziani source setup: normal actions v1

`sandtable.legal-actions.graziani-source-setup.v1` exposes the requesting side's
currently available source setup choices through `CampaignExercises.Query` and
`Submit`. This policy is setup-only: `positionId` remains
`land.graziani-source-setup`, and no game clock, turn progression, movement,
or arrival has been admitted.

Each canonical `source-setup` candidate binds its authority state version,
source family, and an allowlisted side-visible choice object into its action
ID. The underlying source adjudicator ID is not serialized. Candidates are
ordered by family and canonical choice, not the version-bound ID. Submission
re-queries the requesting side, requires the exact current state/version and
action ID, and delegates to the existing source adjudicator. The ordinary
accepted receipt binds the normal action ID; the existing source event and
checkpoint remain authoritative and are re-adjudicated on journal replay.
Historical source action kinds and pregame policies remain unchanged.

The Host owner opening publishes each side's already-authorized family state
and caveats plus choices, omits authority action IDs and state versions, and
maps choices to opaque owner references. Opposing source state, seed, internal
source IDs, and event bytes are not published. System admissions and source
content migrations remain authenticated system operations.

The GT2 supply correction remains a source setup decision, not an arrival.
Future convoy execution must require the finalized correction and use its
allocations as the effective GT2 plan; the original plan remains historical
evidence. Other convoy cargo, full scenario setup, movement, phase obligations,
and Polish arrival are not certified by this policy.
