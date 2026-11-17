# Engine capability deep audit — November 17, 2026

**Purpose:** go past the status-tracking in
[the exhaustive RC status](2026-11-17-rc-exhaustive-status.md) and actually
look at the code for what works, what's missing, and — the part existing
planning docs mostly don't cover — what's implemented but weak. This is a
read-only investigation; nothing here was fixed, and severity/estimates are
not invented where the evidence doesn't support them.

**How to read the labels:** **Done** (implemented + tested, real evidence
cited), **Partial** (real but narrower than it looks, or untested on a
reachable path), **Open** (not meaningfully started), **Blocked** (needs a
decision or source material), **Missing from tracking** (real, found this
pass, not written down anywhere before now — independent of the above
labels, which it also gets one of).

## Combat

**Done**, strongly evidenced. RBA/Force/Cycle-control/Reserve/Custody/
Retreat/Selection deadlines each have fresh-process crash-recovery proof (per
[the RC checklist](2026-10-24-rc-checklist.md#native-combat-reliability)).
The native second-side movement chain (V43 Reserve → V44 one-hex Move → V45
Movement-completed → V46 Breakdown) is fully implemented and wired into
`CampaignHostAdapter`, confirmed by a dedicated investigation earlier this
session — see that session's finding, now also reflected as corrections in
`docs/specs/native-second-movement-v44.md` and the RC checklist. Not
re-verified independently in this pass; taking it as settled.

**Exception-handling hygiene good:** `CampaignDurableOutwardApi.Execute`
(`src/Cna.OrleansHost/Api/CampaignDurableOutwardApi.cs:971-993`) maps every
reachable Core exception type (`UnauthorizedAccessException`,
`JsonException`, `InvalidContentPackException`, `KeyNotFoundException`,
`FormatException`, `InvalidOperationException`, `ArgumentException`,
`OverflowException`) to a specific HTTP status — no silent 500 fallthrough
found for this API surface.

**Open, same as before:** arbitrary crash cuts / real power-loss recovery
(only process-termination kill-points are proved); the broader "integrated
durable Combat flow" gate (one retained genuine history is exercised, not
every reachable combination).

## Movement / ZOC

`CampaignSourceGroupedZoc.CanDeriveForSide` and `Cna1979Zoc` (the rules
layer) are **Done** for the paths exercised by the pinned Theory test and
the rules-unit tests (`tests/Cna.Core.Tests/Rules/ZocRulesTests.cs`). Two
narrower findings:

- **`source-grouped-movement-domain-incomplete`'s root cause is genuinely
  unknown right now** — a different background agent is tracing it as part
  of PR #361; not re-investigated here to avoid duplicate work on the same
  question.
- **Missing from tracking, minor:** `Cna1979Zoc.EvaluateProjection` returns
  `ZocProjectionResult.Unsupported(TopologyFeature)`
  (`src/Cna.Core/Rules/Cna1979Zoc.cs:199-203`) for any hexside feature ID not
  present in the hardcoded `TopologyAuthority` list. This branch is unit
  tested (`ZocRulesTests.cs`), but no evidence was found of a check that
  every hexside feature actually used on the real Graziani map resolves to a
  `TopologyAuthority` entry — i.e., the unit test proves the branch behaves
  correctly when hit, not that it's unreachable on the admitted map. Likely
  fine (the map work this session didn't surface a failure here), but
  nobody has written down that this cross-check happened.

## Supply / Logistics / Trucks

**Partial, and already mostly tracked** — just not all in one place. The
supply/convoy source-world classes (`CampaignSourceAxisSupplyWorld`,
`CampaignSourceCommonwealthSupplyWorld`, `CampaignSourceOpeningConvoyPlan`,
`CampaignSourceSeptemberConvoyPlans`, `CampaignSourceGt2ConvoyCorrection`)
bake explicit `"provisional-*"` policy tags directly into their canonical
serialization — not just comments, actual persisted content:
`"otherMapHexCapacityPolicy": "provisional-three-land-units"`,
`"dummyOmissionPolicy": "provisional-whole-group-optional"`,
`"unallocatedPolicy": "provisional-owner-may-explicitly-finish-with-unused-supply"`,
`"status": "provisional-prestart-planning-not-delivery"`. These are exactly
the open questions the 7 double-entry review packets handed to codex this
session (`2026-11-17-graziani-mvp-gate-01-research-handoff.md`) are meant to
resolve — not a new gap, just confirmation that the code-level stakes of
that handoff are real and already baked into the pinned content hash.

**One specific, well-handled example worth naming directly:** the
Benghazi location-ID conflict (scenario prints `b4827`; the map/location
summary places Benghazi at `a4827`) is a long-standing, explicitly
documented open question (`docs/specs/graziani-source-authority-v4.md:140`,
`docs/specs/graziani-source-control-map.md:100`) — not new. Worth noting
because the code handles it unusually well: `CampaignSourceAxisSupplyWorld`
carries a `benghaziRulingApplied` flag and supports *either* resolution
without rework (`ReceiveGt1BenghaziSupply`,
`src/Cna.Core/Campaigns/CampaignSourceAxisSupplyWorld.cs:116-128`) — this is
a real example of "decision deferred cleanly," not technical debt. Flagging
it only because the exhaustive-status doc's gate table doesn't mention it by
name and a future session might otherwise rediscover it from scratch.

Land-only Air/Logistics abstraction boundary is explicitly scoped (per the
Oct 18 inventory) and not re-investigated here.

## Visibility / Contacts / Fog

Not deeply re-audited this pass beyond what's already tracked — 20 source
files, 21 test files in `tests/Cna.Core.Tests/Observations/`, roughly
proportionate coverage by file count (a weak signal, not a quality claim).
`source-initial-contacts-unavailable`'s root cause is being traced by the
PR #361 agent right now; not duplicated here.

## Host / Journal / replay

**Partial**, with one finding genuinely missing from any tracking doc:

- **No production Orleans clustering provider is configured.**
  `src/Cna.OrleansHost/Program.cs:39-50`: outside `Development`, silo startup
  throws `InvalidOperationException("No production Orleans clustering
  provider is configured...")` rather than silently falling back to
  localhost clustering — deliberate fail-closed design, not a bug. But it
  means this host **cannot run at all** outside a single dev machine today.
  Given the roadmap's own framing (`docs/roadmap/pre-alpha-roadmap.md`) is
  explicitly "local game server," this is very likely **not RC-blocking**
  for a single-machine hot-seat release — but it's real, it's not mentioned
  in the RC checklist or the exhaustive-status doc, and it will matter the
  moment anyone tries to run this Host anywhere but a dev box (a shared
  playtest server, a future cloud deployment). Worth one line in whichever
  doc owns delivery scope (`MVP-GATE-07`) so it isn't rediscovered as a
  surprise later.
- The Orleans journal's O(n²) replay-cost scaling issue was already
  thoroughly investigated this session (separate background agent; not
  re-derived here) — confirmed not safely fixable without real
  architectural investment. Real long-campaign risk, not an RC blocker.

## Source / setup admission

**Partial**, same picture as the exhaustive-status doc already gives — 57
elements admitted across 3 PRs this session, PR #361 doing the integration
pass. Nothing new found in this pass beyond what the other background agent
is already actively working (motor-MG mechanism gap, grouped-ZOC,
initial-contacts) — deliberately not duplicated here.

## Maproom

**Missing from tracking — status is understated in existing docs**, and
**one real, specific, unaddressed requirement gap.**

The RC checklist's `MVP-GATE-05` entry says "Foundation/read preview;
acceptance open." That undersells what's actually there:
`src/Cna.Maproom/projects/maproom-alpha/src/app/` has real application code
— `workspace.store.ts`, `connected-campaign.service.ts`,
`campaign-action-flow.ts`, `campaign-action-draft.ts`, `order-workflow.ts`,
`map-camera.ts`, `operations-map.ts` — plus 6 Playwright e2e specs
(`src/Cna.Maproom/e2e/`: `workspace.spec.ts`, `connected-read.spec.ts`,
`ledger-diary.spec.ts`, `maproom-shortcuts.spec.ts`,
`maproom-workspace-interactions.spec.ts`, `maproom-depth.spec.ts`) covering
ledger filtering/sorting/keyboard access, workspace interactions, and
accessibility (`@axe-core/playwright` is wired into at least the ledger
spec). This is well past "read preview."

**The specific gap:** Sprint 8 of the roadmap requires treating "every local
seat handoff as a privacy boundary: invalidate prior-seat work and isolate
drafts, utterances, parser/worker state, late responses, DOM/live-region
content, and browser storage before the incoming seat can interact"
(`docs/roadmap/pre-alpha-roadmap.md:1136-1138`). Searching the Maproom app
for hot-seat/seat-handoff-specific logic found exactly one relevant hit:
`connected-workspace.ts:150,208` calls `this.actionFlow.clearAudience()` at
two points. That's real, but it's one call clearing one piece of state
("audience") — there's no evidence of anything addressing the other
surfaces the roadmap explicitly names (browser storage, DOM/live-region
content, parser/worker state, late responses), and no test among the 6 e2e
specs is about seat handoff or privacy. (A `SeatHandoffWaitsForAdmittedAction`
test exists in `tests/Cna.OrleansHost.Tests/TemporaryCampaignGrainTests.cs`,
but checked its actual content — it's about serializing *concurrent* grain
access, not UI privacy invalidation; don't conflate the two, they share a
name and nothing else.) This is a real `MVP-GATE-05` requirement with, as
far as this pass found, a single partial implementation and zero dedicated
test coverage — and it isn't called out as its own item anywhere.

## Suite hygiene (not subsystem-specific)

Zero `[Fact(Skip=...)]` / `[Theory(Skip=...)]` found anywhere in `tests/`.
No hidden known-broken tests being silently skipped — a positive signal,
stated plainly rather than only reporting problems.

The two hand-copied-logic duplication findings from the earlier PR #347 code
review (a hand-maintained Cairo neighbor table shadowing an authoritative
map, and a 3-times-hand-copied identification switch) were confirmed
**already fixed** — merged via PR #349. Re-grepped for the same pattern
class-wide (`hand-maintained`, `kept in sync`, `hand-copi`, `shadows the`)
and found zero remaining hits. Not re-flagging; confirming closure instead.

## Next-step candidates (bounded, not "finish subsystem X")

1. **Write a one-paragraph Maproom seat-handoff privacy spec and a single
   Playwright test proving it**: after seat A drafts an action and hands off
   to seat B without submitting, assert seat B's DOM contains no trace of
   seat A's draft text, and that `localStorage`/`sessionStorage` keys
   scoped to seat A's draft are gone. Acceptance: one new, currently-failing
   (or currently-nonexistent-assertion) e2e test that would catch exactly
   the gap described above, passing once real invalidation logic lands.
2. **Add one line to the `MVP-GATE-07` delivery-scope note** (wherever that
   lives — the RC checklist or roadmap) stating plainly that the Orleans
   Host has no production clustering provider configured and cannot run
   outside a single dev machine, so a future reader doesn't rediscover this
   from a production outage.
3. **One grep-and-read pass confirming every hexside feature ID used in the
   admitted Graziani map resolves against `Cna1979Zoc`'s `TopologyAuthority`
   list** — a half-day verification task, not a feature, with a clear
   pass/fail (every feature ID resolves, or it doesn't).

None of these three are urgent relative to the active PR #361 work; they're
offered as the next things worth picking up once that lands.
