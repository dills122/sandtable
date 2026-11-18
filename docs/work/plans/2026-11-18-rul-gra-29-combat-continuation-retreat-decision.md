# Decision request: compulsory-combat retreat overlap (candidate RUL-GRA-29)

**Status: open decision, not a ruling.** This packet exists so the project
owner can make one specific call without first reading the five research
documents behind it. It does not select an interpretation. Once a choice is
made, promote it into `2026-11-12-graziani-ruling-decisions.md` as the next
available `RUL-GRA-NN` entry — **RUL-GRA-29**, assuming PR #418 (currently
open, claiming RUL-GRA-28) merges first; re-check the ledger's last entry
before numbering if the order changes.

## What's actually blocked on this

Issue #394 ("Connect second-side Breakdown to actual Combat selection") is a
separate, narrower authority-continuation bug — `CampaignNativeSecondBreakdownV46.Create`
reports a second-side Combat checkpoint that the normal owner-selection path
(`WithSecondBreakdown` in `ExerciseSession.cs`) never hands to a live Combat
decision. This decision does not fix that bug by itself.

What this decision *does* unblock is the broader compulsory-combat
eligibility/retreat mechanic that Land §§10.31–.36 require and that no code
in this repo currently implements. Checked directly: `CampaignCombatLossRetreat.cs`
and `CampaignNativeCombatRetreatTimeoutV37.cs` both resolve retreat *after* a
combat result is settled (`settlement.Result.RequiredRetreat`); neither
implements the pre-combat obligation this decision is about — a unit sitting
in an enemy's ZOC that cannot legally assault, probe, or hold off, and must
therefore retreat before combat happens at all. That mechanic, and anything
built on top of it (including eventually closing #394-style continuation
gaps in the Combat segment), needs this overlap resolved first; it is a
prerequisite, not the fix itself.

## The conflict

Land rules (printed/PDF p. 23) **§15.34**: a combat unit in the ZOC of an
enemy it cannot attack due to terrain (motorized units barred from assaulting
salt-marsh or escarpment-uphill defenders, §8.44/§15.34) must retreat **one**
hex, unless another unit attacks that enemy.

Land rules (printed/PDF p. 18) **§§10.31–.36**: a unit identified as obligated
by an enemy's ZOC that can neither legally assault nor Hold Off must retreat
**three** hexes, paying movement CPs, taking three extra disorganization
points, with surrender if the retreat path is forced through enemy ZOC
(subject to §10.26's friendly-occupant exception).

A source-constructed synthetic state (see
[`2026-11-17-graziani-retreat-overlap-source.md`](2026-11-17-graziani-retreat-overlap-source.md))
proves both predicates can hold **at the same time** on the same unit: a
non-motorized enemy force in salt marsh projects ZOC onto an adjacent
motorized unit on clear terrain; that unit can't assault (§8.44/§15.34
terrain bar), has no qualifying Hold Off, and no other friendly unit attacks
the enemy. §15.34's "unable to attack due to terrain" and §10.36's "neither
assault nor Hold Off" are both literally satisfied. **Neither the Land rules
nor the September 1979 errata state which prescription governs, or whether
both apply, in this overlap** — confirmed by an independent same-scan review
of the same source pages. This is a genuine, source-confirmed gap, not a
research gap that more searching would close.

A negative control in the same research rules out one easy escape: if the
enemy source is across an escarpment hexside instead of in marsh, §10.21(b)
blocks its ZOC outright, so the overlap never arises — meaning the conflict
is real only in the specific terrain-blocked-assault case, not in general
retreat situations. It isn't a false alarm from an overly generic test case.

## Options

From [`2026-11-17-graziani-retreat-overlap-policy.md`](2026-11-17-graziani-retreat-overlap-policy.md)
(four candidates compared, none selected):

1. **Terrain-specific priority** — apply §15.34's one-hex retreat whenever its
   full predicate holds. Treats the terrain/alternate-attacker case as a
   special case that preempts the general compulsory-combat retreat. Cost:
   none of §10.36's extra disorganization/CP/surrender machinery applies in
   this overlap, by policy choice, not proven source precedence.
2. **Compulsory-combat priority** — apply §10.36's three-hex retreat whenever
   its full predicate holds, treating "can't assault or Hold Off" as the more
   specific failure. Cost: the harsher three-hex/three-disorganization-point/
   surrender-risk outcome applies even in the §15.34 terrain case; again, no
   source text establishes this priority either.
3. **Sequential consequences** — apply both retreats in some order, re-checking
   predicates at each step. Cost: this is the least-specified option — order,
   whether the first retreat's ZOC change invalidates the second predicate,
   and cost-stacking (is it one movement or two?) are all undetermined; no
   source text supports two retreats or an automatic four-hex move.
4. **Defer** — leave the overlap unresolved and keep this code path blocked
   rather than admit either reading. Preserves the evidence faithfully but
   means the compulsory-combat/retreat mechanic (and anything depending on
   it) stays unbuilt until a later decision.

None of these is a "more correct" reading of the source text — the research
is explicit that no hierarchy between §15.34 and §10.36 is stated anywhere
checked (Land rules, the September 1979 errata, or the scenario booklet).
Picking one is a project decision, same as RUL-GRA-24's convoy-loss choice or
RUL-GRA-25's victory-trace choice were.

## What else is settled vs. still open (same research batch)

Separately, from
[`2026-11-17-graziani-compulsory-combat-continuation.md`](2026-11-17-graziani-compulsory-combat-continuation.md):

- **Settled, no decision needed:** ZOC projection for this obligation is
  asymmetric — test whether the *enemy* source projects ZOC onto the phasing
  unit (per September errata §10.3, phasing-player-scoped); the phasing unit
  is not required to project its own ZOC back for the obligation to apply.
  §10.32's exemption (artillery/AT/AA/non-combat-only stacks, or pinned units)
  is also settled as to its trigger, though mixed-stack resolution still needs
  implementation-time care.
- **Still open, separate from this decision:** whether a zero-rated/unrated
  unit can itself constitute a legal assault (§15.11 vs §15.21 tension), and
  the full route-selection authority question for either retreat (neither
  source text names who chooses the route, or a deterministic fallback if a
  route must be assumed). These don't block picking an option above, but they
  do block actually implementing whichever option is chosen.

## If you pick one

Required contract vectors once an option is selected (none implemented yet):

- A new compulsory-retreat mechanic distinct from `CampaignCombatLossRetreat`
  (pre-combat vs. post-resolution), with its own route-legality, cost, and
  (for option 2/3) surrender-on-blocked-path handling.
- Per-enemy, per-unit quantifiers for multi-projector cases (not covered by
  the single-enemy synthetic witness above).
- Side-safe disclosure rules for the retreat obligation itself, since
  eligibility and route legality can leak hidden-unit information across the
  authority boundary (same privacy-gate pattern as other rulings in this
  ledger).
- Versioned ruleset binding, same as every other RUL-GRA entry, so this
  interpretation gets a distinguishable content identity rather than silently
  reinterpreting an existing save if it's ever revisited.

No code, data, or readiness-flag change is proposed by this packet. Docs only.
