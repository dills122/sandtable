# Maproom play experience: current state and flow plan

**Status:** draft plan for review, not an accepted design. Written in
response to a direct request to audit Maproom's real state and start
thinking through how this game should actually feel to play. Every
recommendation below is flagged as a recommendation, not a decision.

## Correction to expectations, up front

The request assumed Maproom is very early with little done or connected.
That's true for exactly one thing — live connection to a real campaign for
*submitting* an action — and false for almost everything else. Read
precisely, not assumed:

- **The hard part is already built, well-engineered, and fully tested.**
  `campaign-action-flow.ts`'s `CampaignActionFlow` class is a complete
  action-submission state machine: `disabled → ready → submitting →
  (pending | uncertain | accepted | rejected)`, with correct handling of a
  lost reply (never re-POSTs; polls a receipt instead), stale/superseded
  views (revision-compared, never trusts an older read), and seat-capability
  revocation. Its own docstring says exactly what's missing: *"The shipped
  workspace constructs this without an adapter or admitted capability, so
  its submit control is always disabled. No route is activated here."*
  It takes a `CampaignActionAdapter` interface (`submit`/`receipt`/`view`)
  that nothing currently implements against a live server.
- **Read-side UX is genuinely mature.** Map camera (zoom/pan/fit, keyboard
  and touch), formation selection synced across map/roster/ledger, War
  Diary and ledger filtering/sorting, responsive layout down to 320px at
  200% text, and full WCAG A/AA coverage (axe-checked) — all covered by 50+
  passing Playwright specs across 6 files (`e2e/*.spec.ts`). This is not a
  skeleton.
- **The contract both ends need is already fully specified and generated —
  just not implemented server-side.** `src/Cna.Game.Contracts/openapi.v1.json`
  defines `POST /api/v1/campaigns/{id}/actions` and
  `GET /api/v1/campaigns/{id}/receipts/{requestId}`, already generated into
  a typed Angular client (`api/generated/api/actions.service.ts`). The
  contract is more specific than this session's earlier API plan assumed:
  submission goes through an opaque `actionSetToken` issued with the
  player's view (not a raw state-version check), `actionId` is "an opaque
  audience-issued action handle, not the Core candidate hash," and rejection
  reasons are a small, deliberately outward-safe set
  (`invalid-action` | `stale-view` | `unsupported-phase`), not a passthrough
  of Core's internal rejection enum. Server-side plumbing for this already
  exists too — `TemporaryOutwardActionIngress.cs`, `TemporaryCampaignGrain.cs`,
  `TemporaryOutwardActionRequest.cs` in `Cna.OrleansHost` — it's just not
  mapped onto an HTTP route in `LocalGameApi.cs` yet, and the `/view`
  endpoint already emits a populated `actionSetToken` via
  `TemporaryPlayerViewProjection.cs`.

**This directly matters for the MAP-09 work already in flight elsewhere in
this session.** That implementation was briefed before this finding existed
and may not be built against this exact pre-existing contract
(`actionSetToken`-scoped submission, the 3-value outward rejection enum, and
the `requestId`-based receipt-lookup endpoint) or the existing server-side
ingress plumbing. Worth checking its design against this doc before it
merges, to avoid a client/server mismatch or duplicated work.

So: "very little done/connected" undersells this by a lot. The real state
is "two well-built, well-tested halves of a seam, with the seam itself not
soldered yet." That's a much smaller, more encouraging gap than the user
expected — but closing it needs to use the contract and plumbing that
already exist, not reinvent them.

## Interaction principles for this game specifically

Not generic game-design advice — each tied to a real mechanic this engine
actually has.

1. **The draft/review/issue split that already exists in `order-workflow.ts`
   is the right shape — keep it, and extend it.** Compose a draft, review it
   explicitly (a distinct, undoable step with no consequence yet), then
   issue it (the one real, committed action). This naturally matches a game
   where actions should feel safe to explore: a player can draft and review
   several options before committing to the one irreversible submission.
