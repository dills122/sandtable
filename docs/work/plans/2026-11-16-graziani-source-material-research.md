# Graziani source-material research handoff — November 16, 2026

**Status:** source transcription and conservative planning disposition; not
implemented or independently rechecked. Findings inform, but do not themselves
clear, blockers in [readiness and combat plan](2026-11-16-graziani-readiness-and-combat-plan.md).
No content identities, contracts, code, tests, or blocker diagnostics changed.

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
  This creates a chronology/assigned-parent question against scenario
  notation. Assignment and attachment are distinct; note `f` does not erase
  either explicit Matruh attachment.
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

**Implementation work:** preserve all five distinct identities and separately
record source-stated attachment, assignment, chronology, and any chosen setup
ruling. Resolve initial assigned parent for Essex/Durham and the errata/OA
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
| `1-ccnn-hq` | `g` | 20 | 1 artillery point | determined by assigned weapon | OA says `N`; chart permits one artillery point but does not identify its gun type |
| `4-ccnn-hq` | `b` | 30 | `(1)` | `0/(1)` | parenthesized values |
| `201-ccnn-engineer` | `bbb` | 25 | `(1)` | `0/(1)` | parenthesized values |
| `204-ccnn-engineer` | `bbb` | 25 | `(1)` | `0/(1)` | parenthesized values |

OA calls 201st and 204th engineers **battalions**, not companies. Chart key
says a parenthesized rating cannot be used if units in the hex possess ammo
and non-parenthesized numbers for that rating; replacement points do not
prevent its use. Preserve the exact condition and relevant rules when modeling
combat; do not encode unconditional defense 1. The OA's `N` does not identify
the 1st division HQ's gun. Do not borrow the 65/17 from legion HQs. Resolve
weapon type and current strength from evidence before assigning weapon-specific
combat facts. Also check the source meaning of parenthesized maximum TOE rather
than silently treating it as an ordinary unconditional component.

**Implementation work:** reconcile these facts with
`Cna1979GrazianiSourceFacts.cs`, `CampaignSourceCcnnBatch.cs`, and
`CampaignSourceAxisSupplementalForces.cs`; retain unresolved limitations until
their full conditions are supported. Test conditional combat, source identity,
and pinned content/replay compatibility. Re-run the readiness diagnostic;
partial transcription does not prove `ccnn-characteristics-incomplete` clears.
Existing limitation markers are `division-hq-and-engineer-component-details-incomplete`
and `division-hq-and-engineer-facts-incomplete`, respectively.

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
