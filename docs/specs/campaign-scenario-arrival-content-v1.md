# Scenario arrival content: production model boundary

An in-memory scenario may describe a scheduled arrival by stable group ID,
owning side ID, exact game turn and operation stage, member IDs, optional
entry-area ID and source origin. This is scenario data, not evidence that a
scenario or unit is complete, supported, source-reviewed or admitted. Missing
entry areas remain unknown, not an empty destination or a legal-placement rule.
Full formation, unit, map, timing, resource and obligation validation remains
necessary before admission.

The production content validator reports out-of-scenario timing, repeated
groups or members, initial/scheduled overlap, unknown sources or members,
invalid sides, and owner/placement mismatches. An entry-area ID is a source
concept, not a map hex. The scheduled-content successor may bind it to a
source-backed, nonempty set of distinct location IDs; all members must exist
in the content map and the area's source origin must be indexed. This defines
geographic membership only, not legal placement, stacking, control or routing.
An initial source group may record a side, location and distinct constituent
element IDs, without asserting that those elements are independent map
counters or deciding their assignment or attachment relationships. Group IDs,
source references and member/side/location references must validate. An
element cannot occur in two initial groups, in both a group and an independent
placement, or in both a group and a scheduled arrival. Every legacy format
rejects nonempty initial groups. The scheduled successor retains and hashes
them, and its combat-fact seed set includes their members. This source
interchange does not itself provide a stacking or formation rule.
The current `ContentPackV6` profile admits only synthetic
scenarios and restricts formations and combat data to its laboratory profile;
changing an origin flag cannot admit historical content.

Existing canonical content formats do not encode this schedule. Therefore every
old content validator and serializer must reject a nonempty schedule, including
internal draft encoding. An old pack with an empty schedule retains the same
bytes and identity. No campaign may be created from this successor until a
combat-capable content profile, world and event/snapshot admission are
implemented. This prevents silent schedule loss or accidental
`ExplicitNone` behavior.

The normal Snapshot11 no-obligation arrival action must also reject any
scenario whose scheduled land arrival is due or overdue at the current
turn and stage. Snapshot11 has no completed-arrival ledger, so it cannot
prove that an earlier scheduled group was handled. This is a fail-closed
boundary only: it does not adjudicate or admit the scheduled group.

## Canonical scheduled-content successor

The `sandtable.content-scheduled.v1` envelope contains the complete predecessor
content definition and an ordered schedule for each scenario. The predecessor
part is structural data, not a separately admitted artifact: scheduled units
are allowed to be absent from initial placements only when exactly one valid
group accounts for each missing placement. Every other content issue, unknown
reference, conflicting membership, wrong side, out-of-range slot, or missing
origin rejects. Content with no schedules, initial groups or entry areas uses
the old content format instead; an area alone is insufficient for this successor.
Declared
entry areas and their member locations are included in the canonical content;
area membership is not a complete placement rule. The envelope hashes all content and
schedule fields; parsing must verify bounds, shape and exact canonical bytes.
An arrival may also retain printed attached-truck quantities by light, medium
and heavy class. Each quantity is nullable: omitted source information is not
zero. At least one class must be specified and all specified quantities must be
nonnegative. Quantities inherit the arrival's source origin and are raw source
data, not an interpreted Land-only motorization allowance or a runtime pool.
The artifact identity records the successor format, pack ID, ruleset ID and
SHA-256 of those bytes. Reopening with an expected identity must compare the
recomputed identity; caller-supplied hashes do not make an artifact trusted.

This format supports source-backed content interchange only. It does not add
combat seed data, admit a scenario, or permit `CampaignCreationExecution` to
use a historical setup. The production runtime must separately support the
actual unit, map, rules, obligations, persistence and rights before admission.

