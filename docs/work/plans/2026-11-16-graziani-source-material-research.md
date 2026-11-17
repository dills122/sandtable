# Graziani source-material research handoff — November 16, 2026

**Status:** source transcription and conservative planning disposition; not
implemented. Later bounded independent source checks are linked below and in
the current queue; they do not establish complete review. Findings inform, but do not themselves
clear, blockers in [readiness and combat plan](2026-11-16-graziani-readiness-and-combat-plan.md).
No content identities, contracts, code, tests, or blocker diagnostics changed.

**November 17 follow-up:** Matruh ratings and the Essex/Durham attachment
ruling are recorded below. The [remaining source-research queue](2026-11-17-graziani-remaining-source-research.md)
separates open source questions, decisions, independent review, and missing scans.
The [fresh-context unit transcription](2026-11-17-graziani-unit-characteristics-independent.md)
independently checked the Australian, Matruh and French Marine chart values;
source conflicts and implementation remain separate.

**Latest coding-session entry points:**

- [Current research queue](2026-11-17-graziani-remaining-source-research.md)
  and [working decisions RUL-GRA-01–18](2026-11-12-graziani-ruling-decisions.md)
  distinguish source facts, provisional project interpretations and unresolved
  admission gates. None changes an existing content or ruleset identity.
- Both B1705/B1706 and B1706/B1805 slopes have independently checked
  [source evidence](2026-11-17-graziani-slope-directions-next.md) identifying
  B1706 as the high side. Pinned v1 still stores unknown directions; make any
  correction under a new version with replay tests, not by mutating old data.
- The original [counter-sheet scan](2026-11-17-graziani-matruh-counter-review.md)
  confirms Selby/Matruh `X`, `G`, `(3)`; [1st CCNN HQ](2026-11-17-graziani-ccnn-counter-review.md)
  still has no source-identified starting gun type or strength. Do not copy
  a legion HQ weapon or interpret the counter's `(5)` stacking mark as a gun.
- Initial medium/heavy conversion gives source-listed **610 Axis** and **262
  Commonwealth** points as independently checked bounded subtotals, not
  certified full placements. See [Axis accounting](2026-11-17-graziani-initial-motorization-next.md)
  and [Commonwealth accounting](2026-11-17-graziani-cw-motorization-next.md).
- [Map coverage audit](2026-11-17-graziani-map-coverage-review.md) identifies
  a Malta column-00 neighbor-enumeration discrepancy for a focused coding test;
  scenario impact is unverified. Full coverage, seams and incoming ZOC remain
  unadmitted. User will handle the rights inquiry; no reply is recorded.

## Source copies and identification

Page numbers below are **absolute PDF pages**, not OA-section offsets. Temporary
paths are discovery aids, not durable project inputs. Verify a replacement copy
against its SHA-256; do not commit scans without resolving their rights.

| Source | Observed copy | SHA-256 |
|---|---|---|
| Commonwealth charts | `/private/tmp/sandtable-cw-charts.pdf` | `f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981` |
| Axis charts | `/private/tmp/sandtable-axis-charts.pdf` | `9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca` |
| Scenarios | `/private/tmp/sandtable-scenarios.pdf` | `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66` |
| September 1979 errata | `/private/tmp/sandtable-cna-errata.pdf` | `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb` |

The [source register](2026-11-12-graziani-source-register.md) tracks broader
provenance and rights. Preserve these identities when recording source facts.

## 1. 6th Australian Division

Commonwealth OA PDF p. 13, **6th Australian Division**, identifies each unit's
ID code. Commonwealth unit characteristics chart §4.46a, PDF pp. 22–23,
provides the corresponding ratings. TOE below is **maximum TOE**, not an
independent confirmation of current strength. Close assault values are
offensive/defensive, per TOE strength point.

| Unit or component | ID code | Maximum TOE | CPA | Close assault off/def | Other chart facts |
|---|---|---:|---:|---:|---|
| Each of 2/1, 2/2, 2/3, 2/5, 2/6, 2/7, 2/4, 2/8 and 2/11 Australian infantry battalions | `p` | 6 | 10 | 1/2 | — |
| 16th Australian Infantry Brigade HQ, organic antitank | `f` | 1 | 20 | 0/1 | anti-armor 2; vulnerability 1 |
| 17th Australian Infantry Brigade HQ, organic antitank | `e` | 1 | 20 | 0/1 | anti-armor 1; vulnerability 1 |
| 6th Australian Divisional Cavalry Regiment, reconnaissance | `ll` | 8 | 45 | 2/3 | armor protection 1 |

