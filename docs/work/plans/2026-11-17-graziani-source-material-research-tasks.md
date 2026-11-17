# Graziani source-material research tasks — November 17, 2026 (v2)

**Supersedes:** the earlier same-day draft of this file. That draft didn't
account for findings already recorded in
[2026-11-16-graziani-source-material-research.md](2026-11-16-graziani-source-material-research.md)
(PR #353) and re-listed work as open that's already resolved. This version
only lists what's actually still open.

**Status:** handoff packet for a session with access to the original source
scans. Nothing here requires writing code — each task's deliverable is a
fact (a number, a name, a decision), to be handed back so a future coding
session can turn it into a fact-table entry or a spec decision. Do not
invent a value to fill a gap below; if a task turns out to be unanswerable
from the available material, say so plainly rather than guessing — these
numbers get baked into `ContentHash`/pinned checkpoint contract versions,
so an invented value is worse than a confirmed gap.

## Already resolved — do not re-research

- **6th Australian Division ratings** (was Task 1): fully transcribed —
  OA PDF p.13 + unit-characteristics chart §4.46a (pp.22–23). All 9 infantry
  battalions, both brigade HQ antitank components, and the divisional
  cavalry's recon component have ID code, TOE, CPA, and close-assault
  off/def on record. One trap already flagged: 19th Brigade's HQ is code
  `a`, not `e`/`f` — scenario §60.41 excludes it from the training group, so
  don't give it an antitank component by pattern-matching off the other two
  brigades. This is now an implementation task (add to
  `Cna1979GrazianiSourceFacts.cs`, wire into `CampaignSourceGroupedZoc`,
  test, rerun the readiness diagnostic), not a research one.
- **4th CCNN HQ + both CCNN engineer battalions** (3 of Task 3's 4
  elements): fully transcribed — Axis OA PDF p.18 + chart §4.46b (pp.26–27).
  Note the OA calls the 201st/204th units *battalions*, not companies, and
  their parenthesized TOE is conditional (unusable in a hex that also has
  ammo and non-parenthesized ratings) — preserve that condition, don't
  encode it as an unconditional defense value.
- **`battlefieldComplete` / `controlCertified`** (was Task 4): decided.
  Ruling: both stay `false` until the five-step source-backed playable-map
  work in `docs/specs/graziani-source-control-map.md` is complete. No
  further decision needed; this is a recorded ruling, not an open question.

## Task A — 1st CCNN HQ weapon identification

**What's needed:** `1-ccnn-hq`'s maximum TOE is "1 artillery point" per
Axis OA p.17, but the OA lists its weapon as unidentified code `N` — not
yet resolved to a real weapon type or combat rating. Already checked: do
not borrow the 65/17 gun from the CCNN Legion HQs — confirmed not the same
unit type, don't reuse that value here.

**Where to look:** the Axis OA chart PDF's weapon-code legend/key (same
chart section as the TOE table on pp.26–27) and the September 1979 errata
PDF for any errata note on code `N`. The temp copy of the Axis chart used
for the Nov 16 research
(`/private/tmp/sandtable-axis-charts.pdf`, SHA-256
`9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca`) has
since been cleaned up from that path — relocate from the original source
drop and verify the hash before trusting a replacement copy. The errata PDF
(`/private/tmp/sandtable-cna-errata.pdf`, SHA-256
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`) is
still at that path as of this writing.

**Deliverable:** the real weapon/gun type for `1-ccnn-hq`'s artillery
point, with a page/table citation, in the same shape as the other CCNN
facts already in the Nov 16 research doc.

## Task B — Matruh garrison: real combat ratings for all 5 candidate units

**What's needed:** identity and assignment conflicts for the Matruh
garrison are now understood (see the Nov 16 doc's §2), but none of the 5
candidate units — `1-essex`, `1-durham-light-infantry`,
`1-south-staffordshires`, `1-buffs`, `1-hampshire` — have real,
distinguishing combat facts. `CampaignSourceMatruhFormation.cs` currently
hardcodes identical placeholder values (TOE 6, CPA 10) for every member
regardless of identity; that's inherited from PR #345 (Nov 14) and predates
any of this research, not a considered value.

**Where to look:** the Commonwealth OA PDF
(`/private/tmp/sandtable-cw-charts.pdf`, SHA-256
`f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981`, still
present) for each unit's ID code, and the unit-characteristics chart
§4.46a (same PDF, pp.22–23) for TOE/CPA/close-assault values — same method
and same chart section already used for the 6th Australian Division above.

**Deliverable:** ID code, TOE, CPA, and close-assault off/def for each of
the 5 units, with citation, ready to replace the placeholder values in
`CampaignSourceMatruhFormation.cs`.

## Task D — Aresca Tank Regiment's CV33 close-assault rating

**New (2026-11-17, discovered while tracing `source-grouped-movement-domain-incomplete`):**
`CampaignSourceGroupedZoc.TryDefensiveCloseAssaultRating`'s hand-maintained
rating table has zero entry for `aresca-tank-regiment`'s CV33 light tank
component. This is one of three formations found missing from that same
table — the other two are the 6th Australian Division and the Matruh
Garrison, both already covered by Task A/B above once their facts land.
This is the one of the three with no existing research queued anywhere.

**Where to look:** the Axis OA chart's Aresca/tank-regiment entry (same
general area as the other Axis armor citations — check alongside the
existing `oa-7-armored-p6`-style citations for the matching Italian-side
page) and the unit-characteristics chart for the CV33's close-assault
off/def values specifically — this is a light tank, so check whether it's
even eligible for a close-assault rating at all per the source's own
vehicle-classification rules, rather than assuming it must have one.

**Deliverable:** either a real close-assault off/def rating with citation,
or a sourced statement that CV33 light tanks are not assigned a
close-assault rating at all (if that's what the source actually says) —
either answer resolves this gap, a guess does not.

## Task E — Giarabub motor-machine-gun: double-check the "no pool" finding

**New (2026-11-17).** This is a verification task, not a fresh lookup — the
codebase already states a conclusion
(`src/Cna.Core/Content/ContentSourceGiarabubMotorMachineGunArtifact.cs:6-7,43`):
"Source's 8+ marks historical motorization; the scenario does not identify
an assigned motor pool or an effective current allowance" —
`historicalMotorizationPriority` is recorded as `"unresolved-no-scenario-pool"`,
and `mobilityId`/`motorPoolId`/`assignedMotorPoints` are all `null` by design.
This predates this session's research effort entirely (from commit `21c6cba7`)
and was never covered by the Nov 16 research doc's citation list.

**What's needed:** confirm this conclusion still holds, rather than build
anything on an unchecked finding from a prior session. The element's own
citation list is: `scenario:59.63a,60.31`; `axis-charts:oa-giarabub-p14,4.46b`;
`air:counter-section-2-p40`; `land:4.23,9.16a,32.54`.

**Where to look:** re-check those exact citations, specifically for any
motor-pool assignment or mobility classification for the Giarabub motor-MG
element that the original pass might have missed — the `air:` and `land:`
citations in particular (counter-section-2-p40, 9.16a, 32.54) weren't the
primary focus of any research doc so far and are worth a fresh look.

**Deliverable:** either a real motor-pool ID / mobility classification /
capability-point value with citation (if one genuinely exists and was
missed), or an explicit confirmation that the "unresolved-no-scenario-pool"
finding is correct and the source genuinely provides nothing further — both
are useful answers. A ruling action to formally accept the "unresolved"
state (mirroring the Central India Horse / `battlefieldComplete` pattern) is
being built in parallel and isn't blocked on this task's outcome — if real
data turns up here, that becomes a follow-up fix to the ruling's accepted
value, not wasted work either way.

## Task C — Matruh: Essex/Durham assigned-parent chronology ruling

This one's a decision, not a fact-lookup — flagged but not settled in the
Nov 16 doc.

**What's needed:** the scenario text attaches Essex and Durham to Matruh
immediately (D3714 / §60.41); Commonwealth OA note `f` says they weren't
**assigned** to 23rd Brigade until June 1941 (3/38). Attachment and
assignment are distinct concepts in this engine's model — does the
pre-June-1941 assignment gap affect either unit's eligibility or behavior
at Matruh, or does the scenario's explicit attachment stand regardless of
assigned-parent timing?

**Where to look:** the scenario booklet's general attachment/assignment
rules (not just §60.41 itself), and OA note `f`'s exact wording — worth
checking whether any other scenario attachment predates its unit's formal
assignment elsewhere, as precedent for how this engine's source material
usually treats the gap.

**Deliverable:** a ruling — either "attachment stands regardless of
assignment timing" with a supporting citation, or a scoped exception —
precise enough to resolve the `scenario-23-70-vs-oa-note-f-conflict` tag
already sitting on both units in `CampaignSourceMatruhFormation.cs`.

## Handoff notes

- Tasks A, B, D, and E share chart-lookup method with the now-finished
  Australian ratings work — if those chart pages are already open, cheap
  to batch with Task A/B/D/E's lookups.
- Task C is a judgment call on text already in hand (scenario rules + OA
  note `f`'s wording) — no new scan should be needed.
- None of A/B/C/D/E block each other; hand off in any order.
- **Tasks A, B, and D together are the real path to closing
  `source-grouped-movement-domain-incomplete`**, per PR #361's traced
  root cause (2026-11-17): `CanDeriveForSide` fails for both sides purely
  because `TryDefensiveCloseAssaultRating` has no entry for the 6th
  Australian Division, the Matruh Garrison, or the Aresca Tank Regiment's
  CV33. Once B's Matruh ratings and D's Aresca rating land (A's CCNN
  weapon ID doesn't feed this particular blocker — that one's purely
  `ccnn-characteristics-incomplete`), re-run the integration fixture in
  `GrazianiSourceAuthorityTests.cs`
  (`IntegratingEveryAdmittedClusterBeforeOwnerFinalizationRevealsRealRemainingBlockers`)
  to confirm this blocker actually clears — it depends on all three
  formations getting real ratings, not just one.
- Once any of these land, the next code step is the same as before:
  add the fact(s)/ruling to the relevant file and rerun the readiness
  diagnostic (`docs/work/plans/2026-11-16-graziani-readiness-and-combat-plan.md`
  §1) to confirm what actually moves.
