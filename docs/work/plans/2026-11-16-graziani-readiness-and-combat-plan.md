# Graziani readiness and combat-obligation plan — November 16, 2026

**Status:** working-tree packet; nothing committed. This note consolidates and
supersedes the session notes previously kept outside the repository at
`side-session-notes/local_1678fba7-c0de-43e2-9884-dbbfe10a5e8e/2026-11-16-grouped-zoc-progress.md`
(that file should be treated as superseded by this one; its content is folded
in below). It is written for a future planning/architecting session, not as a
record of further fixes applied in this one — no further code changes were
made after this document was requested.

**Source research update:** [Graziani source-material research handoff](2026-11-16-graziani-source-material-research.md) supersedes this session's missing-source and Matruh identity assumptions. Australian ratings and partial CCNN characteristics are now transcribed; scenario lists both Essex and Durham. Research findings are not yet implemented, and the blocker diagnostic below has not been rerun.

**Map and setup review:** [seam, supply and truck recheck](2026-11-16-graziani-seam-supply-review.md) records the 11 printed distance discrepancies and source-row comparisons. No new seam is certified; blind independent setup review remains open.

## What changed this session (for context)

`CampaignSourcePlayableWorld.ReadinessBlockers()` previously added three of
its blockers unconditionally, regardless of state — `Start()` could never
succeed. This session built real derivation for those three:

- A new module, `CampaignSourceGroupedZoc` (`src/Cna.Core/Campaigns/
  CampaignSourceGroupedZoc.cs`), derives Zones of Control over the grouped
  Graziani battlefield: per-formation defensive-rating facts (sourced, see
  §4), a sound lower-bound reasoning trick for partially-known formations,
  per-hex stacking/defense aggregation, and real neighbor projection over the
  actual 7073-hex historical map (`Cna1979HistoricalMap`, extended with a
  `NeighborsOf` method) plus the three admitted C/D seam crossings.
- `Cna1979HistoricalTerrainEffects` (`src/Cna.Core/Rules/`) is a new,
  SPI-8.37-sourced terrain-enterability table for the historical map's
  terrain vocabulary.
- `CampaignSourcePlayableWorld.ReadinessBlockers()` now computes
  `source-grouped-movement-domain-incomplete`, `source-movement-map-
  topology-incomplete` and `source-cross-map-seams-unresolved` for real.
  These three are confirmed resolved at the most-advanced real fixture (see
  §1).
- 61 new/affected tests pass (`CampaignSourceGroupedZocTests`: 9;
  `GrazianiSourceAuthorityTests` class: 67 repeatedly; full `Cna.Core.Tests`
  solution: 3,915/3,915 in one full run this session). All regressions are
  clean as of this document.

Everything below is the state as of the end of this session, organized for
someone picking this up cold.

## 1. The eight current `ReadinessBlockers()` at the real fixture

Checked at `motorizedWorld` in `tests/Cna.Core.Tests/Campaigns/
GrazianiSourceAuthorityTests.cs`'s `AxisFinalizesRealSupplyPlanAndReplaysWithoutStartingGame`
test (the single most-advanced real-data fixture in the repository — 7th
Armored, 16th Brigade, Catanzaro/CCNN, 6th Australian, Matruh, Maletti,
Libyan and Italian (Cirene/Marmarica) all admitted, 4th Indian fully
motorized, `mapIdentityBound: true, cdSeamBound: true`). Method used: a
temporary `Assert.Fail("DIAG:" + string.Join(",", motorizedWorld
.ReadinessBlockers()))` inserted right after that variable is built, run with
`just test-method core '*AxisFinalizesRealSupplyPlanAndReplaysWithoutStartingGame*'`,
then reverted — `Assert.Equal`/xunit's own diff view truncates long strings
even when not run through a shell, so a plain failure message is the only way
to see the whole list. Full current result:

```
battlefield-incomplete, control-incomplete,
seventh-armored-shell-stack-correction-required, ccnn-characteristics-incomplete,
central-india-horse-training-state-unresolved, owner-setup-finalization-incomplete,
source-grouped-movement-domain-incomplete, forces-incomplete
```