The combat-facts validator has a separate successor entry point: its seed set
is initial placements, grouped initial members and scheduled members, with exactly one seed per member
and component checks identical to the predecessor. The old v5/v6 entry points
still reject scheduled content and retain their original placement-only meaning.
`ContentPackV5Validator.ValidateScheduledContent` is a public content and
current-vocabulary check. A successful result does not authorize creation,
prove source acceptance or certify any other campaign capability.
Both scheduled-combat and Content6 definitions expose their unchanged topology,
combat facts and scenario-specific TOE seeds through the common read-only
`ICampaignCombatContentDefinition` contract. This view does not change either
canonical format, identity, capability profile or admission decision.
This validates in-memory facts only; the scheduled content envelope above does
not yet encode those facts and is not a combat-capable campaign artifact.

The separate `sandtable.content-scheduled-combat.v1` envelope carries that
scheduled-content envelope, v5 component facts, the additional source-rating
fields and initial/grouped/scheduled TOE seeds, including origins. Structural validation requires
one complete seed set for every initial or scheduled unit and validates source
references, component ownership and maximum TOE. It preserves unsupported
component IDs as source data rather than relabeling them as Infantry. Separate
CNA compatibility validation still rejects unsupported classes at admission.
The successor may retain whether a printed defensive close-assault rating is
parenthesized. That marker is source data, not an implemented condition; the
current-vocabulary check rejects it, and legacy v5/v6 validators and serializers
must reject it rather than discarding the marker. Existing unmarked components
retain their old behavior and legacy bytes.
The successor also preserves an optional printed offensive close-assault rating
and its parenthesized marker. Null means not yet transcribed, not zero. The
current v5/v6 combat capability rejects populated offensive values; retaining
them in source content does not implement the attack rules.
Optional printed barrage ratings and their parenthesized marker are retained
with the same unknown-versus-zero distinction. Legacy and current v5/v6 combat
capabilities reject populated barrage values; the number alone neither selects
eligible firing TOE nor implements ammunition, range, targeting or results.
An optional weapon-source profile retains printed weapon CPA, AA, anti-armor,
vulnerability, armor protection, fuel rate and breakdown notation. Numeric,
parenthesized, printed-dash and unknown ratings are distinct. A printed dash
does not decide whether the rule means zero or not applicable. The profile is
source data only and old and current v5/v6 combat capabilities reject it; it
does not implement the corresponding movement, firing, fuel or breakdown rules.
Weapon CPA is a weapon-chart value, not an inferred unit movement allowance;
for example the NZ artillery unit's `x` row has CPA 20 while its 18/25-pounder
weapon row has CPA 15. Their interaction must follow the applicable rules.
An element may declare a source-backed maximum combined TOE across components.
Every declared initial or arrival seed must have a sum at or below that limit.
The legacy and current v5/v6 capabilities reject the new limit because they
do not enforce it throughout play; the content envelope preserves and hashes it.
The successor may retain a headquarters with no intrinsic combat components
when its source states that fact; its empty seed set is explicit. This is
structural content only and does not implement headquarters behavior. Legacy
v5/v6 entry points still reject such empty headquarters facts.
This does not encode the full gun characteristics, barrage targeting or
results, ammunition or every scenario obligation.
The corresponding artifact hashes the complete combat envelope, including the
underlying schedule, area membership, component facts and seeds. Reopening
requires an exact expected format, pack, ruleset and hash identity.

The Commonwealth chart §4.43a lists 6th NZ Field Artillery Regiment in the turn 2,
stage 3 row; its OA marks `3/2`, with six 18/25-pounders. The same row lists
2 RTR and 7 RTR. The chart's general entry rule refers to Cairo hexes subject to
exceptions and formation/stacking restrictions. These facts can exercise the
model, but one record is not the whole scenario and must not be serialized as
an admitted pack. Source rights and independent member-level review remain open.
The schedule, entry instructions and OA were visually rechecked from PDF pages
3, 4 and 18 of the Commonwealth chart package with SHA-256
`f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981`.
This is a same-stream check, not independent content approval. The test's
`cairo` value is an area identifier, not an established map-hex identity.
The turn-2 source row contains multiple arrivals, so the content model allows
multiple groups at one slot. It does not infer that those groups must share a
hex or that their placement order has been settled. A same-stream check of
the OA on PDF page 11 confirms the 2 RTR and 7 RTR markers both read `3/2`;
the unit details and legal placement are still subject to review.
Land §20.12–20.14, printed p. 31, states the arrival occurs at the beginning
of Naval Convoy Arrival, has no CP debit for debarkation or the first hex, and
may move during that same operation stage; ordinary Commonwealth reinforcements
enter at Cairo subject to the printed exceptions. This does not decide any
separate training, stacking or control restriction. The source's §20.11
example conflicts with the brigade's later schedule and remains a ruling issue.

