# Graziani Land-only initial supply: accounting pilot

**Status:** single-reader research transcription for review, not a content pack,
approved setup, exhaustive inventory, or independently checked source record.
Scope: printed scenario §60.92(B–C) only. Original [scenario scan](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
pp. 28–29 visually read; SHA-256
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`.
See [source register](2026-11-12-graziani-source-register.md) LA-03/04/09–16 and
[source policy](../../research/cna-source-material-spike.md#recommended-authority-policy).
Numerical facts below need independent double entry and rights/provenance review.
The [research-only data](2026-11-12-graziani-supply-pilot.json) and
[arithmetic check](verify-graziani-supply-pilot.py) make the calculation reproducible;
the check cannot establish that the transcription matches the printed source.

## Fixed rows before placement and optional extras

Columns are quantity of regular units, ammunition per unit and fuel per unit;
`—` preserves the printed mark without assuming a numeric meaning. These are
source facts for checking, not canonical identifiers or player-facing content.

| Side | Location/group | Regular units | Ammo each | Fuel each | Placement/source note |
| --- | --- | ---: | ---: | ---: | --- |
| Axis | Tobruk | 3 | 15 | 50 | §60.92(B) |
| Axis | Bardia | 2 | 10 | 40 | §60.92(B) |
| Axis | Benghazi | 1 | 10 | 10 | §60.92(B); map identity needs separate reconciliation |
| Axis | Derna | 1 | — | 10 | §60.92(B); meaning of dash not assumed |
| Axis | Tripoli box | 8 | 5 | 60 | §60.92(B) |
| Axis | Dump 1 | 3 | 40 | 50 | One eligible map-C hex in Libya, outside stated Commonwealth proximity restriction. |
| Axis | Dump 2 | 3 | 40 | 50 | Same class of choice; distinct/co-located legality needs source check. |
| Axis | C0716 | 1 | 20 | 6 | §60.92(B) |
| Commonwealth | Mersa Matruh | 5 | 40 | 60 | §60.92(C) |
| Commonwealth | Sidi Barrani | 1 | 40 | 60 | §60.92(C) |
| Commonwealth | Dump 1 | 3 | 40 | 30 | One eligible map-D hex west of Mersa Matruh. |
| Commonwealth | Alexandria | 3 | 40 | 60 | §60.92(C); city-hex placement requires separate rule check. |
| Commonwealth | Cairo | 3 | 40 | 60 | §60.92(C); city-hex placement requires separate rule check. |

Arithmetic over these rows: Axis **22** regular units, **1,036** fuel points and
**375 known** ammo points plus one printed dash; Commonwealth **15** regular units,
**810** fuel and **600** ammo points. Totals multiply each row's quantity by its
per-unit contents. They are useful reconciliation checks, not admitted scenario
totals; treating the dash as zero requires a sourced interpretation. §60.92 gives
two Axis groups of three dummy units and one Commonwealth group of three, each
with placement language; legal individual placement and whether other general
dummy provisions apply require reconciliation.

## Cross-rule questions and failure-sensitive checks

| Question | Evidence and required disposition | Proposed negative/boundary check |
| --- | --- | --- |
| Replacement vs duplication | §60.92(B–C) explicitly replaces earlier initial supply cases; Matruh repair is a separate working ruling. | Reject combining original dump amounts with replacement units; changing ruling or source revision changes scenario identity. |
| Fixed rows vs optional initial loads | §59.66 permits additional loaded motorization subject to eligibility and grouping; errata §59.45 discusses extra truck loads. Their interaction in Land-only mode needs interpretation. | Preserve fixed and optional provenance; check zero and selected extra loads without double counting or inventing an upper total. |
| Unit capacity | Land §32.15 provides per-unit ceilings. Verify each row and any special-source exception rather than extrapolating fuel or ammo from totals. | Test maximum, one above maximum, printed dash, depletion and no negative amounts. |
| Stacking and placement | §59.65 allows limited setup exceptions; §32.14 and §60.92 specify unit and location conditions. Same-hex choices may interact with dummy groups. | Test eligible/ineligible hex, proximity and map boundary, exact capacity, unavoidable setup exception, later noncompliant state and hidden dummy disclosure. |
| Source and identity | Initial placements, contents, choices and rule/ruling identities must be canonical and replayable. | Reject unknown locations, repeated identifiers, conflicting versions and tampered totals; prove same inputs reproduce identical event/snapshot hashes. |
| General dummy rule vs scenario counts | §32.18 has a general dummy allowance; §60.92 names scenario groups. Their composition is not resolved by this pilot. | Preserve alternatives; test zero, stated groups and any additional allowance only after source ruling. No hidden-state leak from public actions. |

## Initial Axis first-line truck conversion pilot

The 13 explicitly truck-quantified rows read from §60.31 total 55 light, 260
medium and 45 heavy truck points. Applying §59.63(A)'s medium-plus-heavy conversion
to those rows gives **305 candidate attached motorization points**. The 55 light
points are not included in this specific conversion. This arithmetic is a
single-reader cross-check, not an independently verified full force or setup total;
it does not count later arrivals, second/third-line pools, optional extra loads,
air-facility allocations or implicit/errata-derived changes. Initial allocation
choices and §32.54 priorities still require source and contract review.

| Setup row | Light | Medium | Heavy | Medium + heavy |
| --- | ---: | ---: | ---: | ---: |
| C4218 | 10 | 25 | 10 | 35 |
| C4120 | 0 | 45 | 5 | 50 |
| C4020 | 10 | 15 | 0 | 15 |
| C3920 | 15 | 15 | 0 | 15 |
| C3919 | 0 | 5 | 5 | 10 |
| C3918 | 0 | 40 | 5 | 45 |
| C3617 | 5 | 30 | 5 | 35 |
| C4707 | 0 | 30 | 5 | 35 |
| C4507 | 10 | 25 | 10 | 35 |
| Bardia | 0 | 20 | 0 | 20 |
| Tobruk | 0 | 5 | 0 | 5 |
| Barce | 0 | 5 | 0 | 5 |
| Within three hexes of C0716 | 5 | 0 | 0 | 0 |

Zeros in this table mean a type is not listed on that row; they are not proof
that no other unit or pool can contribute trucks. The 4th CCNN row yields 35,
matching §59.63's worked example. These are transcription checks, not formation
roster or eligibility acceptance. Confirm the 13-row inventory by independent
source entry before storing production data.

## Initial Commonwealth first-line truck conversion pilot

The 12 explicitly truck-quantified rows read from §60.41 total 30 light, 125
medium and 22 heavy truck points; §59.63(A)'s medium-plus-heavy calculation gives
**147 candidate attached motorization points**. As with the Axis pilot, this is
single-reader arithmetic over printed rows, not a verified complete setup, legal
allocation, separate supply pool, or total of later reinforcements.

| Setup row | Light | Medium | Heavy | Medium + heavy |
| --- | ---: | ---: | ---: | ---: |
| C4131 | 0 | 10 | 0 | 10 |
| C3926 | 0 | 2 | 0 | 2 |
| C3922 | 0 | 15 | 5 | 20 |
| C3721 | 5 | 5 | 0 | 5 |
| C3520 | 0 | 5 | 0 | 5 |
| C3320 | 0 | 10 | 0 | 10 |
| C3020 | 0 | 3 | 0 | 3 |
| D3714 | 5 | 15 | 5 | 20 |
| D3612 | 5 | 5 | 5 | 10 |
| D3615 | 5 | 20 | 2 | 22 |
| E1829 | 0 | 20 | 0 | 20 |
| Alexandria | 10 | 15 | 5 | 20 |

Review the French Motor Marines' OA arrival correction against C3926 without
duplicating the scenario's explicit starting unit. The source's side-specific
second/third-line and air-facility pools are separate; source checks must also
account for initial troop organization, eligible assignments, optional loads and
map/stacking constraints. `0` means the type was not listed in that row only.

## Second/third-line source pool cross-check

Original scenario §60.33/§60.43 lists these pools separately from the first-line
rows above. For the Land-only profile, §59.63(B) uses medium plus heavy points for
unattached motorization and excludes trucks assigned to air facilities when Air
is abstracted. These are single-reader source observations, not validated legal
placement or an admitted initial scenario total.

| Side and printed pool | Light | Medium | Heavy | Candidate medium + heavy |
| --- | ---: | ---: | ---: | ---: |
| Axis: Tripoli | 25 | 140 | 40 | 180 |
| Axis: anywhere in Libya | 30 | 100 | 25 | 125 |
| Axis: air facilities | 10 | 50 | 0 | excluded under candidate Land-only reading |
| Commonwealth: any hex in Cairo | 0 | 40 | 10 | 50 |
| Commonwealth: Alexandria | 10 | 20 | 0 | 20 |
| Commonwealth: anywhere, maps | 15 | 40 | 5 | 45 |
| Commonwealth: air facilities | 5 | 30 | 20 | excluded under candidate Land-only reading |

The non-air rows arithmetically yield **305 Axis** and **115 Commonwealth**
candidate unattached points, before validating source exceptions, location,
assignment or setup choices. Air rows are shown for reconciliation, not added
to these totals. Do not mix these initial pools with reinforcement trucks, claim
their points are all legally transport-ready, or infer a maximum number of extra
supply units solely from pooling totals. Under §59.66(C), extra-unit eligibility
depends on grouping and actual location. The printed §59.66(C) expressly treats
Tripoli/Tunisia boxes as hexes for that case; this does not by itself settle
cross-pool placement, other off-map rules or every scenario choice. The earlier first-line figures are separate attached-point
candidates and must not be double-counted in this calculation.

The source prints no heavy points in the Axis air-facility row or the
Commonwealth Alexandria row; zero here records an unlisted type in that row,
not a claim that a general category cannot exist. Re-enter all seven rows and
confirm errata and applicability before using any figure in a rules artifact.

## Review and next step

Independently re-enter every row from the original scan and compare each field,
printed dash and subtotal; verify map names/coordinates against map and errata;
resolve stacking, dummy and optional-load interactions before schema or runtime
implementation. Group totals do not authorize omitting individual units, choices,
source references or the required provenance/rights decision.

A [blind supply review packet](graziani-supply-review/README.md) now binds the
13 fixed rows and two grouped statements to this source hash. Its independent
reading remains outstanding.
