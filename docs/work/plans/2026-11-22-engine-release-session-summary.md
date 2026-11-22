# Initial engine release — end-of-session summary (2026-11-22)

**Purpose of this doc:** a plain-language summary of where the release
actually stands after this session's work, for picking this up again later
without re-reading the full, dense ruling-by-ruling history in
[`2026-11-21-initial-engine-release-scope.md`](2026-11-21-initial-engine-release-scope.md)
(that doc remains the detailed, citation-backed record — this one is the
quick orientation). Every claim below is backed by something in that doc or
a merged PR; nothing here is new, restated, or softened.

## The headline result

**The real end-to-end proof is done.** A single test drives real HTTP
campaign creation through both sides' complete Operation Stage 1 — zero
direct Core/Journal calls in the test body, zero test-fixture shortcuts —
landing at `land.position.operation-2.initiative-declaration` with cold
replay verified. That is the actual, literal proof of the owner's own stated
goal from this session: *"both players can conduct moves/actions and it be
sent over properly and accurately to the game and other player to action
on."* Everything else below is how we got there and what's still open.

## Why this took as long as it did

This session started by catching a real, repeated error: status updates had
been measuring progress against the Exercise Harness (a test-automation
tool, `Cna.ExerciseRunner`) instead of the real production path
(`CampaignDurableAuthorityJournal` / `/api/v2/campaigns`). Once that was
corrected and the team committed to verifying every claim against actual
code and tests — not against prior docs, not against isolated unit tests —
a chain of real, previously-invisible gaps surfaced one at a time, each
exactly because nobody had ever tried to drive the *whole* chain as a real
player with zero backdoors. That discipline is why this took hours instead
of being "basically done" early on — and it's also why what's now proven is
actually trustworthy.

## What shipped and merged tonight

In roughly the order they landed:

- **#489** — a real production bug fix: third-cycle assaults were silently
  stalling from a double-spent ammunition ledger.
- **#491** — both players can complete a full Combat cycle, including
  second-side resolution to a real result (not just reaching Selection).
- **#492 / #494 / #496** — closed a real stall risk (an idle second player
  could freeze the game forever at Combat Selection) and proved restart
  recovery for every second-side round.
- **#493** — adversarial testing filled a real coverage gap (second-side
  retreat/custody decisions, not just the no-op paths).
- **#497** — fixed a release-blocking gap: a real player could not start
  *any* game, because the very first decision (who acts first) had no path
  in production code.
- **#499** — the same gap, one step later (Reserve Designation).
- **#502** — Reaction window resolution now fails closed with an explicit
  "not supported yet" rejection instead of crashing, after finding its real
  fix would require touching an acknowledged, deliberately-unresolved piece
  of Combat's state machine (`#394`) that a prior PR explicitly declined to
  touch for determinism-safety reasons.
- **#505** — fixed the long-standing `tests (long-01)` CI flake (4-way
  parallel heavy test lanes exceeding the CI runner's memory, confirmed from
  the run's own measurements, not guessed).
- **#506** — a from-scratch, citation-verified reference of Core's public
  API surface and the real outward HTTP API, with Stable / Experimental /
  Omit classifications for every endpoint.
- **#508** — the foundational fix: `CampaignActionAudience.System` steps
  (automatic dice-roll/resolution steps with no human decision) are now
  auto-drained transparently by the Host, wherever they occur. This is what
  actually let a real HTTP player reach every mechanic above from true
  campaign genesis — before this, every "done" claim above was only proven
  reachable from its own already-set-up starting point.
- **A fourth and fifth missing owner-opening** (`CompleteMovementSegmentAction`,
  `StopElementMovementAction`) — ordinary Movement's "I'm done" and "stop
  this element" decisions, found only because building the real end-to-end
  test exposed that nothing had ever driven ordinary Movement as a real
  player either.

## What's still in flight as of writing this

Nothing. **#511 (merged, commit `788b5430`)** landed `StopElementMovementAction`
alongside `CompleteMovementSegmentAction` — the two share one owner
opening/HTTP stage since Core's dispatch makes them strictly either-or — and
the end-to-end test that found this gap (`TwoRealHttpPlayersConductAnEntireOperationStageOne`)
now passes with zero workaround. Every item in the scope doc is Done or
explicitly out of scope. Zero open PRs as of this update.

## What's deliberately out of scope or deferred (not forgotten, tracked)

- **All 6 turns / multi-stage looping**: ruled out of scope this release.
  Core has no authority past Operation Stage 1 of Turn 1 for either side —
  this is a large, previously-unscoped architecture rework, not a bug. The
  revised release bar is one full Operation Stage 1, both sides, which is
  now proven.
- **Reaction's real fix** (`#394` Gaps 1 and 3): tracked as its own future
  design decision. Gap 1 is sidestepped (confirmed working); Gap 3 needs
  genuinely new Core modeling work the project's own docs say has "the
  least existing precedent... highest remaining uncertainty."
- **Rail, Repair, Patrol, victory/termination**: confirmed not started: not
  present in the real Host API at all. Explicitly deferred — the release
  bar doesn't require them, just that the loop never silently halts or
  fakes an answer.
- **Real vehicle-Breakdown resolution, the ZOC topology-feature gap,
  `StopElementMovementAction`-as-dead-code (corrected)**: all confirmed
  genuinely unreachable in the content this release actually ships, by
  driving the real fixtures — not assumed.

## The standing discipline worth carrying forward

Every single correction and discovery tonight came from the same habit:
verify against the actual code or a passing test, cite it, and say
"unverified" rather than infer. The two biggest wrong turns this session
(the Exercise-Harness-vs-real-path conflation that started the night, and a
documentation agent's own misread of a stale code comment later on) were
both caught by that same habit, applied a second and third time. Keep
applying it before trusting any "done" claim — including the ones in this
document.