`map-incomplete`, `mobility-incomplete`, `source-movement-map-topology-
incomplete` and `source-cross-map-seams-unresolved` are **not** in this list —
confirmed resolved by this session's fix.

### 1a. `battlefield-incomplete` / `control-incomplete`

- **Status:** permanently false today, by design (not a bug left for a future
  session to "just fix"). `CampaignSourceBattlefield.SerializeCanonical()`
  (`src/Cna.Core/Campaigns/CampaignSourceBattlefield.cs:1296-1297`) writes
  `battlefieldComplete`/`controlCertified` as fixed `false` constants.
- **Root cause / evidence it is intentional, not an oversight:**
  - `git log --follow` on that file shows exactly one commit (`efad95e6`,
    "Add source-backed Graziani setup and durable replay (#345)"); both
    flags were `false` from that first commit, with no later change and no
    commit-message discussion.
  - `docs/specs/graziani-source-forces-v5.md:134`: "Source-evidence entries
    are not treated as combat-ready or proof of control; the battlefield and
    control status remain explicitly incomplete" (written at the earliest
    battlefield version, 10–11).
  - `docs/specs/graziani-source-forces-v5.md:596-597` (decisive): "Start
    rejects incomplete battlefield, control, topology, mobility, force and
    owner setup evidence. **The current historical source world does not
    pass this gate.**" — a prior session's own explicit statement that the
    real campaign is not expected to pass `Start()` yet, enumerating six
    categories that map 1:1 onto six of the eight blockers above.
  - `docs/specs/graziani-source-control-map.md` (the design doc this
    session's ZOC work independently matches almost line-for-line) step 4:
    "Remove the readiness blockers in `CampaignSourcePlayableWorld` only when
    their stated domains are supported, not when a small excerpt has been
    added." It never defines `battlefieldComplete`/`controlCertified` by
    name or gives a computation for them.
  - Decisive: `Assert.False(...battlefieldComplete...)` /
    `Assert.False(...controlCertified...)` are **existing, passing**
    assertions in both `tests/Cna.Core.Tests/Campaigns/
    GrazianiSourceAuthorityTests.cs:683-684` and `tests/Cna.OrleansHost.Tests/
    GrazianiPregameJournalTests.cs:893-894`. These lock in today's `false` as
    the intended, tested value — not an unchecked placeholder.
  - Two attempts this session to infer a real computation from the five
    evidence-to-active promotion receipts `CampaignSourceBattlefield` tracks
    (Cairo setup + four `*OriginHash` fields set by `ActivateCwFrontline`/
    `PromoteAxisFreeArtillery`/`PromoteAxisCorpsArtillery`/
    `PromoteMatruhDetached`/`PromoteCwFreeGuns`) both broke the `:683-684`
    assertion — all five are already true long before that assertion
    expects `battlefield-incomplete` to still hold. Whatever gates it is not
    those five receipts.
- **What's needed:** the actual historical map/order-of-battle completeness
  work `graziani-source-control-map.md` lays out in full (§"Shortest route to
  a source-backed playable map", 5 steps: freeze/transcribe source scope,
  resolve map identity and completeness, publish versioned content, connect
  actual play, validate admission and replay). This is a multi-week research
  project (reading original scans, resolving all cross-section seams, full
  OB inventory), not a code fix.
- **Research disposition:** keep both flags false until the five-step map
  completeness work above is complete. This is a conservative planning ruling,
  not an implemented certification predicate. Define distinct, fail-closed
  coverage and control contracts during that work; do not infer them from the
  existing promotion receipts. See the [research handoff](2026-11-16-graziani-source-material-research.md#4-battlefield-and-control-readiness).

### 1b. `seventh-armored-shell-stack-correction-required` / `owner-setup-finalization-incomplete` / `central-india-horse-training-state-unresolved`

- **Status:** believed **not** real blockers at full setup completion —
  each has a known, already-implemented resolution action that this
  particular test simply hasn't called yet at the `motorizedWorld` checkpoint
  (the test continues past this point and calls `CorrectSourceSeventhShell`
  and owner-finalization actions later in its own flow; see
  `GrazianiHorseSetupTests.RealSetupOwnerChoicesArePrivateReversibleAndReplayable`
  for the horse-training resolution path).
- **What's needed:** verification against a fully-complete setup run — take
  the same fixture further (call `CorrectSourceSeventhShell`, both sides'
  owner-finalization, and the horse-training-stage-one decline/resolve
  action) and re-run `ReadinessBlockers()` to confirm these three actually
  drop out. Not done this session — flagged as "believed," not confirmed.
- **Who decides:** a future session — this is pure verification, no design
  question.

### 1c. `ccnn-characteristics-incomplete`

- **Status:** pre-existing real gap, unrelated to this session's work.
  `CampaignSourcePlayableWorld.ReadinessBlockers()` checks CCNN batch members'
  `CapabilityPointAllowance`/`ComponentType` directly; this was not touched.
- **Research update:** Italian chart rows for both HQs and engineer battalions
  are transcribed in the [research handoff](2026-11-16-graziani-source-material-research.md#3-ccnn-headquarters-and-engineers).
  The 1st HQ gun type remains unspecified and parenthesized ratings require
  conditional handling. Implement only supported facts, then rerun the blocker
  diagnostic; these findings do not establish that the blocker will clear.

### 1d. `forces-incomplete`

- **Status:** likely a fixture-branch completeness issue, not a real gap at
  full setup. The `AxisFinalizesRealSupplyPlanAndReplaysWithoutStartingGame`
  test explores many branches (its own variable names like `noMaletti`,
  `matruhBeforeDetach`, `matruhAfterDetach` show it testing orderings and
  rejections, not one single linear path to "everything admitted"); the
  specific branch reaching `fullyMotorized` may not include every possible
  grouped formation (e.g. Aresca — `CampaignSourceArescaGroupedDefinition` —
  is referenced throughout the codebase's hash-selection logic but its
  admission was not traced in this session).
- **What's needed:** checking which formations are actually represented vs.
  defined-but-unadmitted at this fixture point, and whether a fully-complete
  setup run (every group admitted) clears this blocker.
- **Who decides:** a future session — verification, not a design question.

### 1e. `source-grouped-movement-domain-incomplete`

- **Status:** real, honest signal from this session's own new code
  (`CampaignSourceGroupedZoc.CanDeriveForSide`) — not yet isolated to a single
  cause. By construction it fails closed on the *first* unresolved thing it
  finds for either side, so there could be more than one cause stacked; this
  session did not narrow it down further.
- **Root cause (prime suspects, not confirmed):**
  - **6th Australian Division** — source ratings are now transcribed in the
    [research handoff](2026-11-16-graziani-source-material-research.md#1-6th-australian-division).
    They have not yet been added to the fact table or tested at this fixture.
    OA citation `p13` means absolute PDF page 13; no offset calculation is needed.
  - **Independent (ungrouped) combat-classified elements** elsewhere on the
    battlefield — `CanDeriveForSide` deliberately fails on any of these too
    (this module does not yet derive ZOC for independent elements at all;
    see §4 and the architecture note in `CampaignSourceGroupedZoc.cs`'s own
    comments). Candidates not ruled out: CW frontline units
    (`ContentSourceCwFrontlineArtifact`), Cairo garrison units, minor Axis
    garrisons. Not isolated which, if any, are still independent at this
    exact fixture point.
- **What's needed:** instrument `CanDeriveForSide` (or just call
  `EvaluateFormationSource`/`EvaluateDetachedSource` per group/detached
  member directly, plus enumerate the independent-representation loop) at the
  `motorizedWorld` fixture to print exactly which group/element fails first,
  for both sides. This is the single most concrete, bounded next step in the
  whole readiness line of work — more useful than guessing further.
- **Next:** isolate each failing element, then implement applicable sourced
  ratings and separately account for independent elements. Re-run the diagnostic;
  no source finding alone proves this blocker clears.

## 2. Compulsory-combat obligation ledger — design sketch (not started)

Per the original brief: "Track compulsory-combat obligations by enemy source
hex and prevent combat completion until each is attacked, exempted, or
resolved under the applicable rules." Nothing has been built for this. What
exists that a future session should build on:

- **Rule text** (already researched this session, not re-derived from
  scratch): `/private/tmp/sandtable-cna-map-source/rules/60-combat.md:22-24`
  — "Combat is compulsory, not optional: once every move and reaction of the
  segment is over, the phasing player must attack every enemy hex that
  projects a zone of control onto a friendly unit, and may attack any other
  adjacent enemy hex the terrain allows." Exemptions and the holding-off
  alternative are in `/private/tmp/sandtable-cna-map-source/rules/
  50-stacking-and-zoc.md:241-273` ("ZOC combat requirement (holding off)"):
  exempt hexes are those whose friendly contents are only artillery,
  anti-tank, AA or non-combat units, or whose other units are pinned; the
  attack may be a holding-off barrage meeting a computed strength threshold,
  or close assault with no minimum; a unit that can do neither must retreat
  three hexes (R-118) or the whole stack surrenders if the retreat route is
  itself blocked by enemy ZOC.
- **The hard part is already built**: `CampaignSourceGroupedZoc
  .TryDeriveControlledLocationIds(world, enemySideId)` (added this session)
  returns the exact set of hexes the *enemy* side controls via ZOC, derived
  from real per-hex stacking/defense aggregation and real historical-map
  neighbor projection. Calling it for the enemy side and intersecting with
  hexes containing the *phasing* player's own units gives exactly the
  "which of my hexes are enemy-ZOC'd and therefore must be attacked" set the
  rule requires. This is new, tested (§"Verified" in the prior session notes,
  folded into this document) infrastructure that did not exist before this
  session — the ledger itself is the only remaining piece.
- **What the ledger itself needs** (not designed in code, sketch only):
  - A per-source-hex obligation record keyed by (owning/phasing side, enemy
    hex location, game turn, operation stage) — created once at the start of
    the combat segment from the controlled-hex intersection above, cleared
    only by: an attack being resolved against that hex (barrage meeting the
    holding-off threshold, or close assault — the combat resolution engine
    itself is out of scope here and elsewhere in this codebase), an
    exemption applying (hex's friendly contents are only artillery/
    anti-tank/AA/non-combat, or pinned), or the owning stack retreating per
    R-118.
  - A check gating combat-segment completion: the segment cannot be marked
    complete while any obligation record for the phasing side remains
    neither attacked nor exempted nor resolved-by-retreat.
  - This belongs in the same family as `CampaignSourceTurnAuthority`'s
    other turn-position transitions (see §3) — likely a new field on that
    class tracking open obligations, created when entering the combat
    segment and checked before leaving it, journaled the same way other
    source-turn state is.
  - Scope explicitly excludes: barrage/anti-armor/close-assault resolution
    mechanics themselves (a separate, much larger system — `60-combat.md` in
    full is hundreds of lines covering gun positions, forward/back, assault
    assignment, anti-armor fire, casualties). The ledger only needs to know
    *whether* an attack against a given hex happened and was exempted or
    not, not resolve it.
- **Who decides:** a future session can design and implement the ledger
  itself from this sketch; no open historical/rules question blocks it (the
  compulsory-combat rule text and exemptions are already settled and cited
  above).

## 3. GT1 progression and the Polish arrival through durable Host replay

What's actually proven today, versus what's only exercised through
reflection or manually-constructed state, versus the normal admitted-campaign
path end to end:

- **Proven, real, already wired (no further work needed here):**
  - Disclosure journaling for the two existing bounded transitions (the 4th
    Indian opening move and the Polish arrival) is real and wired on both
    the live-submission path and the cold-replay/reconstruction path:
    `CampaignContactJournal.AppendSourceMovement`/
    `AppendSourceTurnStart`/`AppendSourcePolishArrival`
    (`src/Cna.Core/Visibility/CampaignContactJournal.cs`) are called from
    `CampaignExercises.cs` at both the submission call sites (around lines
    350, 523-529, 560) and the replay/reconstruction call sites (around
    lines 6938, 7011-7016, 7202-7206).
  - Orleans Host wiring for *every* `CampaignSourceTurnAction` — including
    movement and the Polish arrival, not just the ~20 parameterless system
    steps that have dedicated `POST /source-turn/xxx` routes — already
    exists via one generic mechanism: `CampaignExercises.Submit`
    (`CampaignExercises.cs:4072`) dispatches every source-turn
    phase/segment by position, including `LandSegmentIds.Movement =>
    ...SubmitSourceFourthIndianOpeningMove(...)` and the default case
    `_ => SubmitSourcePolishArrival(...)`. This is called by
    `CampaignHostAdapter.Submit`, called by the Orleans Host's generic
    `POST /{campaignId}/actions` endpoint (`CampaignOwnerOpeningKind
    .SourceActions`, the `"source-actions"` opening stage), backed by
    `CampaignHostAdapter.MatchesIssuedSourceAction` (line 91), which already
    reconstructs and validates the `"fourth-indian-opening-move"` and
    `"polish-arrival"` choice families by name. (This session initially
    misjudged this as a missing Host-wiring gap by grepping only for the
    dedicated-route pattern; that was corrected mid-session — the generic
    path already covers it.)
- **Exercised only via reflection or manually-constructed state (not
  normal-path proof):**
  - `GrazianiSourceAuthorityTests.cs`'s own comment at the relevant point:
    "Test the wire contract on the retained historical world without
    claiming that the still-gated campaign can start yet." It constructs
    `CampaignSourceTurnAuthority` via reflection (bypassing `Start()`'s
    `IsGameplayReady` check entirely) specifically because `Start()` cannot
    succeed normally. Every GT1-stage/Polish-arrival exercise in that test
    file uses this reflection-constructed authority, not a normally-admitted
    one.
  - No Orleans Host test (`GrazianiPregameJournalTests.cs`,
    `GrazianiHostStressTests.cs`) exercises movement or the Polish arrival
    through `journal.SubmitSourceActions` — they only reach
    `AdmitSourceFourthIndianWorld` (the setup/grouping step) at the Host
    layer. This is because no test gets far enough (`Start()` blocked), not
    because the Host wiring is missing (see above) — but it does mean the
    generic-dispatch path for these two *specific* actions, while
    structurally present and used for many *other* actions, is not directly
    exercised by any existing test either.
- **Not proven at all, normal admitted-campaign path end to end:** nothing —
  because `Start()` cannot succeed for the real full roster today (§1). This
  is the single blocking prerequisite for proving GT1 progression and the
  Polish arrival "for real." Once §1's blockers are cleared (or at least
  `source-grouped-movement-domain-incomplete`, `battlefield-incomplete` and
  `control-incomplete`), the mechanism to prove it end to end already exists
  and needs no new code: call `CampaignSourceTurnAuthority.Start()` normally,
  drive it through the normal `CampaignExercises.Submit`/Orleans-Host
  `SubmitSourceActions` path to the Polish arrival, and add a Host-level test
  exercising exactly that (cold restart + replay comparison), mirroring the
  pattern `GrazianiPregameJournalTests.cs` already uses for setup actions.

## 4. Grouped-ZOC defensive-rating fact table

`CampaignSourceGroupedZoc.TryDefensiveCloseAssaultRating`
(`src/Cna.Core/Campaigns/CampaignSourceGroupedZoc.cs`) is scoped, per-formation,
per-component, to chart cells with a real citation already present and
verified elsewhere in this codebase (reused from `Cna1979GrazianiSourceFacts
.cs`'s `InfantryFacts`/`AxisInfantryFacts`/`InitialComponentFacts`/
`AxisRegimentalHeadquartersFacts` calls for the same physical units'
independent-track combat facts — no PDF was read and no new number was
invented for any entry below).

**Sourced (9 formations):**

| Formation | Group ID | What's covered | Citation |
|---|---|---|---|
| 4th Indian Division | `4-indian-division` | 6 rifle battalions (TOE6/DR2), Central India Horse (TOE6/DR2, infantry code 'l' ruling), 25th Field Artillery's howitzer (TOE6/DR0, known zero) | `4.46a` + `oa-indian-p15` |
| 16th Infantry Brigade | `16-infantry-brigade` | 3 rifle battalions (TOE6/DR2) | `4.46a` + `oa-70-infantry-p9` |
| 7th Armored Division | `7-armored-division` | 6-RTR/7th Queen's Own Hussars Mark VI light (TOE10/DR2), 8th Hussars A10 Cruiser (TOE10/DR3) | `oa-7-armored-p6` |
| Catanzaro Division | `catanzaro-division` | 141st/142nd regiments' 6 rifle companies (TOE7/DR1), 64th machine-gun company (TOE2/DR7) | `4.46b` + `oa-catanzaro-p10` |
| 1st CCNN Division | `1-ccnn-division` | 6 rifle battalions (TOE5/DR1), 201st machine-gun company (TOE2/DR7) | `4.46b` + `oa-ccnn-p17` |
| 4th CCNN Division | `4-ccnn-division` | 6 rifle battalions (TOE5/DR1), 204th machine-gun company (TOE2/DR7) | `4.46b` + `oa-ccnn-p18` |
| Cirene Division | `cirene-division` | 157th/158th regiments' 6 rifle companies (TOE7/DR1), 63rd machine-gun company (TOE2/DR7) | `4.46b` + `oa-cirene-p4` (September errata) |
| Marmarica Division | `marmarica-division` | 115th/116th regiments' 6 rifle companies (TOE7/DR1), 62nd machine-gun company (TOE2/DR7) | `4.46b` + `oa-marmarica-p4` (September errata) |
| 1st & 2nd Libyan Divisions | `1-libyan-division`, `2-libyan-division` | 12 rifle battalions total (TOE5/DR1) | `4.46b` + `oa-libyan-p5` |
| Maletti Division | `maletti-division` | 5 Libyan-coded motor-infantry/infantry attachments (TOE5/DR1, same rating as the parent Libyan division's own battalions) | `4.46b` + `oa-maletti-p7` |

**Subsequent source research, not yet implemented:**

- **6th Australian Division:** infantry `p` TOE 6 / DR 2; 16th and 17th
  brigade HQs `f` and `e`, each TOE 1 / DR 1; cavalry `ll` TOE 8 / DR 3.
  See [full citations and qualifications](2026-11-16-graziani-source-material-research.md#1-6th-australian-division).
- **Matruh:** scenario explicitly lists both Essex and Durham as attached,
  alongside South Staffordshires. OA note `f` raises a separate assigned-parent
  chronology issue; errata separately assigns both Buffs and Hampshires to
  Matruh. See [source comparison](2026-11-16-graziani-source-material-research.md#2-matruh-identity-and-relationships).
  Do not conflate assignment, attachment, or source conflict with identity.
- **Independent combat-classified elements** remain outside grouped ZOC
  derivation; `CanDeriveForSide` fails closed for them. Isolate their effect at
  the fixture rather than assuming the Australian facts clear the blocker.

## Recommended path, in order

1. **Isolate `source-grouped-movement-domain-incomplete`'s exact cause(s)**
   at the real fixture (§1e). This shows whether the now-sourced Australian
   ratings or other independent elements are actual blockers at that checkpoint.
2. **Verify §1b's three "believed resolved later" blockers** against a
   fully-complete setup run. Also bounded and mechanical.
3. **In parallel or after**, scope `forces-incomplete` (§1d) — likely the
   same "take the fixture further" exercise as #2.
4. **Build the compulsory-combat obligation ledger** (§2) — the hard
   prerequisite (grouped ZOC over the real map) is now built and tested; the
   ledger itself has no open rules question blocking it.
5. **Once `Start()` succeeds** (after #1–#3, or sooner if §1a turns out not
   to gate the specific fixture a future session cares about — confirm this
   per-fixture, don't assume), add the Host-level end-to-end test for GT1
   progression and the Polish arrival described in §3's last paragraph.
6. **Use the source research packet** for Australian ratings and partial CCNN
   facts; preserve the unresolved Matruh relationship and 1st CCNN HQ gun
   type. Keep both map/control flags false pending the five-step completeness
   work, and define their checks with that work.

## Remaining source and design questions

- Determine the 1st CCNN division HQ's artillery type from authoritative source
  evidence; do not infer it from legion HQs. Preserve conditional engineer and
  4th HQ ratings.
- Resolve Essex/Durham's initial assigned-parent chronology and the Matruh
  assignment/attachment interactions, including the provisional Buffs/Hampshire
  errata exception, without dropping any scenario-listed units.
- Define versioned battlefield coverage and territorial-control certification
  predicates as part of the five-step map project. Until then both flags remain
  false. Full evidence and implementation cautions: [research handoff](2026-11-16-graziani-source-material-research.md).
