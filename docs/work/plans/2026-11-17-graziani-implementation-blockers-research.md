# Graziani implementation blockers: focused source findings

**Scope:** Fresh same-scan research into the three implementation-agent questions
in the supplied handoff. Source facts, unknowns, and implementation decisions
remain distinct. No runtime or pinned content changed; no gameplay tests ran.
An [independent synthesis audit](2026-11-17-graziani-implementation-blockers-research-audit.md)
confirms the scoped findings; Giarabub was a verification item, not a current
`ReadinessBlockers()` entry in the supplied handoff.

## 1. 1st CCNN HQ gun

**Source result: weapon model and initial gun assignment remain unknown.** Axis
OA PDF p. 17 lists `N` for the HQ; the key on p. 4 defines `N` as maximum TOE
strength, not a gun model. Characteristics §4.46b p. 26 permits one artillery
TOE but does not name its weapon. The OA key also calls for a gun's quantity
and weapon system to be listed; this row does not do so. A literal maximum
reading supports an initial point, but the missing gun listing leaves its exact
assignment unsettled. The counter and reviewed September errata do not provide
an answer. Do not substitute 65/17, zero, or a guessed combat rating.
[Source check](2026-11-17-graziani-blocker-ccnn-hq.md),
[independent review](2026-11-17-graziani-blocker-ccnn-hq-review.md).
This completes the bounded check of the available cited sources, not a claim
that no other edition or clarification exists. Whether the engine may accept
an explicit unknown is an implementation/policy decision, not a source value.
One [unaccepted policy candidate](2026-11-17-graziani-ccnn-gun-capacity-policy.md)
would interpret `N` as one present but unidentified artillery point; its
[independent critique](2026-11-17-graziani-ccnn-gun-capacity-policy-review.md)
finds no basis to rank it decisively above the unresolved-assignment reading.
The [unknown-state implementation evidence](2026-11-17-graziani-ccnn-unknown-implementation-evidence.md)
maps those source facts to proposed provenance, replay and consumer tests;
it does not admit an operational rating or make a new ruling. Its [audit](2026-11-17-graziani-ccnn-unknown-implementation-evidence-audit.md)
emphasizes that an unknown weapon does not establish that a point is present.

## 2. Aresca Regiment CV33 close assault

**Source result: CV33 (L3/35) is 1 offensive / 2 defensive per tank TOE
point.** Axis charts PDF p. 29 §4.48, Italian Tank and Gun Characteristics,
prints the unparenthesized `1/2` rating. PDF p. 6 lists Aresca HQ as one CV33
and XXI(L) battalion as nine CV33; scenario §60.31 lists Aresca at C3919 with
all except XXI detached. It is incorrect to infer that light tanks have no
close-assault rating from the dashes in the unit-characteristics row.

The nine-point XXI baseline yields 18 raw defensive points if present,
unchanged and eligible; it does not establish an actual ZOC on its own. HQ's
own point is a separate participation question: Land §3.35 restricts armored
division/brigade HQ tanks while combat units are attached, and §9.22 may make
an Italian regiment brigade-equivalent based on printed stacking. Section 10.15
uses raw defensive close-assault points; §15.4's actual-combat adjustment is
not automatically a change to that raw threshold. Do not automatically add HQ
and XXI as 20 or infer an unconditional legal ZOC.
[Source check](2026-11-17-graziani-blocker-aresca-cv33.md),
[independent review](2026-11-17-graziani-blocker-aresca-cv33-review.md).
The [HQ size follow-up](2026-11-17-graziani-aresca-hq-size-eligibility.md)
and [independent review](2026-11-17-graziani-aresca-hq-size-eligibility-review.md)
find nominal super-brigade equivalence strongly supported by the counter's
printed stacking value. Shell status remains conditional: the four OA-listed
battalions are not by themselves proof of §9.26's assignable maximum, and
§9.27 excludes attached shell subunits from the full-parent count. Applying
§3.35 to this HQ and its current status remains an interpretation.
The [formation chart follow-up](2026-11-17-graziani-aresca-assignment-maximum.md)
and [review](2026-11-17-graziani-aresca-assignment-maximum-review.md) support
five tank-battalion slots under the normal `(3)` tank-regiment chart, subject
to singular-structure exceptions. With one qualifying battalion actually
attached, that normal-chart interpretation yields shell status; four qualifying
attachments meet its full threshold. The OA's four listed assignees do not
alone establish a four-slot maximum. The review relied on the earlier visual
counter reading, not its own counter image.
[Participation alternatives](2026-11-17-graziani-aresca-hq-participation-alternatives.md)
and [review](2026-11-17-graziani-aresca-hq-participation-alternatives-review.md)
preserve competing nominal-size and shell-adjusted readings. A shell combat
subunit may trigger §3.35 while not counting toward §9.27's full-parent test;
do not conflate those predicates.

## 3. Giarabub motor-machine-gun motorization

**Source result: no unit-specific initial motor pool or current effective
motorized CPA was identified in the cited setup.** Axis OA PDF p. 14 identifies
`Grbub`, characteristics `hh`; §4.46b p. 26 gives printed CPA `8+`, close
assault 5/7 and maximum TOE 1. The `+` key marks historical motorization; it
is not a specified current truck assignment. The counter identifies a
motorized machine-gun battalion. Scenario §60.31 does not list first-line
trucks for Giarabub, while §59.63A converts listed medium/heavy first-line
trucks into initial motorization points.

Land §32.54 prioritizes historically motorized infantry when assigning points;
scenario §60.33 also provides separately placed second/third-line trucks. These
facts do not establish an initial unit-specific pool, and they preclude the
broader claim that no trucks can exist there or be assigned later. Accordingly,
`unresolved-no-scenario-pool` is supported only as shorthand for the narrow
missing initial assignment, not a known-zero pool or absence of a printed CPA.
[Source check](2026-11-17-graziani-blocker-giarabub-motorization.md),
[independent review](2026-11-17-graziani-blocker-giarabub-motorization-review.md).
An [allocation follow-up](2026-11-17-graziani-giarabub-initial-allocation.md)
and [independent review](2026-11-17-graziani-giarabub-initial-allocation-review.md)
distinguish detailed-game initial motorization from abstract §59.63(B)'s
initially unattached second/third-line-derived points. Whether those points
can be transferred before play starts remains unresolved.

## Implementation follow-up

Use the verified CV33 rating with focused tests for source provenance, current
TOE, raw defensive strength and the distinct HQ/stack eligibility question.
Treat the CCNN and Giarabub unknowns under an explicitly versioned unknown-state
or acceptance policy if chosen; do not populate missing facts or silently treat
unknown as zero. Preserve content hashes and replay identity. No claim here
that any particular readiness blocker is already cleared in the current code.
