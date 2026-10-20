# P01 AC-003 bounded action classification — owner decision proposal

**Status:** proposed decision, not accepted policy or an executable support certificate. This
packet does not change the canonical [P01 acceptance matrix](../specs/combat-actual-moved-repeat-position-v1.md),
the provisional assessor, any legal-action generator, or native/public admission. All 300 original
R03 rows remain `unsupported`, `admitted=false`, with zero public actions until the separate gates
are proved and reviewed.

## Decision needed

The current `P01-AC-003` asks for “no alternate selected-content action.” Read literally as no
other **source-legal** choice, this cannot be true for the selected two-infantry content. The
retained Land rules permit Probe using the same infantry, partial or withheld assault allocation,
attacker no-assault, defender no-allocation, defender Retreat Before Assault (RBA) movement, and
voluntary surrender. Some alternatives are absent only when a particular source condition fails;
others are deliberately excluded by the accepted initial-RC exercise. The existing
[action matrix](../work/combat-p01-action-matrix-evidence.md) labels itself non-exhaustive and its
verifier reports `sourceLegalActionExhaustive=false` on all 300 authentic rows.

The owner already accepted [CMB-POL-001](combat-cycle-policy-reconciliation.md) as a **bounded**
full-10-TOE singleton infantry exercise with decline-only RBA and no gun/armor, and
`CMB-POL-009` explicitly excludes source-legal voluntary surrender. The latter decision states
that this exercise cannot claim complete source-legal Combat action coverage. The decision here
is whether `P01-AC-003` should demand exhaustive **classification of source-legal alternatives**
for this fixed content and explicit **closure of the admitted bounded profile**, while retaining
`sourceLegalActionExhaustive=false`.

### Recommended exact replacement for `P01-AC-003`

> Recreate and authenticate each original CreationRequest and eleven separate R03 ledgers before
> classifying any action. For every one of the 300 original ordinal-two terminals, prove the
> original-unit/representation bijection, acting side, exact target occupants, current-versus-prior
> segment target use, current Weather fact and receipt, and all ZOC, terrain, resource and mandatory
> obligations from pinned source and selected Content. Enumerate every source-legal Combat choice
> or obligation reachable by the selected content at this position and classify it as (1) a
> supported bounded-profile path, (2) absent because an authenticated source condition is false,
> (3) expressly excluded by an accepted, named bounded-profile policy, or (4) unsupported and
> fail-closed. Prove that no unclassified or additional **admitted bounded-profile** action can be
> issued. Source-legal alternatives in classes (3) and (4) remain legal in the physical game and
> may not be silently converted to an empty candidate set, a synthetic decline, or an ordinary
> full-TOE assault. A class-(4) alternative reachable in a row blocks that row; **any** such row
> blocks the all-300 `boundedProfileActionClosed` certificate until the alternative has a
> supported path, is proved absent by an authenticated source condition, or is covered by an
> accepted named exclusion. Merely recording class (4) does not close the profile.
> `sourceLegalActionExhaustive` remains false. A separately versioned
> `boundedProfileActionClosed` certificate may become true only after all 300 rows have no
> reachable class-(4) or unclassified alternative, the mutation-sensitive classification and
> explicit source/policy pins are reviewed, and the typed attacker finish-without-attack path is
> independently proved. It does not by itself admit P01 or waive AC-004 through AC-009.

This changes the proof target, not the accepted exercise. If the owner instead wants literal
source-legal action support, `CMB-POL-001` and `CMB-POL-009` must be reopened; this packet supplies
no authority to do so.

## Retained primary evidence

The retained Land PDF SHA-256 is
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`; September
errata SHA-256 is `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
The original 300-row catalogue and eleven chunk pins are fixed by the
[P01 contract](../specs/combat-actual-moved-repeat-position-v1.md) and retained outside Git.
The following locators were checked against the retained scans, not inferred from an empty API
action list:

| Source | Relevant choice or condition |
| --- | --- |
| Land PDF p18, §§10.11, 10.31–10.36 and §11.0 | ZOC threshold and mandatory assault/Holding Off/forced retreat; four basic combat activities and their order. |
| Land PDF pp19–20, §§11.21–11.27 and 12.0–12.6 | CP costs, gun position and Barrage; the selected two infantry components contain no gun unit. |
| Land PDF p21, §§13.0–13.28 and 14.0–14.6 | An unpinned non-phasing defender can choose RBA after Barrage even without initial adjacency; Anti-Armor requires the appropriate capability/target. |
| Land PDF pp22–23, §§15.11–15.29 | Infantry Close Assault, ammunitionless phasing-unit exclusion, full/partial/withheld allocation, Probe at less than 50%, attacker no-assault, and defender no-allocation with automatic retreat. |
| Land PDF p25, §§15.88–15.96 | Source-legal voluntary surrender; event-triggered automatic surrender; Probe as a modified Close Assault. |
| September errata §§15.27, 15.88, 50.12 | Corrected assault and surrender conditions; zero ammunition at Combat Position alone is not an automatic surrender event. |

