# Post-combat phase pass, event 41

Land §5.2.H–L lists Truck Convoy Movement, Commonwealth Rail Movement, Towing,
Maintenance and Patrol after the Movement and Combat cycle. The acting player
may finish each phase without performing any activity. The opening says
`finish-without-activity`: it does not claim that an activity is available.
In particular, §5.2.J permits rail movement only when Commonwealth is acting,
and §5.2.L permits patrols only if the acting player has not assaulted. Neither
activity is implemented here. Finishing does not assert that the campaign has
no trucks, broken vehicles, rail cargo or patrols.

The trusted cut supplies the campaign, current state version, acting side,
sequence position, World hash and immediately preceding event hash. A command
binds those values, the exact position and an owner action ID. Only the side
whose phase is active may pass. Core creates one event with the prior version,
prior event hash, position, side, unchanged World hash and exact next catalog
position; event version 41 and canonical ordering distinguish it from retained
cycle-control events. RNG, World, contacts and other players' private data do
not change. Every pass increments authority once. Stale, foreign, repeated,
wrong-side and non-phase requests reject without mutation.

Cold recovery recomputes the event from its preceding accepted cut and compares
canonical bytes and checkpoint. Host stores the command, event, receipt,
checkpoint and contact evidence in its existing journal transaction. An owner
opening binds the seat, revision and action reference; retry returns the same
receipt. A cut with obligations that make passing illegal must reject rather
than infer completion. Existing v28/v34 histories remain unchanged.

Event 41 records the convoy pass; successor event 42 records one pass for each
subsequent rail, towing, maintenance or patrol position, binding the exact
previous event. The normal Host exposes `truck-convoy-pass` and
`post-combat-pass` owner openings and commits them through the existing journal.
After the final pass the checkpoint reaches the second acting side's Reserve
Designation, handled separately by [V43](native-second-reserve-v43.md). These
passes do not implement moving trucks, rail, towing, repair or patrols, and do
not establish a playable Graziani campaign.