Same-stream visual check of the same chart package p. 22 (§4.46a) identifies
the NZ unit's `x` ID as an artillery battalion-equivalent with unit CPA 20 and
maximum six artillery TOE points. Page 27 (§4.47) gives the 18/25-pounder row
gun CPA 15, barrage 8, anti-armor 2, vulnerability 5, close-assault 1/1 and
fuel rate 1, with dashes for AA, armor protection and BAR. Preserve those
distinct unit and gun fields; do not infer how their CPA values interact from
the table alone. Land printed pp. 19–20 §§11.11–11.15 and 12.1 give artillery
and combat-position rules, including Forward/Back distinctions. The normal
engine currently supports Infantry components only; a 1/1 close-assault rating
does not make this gun an Infantry component or implement Barrage. A separate
source and errata review is still required before admitting these values.
The production source-facts catalog exposes the Polish brigade's eight OA
members, the NZ artillery, 2 RTR, 7 RTR, 1st field artillery, 3rd Hussars and
7th Indian brigade schedule records, five listed Cairo area members and the NZ gun component's
maximum TOE and defensive close-assault rating as explicitly partial data.
The catalog also retains those two fields for 2 RTR's A13 Cruiser and 3rd
Hussars' Mark VI Light; armor is unsupported by the current combat vocabulary.
It also retains the ID-chart maximum TOE and defensive rating for the three
Polish infantry battalions, Kopanski machine-gun company, three 7th Indian
brigade battalions and three 6th NZ battalions. These are not seeded strengths,
training states or complete unit definitions.
The Polish, 7th Indian and 6th NZ brigade headquarters are separately recorded
with the source's `a` headquarters type and no intrinsic combat components.
The Polish cavalry, anti-tank company and artillery component records retain
their chart classes, maxima and defensive ratings. For the anti-tank company,
maximum TOE is two while the OA lists one 2-pounder; capacity is not an arrival
strength. Reconnaissance and anti-tank components are not supported by the
current combat vocabulary.
The catalog exposes the eight grouped schedule candidates and partial combat
facts for 20 of the 21 OA members. The remaining 7 RTR OA entry says `Matilda`,
while the only Matilda entry on the game's weapon chart is `Matilda Mk.II`.
The [regimental history](https://4and7royaltankregiment.com/1940-1941/) also
states that 7 RTR was re-equipped with Matilda IIs before going to Egypt. The
catalog exposes the Mk II match as an explicit candidate, excluded from the
default fact set until source review accepts the inference. None of the partial
records is a complete unit.
Twelve separately named Commonwealth placements from §60.41 are retained as
candidates, including co-location at C4131 and C3922. Axis garrison rows name
formations containing multiple counters, not single units; they require OA
expansion before they can be expressed as element placements. These candidates do not supply a complete setup, map,
formation relationships or legal stacking policy.
The French Motor Marine company candidate starts at C3926
with two medium truck points under §60.41. The OA's printed `3/65` arrival is
corrected to deployed (`D`) by errata §4.44b; its `v` characteristics and `N`
strength yield one infantry TOE with offensive/defensive ratings 1/1. The `10+`
CPA and trucks require motorization authority; this source candidate does not
implement that behavior or independently review the transcription.
The 16th Infantry Brigade candidate records four deployed OA members at E1829:
its headquarters and three infantry battalions. Its HQ's three 3.7-inch
howitzers and the battalions' normal six-point strengths are retained with
source-linked component facts; the separately scheduled anti-tank company is
not placed at setup. The 20 medium truck points are group source data, not
unit assignments. One OA deployment marker appears lowercase in the scan;
independent reading, formation relationships and the Cairo map remain open.
The 4th Indian Division candidate records its 11 listed deployed members at
D3615, excluding 31st Field Artillery and the later 7th Indian Brigade and
1st Field Artillery. Ten have partial combat facts; Central India Horse's
initial training-dependent characteristics remain unresolved. The group keeps
the printed truck totals without assigning them to members.
The 7th Armored Division candidate retains the seven deployed OA members left
at D3612 after the scenario's individually listed detachments, including 8th
Hussars' seven A10 points. Its three HQ tank components retain conditional
ratings. The later 7th Motor Brigade HQ and the separately deployed units are
not included. Attachment, motorization and vehicle breakdown remain open.
The Axis setup candidates also retain Aresca HQ and XXI(L) at C3919, plus a
partial Tripoli/Trivoli regiment subset at Bardia: HQ, XX(L) and LXI(L).
The OA lists each battalion with nine CV33 points and each HQ with one; the
HQ tank ratings are conditional under the parenthesized headquarters rule.
IX(L) and II(M) are detached elsewhere. LX(L) is deliberately excluded from
the Bardia subset because the OA places one of its companies with Maletti;
remaining strength and location need reconciliation. The scenario's 20 medium
trucks are shared by the Bardia entry and are not assigned to the subset.
The scenario and OA spell the regiment differently, so the match remains a
source candidate pending independent review.
The Maletti source candidate lists its 15 printed OA members at C3617 and the
scenario's five light, 30 medium and five heavy truck points. Eleven members
have partial component facts and initial TOE: five Libyan infantry, three
Sahariano reconnaissance battalions, two artillery units and II(M) tanks. The OA
says the regiment was camel in name only. Headquarters facts
and the OA's separate LX(L) company remain unresolved. Co-location does not
implement attachment, vehicle state, motorization or split-unit handling.
The Catanzaro source candidate records the 13 listed counters at C4707 and its
30 medium and five heavy truck points. Partial facts cover 11 counters; the
division headquarters and engineer remain unresolved. Its 203rd Artillery has
six 75/18 gun-howitzers, three 105/28 guns and three light AA points, with the
separate nine-gun and 12-point combined limits retained. This is a source
candidate, not admitted combat or independently verified scenario content.
The 1st CCNN candidate records its 14 listed members at C4218, including the
attached I(M) tank battalion, and the scenario's 10 light, 25 medium and 10
heavy truck points. Twelve members have partial component facts; the division
HQ and engineer remain unresolved. The 201st Artillery seed retains six 75/27,
three 100/17 and two heavy AA points, with conditional AA ratings and the
separate gun and combined TOE limits. Source membership does not implement
attachment, motorization or historical combat.
The 4th CCNN candidate records 12 listed members at C4507 with 10 light, 25
medium and 10 heavy truck points. Ten have partial component facts. The 204th
Artillery seed retains six 75/18, three 105/28 and two heavy AA points within
the nine-gun and 12-point combined limits. The division HQ and engineer remain
unresolved; the group is not admitted.
The catalog separately retains the Tobruk OA's 13 named infantry/headquarters
and nine artillery/anti-aircraft counters at C4807 as two source groups. It does
not model their special restrictions, attachment or the naval San Giorgio entry;
this is not an accepted or complete setup.
The nine Tobruk artillery/AA counters also retain their printed component
strengths, including mixed gun and AA values. Parenthesized land-barrage
eligibility, gun emplacement, AA motorization, conditional ratings and
aggregate TOE limits are not represented by those numbers and must be
implemented separately. The source says the listed AA points are not emplaced
and may be motorized; do not infer that they are fixed like the guns.
The three non-coastal artillery counters have source weapon profiles, and the
standalone AA counter retains its parenthesized close-assault rating. The five
coastal units still lack combat facts because their component restrictions and
combined capacities need a richer runtime contract.
Italian unit chart §4.46b identifies ten of those infantry counters as `x` or
`v`, with maximum TOE five or six respectively and defensive rating one; their
OA `N` markings supply starting strengths. The three headquarters have
parenthesized values and are left unmodeled until those conditions can be
represented; the Commonwealth code table must not be applied to them.
The Bardia OA lists 11 land counters at C4321. Its row names a 30th GaF
headquarters but prints the counter abbreviation `32GaF`. The original counter
section 2 labels a `30 GaF` counter Bardia and a separate `32 GaF` counter
Tobruk, supporting the 30th identification and an OA abbreviation typo. The
candidate retains both source references; independent review remains open. Its
three infantry battalions use Italian code `u`, and its VEC machine-gun
battalion uses code `aa`. Their maximum TOE and defensive ratings are derived
from §4.46b; the OA `N` marks supply starting strengths.
The Bardia anti-tank battalion, 8th artillery regiment and AA group also have
partial component facts; all six support counters have printed TOE seeds, including
mixed gun types. Coastal-defense restrictions, AA conditions and combined unit
capacity are not inferred from those counts. The 9th and 10th artillery
regiments declare the chart's shared maximum of nine across their two weapon
types. The 75/27 gun's printed anti-armor value has a time-dependent footnote;
its source-profile value remains unknown until that condition is represented.
The Giarabub OA lists six counters at C1014; its machine-gun unit is `U@4`.
Six partial combat-fact records and all six printed strengths are retained.
The light anti-aircraft defensive rating is preserved as parenthesized, not
treated as an unconditional rating.
The minor-garrison OA separately identifies single counters at Scheferzen,
Fort Maddalena, el Grein, Mechili and Benina, plus a Soluch anti-tank company
and artillery regiment. Counter section 2 depicts the two Soluch counters
separately, corroborating their identities. All seven have partial component
facts and printed strengths; the regiment has twelve 100/17 howitzers. Benina's AA combat
rating is marked conditional but its condition is not implemented. Fort Maddalena
and Mechili are `U@2`. Independent source review and runtime stacking remain open.
The Derna OA lists four land counters at B5925. The 54th Territorial unit is
one brigade-equivalent counter at normal TOE; its infantry facts and all four
printed strengths are retained. Coastal-defense and AA restrictions remain
unmodeled.
The September errata's replacement OA lists the initial Cirene and Marmarica
divisions as 15 and 14 counters respectively, including their initially
attached tank battalions. The source groups preserve the members at C4120 and
C3918 without implementing assignments or attachment. Their twelve infantry
battalions and two machine-gun battalions have partial chart characteristics
and `N` strengths; both anti-tank companies have their printed two 47/32 guns.
The two divisional artillery regiments retain their printed gun and light-AA
strengths. A component-pool limit binds the two gun types to at most nine TOE
points, separately from their three-point AA allowance. Pool IDs, member IDs,
limit, and source origin are canonical content identity; initial TOE must satisfy
every pool limit. The shared World component validator now checks combined and
per-pool limits against current TOE as well. Legacy formats still reject these
limits, and a historical successor must invoke the validation on every change
before it can admit them. This does not describe every weapon
that could later be assigned to these regiments.
The initial group record retains 45 medium and five heavy truck points for
Cirene and 40 medium and five heavy for Marmarica. Unspecified light quantities
remain unknown. These quantities are source data, not allocated vehicles or
implemented motorization. The remaining division members and their mechanics
are incomplete; the records are not admitted campaigns.
The original Axis OA p. 5 and scenario §60.31 also give candidate groups for
1st Libyan at C4020 and 2nd Libyan at C3920, including the latter's attached
IX light-tank battalion. Their twelve `x` infantry battalions each have five
starting TOE points, and each of their four regimental headquarters has one
65/17 gun. The source lists six 75/27 guns and two 47/32 guns for
1st Libyan, and nine 75/27 plus three 100/17 guns for 2nd Libyan; the latter
shares a twelve-point artillery capacity. The groups preserve the printed
10 light/15 medium and 15 light/15 medium trucks respectively. The Libyan Tank
Command OA p. 6 lists IX's nine CV33s, and the weapon chart gives their
characteristics; the source catalog retains those facts without implementing
armor or deciding how its attachment behaves. Division headquarters, vehicle
allocation and runtime mechanics remain incomplete.
The checked counter sheet is PDF page 182 of the supplied full scan, SHA-256
`836c14949c4fef1b066043e19a570256b22598d8ae2948d26844b949f53b60a5`.
The Benghazi OA (Axis charts p. 12) lists II Libyan coastal defense, VII
coastal-defense AA, XX Corps artillery and VIII/II engineers. The source
candidate retains those counters, a five-point Libyan battalion, six light AA
points and six 100/17 howitzers against the artillery's nine-point maximum.
The engineer has no combat facts or strength seed yet. The OA labels VIII/II
an engineer battalion but
prints ID code `bb`; Italian characteristics §4.46b assigns `bb` to a
machine-gun battalion and `bbb` to engineers. The original counter section 2
(full scan PDF p. 182, SHA-256 above) labels VIII/II Benghazi and depicts the
same engineer symbol used by the 62nd and 63rd engineer counters. That supports
`bbb` as a candidate correction to the OA, not an independently accepted ruling.
The same OA lists
the garrison at A4827, consistent with the location summary and RUL-GRA-05's
working coordinate, while scenario §60.31 prints B4827. This source candidate
is incomplete and not admitted. A text search of the
retained September errata found no `Benghazi` or `VIII/II` match; this does not
prove that no correction exists in another source or in unrecognized scan text.
The `2-rifle-brigade` identifier preserves the printed unit name; it does not
assert a brigade-equivalent game size.
The OA and unit/weapon charts also supply partial component facts and TOE
seeds for the 12 named Commonwealth starting units. In particular, 1 RTR
starts with seven A9s despite a maximum of ten, 11th Hussars starts at six
despite a maximum of eight, and 3 RHA starts with six 2-pounders despite a
maximum of eight. These facts do not represent fuel, morale, readiness,
complete weapon capabilities, legal stacking, or a complete initial force.
Four alternative source groups retain the nine Commonwealth units
at C4131, C3922, C3721 and C3926 and their printed truck quantities. They overlap the
individual placement candidates and therefore cannot be included together in
one scenario; neither representation decides unit-level truck assignments.
Land §4.45 defines `N` as maximum TOE and `U` followed by a value as an
understrength arrival. It also says tank and gun OA rows list their actual TOE
and weapon type. The catalog may therefore construct source-linked TOE seeds
for all 21 members, including 7 RTR's explicitly listed ten Matildas without
deciding the weapon variant: notably one point for Kopanski (`U@1`) and one 2-pounder
for the Polish anti-tank company (whose capacity is two). This does not decide
training, the NZ date dispute, unit composition beyond the OA, or admission.
The Indian brigade record retains its printed 5 light and 20 medium attached
truck points; its unspecified heavy value remains unknown.
The 6th NZ Brigade chart candidate retains its four OA members and 25 light,
85 medium and 5 heavy points at turn 6, stage 3. Land §20.11's example
conflicts with that date; this candidate is not an accepted ruling or admission.
The gun's other ratings and an arrival strength are not provided by that record.
It does not represent the complete scenario, map topology or an admitted
content pack. Source identifiers must be
present in any pack that incorporates these records.
Common chart §9.4 lists a full battalion-equivalent as one stacking point;
the unit's `x` chart entry identifies its equivalence. This is not by itself
proof that a particular Cairo hex has space or that co-arriving groups may
share it. Land §9.24 also gives artillery one stacking point with stated
exceptions, and §9.26 uses a less-than-25% TOE shell threshold for artillery,
distinct from the ordinary battalion's less-than-50% threshold. Those later
strength-dependent rules must not be collapsed into a fixed one-point value.
As a bounded arithmetic check, six available TOE points at barrage rating 8
would yield 48 raw barrage points and five actual points under §11.32 rounding,
before terrain shifts and the Barrage Table. This is conditional on legal
participation and ammunition; it is not an adjudicated attack or a claim that
the complete unit arrives combat-ready. The source's §12.1 position choice,
target/stacking rules and logistics must also be implemented and replayed.
The normal `CampaignNativeCombatBarrageV14` is explicitly an Infantry-only
no-effect transition and rejects any non-Infantry component. Historical
artillery needs a distinct successor Barrage event and adjudication path;
reinterpreting that V14 event would change old histories.