The [source audit](../work/combat-p01-action-matrix-evidence.md) records an authentic
nonadjacent witness `r012-c02`: the defender is unpinned with a vacant clear retreat edge and CP
headroom. Nonadjacency proves no adjacent Close Assault target, **not** absence of RBA. The
selected Content7 contains exactly one independent infantry component per side, no gun or armor
component, and one stacking point in each occupied hex. Those are current-content facts, not a
general rule that infantry cannot perform any other source-legal activity.

The adopted [step design](combat-step-transitions-v1.md) distinguishes the phasing player's
`finish-without-attack` from System closure when there are zero candidates. The
[selection contract](../specs/combat-selection-steps-v1.md) assigns the player-owned choice an
exact live decision ID and null candidate, whereas `close-empty-selection` is System-only and
does not open a player decision. A trusted expiry is a third path. The prior ordinal-one
implementation does not by itself authenticate the proposed ordinal-two path.

## Proposed mode register for the bounded certificate

The certificate must derive status from authenticated source and content facts. This table is the
minimum review register, not an executable truth table or a claim that the list is already proven
complete. Any newly found source mode requires a versioned register update and makes old
certificates unsupported until re-reviewed.

| Family | Current bounded disposition | Required proof or failure behavior |
| --- | --- | --- |
| Adjacent loaded Close Assault with full 10-TOE assignments | Candidate only for the 44 armed adjacent rows under `CMB-POL-001`. | Recheck original units, target, current segment, Weather, full current TOE/ammo/CP and absence of mandatory blockers before a supported path. Do not infer P02/P03 admission. |
| Attacker `finish-without-attack` on an armed positive row | Source-legal voluntary choice, distinct from a no-candidate state; `CMB-STEP-002/006` permit the authenticated phasing side to finish an open selection without choosing the offered assault. The ordinal-two path is not yet proved and blocks closure until it is. | Require an ordinal-two, actor-owned command while its selection decision is live, candidate null, exact decision/version/prefix/receipt and accepted time before deadline. It creates its own no-selection receipt and later structural closure, without a Force Assignment round or zero-allocation seal. |
| System zero-candidate closure or selection timeout | Different System-owned outcomes under `CMB-STEP-002/006` and `CMB-POL-004`; neither is a player's finish choice. Each unproved reachable ordinal-two path blocks closure. | Zero candidates use `close-empty-selection` with no live decision ID; expiry uses the live decision ID and a trusted deadline observation. Neither may emit an attacker finish receipt or stand in for an outstanding defender RBA decision. |
| Probe and partial attacker allocation | Source-legal with the same infantry under Land §§15.25, 15.9; outside full-TOE `CMB-POL-001`. | Reject a sub-50% Probe or other non-full assignment before payment or result; never normalize it to a full assault. |
| Defender withheld, partial, or no allocation | Source-legal under §§15.16, 15.29; outside the full-TOE exercise. | A no-allocation request cannot bypass its source automatic-retreat consequence by pretending a full defender seal. Reject or cancel before debit. |
| RBA movement | Source-legal for eligible unpinned defender, including nonadjacent rows; explicitly outside decline-only `CMB-POL-001`. | Reject movement in this profile; a deadline or unavailable clock cancels without a fabricated decline under `CMB-POL-004`. |
| RBA explicit decline | Only accepted defender-owned RBA branch of the bounded exercise. | Authenticate side, participant, position, deadline and predecessor receipt; never infer from silence or from initial nonadjacency. |
| Voluntary surrender | Source-legal under §15.88; expressly excluded by `CMB-POL-009`. | Reject as policy-excluded without invoking automatic-surrender settlement or treating it as no-selection. |
| Automatic surrender | Mandatory effect at a qualifying actual assault or enemy-movement trigger, not a selection option. | Keep AC-006 trigger/settlement gate separate; unsupported trigger fails closed before any completed P01 transition. |
| Gun position, Barrage and Holding Off Barrage | Source activity requiring gun/Barrage capability; absent from selected Content7. | Mutated gun component, artillery capability or target must invalidate this bounded certificate; no empty Barrage step from a changed content pack. |
| Anti-Armor fire | Requires the corresponding selected capability and armored target; absent from selected Content7. | Mutated Anti-Armor/armor component or target invalidates the certificate. |
| Mandatory ZOC assault, Holding Off and forced retreat | Source condition not met by current one-stacking-point occupied hexes. | Recompute from exact current stacks and Land §10.11 plus pinned chart 9.4; added stack, ZOC or obligation invalidates this certificate. |
| Terrain, fortification, Weather, combined-arms, size and target modifiers | Source conditions are absent or fixed for the selected clear, Normal-Weather, two-infantry content. | Any changed terrain, edge, Weather, participant or rating returns unsupported unless a separately certified result/profile covers it. |

## Required fail-sensitive executable evidence after approval

