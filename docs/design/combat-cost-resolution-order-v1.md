# Combat Cost and Resolution Ordering

**Status:** `CMB-DES-004` bounded design complete for review; production remains gated.
**Date:** 2026-09-06. **Input checkpoint:** `063ca2e` (merged PR #90).

This packet joins [identity](combat-opportunity-identity-v1.md),
[sealed decisions](combat-sealed-decision-protocol-v1.md) and
[step transitions](combat-step-transitions-v1.md). It defines the irreversible cost boundary,
one deterministic Close Assault result and the settlement handoff. It preserves the selected
singleton infantry fixture. [CMB-DES-005](combat-settlement-disclosure-v1.md) owns losses, retreat,
custody and disclosure.
These are proposed semantic contracts, not implemented events or approved production schemas.

## Evidence and scope

| Evidence | Consequence |
| --- | --- |
| [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), PDF p13, 6.21-6.24 | CP over allowance causes immediate Cohesion loss before Morale; actual combat loss and victory recovery are separate later causes. |
| Land rules, PDF p18-19, 11.0 and 11.21-11.27 | Barrage and Anti-Armor precede Close Assault; ordinary full assault costs 5 phasing/3 non-phasing CP. Combat-segment caps and all-unit hex exposure differ from strength assignment. The defender's final-differential -4-or-worse exception is outside the selected surface. |
| Land rules, PDF p24-25, 15.61 and 15.71-15.87 | Both Morale checks determine one final differential; each side's ordered assault pair also supplies its sum tests. Preserve pre-loss strength, asymmetric rounding and Retreat priority. |
| [Full rules](https://www.spigames.net/PDFv2/CampaignNorthAfrica.pdf), PDF p67-68, Logistics 50.0-50.17 | Debit accessible ammunition when committed TOE is used, including defensive assault. Stock elsewhere or still on a convoy is not carried ammunition. |
| [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf), 50.2 and 50.12 | Infantry spends one Ammo Point per TOE used; ammunitionless surrender needs an assault, not mere adjacency. |
| [Mutable-state research](../research/combat-mutable-state-spike.md) | Reuse current TOE and CP/Cohesion; one-use carried ammunition and explicit result obligations remain proposed additions. |
| [RNG research and goldens](../research/combat-rng-golden-spike.md) | Proposed role-ordered eight dice plus conditional capture die; all five selected columns and correlated results must be covered. No seed selects admission. |

Land pages 13, 19 and 24 and Logistics pages 67-68 were visually checked for this packet; errata
text was checked. Source locators establish rules facts. Event atomicity and serialization below
are Sandtable design choices; adoption of pending research policies still needs owner approval.

Current [World operational state](../../src/Cna.Core/Campaigns/CampaignWorldV5.cs) already retains
stage identity, exact CP expenditure, Cohesion and current component TOE. [CapabilityPointAmount](../../src/Cna.Core/Rules/CapabilityPointAmount.cs)
provides exact rational arithmetic. [Movement](../../src/Cna.Core/Campaigns/CampaignElementMovedV3Factory.cs)
currently rejects expenditure above CPA; that bounded implementation is not a general combat DP
calculator. [SandtableRandom](../../src/Cna.Core/Randomness/SandtableRandom.cs) supplies the existing
campaign stream. None of these implements the Combat lifecycle proposed here.

## Resolution decisions

| ID | Decision |
| --- | --- |
| `CMB-RES-001` | Certify complete input, cost and reachable-result support before active choices; revalidate frozen authority at commit. No outcome-dependent admission or mutation to force the fixture. |
| `CMB-RES-002` | Extend DES-002's single `CombatAttackCommitted` event with both sides' exact CP and ammo debits. Publish costs, commitment, history and target use atomically before RNG; no separate charge command. |
| `CMB-RES-003` | Derive post-cost Morale inputs before rolls; both assault results use the same final differential and each side's own frozen pre-loss strength. Never let one result weaken the other's calculation. |
| `CMB-RES-004` | Continue the campaign stream in the proposed role/purpose order; publish all rolls, derived result facts and final cursor together in one `CombatAssaultResolved` event. |
| `CMB-RES-005` | A resolved roll enters Settling with immutable result evidence and explicit obligations. It applies no final TOE loss, retreat, custody, loss DP, victory RP or relationship disposition. DES-005 owns those transitions. |
| `CMB-RES-006` | Empty Barrage/AA consume nothing. Future nonempty simultaneous stages require frozen stage inputs and joint effects; separate Close Assaults resolve sequentially against updated authority. Neither extension is admitted here. |
| `CMB-RES-007` | Strict replay recomputes costs and result facts once; duplicate/rejected commands and restart add no draws or charges. Side artifacts receive only approved projections, never raw receipts or random-stream facts. |

## Admission and irreversible costs

Keep one independent infantry unit and one 10-TOE component on each side, ratings 1/1, Basic Morale
0, Cohesion 0, CPA 10, Clear terrain, full assignment and a real RBA decline. Admission also proves
that these are the only units subject to the two hexes' combat CP exposure. No omitted occupant,
prior combat charge in this segment, gun, armor, Reserve, pin, special modifier, mandatory attack,
existing custody or unsupported readiness state fits this profile. DES-001's hidden-binding and
DES-005's public-legality gates still apply; an empty visible list is not that proof.

Each side has exactly 10 carried Ammo Points with ownership/location/use provenance, and the
approved water/stores readiness facts. Use existing cumulative stage expenditure `E` without
resetting earlier Movement or Breakdown history. For this proposal require `E_attacker + 5 <= 10`
and `E_defender + 3 <= 10`, using exact CP arithmetic. Current Cohesion must already be 0.
Thus the fixed cost produces no over-CPA DP and both pre-roll Cohesion values remain 0. Equality
at CPA is allowed; any excess, even a fractional amount, is outside this profile before choices.
This is a capability boundary, not a source rule forbidding all combat beyond CPA. DES-005 further
requires integer settlement expenditure and certified retreat/custody/reunion geometry; these cost
thresholds alone are not complete admission.

Certify these conditions before selection and revalidate at round opening and commit. DES-003's
permitted precommit suffix changes bookkeeping only; nothing can consume these resources between
certification and use. A changed resource, participant or profile after sealing is an invalid
history, not a new voluntary cancellation path. Prepared choices cannot be discarded by timers,
controller loss or an unrelated spender. Full result/settlement support must be certified in
advance; discovering a missing table row after commitment is an implementation defect.

At Close Assault, `CombatAttackCommitted` validates Prepared plus the exact DES-003 suffix, then
publishes all of the following as one authority transition:

- commitment identity, two canonical allocations and frozen pre-loss unit/component/hex bindings;
- the once-only attacker-to-defender history edge and target-hex use required by DES-001;
- attacker CP `E -> E + 5`, defender CP `E -> E + 3`, both bound to their current stage ledgers;
- each carried ammo balance `10 -> 0`, charged for 10 committed TOE, not Actual Points or survivors;
- cost reasons, prior/result balances, readiness-at-use proof, unchanged pre-roll Cohesion and
  pre-result campaign RNG state/cursor, plus the frozen rules/Content/config/procedure bindings.

That atomic transition is the selected assault's instant of use. It consumes no RNG and changes
neither TOE nor positions. Costs are not charged at selection, decline, sealing or empty steps;
all DES-003 no-attack paths remain free. There is no intermediate committed-but-unpaid state,
refund on zero losses, second cost event or client-triggered charge. A failed atomic publication
leaves the entire prior state intact; a lost response after publication recovers the same receipt.

The resulting zero ammo does not invalidate the use just paid for or turn this attack into an
ammunitionless surrender. Resolution uses retained eligibility-at-use evidence. Later uses must
check the depleted balance; this fixture supplies no resupply or automatic repeat authorization.
No generic post-cost validation may insist the original ammo balance is still 10.

For a broader profile, newly exceeded CP and immediate Cohesion changes must be derived from
prior cumulative expenditure before the relevant Morale check. Do not introduce a second DP
balance or reset Cohesion to 0. Source 11.25 exposes other units in the hex to CP even when their
strength is withheld; 11.24 caps combat charges per segment, whereas ammo charges actual uses.
Effectful presteps, repeated participation and the final-differential-dependent 11.27 exception
need a reviewed incremental-cost/Morale policy. Charging 5/3 anew at every fire stage is invalid.

## One committed assault, one result

The resolver is deterministic Core work with no provider, clock or remote I/O. `Committed` has
one system continuation into `CombatAssaultResolved`; no further player decision is needed to
roll. Compute against retained post-cost/pre-loss inputs and the committed pre-result cursor.
Both roles retain 10 Raw Points and 1 Actual Point; Basic Differential is 0.

Use `CMB-RSH-004-D1` as the proposed procedure, without marking its policy approved:

1. attacker Morale tens/ones, then defender Morale tens/ones;
2. derive both adjusted Morale values and their difference, yielding final differential -2..+2;
3. attacker assault tens/ones, then defender assault tens/ones, using that same differential;
4. only if a capture trigger exists, draw that affected side's capture-share die.

Purpose labels and byte cursor semantics are those in the RNG packet. Attacker/defender are roles,
not fixed faction names or seal arrival order. Tens/ones preserve distinguishable die order;
never sort their numeric values. Each assault pair supplies its percentage coordinate and its
sum for Engaged/Retreat/Captured tests. No rerolls or independent sum-test pairs are allowed.
The selected surface permits at most one capture trigger; eight or nine accepted dice may consume
more bytes because rejection advances the cursor. Do not reserve an unused ninth die or reseed.

Calculate both results before applying any casualty. Sequential *assault instances* do not mean
attacker-first casualties inside one assault. Loss percentages use each side's frozen pre-loss Raw
Points; defender losses cannot reduce its already-committed effect on the attacker. Keep raw
Engaged and required Retreat separately, including zero-loss results, for DES-005's precedence.

`CombatAssaultResolved` binds commitment/prior version, post-cost input proof, table/procedure
identities, purpose-tagged dice with cursor evidence, both Morale values, basic/final differential,
both table loss percentages, raw Engaged, required Retreat distance, capture-trigger side and
optional share. Publish that complete evidence, final RNG state and the pending settlement
obligation set atomically; increment authority once and derive `Settling`.
No per-die or first-side-result authoritative checkpoint is introduced for this bounded resolver.

A failed derivation/publication emits no partial result and advances no authoritative cursor.
Restart before result publication recomputes from the committed cursor; restart afterward reads
and verifies the retained result without drawing again. Duplicate resolution returns the existing
receipt with no event. Wrong commitment, altered inputs, unknown table coordinate, cursor overflow
or malformed evidence fails without a partial mutation; it never produces a synthetic zero result,
refund, cancellation or alternative seed. Storage/corruption faults require operational recovery;
they are not controller timeouts and cannot advance the campaign. The combined freeze must specify
bounds and fault handling, and prove valid admitted input cannot reach a missing semantic case.

## Settlement handoff and stage ordering

DES-005 receives immutable pre-loss strengths, committed use/cost proof, both result facts and
any capture share. It must close every reachable obligation before DES-002's round closure and
DES-003's final step completion. Result publication alone is not combat completion.

| Retained fact | DES-005 responsibility |
| --- | --- |
| Table percentage and required Retreat | Validate retreat/completion or source refusal; add ten percentage points per unfulfilled hex before rounding. Do not freeze final defender losses before that choice. |
| Pre-loss Raw Points and committed TOE | Attacker rounds up, defender down; rating-1 mapping is fixture-specific. Conserve TOE and apply actual-loss DP at its source boundary. |
| Capture trigger and share | Calculate captured subset of eventual loss once; no second casualty debit. Retain origin and settle custody/guard/escape. The RNG packet's share die is not a custody choice. |
| Raw Engaged and Retreat | Retreat priority and final positions/survivors determine relations, including zero-loss branches. |
| Paid costs and cumulative stage ledgers | Preserve combat debits; add reasoned retreat CP/loss DP and actual-victory RP in reviewed order. Do not award RP merely because a retreat was rolled. |

No-selection and cancelled attempts have neither commitment nor resolution event. Successful
settlement closes exactly the committed round; DES-003 owns the single CA->Reserve Release
completion. It cannot admit another assault, release a unit or close/repeat the cycle by itself.

Barrage/AA are certified empty in this profile: DES-003's step receipts are their whole evidence,
with zero effects, cost and RNG. There is no fake empty `CombatAssaultResolved` for either stage.
The extension requirements preserve the source distinction:

| Future stage | Required boundary before admission |
| --- | --- |
| Nonempty Barrage | Freeze all eligible declarations and source target scopes before evaluating effects. Derive both sides from that stage base; aggregate/publish effects without suppressing return fire due to earlier iteration casualties. Settle Barrage before RBA. Include source pinning/errata, costs and a reviewed draw order. |
| Nonempty Anti-Armor | Use closed assignments and source exposure rules; compute simultaneous effects from pre-AA inputs, settle losses, then derive surviving Close Assault strength. Extend DES-003's no-effect suffix explicitly; never silently mask the changed base. |
| Multiple Close Assaults | Resolve one whole assault and its mandatory settlement before starting the next against updated authority. Define legal order selection, identity, resource/hex/history accounting and disclosure. Do not precompute all against one segment-opening strength or choose order from hidden IDs. |

These extension contracts are requirements, not added commands, participant sets or implemented
nonempty stages. Current profile still admits just one full Close Assault per segment.

## Persistence, disclosure and compatibility

Archives must retain the prepared proof, atomic commitment/cost receipt, current result state,
pre-loss basis, exact RNG continuation and unresolved settlement evidence. Replay recomputes
allowed precommit steps, balances, history/hex use and the complete result against pinned tables
and procedure. It must reject duplicate cost effects, changed dice/order/cursors, inconsistent
Morale/differential, mismatched conditional capture and result facts detached from their commitment.
No live rules replacement may reinterpret retained results; old artifact bytes and hashes remain.

Authoritative receipts contain hidden facts. Keep rolls, exact enemy strength/resources/Morale,
seed/cursor, real bindings, internal hashes and pending-cause details out of Dispatch, observations,
Runner and side-visible Chronicle unless an explicit DES-005 projection allows a particular fact.
Own cost visibility and shared phase progress need that same approved policy; raw event reuse is
not a projection. Equal authorized histories must preserve side bytes and submission outcomes.

No current protobuf field or contract version is allocated. Combined freeze owns exact event/
snapshot unions, canonical codecs, size/version limits, migrations and fault/recovery contracts.
Disabling new admission must retain compatible recovery for committed or settling rounds.

## Acceptance and verification plan

| Case | Required observation | Decisions |
| --- | --- | --- |
| `CMB-RES-AC-001` | Exact CP thresholds E=5 attacker/E=7 defender admit at CPA10; any excess or changed Cohesion/readiness fails trusted profile admission before choice/commit. No hidden-state-dependent outward workaround. | 001-003 |
| `CMB-RES-AC-002` | Commit publishes history, hex use, E+5/E+3 and both ammo10->0 together, with unchanged RNG/TOE/positions. Inject publication failure: all prior facts remain. | 002/007 |
| `CMB-RES-AC-003` | No-selection/cancellation spends nothing; retry after successful commit does not charge again. Post-debit zero ammo permits this paid use but grants no supplied repeat. | 001-002/007 |
| `CMB-RES-AC-004` | Both seal orders yield equal gameplay/cost/dice results using role order; each history independently replays its own receipt/version bytes. | 003-004/007 |
| `CMB-RES-AC-005` | Both sides use the same final differential and frozen pre-loss strength. Swapping casualty application order cannot change either result. CP-induced row changes may not be reset to fixture Cohesion. | 001/003 |
| `CMB-RES-AC-006` | All 36 Morale coordinates and every selected differential/role/assault coordinate exist in independently verified tables. Missing/extra/ambiguous rows fail artifact admission; sampled goldens alone cannot certify tables. | 001/004 |
| `CMB-RES-AC-007` | Existing seed18 favorable retreat, seed47/31707 captures, seed208 refusal, seed1296 zero-loss Engaged and rejection/block-crossing vectors retain their correlated facts. Seed208's final losses remain conditional on DES-005 refusal evidence. | 003-005 |
| `CMB-RES-AC-008` | Eight accepted dice without capture, nine when triggered; rejected bytes advance only cursor. Changed purpose, sorted pair, extra draw or altered final cursor fails strict readback. | 004/007 |
| `CMB-RES-AC-009` | Restart before/after commit and result publication preserves exactly one cost/result and RNG continuation. Failed resolution publishes no partial dice/result/cursor; no provider or player needed to resume rolls. | 002/004/007 |
| `CMB-RES-AC-010` | Result leaves retreat/refusal, final losses, custody and relations pending. Zero losses do not erase Retreat/Engaged; capture is a subset and RP waits for proved victory. No unresolved round advances to Reserve Release. | 005 |
| `CMB-RES-AC-011` | Empty Barrage/AA yield only ordered DES-003 receipts with no cost/draw. Nonempty stages, second assault, extra exposed occupant or reduced-cost differential require extension before active admission. | 001/006 |
| `CMB-RES-AC-012` | Tampered cost/base/result/pending evidence fails restore; equal authorized histories preserve projected bytes and outcomes. Raw resolver receipts and RNG state never reach side artifacts. | 007 |

These are future executable obligations. This packet's evidence is source inspection, semantic
traces, existing research-oracle runs and local link/ID/diff checks. It does not establish runtime
Combat support or approve pending research policy. DES-005 now defines the bounded settlement
and disclosure layer; cycle composition and combined contract/implementation-plan review follow.

**Verification (2026-09-06):** both existing research verifiers passed: 12 RNG vectors with a
6,480-coordinate domain and 1,900,656 expanded leaves; eight mutable-state boundary cases,
108 conditional arithmetic combinations and three guard probes. Local checks resolved 177 path
links across five changed documents and validated seven ordered decision IDs and twelve acceptance
IDs. `git diff --check` passed. Ordinary second-model quality review checked source/cross-document
ordering and found no actionable issues. This is not a new independent-review checkpoint.
No .NET build or tests ran for this documentation-only change.
