# Graziani source decisions: repair facilities and victory reference

**Status:** selected working interpretations for planning and contract drafting;
not implemented or admitted into a ruleset. An independent agent checked the
bounded Matruh and 6th NZ transcriptions, not the interpretations themselves. These choices
respond to the requested source investigation, but do not close MVP-GATE-01 or
replace the project's required source/contract review. A release implementation
must version the adopted result and bind tests to its ruleset identity.

## RUL-GRA-01: Commonwealth temporary repair facility

**Selected interpretation:** Land-only §60.92(C) replaces the Commonwealth
initial **supply** portion of §60.44; retain the separately stated temporary
repair facility at Mersa Matruh. Evaluate major facilities under Land §22.31,
subject to any scenario-specific conflict and later source ruling.

**Evidence:** original scenario pp. 28–29 put the facility statement after the
supply paragraphs, and the replacement is labelled Commonwealth initial supply.
Original Land p. 35 distinguishes scenario-designated temporary facilities from
generally listed major facilities. The same text appears in the separately
packaged [Air/Logistics booklet](https://spigames.net/PDFv10/CNA_AirGameRules.pdf)
pp. 28–29. Its SHA-256 is
`9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`.
This is a second source package of the same printed material, not an independent
edition or independent reader. The [community scenario interpretation](https://basmith.net/cna/rules/scenarios/10-the-italians)
retains Mersa Matruh, but is corroboration only; it does not explicitly settle the
replacement wording.

**Alternative:** replacement of the entire numbered case removes the temporary
facility. This is supported by the literal case reference. The selected reading
gives effect to the replacement's supply-specific heading without discarding a
separately described game object. It is an interpretation, not a discovered
official correction. If source review rejects it, retain both possibilities and
change the versioned ruling before implementation; never reinterpret an existing
save silently.

**Separate conflict:** §60.33 lists a temporary facility at Tobruk while §22.31
lists a major facility there. Do not extend this decision to Tobruk: record its
scenario-vs-general precedence, control and later capture behavior separately.

**Required contract vectors:** initial Matruh temporary facility present; §60.92(C)
supply replaces rather than adds to §60.44 supply; major-facility behavior has
separate source and control checks; absence or mixed interpretation fails identity
validation; repair eligibility, resources, capture, checkpoint and replay covered.
Fixture construction is not evidence until source cross-check and scenario schema
are accepted.

## RUL-GRA-02: victory reference

**Selected interpretation:** treat §60.22's pointer to §60.71 as a mistaken
cross-reference and use the specifically named Graziani victory conditions in
§60.81. Section §60.7 concerns initial construction, while §60.82 is explicitly
the longer Italian Campaign. The separately packaged booklet repeats the same
cross-reference and named headings; this corroborates transcription, not an
independent edition. The September errata text search did not locate an explicit
§60.71 correction; absence in searchable text is not proof no correction exists.

**Alternative:** follow the printed reference literally, leaving the scenario
without identifiable victory criteria. Rejected for this working interpretation:
the named section and scenario duration identify a specific matching rule. This
does not choose winner precedence, no-victory outcome, simultaneous outcomes,
or the abstract-supply route semantics; those remain separate open decisions.

**Required contract vectors:** select the six-turn scenario's own victory table,
never the twenty-turn Italian Campaign's table; evaluate only at the accepted
terminal boundary; cover each side and level, overlapping and no-match cases,
source-specific supply checks, stable result event and replay. Winner precedence
and route fixtures cannot be frozen until their separate rulings exist.

## RUL-GRA-03: Matruh initial assignment, working interpretation

**Selected interpretation for planning:** treat the specific September errata
§4.44b statement as an exception to the Matruh OA sheet's no-assignment sentence:
the 1st Buffs and 1st Hampshires begin assigned to Matruh Garrison. They must
each have one identity, rather than being duplicated by scenario/OA expansion.
Source §19.1 distinguishes assignment from attachment; do not change the
assignment to a mere attachment or assume the two are always co-located after
play begins. This is a new working interpretation, not an accepted ruleset or
proof of a complete setup.

**Evidence and competing interpretation:** the erratum names both units and
explicitly uses assignment, while the Matruh OA sheet disallows assigned units.
Specific later errata can reasonably override that general sheet statement. An
[independently maintained project](https://tangled.org/glassparency.bsky.social/cna-agent-sim/blob/main/informatiion/CURRENT-CHECKPOINT.md)
reports the same reading, which is corroboration rather than an official ruling
or independent transcription here. Another interpretation treats “assigned” in
the erratum as informal language for attachments, preserving the OA's general
prohibition. A third defers content admission. Keep those alternatives reviewable.

**Not decided:** default physical co-location/attachment under §4.25 versus the
§59.2 pregame reorganization choice; whether exceptional assignees count against
the OA's six-attachment cap in light of errata §19.5; retained assignments after
detachment, initial stacking, and HQ movement's close-assault exception. Source
and contract review must select and version these separately. The three additional
units expressly attached in §60.41 are not thereby assigned to Matruh.

**Required vectors:** each named unit exists once; assigned and attached
relationships can vary independently where rules permit; start-time choices,
capacity and actual co-location are validated; different attachment/detachment
timing and CP costs are preserved; historical ruleset identities cannot silently
change. Cover the OA's normal HQ movement restriction and its close-assault-stage
exception, loss/capture and deterministic replay. Precise positive cases require
the unresolved subdecisions and an independent review.

## RUL-GRA-04: 6th NZ Brigade arrival, working interpretation

**Selected interpretation for planning:** use the Commonwealth schedule and OA
sheet, both of which place the brigade HQ and three battalions at turn 6/stage 3;
the separately listed 6th NZ Field Artillery arrives at turn 2/stage 3. Treat
§20.11's example placing both at turn 2 as an apparent example error. A schedule
and its OA chart are separate source records but not independent editions. A
fresh-context agent separately checked the bounded transcription; see the
[review disposition](graziani-source-review/review-disposition.md). This does
not independently settle the conflicting example or approve the interpretation.

**Alternatives:** follow the example's turn-2 brigade date, or defer admission
until new official evidence is available. The selected reading follows §20.11's
own general rule that the reinforcement track specifies arrivals and preserves
the two explicit, mutually consistent chart records. No official erratum for
this example was found in the visually checked September packet; later sources
remain possible. This is a working interpretation, not accepted runtime data.

**Required vectors:** the artillery may arrive on turn 2 while all four brigade
records and their listed truck allocation remain absent until turn 6; the
brigade appears exactly once at the correct phase with authorized Cairo placement,
training and movement rules; malformed or duplicate arrivals reject atomically.
Check pre-arrival/arrival checkpoints, source/ruling hash changes, interruption,
replay, side-safe views and all accepted initial choices. Verify chart data and
the complete scenario schedule independently before a positive fixture is frozen.

## RUL-GRA-05: Benghazi setup coordinate, working interpretation

**Selected interpretation for planning:** use the named Benghazi city hex A4827
for both the initial garrison and the Land-only supply row, treating B4827 in
scenario §§60.31 and 60.92(B) as a repeated section-letter error. This interpretation
is not admitted content or an approved release ruling.

**Evidence:** the retrieved original Map A contains Benghazi and its location
summary identifies A4827; the original §37.42 distance chart and an additional
location summary agree. The city's printed map symbol obscures the underlying
number in the inspected render, so the map's summary and adjacent numbering are
the direct evidence. These records share an edition and cannot be counted as
independent readers. A separately maintained project's published notes report
using A4827, which corroborates another interpretation but is not official
clarification or a second source transcription here. The [map pilot](2026-11-12-graziani-map-identity-pilot.md)
records hashes, links and caveats.

**Alternatives:** use B4827 literally despite the named location conflict; or
defer both rows pending official clarification. The first would require explaining
why named city and port features refer to a different section; the second remains
the safe admission boundary until independent review. Preserve both printed
source values and version the selected behavior; do not alias all same-number
hexes across sections or silently alter prior saves.

**Required vectors:** named Benghazi and both initial rows resolve to the one
accepted city identity; B4827 remains a distinct location if admitted by map
topology; wrong section or missing named city rejects rather than relocating
silently. Check the selected city, control/port, placement, content/ruling hash,
replay and old-version compatibility with independent source review. This
coordinate choice does not authorize assuming roads, distances or legal supply
routes from the still-unresolved map seams.

## RUL-GRA-06: French Motor Marine company setup, working interpretation

Use the September erratum's `D` marker for the 1st French Motor Marine Company
instead of the OA sheet's printed `3/65`. Land §4.45 defines `D` as deployed
at the first scenario start; §60.41 lists a matching 1st company at C3926 with
two medium trucks. For planning, treat these as one initial company and do not
create a second arrival from the superseded marker. The [source comparison](2026-11-12-graziani-arrival-scope.md)
retains the original marker, correction and locators.

This follows an explicit correction rather than inventing a new date. Verify
counter and TOE identity, the exact relationship to any similarly named later
formation, and the independent second reading before content admission. Version
the selected mapping and test one initial identity, no duplicate schedule entry,
truck conservation, scenario selection and replay.

## Review boundary

The original scenario and Land rules were visually inspected in this session chain;
the two scenario PDF packages contain the same printed pages. A fresh-context agent
independently double-entered a bounded set of Matruh, NZ and formation source
facts; [review disposition](graziani-source-review/review-disposition.md) preserves
its scope and unresolved locator wording. It did not approve these decisions or
provide a separate human review. No authoritative physical copy of the formation
addenda has been obtained. Review must include source hierarchy, competing
interpretations, applicable errata, rights/provenance and the proposed negative
cases before these working choices become release rules.

## Open conflict: Tobruk port efficiency

Scenario §60.7 gives Tobruk an initial efficiency value of 7 and identifies the
partially blocked harbor. The [common port chart](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf)
§55.3 lists Tobruk's normal maximum as 5, and [Air/Logistics rules](https://spigames.net/PDFv10/CNA_AirGameRules.pdf)
§55.25 assigns the obstruction a three-level reduction. The three printed facts
cannot be combined into a value of 7 within the chart's normal maximum.

Candidate readings: preserve scenario-specific 7 as an override and define how
values above the chart maximum behave; use chart maximum 5 minus obstruction 3,
giving **2**; or reject the setup until authoritative clarification. A
[community interpretation](https://basmith.net/cna/rules/scenarios/10-the-italians)
chooses 2, but it is corroboration rather than authority. This packet does not
select or adopt a value: the Land-only profile must first establish whether and
where port efficiency affects §32 simplified convoy or other reachable behavior.
Do not infer that the conflict is irrelevant merely because detailed logistics
is abstracted.

One concrete interaction exists in the original Land §32.36: Commonwealth sea
transport of a supply unit has a destination-port eligibility threshold tied to
the port's available capacity. If Tobruk is a legal destination at that point,
the selected initial value, later damage and control can affect eligibility.
The original §55.14 example and §55.3 chart express capacity as a fraction of
listed maximum efficiency. If the candidate value 2 means two remaining levels
of Tobruk's listed five and no other modifier applies, that is 40% of listed
capacity, below the §32.36 threshold. This is a conditional arithmetic example,
not an adopted starting value. Whether Commonwealth control and the relevant sea
transport are reachable in this six-turn scenario remains unproved; a unit test
may use a synthetic state to prove the conditional rule but cannot establish
scenario reachability. §32.61's no-tonnage-limit statement is a separate
Axis convoy rule and does not by itself settle §32.36.

Review requires original source/errata and independent scenario applicability
checks. If a value is adopted, version it and test initial state, capacity bounds,
arrival/repair/blockage interactions, control changes and replay. Keep this issue
separate from the Tobruk **repair facility type** conflict in RF-01.

## Open interaction: starting city fortifications

Scenario §60.7 says there are no fortifications at the start. Land §§25.11–25.12
state that major cities have intrinsic fortification levels, with Cairo and
Alexandria at level 3 and other major cities at level 2. Two possible readings
are that the scenario removes every fortification effect, including inherent
city effects, or that its setup statement concerns separately constructed
fortifications while city properties remain. The scenario sentence does not
explicitly distinguish these categories; its heading concerns construction on
the map at the start, which supports considering the second reading but does
not by itself settle it. The original Land p. 38 and scenario
p. 28 were visually checked by one reader. September errata §25.15 corrects a
repair cross-reference; a text search of that errata did not identify a direct
§25.12 or §60.7 correction, which does not prove none exists elsewhere. Verify
other cases, corrections and scenario precedence before adopting either
interpretation.

This matters to Combat and to objective cities; it is separate from whether a
player can construct a new fortification later. Review must pin each city hex,
control, initial level, source-specific effects, construction/destruction and
rebuilding behavior. Test a named major city, an ordinary village, a separately
constructed fortification and the highest city level under the selected rule.
No scenario map or fortification effect is admitted by this note.

## Open interaction: repair supplies in Land-only mode

The [repair resource matrix](2026-11-13-graziani-repair-resource-matrix.md)
separates source categories and records a candidate no-ordinary-debit reading;
repair availability and independent review remain open.

Original Land §22.13 makes repair conditional on supplies, while §§22.26 and
22.35 specify fuel and, for facility work, stores expenditures. Section 32.11
replaces ordinary resource usage/expenditure rules with §32's listed supply
procedures; abstract supply units themselves contain fuel and ammo rather than a
separate stores balance. Whether the abstract mode waives those repair expenses,
requires an alternative availability check, or needs a specific additional ruling
is not established by the inspected provisions. Section 22.42 also explicitly
qualifies a different destroyed-tank expenditure as Logistics Game only; that
qualification does not by itself settle all other repair cases.

Review each repair category, timing, eligible resources, source exception and
versioned result before implementing the scenario. Negative vectors must cover
insufficient resources under any adopted cost, no invented stores in Land-only
mode, failed repair's source-defined consumption, weather/ZOC restrictions,
randomness and replay. Facility existence/type decisions do not close this
separate expenditure question.