1. Pin and hash the original source PDFs, common-chart stacking and assault data, accepted
   policies, selected Content, 300-row summary, manifest and every compressed chunk. Recreate
   each original CreationRequest plus eleven independent ledgers; compare canonical terminal
   Source/Control/Proof and exact ordinal-two suffix. Tamper, omit, reorder and substitute one
   ledger separately.
2. Derive all 300 statuses without trusting row labels or caller-supplied World. Check the 44
   adjacent armed, 12 adjacent paid/ammunitionless and 244 nonadjacent partitions, both acting
   sides, current Weather receipt/hash and original-unit/representation identity. Every row must
   have every register family classified. A reachable class-(4) mode blocks its row and the
   all-300 closure bit even if the other 299 rows pass. Unknown mode, unapproved exclusion or
   missing source/policy pin also blocks closure; a label alone is not proof of absence.
3. For each of the 44 armed rows, mutate an offered full allocation to 0, below 50%, exactly
   50%, and a partial value above 50%. The first three must not become a full Close Assault;
   the partial value must not be silently padded to 10 TOE. Mutate the defender's allocation to
   zero and assert no full-seal or skipped automatic-retreat effect. Request voluntary surrender
   and RBA movement and assert explicit bounded-policy rejection before CP/ammo/history debit.
4. On armed positive rows from **both** acting sides, accept an authenticated phasing player's
   `finish-without-attack` while the selection decision is live and before its deadline. Assert
   a distinct no-selection receipt, no chosen candidate, no Force Assignment seal, no CP/ammo/RNG
   debit, and the correct no-attack structural continuation. Reject wrong-side actor, stale or
   changed decision/receipt/version, late submission, and changed-input duplicate; an identical
   retry returns the original receipt without another event. Separately test a zero-candidate
   System `close-empty-selection` (no decision ID), trusted selection expiry (live decision ID),
   and Force Assignment zero allocation rejection. None may reuse the attacker's finish receipt
   or masquerade as the other outcome. Historical ordinal-one C3a behavior is a precedent, not
   authentication of an ordinal-two command.
5. In an authenticated nonadjacent witness such as `r012-c02`, prove zero adjacent assault
   candidates while retaining the defender-owned RBA choice. Test accepted decline, wrong actor,
   late/missing response and clock unavailability separately. Only the explicit decline has a
   decline receipt; cancellation has its own receipt and never becomes supported-empty by itself.
   Repeat against the 12 adjacent paid rows, where zero acting ammo is not surrender at position.
6. Inject a third unit/representation, gun, armored target, >1-stacking-point ZOC, changed edge or
   terrain, non-Normal Weather, hidden participant, changed current/prior target-use record and
   altered original-unit binding. Each must produce unsupported or reject at authentication, not
   an empty candidate list, a hidden different public action, or a policy exception.
7. Mutation-test the register itself: delete a family, switch `policy-excluded` to
   `source-condition-absent`, change a cited source/policy hash, or add an unknown family. No
   certificate may remain true. Inject a reachable unsupported class-(4) mode into **one** row
   and assert that neither the row nor the all-300 closure bit can remain true. Compare canonical
   output roots across cold replay, retry and process readback without exposing hidden identities
   or source hashes to an outward candidate.

These tests should be implemented in a **new** versioned private verifier/schema after the owner
accepts the replacement AC-003 target. Existing provisional verifiers and historical evidence
must remain immutable.

## Stop conditions and separation of gates

- A source-legal family not covered by the register, an ambiguous source condition, an unapproved
  exclusion, or a changed selected content/policy/source hash stops certification. A partial mode
  list may be published as research only; it cannot set `boundedProfileActionClosed=true`. Any
  reachable class-(4) alternative blocks its row and therefore the all-300 certificate until
  supported, proved absent, or covered by an accepted named policy exclusion.
- The retained P01 evidence folder lacks the original common-chart scan. The later verifier must
  bind the already adopted normalized chart/rules input and its source pin, or acquire and verify
  the physical scan; an unpinned copied chart or a missing dependency is a stop condition.
- A missing or forged attacker `finish-without-attack`, RBA decline, System no-selection closure,
  automatic-surrender trigger, Weather receipt or current/previous target-use proof stops the
  affected transition. `unsupported` never
  means supported-empty. An attacker finish, System zero-candidate closure, System expiry and
  Force Assignment zero allocation are four distinct cases and may not borrow one another's actor,
  receipt or fallback. Actual RBA movement and voluntary surrender remain outside this profile.
- `boundedProfileActionClosed=true` would close only the agreed AC-003 action classification for
  this selected profile. AC-004/005 result/geometry, AC-006 automatic surrender and RBA, AC-007
  outward identity/privacy, AC-008 replay/physical negatives/caps and AC-009 review/native gates
  remain independent. No P01 admission, P02/P03 activation, public action or full source-legal
  Combat claim follows from this packet.

## Owner disposition

**Pending.** Approving this packet would authorize a versioned AC-003 contract change and its
executable bounded-profile proof. It would not approve any new source tuple, public action,
outward field set or native activation. Rejecting it retains the current literal AC-003 gate and
the existing fail-closed zero-admission state.
