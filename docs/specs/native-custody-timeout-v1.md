# Native Custody timeout v1

Native event36 `combat-custody-timeout` records System fallback `leave-unguarded`.
Host accepted-step30 uses `native-combat-custody-timeout`; those version domains
are separate. Owner V25 `combat-custody-disposed` bytes remain unchanged.

## Authority and effects

Bind the accepted settlement hash/state, captive lot, source-legal escape route,
result World and custody-state hashes, configuration hash, immutable opening,
deadline/high-water and observed timeout evidence. No owner action or guard is
fabricated. Existing V25 source projection leaves World/TOE/ammunition unchanged,
marks the lot escaped, retains its quantity and victim identity, and creates the
same delayed replacement entitlement (training pending, twelve operation stages
later). No RNG is consumed. Accepted owner disposition closes the decision;
timeout cannot override it. Current native cut supports exactly one pending lot,
not a partial multi-lot queue.

Canonical event fields: `contractVersion`, `eventType`, `campaignId`,
`priorStateVersion`, `positionId`, `settlementHash`, `lotId`, `route`, `worldHash`,
`custodyStateHash`, `fallback`, `configurationHash`, `openedAtUnixMilliseconds`,
`deadlineUnixMilliseconds`, `highWaterUnixMilliseconds`, optional
`cancelledAtUnixMilliseconds`, `reason`. No `choiceKind` owner field is emitted.
Reasons: `deadline`, `clock-unavailable`, `clock-regression`,
`controller-unavailable`, `custody-unavailable`. Unavailable entry has -1 stored
clocks, no observed timestamp, and the real timing hash or an all-zero SHA256
sentinel when timing is absent. Other evidence requires an opening/high-water
strictly before deadline. Core checks exact rules/content-bound budget; Host
checks immutable persisted clock evidence. Strict replay recomputes disposition
and all canonical bytes. Unknown fields, changed lot/route/effect and duplicate
submission reject. Checkpoint/state version is36; initial and repeated replay
reuse the typed V25 projection through the explicit derived event.

## Supported fallback scope

For a successfully projected current native single-lot cut, `leave-unguarded`
is reachable by construction. `CampaignNativeCombatSettledWorldV24.Project`
creates each captive origin at the paid victim location; attacker never moves,
and defender either stays there or takes one accepted post-assault move.
`CampaignContactRetreatAfterAssaultCut.Project` only offers an adjacent clear
terrain destination. The escape route is consequently zero or one clear edge,
within V25's four-edge/eight-point bound. Focused test checks current clear
movement cost and one accepted captive route. This is a source-path argument for
current bounded native settlement, not an exhaustive public-profile certificate.

If future content/runtime admits multi-lot or different retreat routing, totality
must be reviewed again. Guard-only/unoffered fallback cuts reject without mutation;
no synthetic route, owner choice or new source rule is invented. These unsupported
cuts cannot be silently admitted to a public profile.

Initial session timeout, cold reconstruction, stale/retry, clock/configuration
and forged-evidence cases have focused tests. RepeatAssault uses V25.Replay,
which dispatches36; PostRepeat delegates its active assault to that reducer, and
ExerciseSession.WithRepeatAssaultEvent routes to PostRepeat when active. A new
genuine second/third-cycle captured-lot timeout history is not claimed by the Core
unit tests; any separate Host evidence must be listed explicitly.
