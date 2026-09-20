# H2 candidate — research only pending H1 acceptance

Extend same one-stream dormant router through ordinary Movement E1, route lifecycle E2/G1 and
Breakdown completion G2. No H2 implementation before H1 dev/three-review gate closes.

Accepted readers:
- CampaignCombatInheritedMovement.Replay(request,created,preamble,weather,stage,reserve,moves).
  Requires actual NONE/Normal firstopening; validates exact Move4 bytes/effects, at most32events.
- CampaignCombatMovementLifecycle.Replay(...,reserve,moves,lifecycleEvents).
  Requires actual moves/nonvehicleflow; zero-to-three stop2,Systemresolution2,Movementcompletion3.
- CampaignCombatBreakdownCompletion.Replay(...,reserve,moves,lifecycleEvents,events).
  Requires exactly3lifecycle records/actualMovementEnd+idleBreakdownboundary, zerooronecompletion.

Router must derive Reserve completion boundary from actual typed completion, not eventcount10:
count10/state11 may be designation or directcompletion. Preserve every H1 prefix (including weather/
Reserve profiles that have no admitted ordinary Movement tail); only instantiate Movement reader
when actual next event needs it. Whole retained stream remains bounded/owned by acceptedH1capture.

Selected ordinary element-moved4 differs from F1 reaction-trigger element-moved4 despite same
family/version. H2 must reject actual F1 tail by exact existingreader comparison; H3 later owns
legal Reaction routing. No generic catch-and-ignore/truncate fallback. Stop/Systemresolve/completion
mandatory evenemptycohort; G2 consumes actualendproof and endsCombatentry without actualCombatstate.
Add typed projectionarms for actual accepted family states; no opaque caller-family selection.

Likely fourprimarypaths: existing CampaignCombatHistoryReplay.cs,CampaignCombatHistoryModels.cs,
existing CombatHistoryReplayTests.cs (its H1-validfutureMove tail rejection must evolve once H2
admitsMove), and new CombatMovementHistoryReplayTests.cs. Existingfixturelinks already coverfamily
contracts/H0. A fifthhelper only if warranted and authorizedbyroot. Do not edit accepted family
implementations/schema/oracles/fixtures. Userdev+three reviews required before H3.

Coverage: everyordinary/lifecycle/G2 retainedprefix and bothowners, exactcanonicalfamilygoldens,
H0 fullledger/header/World/route/progress/interrupt/end/cycle evidence and exhaustiveidentityset
for assigned cuts. Do not claimliteralrootserializer beforeH4. Missing/reordered/duplicated/resigned
canonical events, forgedroute/authority, prematureG2, trailingunknown, actualunsupportedReaction,
inputbuffer ownership and legacyreader strictness. FullcumulativeCoregate rootowned.