The current grouped definition includes six battalions of 16th and 17th
Brigades, both HQs and cavalry. OA lists 19th Brigade and its three battalions
separately; its HQ is ID `a`, not `e` or `f`. Scenario §60.41 excludes 19th
Brigade from the initial training group. Do not silently give its HQ an organic
antitank component based on the other brigades.

**OA deployment notation:** Commonwealth OA key §4.44b, PDF p. 5, defines
`N` as maximum chart strength for non-tank/non-gun units and `D` as deployed
at turn 1/stage 1. On OA p. 13, the six 16th/17th Brigade battalions, those
two brigade HQs and the divisional cavalry each print `N` and `D`. This gives
their listed chart maxima as their OA deployment strengths, subject to any
scenario-specific override and the separately modeled training state. The 19th
Brigade and its battalions print `2/9`, not `D`; they are not initial members
of this training group.

**Implementation work:** add source-backed defensive ratings to the appropriate
facts and `CampaignSourceGroupedZoc` consumers; cite both OA p. 13 and chart
§4.46a. Verify applicable unit IDs and actual TOE independently, add focused
deterministic tests, and rerun the fixture diagnostic in plan §1. Other
unresolved or independent elements may still block ZOC derivation.

## 2. Matruh identity and relationships

- Scenario PDF p. 3, printed p. 27, §60.41, D3714: Matruh Garrison starts with
  **1st Essex, 1st Durham Light Infantry and 1st South Staffordshires attached**.
  The scenario prints `23/70` alongside both Essex and Durham. They are two
  distinct units, not alternatives for one slot.
- Commonwealth OA PDF p. 9, 70th Infantry Division, 23rd Brigade, note `f`:
  Essex and Durham were not **assigned** to 23rd until June 1941 (3/38).
  This does not erase either explicit Matruh attachment. Scenario §59.2's
  parent-designation key says the printed parent identifies the counter whether
  or not the unit is still assigned to it; `23/70` is not evidence of assignment
  at scenario start. The same section defines `Att` as attached but not assigned
  to the listed parent.
- September 1979 errata PDF p. 1, §4.44b item 2: both **1st Buffs and 1st
  Hampshires** start the campaign and Italian scenarios assigned to Matruh.
  Commonwealth OA PDF p. 10, Selby Force/The Matruh Garrison, says no units
  may be assigned and up to six may be attached. These are a separate
  assignment conflict, not alternatives to Essex or Durham.
- Existing [formation setup conflict analysis](2026-11-12-graziani-formation-setup-conflicts.md)
  explains the consequences for limits, co-location, and setup choices.
  [Planning ruling](2026-11-12-graziani-ruling-decisions.md) treats the
  specific erratum as an exception, provisionally. That interpretation does
  not establish an official clarification or settle all attachment interactions.

### Matruh combat characteristics

Commonwealth OA PDF p. 9 identifies Essex and Durham; p. 11, **Unassigned
British Infantry-type Units**, identifies South Staffordshires, Buffs and
Hampshire. Chart §4.46a, PDF p. 22, gives the values below. TOE is the chart
maximum; the separate OA notation check below addresses starting strength. Close-assault ratings
are offensive/defensive per TOE strength point.

| Unit | ID code | Maximum TOE | CPA | Close assault off/def |
|---|---|---:|---:|---:|
| `1-essex` | `m` | 6 | 10 | 2/2 |
| `1-durham-light-infantry` | `m` | 6 | 10 | 2/2 |
| `1-south-staffordshires` | `p` | 6 | 10 | 1/2 |
| `1-buffs` | `p` | 6 | 10 | 1/2 |
| `1-hampshire` | `p` | 6 | 10 | 1/2 |

Durham's OA row prints uppercase `M`; chart row is lowercase `m`. The scan
hash is listed above. This is a same-stream visual transcription, not a blind
independent entry.

**Initial-strength cross-check:** Commonwealth OA key §4.44b, PDF p. 5, says
`N` on a non-tank/non-gun unit denotes maximum chart TOE on arrival/deployment,
and `D` denotes deployment at game turn 1, stage 1. All five battalion rows on
pp. 9 and 11 print `N` and `D`; chart rows `m` and `p` have maximum TOE 6.
Accordingly, the OA's source-stated starting strength is **six TOE points for
each of the five battalions**, subject to any applicable scenario-specific
strength override or correction. This does not determine their final owner
setup, later losses, or assignment. Unlike the CCNN HQ `g` row, these are
ordinary infantry entries, not an unspecified gun-point listing.

