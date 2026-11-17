# Initial Commonwealth divisions and Axis convoy lookup

This is a source audit, not a selected setup or a convoy result. Section 32.66's relevant condition asks for a non-shell Allied division east of or on the same hexrow as another non-shell Allied division. Actual attachments at the naval convoy snapshot, other divisions, and relative positions must be evaluated; this audit alone does not establish either outcome.

## Counting rule

Section 9.26 has a separate two-battalion-brigade exception; do not extend the
ordinary two-of-three shortcut below to every brigade. The [independent source
review](2026-11-17-graziani-convoy-divisions-review.md) corroborates the
bounded baselines and flags that exception and §19.45 substitution limits.

Land §9.26 counts a division as a shell when **50% or less** of the brigade-equivalents it may be assigned are attached. A shell brigade attached to a division does not count (§9.27). An ordinary three-battalion brigade is a shell with fewer than two battalion-equivalents attached; its battalions must themselves qualify. A division with three authorized brigade-equivalent slots therefore needs two non-shell brigades. The denominator is authorized slots, not brigades currently on the map. Commonwealth §19.31 gives three slots for the relevant type-I armoured division and three for the infantry divisions; check any specific OA exception before generalizing. Tank battalion code g has maximum 10 tanks (§4.46a).

Land §4.25 supplies a default for assigned units starting in the same hex, unless specifically stated otherwise. Scenario §59.2 also permits owners to organize units listed to begin in the same hex within formation and stacking limits. Alexandria's two-hex placement is itself a choice. Thus the default attachment reading below is a source baseline, not a claim that a player chose or must retain it. Section 19.45's substitution provision and the §19.5 additional-attachment chart are distinct; do not silently turn an extra battalion or company into another qualifying brigade. Whether any particular substitute satisfies type, missing-slot, location and stacking requirements requires its own proof.

| Division | Assigned brigade-equivalent slots and source baseline | Status without owner setup choices |
| --- | --- | --- |
| 7th Armoured | 4th Armoured, 7th Armoured, 7th Support Group; denominator **3**. At D3612, the first has its two listed on-map tank battalions; the second retains only 8 Hussars because 1 RTR is separately placed; the support group's listed combat components are separately placed. Only 4th Armoured qualifies on this baseline: **1/3**, shell. | Source baseline shell. Actual selected arrangement remains **unknown** without the legal setup and attachment snapshot; §59.2 and eligible substitutes cannot be assumed away. |
| 4th Indian | 5th Indian, 11th Indian, 7th Indian; denominator **3**. First two each have three on-map assigned infantry battalions at D3615; 7th arrives later. **2/3**, non-shell on the baseline. | Source baseline non-shell. Actual selected arrangement remains **unknown** until setup organization is recorded; don't impose an imagined player choice. |
| 2nd New Zealand | 4th, 5th and 6th New Zealand infantry brigades; denominator **3**. Only 4th is listed at Alexandria initially; 5th and 6th are future arrivals. Among these assigned brigades at most **1/3** can qualify, hence shell even if 4th is retained. | Shell under this source-listed-brigades bound. Actual status remains **unknown** until legal setup is checked for any qualifying substitute brigade and its attachment. Splitting the Alexandria units cannot create a second assigned on-map NZ brigade. |

## Roster evidence and limitations

* **7th Armoured:** OA lists 6 RTR and 7 Queen's Own Hussars under 4th Armoured, each D with 10 tanks; those two of three tank-battalion slots make that brigade non-shell. OA lists 1 RTR and 8 Hussars under 7th Armoured; scenario §60.41 explicitly places 1 RTR at C3520, leaving 8 Hussars (D, 7 tanks) on the default reading. One of three slots is insufficient. The scenario lists 1 KRRC and 4 RHA at C3922, 3 RHA at C3721, 2nd Rifle Brigade at C3320, and 11 Hussars at C3020 as detached 7th Armoured units. Their separate placement prevents counting them as default D3612 attachments. Do not count a support-group HQ alone as a qualifying brigade. Scenario also provides some units for placement anywhere on maps D or E, so co-location and an allowed substitution must be established from the actual setup, not invented. Future tank arrivals are not GT1 starting attachments.
* **4th Indian:** OA lists 5th Indian's 1 Royal Fusiliers, 3/1 Punjab and 4/6 Rajputana Rifles and 11th Indian's 2 Camerons, 1/6 Rajputana and 4/7 Rajput as D, normal strength. Scenario places 4th Indian at D3615 with 31st Field Artillery and 7th Indian Brigade detached. OA schedules 7th Indian and its three battalions for GT5/1, not the initial hex. The two qualifying assigned brigades give 2/3 before permitted reorganization; later organization must be read from state.
* **2nd New Zealand:** §60.41 lists its HQ, 4th NZ Brigade, 27th NZ MG Battalion, 2nd NZ Cavalry and 4th NZ Field Artillery at Alexandria, E3613 and/or E3714. OA shows 4th Brigade and its 18th, 19th and 20th battalions D, 5th Brigade and its battalions GT35/1, and 6th Brigade and its battalions GT6/3. Even if 4th Brigade and its children are kept co-located and qualify, one assigned brigade of three is not enough. The artillery, cavalry and MG battalion are not independent brigade-equivalents. A later-turn example elsewhere conflicts on 6th Brigade's arrival; neither reading makes it an initial brigade, and no arrival choice is invented here.

The scenario also starts 6th Australian Division in training at Cairo and/or Helwan, less specified units. It must be audited independently: training alone does not prove shell status. The separately listed 16th Infantry Brigade (70) must not be mistaken for 16th Australian Brigade. No conclusion about the existence of a qualifying pair follows from examining only the three divisions above.

## Suggested acceptance checks

1. Three-slot division: two attached non-shell brigades qualify; one does not. An attached shell brigade contributes zero. Never shrink the denominator to on-map brigades.
2. Assert the source baselines above with explicit baseline setup; do not encode those baselines as mandatory player choices. Check 7th Armoured's 4th Brigade at the exact two-of-three boundary, and 7th Brigade with one of three.
3. Exercise legal same-hex reorganization and either Alexandria hex choice against actual attachments and stacking. Any claimed substitute must satisfy §19.45, relevant type and capacity, co-location and timing; an additional attachment alone is not proof of a second brigade.
4. At the actual convoy lookup snapshot, recompute from authoritative attachments, locations, and other Allied divisions. Preserve unknown if setup or substitution legality is unresolved; never expose hidden opponent formation details as a justification to an unauthorized viewer.

## Source locators

* Scenario booklet, PDF p. 1 / printed p. 25 §59.2; PDF p. 3 / printed p. 27 §60.41: `/private/tmp/sandtable-scenarios.pdf`, SHA-256 `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`.
* Land Rules, printed/PDF p. 9 §4.25, p. 17 §§9.26–.27, pp. 29–30 §§19.3, 19.45: `/private/tmp/sandtable-land-rules.pdf`, SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
* Commonwealth charts/OA, PDF pp. 6, 15, 18, 22 and 24: `/private/tmp/sandtable-cw-charts.pdf`, SHA-256 `f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981`. PDF p. 24 contains §19.31; p. 22 unit characteristics.
* Common charts §19.5, PDF p. 6: `/private/tmp/sandtable-common6.png` rendered from `/private/tmp/sandtable-common-charts.pdf`, source SHA-256 `51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0`. September errata clarifies additional attachments beyond normal assigned attachments: `/private/tmp/sandtable-cna-errata.pdf`, SHA-256 `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
* Axis chart §32.66, PDF p. 34: `/private/tmp/sandtable-axis-charts.pdf`, SHA-256 `9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca`.
