# Second-side one-hex Movement, event 44

After V43 completes second-side Reserve Designation, an eligible owner may
move one independently represented, non-vehicle combat element one adjacent
hex and stop that element for the current Movement Segment. The owner opening
states that this is a move-and-stop choice. Costs, terrain, own stacking,
cohesion, Reserve status and current expenditure use the ordinary Core movement
rules; a unit cannot be offered another move after stopping. Vehicle breakdown,
attachment and historical combat classes remain outside this capability.

Offer membership uses own state, published map and formation-scoped contact
knowledge, never hidden opposing World facts. Core revalidates accepted action
against the actual World. The event binds the predecessor, source and destination,
cost, exact resulting World, current contact authority and any Reaction window.
Normal adjacency contacts are recorded for both sides after movement. If a
Reaction window opens, the session remains at that window until its own
authority handles it; it must not silently discard the opportunity. A move with
no window returns to second-side Movement for other eligible elements.
Event 45 completes Movement on an explicit owner choice when no Reaction is
pending, retaining the same World and advancing to Breakdown Determination.
Its canonical event binds campaign, prior version and event hash, acting side,
current and next position, and World hash. Replay derives the successor from
the pinned sequence and verifies those fields and the unchanged contact cut.
The retained V41–V45 successor chain is verified oldest-first without recursive
calls, so its length does not consume one call-stack frame per event. The full
event-stream hash is still computed at the requested head.
**Update (2026-11-17, corrected):** Breakdown (V46,
`CampaignNativeSecondBreakdownV46.cs`) is implemented and tested, and its
*checkpoint* (`CampaignExercises.QueryCheckpoint`) correctly reports a
successor position at the Combat engine's `PositionDetermination` step for
the second acting side. That checkpoint is **not** a working hand-off,
confirmed by direct reproduction (GitHub issue #394): a session at the V46
checkpoint still has `CurrentSnapshot.CurrentPosition` frozen at the
*first*-acting-side's old Combat position from earlier the same turn —
`WithSecondBreakdown` and its V41–V45 siblings never advance it — and
`CurrentCombatDecision` is the stale, unrelated decision retained from that
first-side combat cycle, never cleared by the native second-side `With*`
methods. As a result the normal owner-cut path
(`CampaignExercises.QueryContactOwnerCombatCut`,
`CampaignHostAdapter.QueryContactCombatOwnerDraft`) rejects immediately with
"Combat selection has already advanced this authority cut.", and the
Host's selection-round binding (`QueryNativeCombatSelectionRoundBinding`)
rejects with "No pending repeated Combat selection." — both for reasons
unrelated to the real state. Separately and independently, even with a
correct `CurrentSnapshot`, `CampaignContactCombatSelectionCut.Project` and
`CampaignNativeCombatSelectionTimeoutV38.ValidateInitialScope` hard-require
`LandActorRole.FirstActingSide` and `order.FirstSide`, so they would still
reject a genuine second-acting-side Combat entry. There is also no
`CampaignOwnerOpeningKind` on the Host side for a second-side Combat
opening, and no call analogous to `TryInitializeSelectionRound`/
`InitializeSelectionRound` is wired after `SecondMovementCompletion`/
`SecondBreakdown` persist. Combat is **not yet** a working hand-off for the
second acting side; the Reaction-window-resolution gap and vehicle-cohort
breakdown accounting remain additional, separate unimplemented cuts.

The normal Host authenticates an owner opening for the acting element and
persists command, event, receipt, checkpoint and contacts atomically in the
existing journal. Replay recomputes the movement and Reaction from retained
World and verifies exact bytes. Retry returns the original receipt; stale,
wrong-side, hidden-state-dependent, illegal and forged selections reject.
This does not admit Graziani's content or its six-turn lifecycle.
