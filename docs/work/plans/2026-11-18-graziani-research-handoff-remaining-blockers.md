# Graziani research handoff — historical priority snapshot (November 18, 2026)

**Subsequent research:** [Focused source findings](2026-11-17-graziani-implementation-blockers-research.md)
confirm Aresca's CV33 close-assault rating as **1 offensive / 2 defensive per
TOE point**. They retain CCNN's weapon and Giarabub's specific initial motor
pool as bounded unknowns, with separate HQ participation and motorization
qualifications. These findings have not been admitted into runtime by this
research pass; the status below describes the earlier handoff.

**Historical purpose:** a focused, standalone handoff for the research believed to block
`CampaignSourcePlayableWorld.ReadinessBlockers()` from going fully clean. Everything else
from the Nov 16/17 research batches (Matruh, 6th Australian, the large Nov 17 map-geometry
batch) is either already landed in code or tracked separately and does not block this list.
The original request grouped two runtime blocker topics with a third verification task;
current source findings and remaining qualifications are in the linked synthesis above.
Do not use this historical snapshot to infer current runtime readiness.

**Historical status at handoff:** none of the 3 items below had moved. Task A was re-attempted
in the Nov 17 batch and reconfirmed unresolved: a counter-sheet check located the 1st CCNN HQ
but did not identify its weapon model or gun TOE. See the direct source-check links under Task A
below for that historical result and the synthesis above for subsequent findings. Tasks D and E
were not attempted at all in that batch — they don't appear anywhere
in its 361 files.

**Ground rule, unchanged from every prior handoff:** don't invent a value. A confirmed "the
source doesn't say" is a complete, useful answer. A guess is worse than a gap, because these
numbers get baked into pinned `ContentHash` values.

## Source files — historical availability snapshot

These were cited as possibly-cleaned-up temp paths in earlier handoffs. As of 2026-11-18 they
are present on this machine with hashes matching every prior citation exactly — confirmed by
`shasum -a 256` just now, not assumed:

| File | Path | SHA-256 |
| --- | --- | --- |
| Axis OA chart | `/private/tmp/sandtable-axis-charts.pdf` | `9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca` |
| Commonwealth OA chart | `/private/tmp/sandtable-cw-charts.pdf` | `f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981` |
| September 1979 errata | `/private/tmp/sandtable-cna-errata.pdf` | `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb` |

If a research pass runs in a different environment where these have actually been cleaned up,
re-locate from the original source drop and re-verify these exact hashes before trusting a
replacement copy — a file with the same name but a different hash is not the same evidence.

## Item 1 — 1st CCNN HQ weapon identification (`ccnn-characteristics-incomplete`)

**Current finding:** Axis OA `N` is maximum-strength notation, not a weapon code. The HQ
maximum permits one artillery TOE point, but its weapon model and initial presence remain
unknown. See the [focused findings](2026-11-17-graziani-implementation-blockers-research.md);
do not infer an assigned gun or copy a neighboring HQ's weapon.

**Already ruled out, don't repeat:**
- The 65/17 gun from the CCNN Legion HQs — confirmed not the same unit type.
- A second bounded public-source search beyond the OA/errata pair above (see
  [`2026-11-17-graziani-ccnn-additional-source.md`](2026-11-17-graziani-ccnn-additional-source.md)
  and its [independent review](2026-11-17-graziani-ccnn-additional-source-review.md)) — came up
  empty.
- A reacquired counter-sheet check (see
  [`2026-11-17-graziani-ccnn-counter-next.md`](2026-11-17-graziani-ccnn-counter-next.md) and
  [review](2026-11-17-graziani-ccnn-counter-review.md)) — locates the physical 1st CCNN HQ
  counter but finds no weapon model or gun TOE printed on it; the `(5)` visible there is
  stacking notation, not a weapon code.

**Remaining evidence:** a provenance-identified authoritative correction or other source
establishing the gun model and starting assignment. The OA, checked errata, counter and
bounded public searches did not settle those facts; this is not an exhaustive claim about
all sources. Unknown remains a complete source result.

## Item 2 — Aresca Tank Regiment's CV33 close-assault rating (`source-grouped-movement-domain-incomplete`)