2. **Status must always say *why*, in plain language, not just *what*.**
   The existing `rejected`/`stale`/`uncertain` states already have the right
   skeleton (`aria-live="polite"` status regions with distinct messages per
   state) — the real contract's 3 rejection reasons
   (`invalid-action`/`stale-view`/`unsupported-phase`) should map to 3
   distinct, specific, in-character explanations, not one generic "action
   failed" message. A stale-view rejection ("the world moved while you were
   deciding — here's what changed") is a completely different player
   feeling from an invalid action ("that unit can't do that right now, here's
   why"), and the UI should treat them differently, not just show different
   error codes.
3. **Fog-of-war is a feature to show off, not just enforce.** The engine
   already does the hard, correct thing — the server decides what a seat
   sees, nothing about the hidden world leaks client-side
   (`GET .../view` + `TemporaryPlayerViewProjection`). The UI should make
   *not knowing* feel like real fog of war (uncertain contact markers,
   "apparent contact" framing already present in `ApparentContact` model)
   rather than an absence of data. Lean into what's hidden, don't just
   render what's visible and ignore the gap.
4. **A Reaction window is a dramatic beat, not a form field.** This session
   found the native second-side movement chain's one real remaining engine
   gap is resolving/waiving a Reaction window. When that's built, the UI
   moment matters: a Reaction window is the one point where the *other*
   player's hidden choice can interrupt your plan — that's tension a
   strategy game should earn, not bury in a confirmation dialog.
5. **Hot-seat handoff is a privacy boundary, not a loading screen.** The
   deep engine-capability audit from earlier this session already found the
   real gap here: the roadmap's seat-handoff privacy requirement
   (invalidate drafts/DOM/storage/worker-state) has one partial call site
   (`clearAudience()`) and no dedicated test. The handoff moment is also a
   *players'* moment — "pass the device, the next seat's turn is starting"
   deserves a deliberate, clearly-branded transition screen, which would
   also be the natural place to force the state-clearing this project
   already knows it needs.
6. **The War Diary / Chronicle is narrative, not a log viewer.** `DiaryExcerpt`
   already exists as a model and `ledger-diary.spec.ts` already tests it
   being driven by accepted/rejected outcomes. Treat its entries as the
   game telling the player a story about their campaign, not a changelog.

## Flow sketches

### A. Composing and issuing one order (the core turn loop)
*What the player sees:* their formation selected on the map (already
synced across map/roster per `maproom-depth.spec.ts`), a context-specific
set of legal actions for that unit — not a generic form — drawn from the
real `CoherentPlayerView.legalActions` the server already computes.
*What they do:* pick an action, see it previewed on the map (destination
highlighted) before committing, hit "Review" (already a distinct step in
`order-workflow.ts`), confirm, then "Issue."
*Feedback:* an immediate, named status the instant the request is sent
("Sending — your unit's current position holds until confirmed"), then a
specific resolution (accepted → map updates, rejected → the 3-reason
specific explanation above, uncertain → explicit "reply lost, don't resend,
here's how we're finding out").
*Why this helps:* the multi-step draft→review→issue shape (already built)
keeps the complexity of "what's legal right now" contained to one small,
contextual palette instead of one big form — this is the single highest-
leverage thing for a deep ruleset that still needs to feel playable.

### B. A Reaction window opens on you
*What the player sees:* a clear, distinct visual state change — not another
inline form row — signaling "the enemy may react to what just happened,"
tied to the specific unit/hex involved, with the game explicitly saying
what triggered it.
*What they do:* resolve or (once the engine supports it) waive the window;
this is a real decision point, worth its own focused UI state rather than
folding it into the ordinary legal-action palette.
*Feedback:* an explicit before/after — "the enemy chose to react" vs. "no
reaction" — narrated through the War Diary, not just a silent state change.
*Why this helps:* this is literally the moment this engine models tension
and uncertainty between two sides; underselling it as "just another legal
action" wastes the one mechanic built for drama.

### C. Hot-seat handoff between two local players
*What the player sees:* an explicit, full-screen "pass the device" screen
between seats — not a silent return to a shared view — naming whose turn
is starting next.
*What they do:* actively confirm "I am now playing as [seat]" before any
side-safe state loads, which both reads as a game moment (ceremony around
the handoff) and gives a natural, forcing point to invalidate the previous
seat's drafts/DOM/storage — closing the real gap the deep audit found,
through UX rather than a bolt-on fix.
*Feedback:* a visibly different look/theme per seat (even subtle) so the
player always knows, at a glance, which side they're currently seeing.
*Why this helps:* turns an awkward technical requirement (privacy
invalidation) into something that reads as normal game flow instead of a
chore, and gets the thing the engine audit flagged as untested for free as
a side effect of good UX.

### D. Reading a War Diary / Chronicle explanation
*What the player sees:* entries written as what happened to their forces,
in-setting language, tied to a specific location/formation they can click
to locate on the map — this already exists per `ledger-diary.spec.ts`'s
"distinguishes locate actions."
*What they do:* skim recent entries, click one to jump to its location,
optionally filter by formation.
*Feedback:* the diary and the map stay visually linked (already tested
behavior) so cause-and-effect is never abstract.
*Why this helps:* this is the lowest-cost, highest-payoff flavor surface
already built — leaning into its narrative voice costs little and does a
lot of the "feels like a game" work for free.

## Smallest first slice

Not "redesign the UI." The smallest real next step, given everything above:

**Implement a real `CampaignActionAdapter` against the actual
`src/Cna.Game.Contracts/openapi.v1.json` contract, and wire it into the
existing `CampaignActionFlow` for exactly one already-working flow (Flow A
above).** The state machine, the UI, and the contract all already exist —
this is connecting three already-built things, not building a fourth.

**Acceptance criterion:** with a real adapter wired in, `canSubmit` becomes
true for a real campaign (today it's structurally always false per the
class's own docstring), one legal action can be submitted end-to-end against
a live local campaign, and the existing `workspace.spec.ts` lost-reply/
stale/rejected test *behaviors* (not necessarily the exact fixture tests
themselves) hold against the real server, not the simulated one. This is
also exactly MAP-09/MAP-11's own stated scope — this slice and that one
should be the same piece of work, not two.

## Open questions — genuinely the user's call

- **Visual style/theming direction.** Nothing here picks art direction; the
  `field-atlas` design system already exists as a foundation
  (`projects/field-atlas`) — is the plan to extend it, or is a visual
  direction decision still pending separately?
- **How much hand-holding vs. assuming wargame-literate players.** The
  flows above (contextual legal-action palettes, explicit review steps,
  narrated Reaction windows) help either audience, but things like a
  tutorial/practice mode, rules tooltips, or a "what can I do here" helper
  are a real scope decision not made here.
- **Is a tutorial/practice/sandbox mode in scope before real campaigns?**
  Not assumed either way.
- **Mobile/touch as a real target vs. "works but not the point."** The
  existing e2e coverage already tests touch targets and narrow viewports
  seriously — worth confirming whether that's because mobile is a real
  target, or because accessibility/responsive testing was done thoroughly
  for its own sake regardless of platform priority.
- **Should the MAP-09 work in flight be paused/checked against this doc's
  contract finding before it merges?** Flagged above; not decided here —
  that's a coordination call for whoever's running that work.
