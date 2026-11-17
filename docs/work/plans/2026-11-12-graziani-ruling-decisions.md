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

**Scoped setup and capacity interpretation for planning:** apply §4.25 as the
default for co-located assigned units, then permit the owning player's §59.2
initial reorganization of units in the same hex within applicable organization
and stacking restrictions. The source-listed Essex, Durham and South
Staffordshires attachments are the listed disposition, not a prohibition on a
legal owner-selected rearrangement. Treat the erratum's two exceptional
assignees as assigned units under §19.4 and errata §19.5: while assigned to
Matruh, they do not consume the chart's six **additional** attachment slots,
whether attached or detached. Count source-listed non-assigned attachments
against that limit, subject to the chart's type limits and company modification.
Assignment persists when attachment changes unless a separately legal
reassignment occurs. The ordinary post-setup timing and CP costs of §§19.41–19.44
remain distinct from the §59.2 setup choice.

This resolves the interaction for planning under RUL-GRA-03; it is not an
official clarification or admitted runtime authority. This selects the general
errata's additional-attachment rule over the OA's literal statement that at most
six units may be attached to Matruh; the latter also supports an absolute count
of all attached units. The [independent ruling review](2026-11-17-graziani-rulings-independent-review.md)
retains both as viable readings. Section §59.2 also has a special rule for
source-listed formations exceeding organization limits; do not reject those
automatically or allow new attachments in violation of its restrictions.
Review that alternative, the unit's actual
starting locations, initial stacking, and the HQ's close-assault movement
exception before versioned admission. Do not infer Essex or Durham's assigned
parent from their `23/70` counter designation. The three additional units
expressly attached in §60.41 are not thereby assigned to Matruh.

The [independent capacity follow-up](2026-11-17-graziani-matruh-capacity-followup.md)
supports the additional-slot interpretation without resolving the OA's
six-total alternative. It also distinguishes a brigade-level HQ from the
chart's brigade-equivalent free-company allowance, so eligibility for that
allowance remains open. Buffs and Hampshire's errata assignment alone does
not specify their hex; apply the co-location default only after actual initial
locations are established. Source-listed over-limit exceptions do not license
an owner to create a new excess.
The reacquired [counter-sheet reading](2026-11-17-graziani-matruh-company-next.md)
finds one `Selby / Matruh` counter marked `X`, `G` and `(3)`. This supports
nominal brigade-sized identity; it does not settle current shell status, the
exceptional assignment denominator or free-company eligibility.

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

**Independent source follow-up:** a [fresh-context map reading](2026-11-17-graziani-map-independent-review.md)
separately confirms both scenario `B4827` entries and the map/land-distance
`A4827` labels; its Map B inspection finds `B4827` inland without a visible
Benghazi city/port mark. This is same-edition source corroboration, not an
independent approval of this interpretation or a seam certificate.
An [independent ruling review](2026-11-17-graziani-map-followup.md) separately
supports using A4827 as a versioned named-city interpretation while preserving
both printed B4827 references. It found no coordinate correction in its bounded
errata search; this is not an official correction or release admission.
A [300-dpi geometry check](2026-11-17-graziani-map-next-review.md) uses the
six neighboring printed cell numbers to corroborate A4827 despite its obscured
center, and directly reads B4827 as a different rock/gravel hex. The neighbor
identification is geometric inference, not proof of edge traversability.

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

## RUL-GRA-07: Bounded facility and victory planning policies

For subsequent contract drafting, select the following **planning** policies
from the [independent facility review](2026-11-17-graziani-facilities-independent-decision.md)
and [victory review](2026-11-17-graziani-victory-independent-decision.md).
These choices interpret ambiguous text; they are not official errata, admitted
runtime data or proof of scenario reachability. Bind any implementation to new
immutable ruleset identities, and retain alternatives for review.

- `graziani-facility-start-v1`: initially use the scenario-specific temporary
  facility at Tobruk, not a duplicate major facility. Facility type following
  a control change remains unsupported pending a separate decision.
- `graziani-cw-facilities-v1`: follow RUL-GRA-01's narrow supply replacement;
  retain Matruh's temporary facility and apply the general Alexandria/Cairo
  major-facility list, subject to verified locations and control.