**Historical request and status below:** the [focused findings](2026-11-17-graziani-implementation-blockers-research.md) now establish the CV33 rating, with separate HQ eligibility qualifications. Runtime admission remains a coding decision.

**What's needed:** `CampaignSourceGroupedZoc.TryDefensiveCloseAssaultRating`'s rating table has
zero entry for `aresca-tank-regiment`'s CV33 light tank component. This is now the single
remaining cause of `source-grouped-movement-domain-incomplete` — the Matruh Garrison and 6th
Australian Division gaps that used to share this blocker were both resolved and landed in code
(PRs #378, #381).

**Where to look:** the Axis OA chart's Aresca/tank-regiment entry — check alongside the
existing `oa-7-armored-p6`-style citations already in `CampaignSourceGroupedZoc.cs` for the
matching Italian-side armor page — and the unit-characteristics chart for CV33's close-assault
off/def values specifically.

**Important: check eligibility before assuming a value exists.** CV33 is a light tank. Check
whether the source's own vehicle-classification rules even assign light tanks a close-assault
rating at all, rather than assuming every unit type has one. If light tanks are categorically
excluded from close-assault ratings per the source's own rules, that is itself the answer.

**Deliverable:** either a real close-assault off/def rating with citation, **or** a sourced
statement that CV33 light tanks are not assigned a close-assault rating at all (if that's what
the source's vehicle-classification rules actually say). Either answer resolves this gap.

## Item 3 — Giarabub motor-machine-gun: double-check the "no pool" finding (not currently a `ReadinessBlockers()` entry, but open)

**Historical request and status below:** the [focused findings](2026-11-17-graziani-implementation-blockers-research.md) completed the source verification and retain unknown initial allocation and effective CPA. A [later conditional chart reading](2026-11-18-graziani-initial-motorization-source.md) does not establish either value.

**What's needed:** this is a verification task, not a fresh lookup. The codebase already
states a conclusion
(`src/Cna.Core/Content/ContentSourceGiarabubMotorMachineGunArtifact.cs:6-7,43`): "Source's 8+
marks historical motorization; the scenario does not identify an assigned motor pool or an
effective current allowance" — `historicalMotorizationPriority` is recorded as
`"unresolved-no-scenario-pool"`, and `mobilityId`/`motorPoolId`/`assignedMotorPoints` are all
`null` by design. This predates this session's research effort entirely (from commit
`21c6cba7`) and has never been covered by any research doc's citation list.

**What's needed:** confirm this conclusion still holds rather than build anything further on an
unchecked finding from a prior session. The element's own citation list is:
`scenario:59.63a,60.31`; `axis-charts:oa-giarabub-p14,4.46b`; `air:counter-section-2-p40`;
`land:4.23,9.16a,32.54`.

**Where to look that hasn't been tried yet:** re-check those exact citations, specifically for
any motor-pool assignment or mobility classification the original pass might have missed — the
`air:` and `land:` citations in particular (`counter-section-2-p40`, `9.16a`, `32.54`) were not
the primary focus of any research doc so far and are worth a genuinely fresh look, not a
re-confirmation of the scenario/axis-charts citations alone.

**Deliverable:** either a real motor-pool ID / mobility classification / capability-point value
with citation (if one genuinely exists and was missed), or an explicit confirmation that
`"unresolved-no-scenario-pool"` is correct and the source provides nothing further. Both are
useful, complete answers. (Note: a ruling action formally accepting the "unresolved" state,
mirroring the Central India Horse / `battlefieldComplete` pattern, may be built independently of
this task's outcome — if real data turns up here later, that becomes a follow-up fix to the
ruling's accepted value, not wasted prior work either way.)

## Historical rationale for this scope

The Nov 17 batch's ~361 files are almost entirely new map-geometry transcriptions and two
planning-only policy rulings (RUL-GRA-24, RUL-GRA-25) — none of it is runtime-admitted, and
none of it touches these 3 items. Separately, that batch's own
[handoff doc](2026-11-17-graziani-batched-coding-handoff.md) flags a real, independent
*engineering* bug (not a research gap): `Cna1979HistoricalMap.NeighborsOf` asymmetrically fails
to enumerate Malta's nine column-`00` hexes as neighbors in one direction, while `EdgeBetween`
correctly treats them as adjacent. That's being fixed in code, not research — tracked
separately from this doc.