### Essex/Durham attachment ruling

For the source-listed setup, both battalions' Matruh attachments stand regardless
of their later assignment to 23rd Brigade. Scenario §60.41, PDF p. 3, lists both
after `Att`; §59.2, PDF pp. 1–2, distinguishes attachment from assignment and
explains the parent designation. OA note `f`, PDF p. 9, dates the later assignment;
it does not make either battalion ineligible at Matruh. This is a project ruling
on the cited texts, not an official erratum.

Do not infer their initial assigned parent from `23/70`; preserve it as unknown
unless separately established. Assigned versus unassigned status can affect
subsequent attachment/detachment rules (Land §§19.41–19.44). Scenario §59.2
also permits initial reorganization of co-located units within organization and
stacking limits, so the source-listed attachment is not by itself a ruling that
every permitted owner setup must retain it. The separate Buffs/Hampshire
assignment conflict and attachment-limit interactions remain open.

**Historical comparison requiring separate treatment:** a [secondary brigade
order of battle](https://www.generalstaff.org/NAF/Pt_I_1939-1940/939bxie.pdf),
printed p. 11, lists 1st Essex under 21st Infantry Brigade from August 6 to
October 11, 1940, and describes that brigade's HQ as formed in Sudan. A
[campaign history](https://www.ibiblio.org/hyperwar/UN/India/EAfrica/EAfrica-3.html)
also places 1st Essex with forces defending Khartoum in October 1940. These
are comparison leads and possible historical tension with SPI's Matruh setup;
neither is SPI scenario authority nor establishes the game's intended assigned
parent or justifies deleting the scenario's explicit attachment. Verify dates,
battalion identity and source provenance independently before any historical
variant or chronology decision.

**Implementation work:** preserve all five distinct identities and separately
record source-stated attachment, assignment, chronology, and setup choice. Apply
the scoped ruling to the `scenario-23-70-vs-oa-note-f-conflict` markers without
claiming an initial assigned parent was established. Resolve the errata/OA
relationship and attachment-limit interactions with evidence or an explicit
versioned ruling. Revisit both `CampaignSourceMatruhFormation.cs` and
`ContentSourceMatruhDetachedPackArtifact.cs`; do not delete conflict markers as
if the identity question alone resolved the relationship discrepancy.
The former marks both `1-essex` and `1-durham-light-infantry` with
`scenario-23-70-vs-oa-note-f-conflict`; the latter records
`buffs-hampshire-provisional-errata-essex-durham-conflict`.

## 3. CCNN headquarters and engineers

Axis OA PDF p. 17, **1st CCNN Division**, and p. 18, **4th CCNN Division**,
identify the units. Italian unit characteristics chart §4.46b, Axis PDF pp.
26–27, supplies the following. TOE is the chart maximum; preserve parentheses.

| Element | ID code | CPA | Maximum TOE | Close assault off/def | Source constraint |
|---|---|---:|---:|---:|---|
| `1-ccnn-hq` | `g` | 20 | 1 artillery point | determined by assigned weapon | OA says `N`; chart permits one artillery point, but the OA key's gun-listing rule makes initial equipment ambiguous |
| `4-ccnn-hq` | `b` | 30 | `(1)` | `0/(1)` | parenthesized values |
| `201-ccnn-engineer` | `bbb` | 25 | `(1)` | `0/(1)` | parenthesized values |
| `204-ccnn-engineer` | `bbb` | 25 | `(1)` | `0/(1)` | parenthesized values |

OA calls 201st and 204th engineers **battalions**, not companies. Chart key
says a parenthesized rating cannot be used if units in the hex possess ammo
and non-parenthesized numbers for that rating; replacement points do not
prevent its use. Preserve the exact condition and relevant rules when modeling
combat; do not encode unconditional defense 1. **Engineer-specific correction:**
September 1979 errata PDF p. 2, §23.11, says engineers may use parenthesized
strengths only when not stacked with a friendly combat unit. This differs from
the general chart key's ammo/rating test. Reconcile the engineer-specific
correction with the general key, including which other units count as friendly
combat units; do not assume the general key alone is the complete engineer
condition. The OA's `N` does not identify
the 1st division HQ's gun. Do not borrow the 65/17 from legion HQs. Resolve
weapon type and current strength from evidence before assigning weapon-specific
combat facts. Also check the source meaning of parenthesized maximum TOE rather
than silently treating it as an ordinary unconditional component.

Land §23.11, printed p. 35, originally says engineers use parenthesized strength
only when stacked with a friendly combat unit; the September correction reverses
that particular condition. Land §3.23 also makes combat classification
case-dependent, and §23.14 distinguishes engineer-capable HQs from engineer
units. Those distinctions must survive any implementation. Land §4.46 further
explains that when a **headquarters** has a parenthesized assigned-TOE limit,
weapons assigned to it use parenthesized combat values; it does not by itself
explain every implication of the engineer battalion's `(1)` maximum.

**Implementation work:** reconcile these facts with
`Cna1979GrazianiSourceFacts.cs`, `CampaignSourceCcnnBatch.cs`, and
`CampaignSourceAxisSupplementalForces.cs`; retain unresolved limitations until
their full conditions are supported. Test conditional combat, source identity,
and pinned content/replay compatibility. Re-run the readiness diagnostic;
partial transcription does not prove `ccnn-characteristics-incomplete` clears.
Existing limitation markers are `division-hq-and-engineer-component-details-incomplete`
and `division-hq-and-engineer-facts-incomplete`, respectively.

**November 17 follow-up, hash-matched Axis PDF:** OA §4.44b key on PDF p. 4
defines `N` for non-tank/non-gun units as the chart maximum on deployment or
arrival. It also says tank and gun TOE points, including those assigned to
non-tank/non-gun units, are listed by number and weapon system. The 1st CCNN
division HQ row on p. 17 prints `g`, `N`, `D`, while its two legion HQs
explicitly list `1 × 65/17 Gun`. Chart §4.46b p. 26 gives code `g` CPA 20 and
capacity to assign one artillery point; §4.48 p. 29 lists ratings by actual
gun type. Thus `N` is a strength notation, **not a gun type**. Applied literally
to this HQ, the maximum-strength notation and the missing weapon-specific listing
do not unambiguously establish either an initially present gun or its type.
The chart capacity is not proof of deployment; do not encode an initially
present gun, a weapon-specific rating, or zero without further evidence or an
explicit, versioned ruling. September errata PDF p. 1, §§4.44b and 4.46, does
not identify this gun. This is a same-stream visual check, not independent review.

**Historical comparison, not game authority:** a [secondary historical order
of battle](https://wargaming.info/1996/camicie-nere-the-blackshirts-mvsn-a-ccnn-combat-units/)
lists a divisional HQ, 65/17 batteries under each legion, and the 201st artillery
regiment separately; it does not identify a gun assigned to the divisional HQ.
It supports keeping the legion guns distinct, but cannot establish the SPI
counter's starting composition or prove that it had no gun. The original Land
rulebook's printed addenda, pp. 45–46, and September errata were also searched
for a specific HQ correction; none was identified. This is a bounded search,
not proof that no further official correction exists.

**Independent follow-up:** a [fresh-context source review](2026-11-17-graziani-ccnn-independent-review.md)
confirms the OA key, HQ row, chart capacity and engineer-specific errata. It
does not identify the gun or resolve how the mixed-stack restrictions compose.

## 4. Battlefield and control readiness

**Conservative planning ruling:** keep `battlefieldComplete` and
`controlCertified` false until the five-step source-backed playable-map work in
[source-control map spec](../../specs/graziani-source-control-map.md#shortest-route-to-a-source-backed-playable-map)
is complete: freeze and transcribe source scope; resolve identity and
completeness; publish versioned, coverage-aware content; connect actual play;
and validate admission and replay. This is a decision to retain today's
fail-closed state, not a claim that a precise predicate exists or a substitute
for deciding the distinct coverage and territorial-control contracts.

Future implementation must distinguish reviewed clear edges and boundaries
from unknowns, account for movement and incoming ZOC at scope boundaries,
reconcile or block unresolved seams, and validate source references, topology,
runtime behavior and replay against the exact content identity. Do not flip
either flag merely because excerpts or promotion receipts exist.

## Validation and handoff

Formation and characteristics pages, scenario p. 27, Matruh OA and errata
were visually inspected. No tests or readiness diagnostic were run for this
research-only packet. Implementation must preserve pinned identities or
introduce new versioned content as appropriate, add focused deterministic
tests, and report the actual post-change blocker results.