- `graziani-city-fortifications-v1`: the scenario starts without separately
  constructed fortifications; retain intrinsic major-city levels 2, or 3 for
  Cairo/Alexandria, and the source rules for later reduction and rebuilding.
- `graziani-repair-abstract-v1`: for Land-only repair, treat the ordinary
  repair supply-availability condition and ordinary resource expenditures as
  displaced by §32.11's exclusive supply procedures. Other timing, type,
  facility, towing, weather and control prerequisites still apply. Detailed
  Logistics remains a separate rules profile. Enumerate admitted repair types
  and source-specific exceptions, including §22.38 construction, in the future
  contract; unreviewed operations remain unsupported. The displacement of the
  availability condition is an explicit interpretation, not a consequence of
  the no-expenditure clauses alone.
- `gra-victory-result-v1`: evaluate all six victory rows on one terminal
  snapshot, preserving match/no-match/unknown and source-specific reasons. A
  known false required conjunct makes its row no-match; otherwise an unavailable
  required conjunct makes it unknown. Any unknown row yields an unresolved
  result; otherwise report each qualifying side's highest level, preserve all
  matches, and report both sides when both qualify without inventing a sole
  winner. Neither side matching is a distinct no-qualifying-victory result.
  Filter authoritative row evidence and reasons separately for each permitted
  player view; do not expose hidden state through the verdict.

Exact terminal ordering, row predicates and Land-only victory routes remain
open. Do not emit a historical verdict from the composition policy without
those prerequisites. Tobruk's initial port efficiency, facility type after
capture, exact facility locations, and all source-specific exceptions remain
separate from these bounded choices. The linked reviews retain competing
readings and distinguishing test vectors; independent source/contract review
and contract-first implementation remain required.

[Independent policy review](2026-11-17-graziani-selected-policies-review.md)
checks these distinctions against the proposals. Block automatic Tobruk facility
conversion on capture, allow intrinsic city levels to fall to zero, and defer
rebuilding until its action rules are specified. Verify city hexes and control
before setup admission. The review did not independently re-read the scans.

## RUL-GRA-08: Abstract supply stacking, bounded planning interpretation

For Land-only planning, interpret §32.14's three-unit restriction as applying
to ordinary **on-map non-major-city** hexes as well as the text's stated other
off-map hexes; retain five for a major-city hex and the explicit unlimited
Tripoli/Tunisia boxes. This is an interpretation of the printed “off-map”
wording, not an official correction. The alternative is to apply three only to
the printed off-map category and leave ordinary on-map capacity unspecified.
Supply-unit stacking value remains zero; the selected per-hex supply-unit count
limit is a separate restriction, not a claim that each supply unit costs one
stacking point.

Scenario §60.92(C) fixes five regular supply units at Matruh, and the map
summary does not identify Matruh as a major city. Under the selected three-unit
ordinary limit, preserve all five as an unavoidable scenario setup exception
under §59.65: admit no additional supply units while the stack exceeds the
selected limit, and once the distribution becomes compliant enforce **all**
normal stacking restrictions for that hex from then on. This is a per-hex
transition, not a reusable overstacking allowance; do not extend it to optional
extra supplies or other owner-selected placements without separate source
analysis. Do not delete or relocate the fixed units to force a nominally legal
initial state. Their resource quantities and placement remain sourced separately.

This is a planning policy, not admitted game authority. Determine whether dummy
units count toward each capacity and setup exception before implementing those
cases; unknown dummy interactions fail closed. Test ordinary on-map and off-map
hexes at/below/above three, major cities at five/six, unlimited box scope, the
five initial Matruh units, attempted additions during overstacking, reduction to
compliance, subsequent re-entry and all normal stacking limits, rejection of
optional excess placement, and deterministic replay under an immutable ruleset
identity. See [source interaction](2026-11-12-graziani-abstract-logistics-crosswalk.md#matruh-supply-stacking-interaction).
The [independent source review](2026-11-17-graziani-supply-stacking-review.md)
confirms the cited rule text and flags limits of its separate map check.

## RUL-GRA-09: Monthly motorization and dummy supply, bounded planning

Select these planning interpretations for a versioned Land-only contract,
subject to review and admission. They do not establish official corrections or
authorize a runtime change. See the independently source-checked
[decision proposal](2026-11-17-graziani-rules-followup.md) for alternatives,
locators and tests.

- Use common chart §58.5 for §32.57's abstract motorization percentages and
  select the previous calendar month as lookup key. Preserve the source dash
  as unspecified, distinct from zero; a loss event that needs it remains
  unresolved. Rounding admission, arrival/snapshot ordering, first-month treatment,
  eligible assigned points and removal shortfalls remain separate gates. In
  particular, §32.65's convoy rounding is not a monthly-loss rule.
  Scenario §60.22 starts at turn 1, Operations Stage 1; Land §5.2 places the
  game's Naval Convoy Stage before that stage. A literal entry at Operations
  Stage 1 skips the turn-1 convoy preamble; whether setup instead accounts for
  or includes it remains an interpretation. If skipped, turn 2 is the first
  encountered convoy stage. Decide whether it counts as September's first
  before assuming October is the first loss event. Under the selected previous-
  month key, a September event reads August's dash. Do not infer an initial-
  month exemption. See [independent calendar check](2026-11-17-graziani-first-month-review.md).
- For a numerical monthly percentage and an independently accepted snapshot,
  use the [round-down planning candidate](2026-11-17-graziani-rules-next.md):
  apply the percentage once to the player's total on-map motorization points
  including Tripolitania–Tunis boxes, and round down using exact integer
  arithmetic, with no minimum or carried fraction. This denominator is distinct
  from the source's restricted removal locations on maps E or A.
  This is a project choice by analogy with the type-specific convoy rule, not
  a printed monthly rule. Insufficient eligible removal points leave the
  transaction unresolved and unchanged pending a separate shortfall policy.
  An [independent review](2026-11-17-graziani-rounding-review.md) flags this
  denominator/removal distinction and leaves assigned-point consequences open.
- Under RUL-GRA-08's selected count limit, count both real and dummy Supply
  Units; the supply-unit name and §32.34's explicit inclusion for rail support
  this interpretation but do not expressly settle §32.14. Use §60.92's fixed
  starting dummy groups as the initial allotment rather than automatically
  adding a second ratio-derived grant. The source does not explicitly resolve
  additive or ratio-only alternatives; future receipts, optional initial real
  units and §59.53's dummy-dump wording require separate decisions.
- Apply the selected cap and §59.65's unavoidable setup exception separately
  to each hex. A voluntary co-location of real and dummy groups is not itself
  an unavoidable exception; prove available placement against certified map
  data first. Preserve fog-of-war before discovery when validating placement
  and reporting actions, failures, costs, aggregate totals and events; allow
  source-authorized disclosure and removal upon discovery under §32.18.

Do not synthesize an outcome for a dash, unavailable eligible
points or unverified initial placement. Admission requires the proposal's
month-boundary, side/rounding, exception, privacy and replay tests, as well as
an immutable source/ruling identity.
The [independent audit](2026-11-17-graziani-rul09-review.md) identifies these
initial-stage, discovery and side-channel limits and does not approve runtime
admission.

## RUL-GRA-10: Tobruk initial port efficiency, planning interpretation

For a future versioned Land-only contract, select Tobruk's listed maximum
efficiency **5**, with **2** initial effective levels after the three-level
San Giorgio obstruction. This selects common chart §55.3, its starting-
efficiency footnote, and Air/Logistics §§55.12 and 55.25 over scenario §60.7's
explicit starting level of **7**. The seven is a genuine conflicting source
statement, not a value to subtract the obstruction from again or an official
erratum. Retain the [independent source review](2026-11-17-graziani-tobruk-port-independent.md)
and competing seven-level and five-level readings with the immutable ruling
identity. This is a project interpretation for planning, not admitted runtime
content or evidence of publisher intent.

With no other modifiers, 2/5 is 40% of nominal capacity and does not meet
Land §32.36's 50% destination threshold for Commonwealth supply by sea.
This is a conditional rules example, not evidence of Commonwealth control or
shipping reachability in the six-turn scenario. Maximum efficiency, current
efficiency and capacity in stacking points or tons are separate quantities;
do not encode level 2 as a tonnage. Preserve the initial obstruction as its
own state rather than both subtracting it from the initial value and applying
it a second time. For the initial snapshot, use nominal 5, remaining blockage
3, and effective 2 derived once; later damage/clearance composition remains
unadmitted. Capture alone does not erase or restore the blockage. An
[independent policy review](2026-11-17-graziani-rul10-review.md) confirms the
conditional calculation and identifies these accounting and capture guards.
The threshold is stated for the **destination**; Tobruk's 2/5 alone does not
bar a hypothetical outbound shipment to Alexandria. A [reachability check](2026-11-17-graziani-tobruk-reachability.md)
finds initial Italian occupation but no complete capture-and-shipment proof;
capture during the last movement phase cannot produce a later in-scenario
arrival phase under the printed sequence. Port control and captured-supply
conditions remain separate questions.

This choice alone does not authorize any harbor clearance action in abstract
mode. Completion costs, eligibility, control, repair order, later damage,
capture and whether original detailed-logistics clearance can be translated
to Land-only are separate gates. Test selected setup, the printed 7 versus 5/3
conflict, no double subtraction, 40% versus the threshold, boundary cases,
control and eligible destinations, changes only through an admitted operation,
independently configured alternate profiles, replay and ruleset versioning.
Obtain independent policy review before admission.

## RUL-GRA-11: Alexandria supply receipt placement, planning interpretation

For a future versioned Land-only contract, select the independently reviewed
[running receipt-cap proposal](2026-11-17-graziani-alexandria-ratio-next.md).
For each Game Turn count actual qualifying Commonwealth receipts, excluding
the scenario's initial setup stock. Once the count exceeds three, allow at
most `floor(received / 3)` of those units to be placed in Alexandria; the owner
chooses zero or more within the remaining allowance, and Cairo is the default.
Apply the cap at each authoritative receipt batch, not retrospectively to
already placed units. Floor and running timing are project choices, not explicit
printed rules; the source's alternative whole-turn reading remains reviewable.

Combine genuinely simultaneous receipts into one source-backed batch; retries
and transport-message partition must not create additional choices. Define a
closed current receipt boundary without demanding foreknowledge of ordinary
future arrivals, and block when a receipt belonging to that boundary has
unknown classification or quantity. A later discovery belonging to an already
committed boundary requires an explicit correction/replay procedure, not silent
insertion. Other receipt categories, city membership/capacity, placement
failure and carryover remain separate gates. Do not count a table entitlement
as a receipt or relocate initial scenario supply.

Bind stock, owner choices and placements atomically and preserve the applicable
fog-of-war, including observable option counts, timing and errors. The
[independent review](2026-11-17-graziani-alexandria-ratio-review.md) confirms
the arithmetic and identifies these admission conditions. Test the strict
three/four threshold, cumulative 4+2, simultaneous versus sequential batches,
unknown and duplicate receipt identities, unavailable destinations, replay,
source versioning and audience-safe output. This is planning, not source
correction, gameplay approval or evidence that synthetic cases are reachable.

## RUL-GRA-12: Engineer parenthesized defense, planning interpretation

For a future versioned Italian `bbb` defensive close-assault contract, select
the reviewed [cumulative precedence proposal](2026-11-17-graziani-engineer-policy-next.md).
Require an actual attack and apply corrected §23.11's prohibition whenever a
friendly combat unit shares the hex, regardless of its ammunition or rating.
Then apply corrected §3.4 to the particular defensive rating: a non-combat
unit's unparenthesized numeric rating, including zero, blocks; a source-verified
not-applicable dash is neutral. Exempt positively identified, separately
represented replacement points from the stack-rating and chart-ammunition
blockers, without exempting their carrier or parent unit. This per-rating,
dash and replacement-point treatment is a project interpretation, not an
official clarification of the conflicting chart and errata.

The chart's ammo-qualified blocker remains recorded, but a redundant check
must not make a determined result unknown merely because ammo is unspecified.
Use actual represented combat attachments when classifying an HQ; an HQ with
no attached combat units is not blocked by §23.11 on that basis alone.
Unresolved attack type, stack representation, classification or rating blocks
eligibility evaluation unless a known disqualifier suffices for that specific
predicate. Eligibility alone does not authorize resolution with other missing
combat inputs. Preserve ordinary ammunition and other combat requirements,
information boundaries and existing printed marks.

The [independent policy review](2026-11-17-graziani-engineer-policy-review.md)
and proposal retain alternate source hierarchies. No runtime behavior is
admitted. Test alone versus a friendly combat unit, non-combat numeric zero,
verified dash versus missing data, replacement points and represented HQs,
known blocker versus unknown inputs, separate attack types, replay and
side-safe outcomes under immutable ruleset identity.

## RUL-GRA-13: Arrival and training order, planning interpretation

Select the reviewed [training-order proposal](2026-11-17-graziani-training-order-next.md)
for future versioned Land-only planning. A whole-unit reinforcement arriving
after the Organization phase does not begin or receive whole-unit morale
training credit in that OpStage; it can elect otherwise legal training at a
later Organization step. A unit may fight at its applicable untrained morale
instead of training, subject to the other rules. This resolves the sequence
for planning; §20.43's cross-reference also supports a competing interpretation
and is not an official correction.

Replacement points are separate: §20.43 explicitly permits their arrival
stage to count. Record a pending arrival-stage attempt and credit it only after
the applicable requirements have been established for the relevant interval;
unknown prerequisites block credit, and a known failure is not an unknown.
Instructor applicability and presence interval, credit/completion timing and
particular point actions remain unadmitted. Under the selected no-reopening
sequence, §20.41's destination designation on arrival is separate from §20.49's
Reorganization-only absorption; ordinary same-stage absorption after arrival
is unavailable because that segment has passed.

For units beginning or continuing training, §5.2's bar on voluntary movement
for the remainder of the stage applies even to a zero-CP move. Reaction is a
source exception to that movement bar if otherwise legal, but §17.33's CP
expenditure interrupts training. Do not infer that interruption resets prior
progress or that zero-CP reaction satisfies every other prerequisite. Do not
extend the reinforcement disembarkation cost exemption to replacement points
without source support. The [independent source review](2026-11-17-graziani-training-order-review.md)
records competing readings and limits.

This is a planning decision, not executable authority. Test arrival after
Organization, the final-stage reinforcement, replacement arrival-stage credit,
designation versus absorption, zero-CP movement, reaction, known failure versus
unknown prerequisites, interruption, replay and player visibility after each
separate supporting contract is admitted.

## RUL-GRA-14: Commonwealth rail supply capacity, planning interpretation

For future Land-only planning, interpret §32.34's two real and/or dummy
Supply Units in one direction as a **stage-wide supply-counter ceiling within**
the ordinary §8.72 rail allowance, not a second service. One otherwise legal
non-supply stack may use the opposite direction if it independently satisfies
all normal rail rules. A mixed load requires a separately accepted definition
of a legal stack and batch; the selected supply ceiling does not decide that
definition. Apply §8.75's separate two-stacking-point limit, alongside the
Supply Unit count even though §32.14 gives those units zero stacking value.
Section 8.77 permits picking up and dropping off units along the line, subject
to stacking, direction and CP restrictions; the selected ceiling does not
override that source permission or assume all passengers travel the full path.
Its interaction with the stage-wide supply count still needs a batching
contract. This is a source-precedence choice, not a publisher clarification.
Land §8.78 separately prohibits Commonwealth rail use west of any rail hex
occupied by an Axis combat unit; its spatial relation and snapshot are not
settled by the selected capacity interpretation. The scope of §8.75's two
stacking points across the two directions also requires review.

The reviewed [rail proposal](2026-11-17-graziani-rail-policy-next.md) retains
alternative additional-service and per-direction readings. Its more restrictive
stationary-screen ZOC proposal is **not selected** here: the interaction of
§8.73's specific rail prohibition and §10.26's friendly-combat-unit movement
exception remains an admission gate, including whether the traveler itself can
qualify. Do not treat an enemy-occupied origin as explicitly prohibited by
§8.73's entered-hex wording, and do not silently erase source-authorized ZOC
or dummy discovery. Actual paths, direction, track, stacking, timing, prior CP,
controls, no-op handling and information-safe validation remain separate.

The [independent review](2026-11-17-graziani-rail-policy-review.md) identifies
these source and privacy boundaries; a [ruling audit](2026-11-17-graziani-rul14-review.md)
additionally checks pickup/dropoff, the west-of-Axis restriction and the
two-point scope. Before admission, test two real or dummy
counters versus three, opposite-direction supply versus ordinary cargo, the
separate stacking-point limit, accepted mixed-load alternatives, both ZOC
precedence readings, source-authorized discovery, atomicity, no-op and replay.
No legal historical rail trip or runtime behavior is asserted by this ruling.

## RUL-GRA-15: Abstract fleet attack order, planning interpretation

For a future versioned Land-only contract, select the reviewed [fleet sequence
proposal](2026-11-17-graziani-fleet-sequence-next.md): obtain the optional
secret Axis plot during Fleet Assignment, complete Fleet Repair, then resolve a
valid abstract attack at a separate boundary before the first player's Reserve
Designation. This is a project choice: §32.73 names a Tactical Naval Movement
Segment that the inspected Land and Air outlines do not locate. An
assignment-before-repair attack remains a materially different alternative.
Sealing plot and assignment until both choices are committed is an additional
privacy policy, not a source-stated simultaneous procedure. Commitment alone
does not authorize revealing either choice; disclosure remains a separate
information contract.

In the selected planning model, a valid executed zero-damage attack consumes
one scenario-wide allowance; a voluntary no-plot does not. Empty harbor,
invalid or storm-blocked plots, actual target and damage allocation, initial
allowance timing and Campaign Game applicability remain separate gates. If the
Campaign modifier applies, it affects the separate single-die allowance and
bomb-column rolls, not the two sequential damage-result digits. Do not invent
aircraft or apply the separate Chariot Raid's timing merely because its result
table is referenced. Preserve the corrected initial fleet and owner deployment
choices, including up to two destroyers at Malta.

An [independent source review](2026-11-17-graziani-fleet-sequence-review.md)
confirms the segment gap and highlights repair ordering, private choice and
crash recovery between already committed phases. A [ruling audit](2026-11-17-graziani-rul15-review.md)
also distinguishes sealed commitments from authorized reveal. Admission requires those
contracts, source-specific weather and map E status, valid harbor snapshot,
damage/repair ordering, replay and side-safe views. No attack, historic die
result or runtime behavior is asserted here.

## RUL-GRA-16: Commonwealth replacement planning, bounded interpretation

For future versioned Land-only planning, use the four-game-turn lead for
Commonwealth truck and infantry production in §20.21 and the specific §20.78
chart instructions, rather than the two-turn sequence-outline summary. Use
the planning period for the production lookup as corrected by errata §20.72,
while preserving §20.73's conflicting arrival-date wording. Four turns match
one month for the inspected four-week rows, but this does not settle every
partial-month boundary, stage or initial planning phase. The [source comparison](2026-11-17-graziani-cw-replacement-planning-next.md)
and [independent review](2026-11-17-graziani-cw-replacements-review.md) retain
the competing readings and chart-specific distinctions.

Interpret §60.47's pre-November Commonwealth Replacement Point arrival ban as
also blocking rate-based motorization received **as replacements** under
§32.52. Section 32.51's medium-truck treatment and §20.74's Medium Truck
replacement example support this choice; the [alternative analysis](2026-11-17-graziani-cw-motorization-arrivals-next.md)
preserves the argument for separate treatment. This does not automatically
bar scenario-specified motorization reinforcements or the trucks accompanying
whole-unit reinforcement rows; their Land-only conversion and actual receipt
need separate source contracts. A blocked arrival is not a zero production roll.

This is a planning interpretation, not an official correction or admitted
schedule. Initial pregame records, exact planning phase, later-than-GT6 plans,
other replacement categories, independent arrival exceptions, allocations and
privacy remain gates. An [independent ruling audit](2026-11-17-graziani-rul16-review.md)
confirms the bounded source basis while retaining truck-chart heading and
physical-roll applicability questions. Test the two- versus four-turn alternative, planning-
versus arrival-period lookup, the November boundary, rate-based replacements
versus whole-unit reinforcements, missing plan versus recorded zero, and
versioned deterministic replay.

## RUL-GRA-17: Local supply range, planning interpretation

For a future versioned Land-only contract, interpret §32.16's range as an
inclusive limit of `ceil(actual combat-unit CPA / 2)` capability points,
with the route traced **from the unit to a friendly Supply Unit** using the
source-specified medium-truck cost or non-motorized-infantry exception. This
choice follows the printed 23-CP typical-recce example when CPA is 45 and is
consistent with the direction of the separately scoped §32.17 rounding rule;
§32.16 does not expressly prescribe a general ceiling. Exact fractional half
remains a distinct source interpretation. Use exact rational route costs,
not per-edge rounding or an assumed universal half-CP grid.

The [independent review](2026-11-17-graziani-supply-range-rounding-review.md)
also compares the printed generic five-CP infantry example with the 1st Royal
Northumberland Fusiliers' printed CPA 8. Section 6.14 describes that example
as a non-motorized machine-gun battalion; whether it qualifies for the
infantry-specific tracing example remains unproved. This policy uses the
actual accepted unit CPA rather than assigning all infantry a range of five.
Determine represented parent/subunit CPA, legal directional edges, impassable
terrain, friendly-unit ZOC exception, endpoints, source stock and selection
separately. Do not impose an unexplained physical medium-truck CPA on the
hypothetical trace or treat it as the scenario victory route.

This is a planning interpretation, not an admitted route or source correction.
Test CPA 45 at 22.5, 23 and 23.5 CP, CPA 8 at 4 and 5 CP, the infantry trace
type, directed terrain, source and incoming-ZOC uncertainty, altered parent
CPA, resource conservation, hidden-state-safe views and versioned replay. The
[ruling audit](2026-11-17-graziani-rul17-review.md) confirms the arithmetic
and direction while flagging that classification qualification.

## RUL-GRA-18: Anti-armor-only ammunition, bounded planning interpretation

For future Land-only planning, treat an otherwise eligible battalion-equivalent
participating **only in anti-armor** during a combat segment as participating
in §32.21's Assault category: one Ammo Point if non-phasing, two if phasing,
at the source-defined combat instant. Land §3.1 describes Assault as
comprising Anti-Armor and Close Assault; §11.21 and the cited §50.12 erratum
provide related terminology. This applies the printed abstract rate by
interpretation, not a separate detailed-logistics rate or a claim that §14.24
alone establishes a Land-only debit.

Do not use this bounded choice to determine whether a battalion split between
anti-armor and close assault pays once or twice, how partial battalions are
counted, how barrage combines with assault, how repeated segments are charged,
or what happens on insufficient supply. Those cases remain unsupported pending
separate source and contract review. Preserve actual component participation,
legal source and timing, protected information and resource conservation.
Assigned-but-not-fired, reassigned, merely targeted, aborted and partial-unit
cases need an accepted participation definition; do not silently debit or
waive them. The [ruling recheck](2026-11-17-graziani-rul18-review.md) is a
fresh pass by an earlier researcher, not independent authorship.
Compare an explicit no-separate-debit alternative and the source's printed
non-phasing/phasing rates; test replay, aborted combat and allocation without
inferring an outcome from a synthetic scenario. The [research and alternatives](2026-11-17-graziani-ammo-combat-next.md)
retain these limits. This is planning, not admitted runtime authority.
An [independent source review](2026-11-17-graziani-ammo-combat-independent.md)
confirms the glossary's anti-armor inclusion while leaving participation and
multi-step charging unresolved.

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
chooses 2, but it is corroboration rather than authority. RUL-GRA-10 now selects
2 of 5 for planning while preserving this conflict and blocking admission until
independent policy review. The Land-only profile must establish whether and
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
