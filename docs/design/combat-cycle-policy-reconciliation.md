# Combat and Cycle Policy Reconciliation

**Status:** `CMB-POL-001`–`008` **accepted by project owner, 2026-09-06**.
Corrected plan at `a10a588` accepted; exact contracts and production activation remain gated.
**Original input:** `d2bc67c`. **Approval checkpoint:** `a10a588`.

This register consolidates the six bounded designs and records the accepted bounded policies.
Approval does not change source rules or turn proposed production fields into frozen contracts. The [combined plan](combat-cycle-implementation-plan.md) owns
contract work, task dependencies and evidence. Historical research remains evidence at its stated
checkpoint; this register replaces its scattered next-step questions with current dispositions.

## Decision register

All eight rows below are accepted. Approval evidence: after the initiating task explicitly proposed
“accept the eight policy recommendations and corrected plan, then begin source/contract freeze,”
the project owner replied “Okay sounds good to me lets do it” on2026-09-06. This accepts
`CMB-POL-001`–`008` and the review4 author correction at `a10a588`; it does not authorize a fifth
review or bypass checkpoint B's exact-contract acceptance. Original alternatives remain rationale.

| ID | Accepted choice and consequence | Reconciled inputs / alternative |
| --- | --- | --- |
| `CMB-POL-001` | Adopt the closed singleton infantry exercise in DES-001–005: full 10-TOE assignments, ratings1/1, Basic Morale0, post-cost Cohesion0, integer CP, ten carried Ammo each, certified one-hex retreat/custody geometry, decline-only RBA and empty gun/armor steps. Normalize and verify **every reachable coordinate/result** in the selected −2…+2 surface before admission. Explicitly label this a bounded exercise. | [Identity](combat-opportunity-identity-v1.md), [steps](combat-step-transitions-v1.md), [costs](combat-cost-resolution-order-v1.md), [settlement](combat-settlement-disclosure-v1.md); supersedes the unresolved “whole selected table or subset” question. Alternative: design a different closed profile first; never filter by seed or drop capture. Multi-unit, real RBA and broader combat remain deferred. |
| `CMB-POL-002` | Adopt `CMB-CNT-DEC-001`–`007`; preserve the exclusions in `008`–`010`. Reuse existing explicit component/classification facts where compatible. Close `011` with versioned scenario seeds for current TOE, ammo and readiness; close `012` with a contract-level class/parent reconciliation before coding. Use a dedicated synthetic pack with rules-lab provenance. | [Static Content research](../research/combat-content-static-schema-spike.md). Existing Content6 facts are not proof of Combat admission. No default strength/ammo, inferred infantry class, copied historical unit values or flat combat-stat bag. Exact tokens/field names remain contract work. |
| `CMB-POL-003` | Adopt mutable-state `D1`–`D3` and RNG `D1`/`D2`: single TOE/CP/Cohesion truth, full-game infantry ammo consumption, explicit loss/capture/retreat obligations, atomic costs before role-ordered rolls and atomic result/cursor publication. Allow resumable settlement; forbid completed closure with immediate obligations. | [Mutable state](../research/combat-mutable-state-spike.md), [RNG](../research/combat-rng-golden-spike.md), DES-004/005. Carry readiness provenance, BP/checked bands/broken lots and future obligations through repeat. No resupply or automatic reset; fixture exhaustion is real. |
| `CMB-POL-004` | Adopt finite persisted decision budgets and DES-002/003/005 fallback: no-selection at selection expiry; missing RBA response cancels without decline; incomplete voluntary assignment cancels without fabricated seals; Prepared/Committed continues without a controller; mandatory retreat defaults to refusal and custody to unguarded escape. Equality with deadline expires; clock regression/unavailability cannot renew it. | [Sealed protocol](combat-sealed-decision-protocol-v1.md), DES-003/005. Budgets are pinned versioned configuration, not hard-coded wall-clock tests. Mandatory-attack profiles need a separate fallback design. Hosted scheduling remains deferred. |
| `CMB-POL-005` | Adopt DES-005's explicit digital custody policy: capture origin is the victim's pre-retreat hex; guarded custody uses the certified ≤3-hex rendezvous and atomic co-located guard formation; unguarded captives escape to the certified surviving original unit's replacement entitlement after one game month. Guards inherit donor resource/CP state. | [Settlement](combat-settlement-disclosure-v1.md). Source does not independently establish this atomic rendezvous timing. Alternative: redesign lawful custody sequencing before activation. Contract work must settle calendar representation/source mapping; no immediate TOE reunion. Future feeding, movement and maturity remain retained, unexecuted obligations at the bounded terminal. |
| `CMB-POL-006` | Adopt DES-005's side disclosure allowlist, including its stated inferences: captor sees own prisoner/guard facts; original owner sees own escaped replacement entitlement; entering assignment in this profile implies RBA decline. Raw enemy strength/resources, dice/cursors, authority hashes and internal event counts remain private. | DES-001/002/005. Equal authorized histories must preserve bytes, candidate IDs and accept/reject behavior. Host timing/traffic privacy needs later evidence; approving this allowlist is not a constant-time claim. A narrower allowlist requires revising the affected decisions and projections. |
| `CMB-POL-007` | Adopt `RESREL-DEC-001`–`004`: first release is friendly-phase-relative; unavailable first-release I converts to II, later II retains; voluntary ceilings use cumulative stage CP; committed offensive-use history and the immediate next-Movement exception survive status changes. | [Reserve research](../research/reserve-release-history-spike.md), [cycle composition](continual-cycle-reserve-composition-v1.md). One release-window budget, no renewed deadline per unit. Pre-release I movement and offensive Combat by released reserves remain excluded until their extension is designed. |
| `CMB-POL-008` | Adopt CYCLE-DES-001's proposed event/codec composition and bounded evidence terminal: retain pre-event prefix, public/authority separation, Movement-end eligibility and semantic progress; repeat needs supported legal continuation; finish reaches same-slot Truck Convoy entry. Retain guard/upkeep/replacement obligations and reject unsupported requested terminals at admission. | [Cycle composition](continual-cycle-reserve-composition-v1.md). This implements, rather than reopens, accepted `CYCLE-DEC-001`–`014`. No seed-derived legality, guessed cycle history, silent stage reset or two-assault claim from one ammo load. Exact production schemas still require freeze. |

## Accepted source amendment

`CMB-SRC-RUL-001` was accepted by owner on2026-09-06 after the research at `4f44c30`, subject to
research-backed fidelity to the original game. The [source manifest](../research/combat-source-freeze-v1.md)
records the acceptance, alternatives, provenance and limits. Assign10% defender loss at final +2,
ordered rolls34/35/36. Preserve all357 defined selected loss values and raw source gaps separately;
no historical designer-intent claim. Include the amendment identity and exact cells in the future
rules/config digest. This completes POL-001's selected-source investigation; contract/runtime gates
remain. Any later verified primary correction requires an explicit versioned amendment.

## Reconciliation outcomes

| Earlier uncertainty | Current disposition |
| --- | --- |
| Missing custody/escape design | DES-005 supplies the design; POL-005 is accepted. Calendar mapping and versioned records are assigned to TASK-001/003. |
| Ammo/readiness unspecified | POL-002/003 select explicit scenario seeds and retained resource/readiness provenance. Exact schema validation is TASK-002/003; general supply and replenishment are deferred. |
| Retreat/Breakdown continuity | The admitted infantry route has no vehicle/BP charge. Existing BP, checked bands and broken lots still survive repetition; TASK-018 tests continuity. Vehicle retreat needs an extension. |
| Cycle identity unresolved | CYCLE-DEC-001–014 remain accepted; POL-008 addresses the later composition proposal, not their approval status. |
| “Reviewed” means ready to implement | Reviews1–3 cover DES-001–005. [Review4](../reviews/combat-cycle-plan-review-4.md) covers cycle/combined planning at9f683d1 and returns Not ready with one break-off handoff gap. Author correction assigns it to TASK-001/003/018/019; policies and corrected plan are now accepted; exact contracts remain pending. |

## Decision and implementation gates

1. **Complete:** owner accepted POL-001–008 and the corrected plan on2026-09-06. G0 is satisfied
   through that acceptance and recorded disposition of the review4 finding.
2. Review4 assessed the combined plan including CYCLE-DES-001. Retain its finding and the author
   correction in plan acceptance; no independent Ready verdict exists for the correction. The user
   approved one additional pass on2026-09-06; the revised budget is exhausted at **4 of 4**.
3. Execute TASK-001–004 to close source/calendar and exact schema/compatibility questions. Review
   their concrete contracts before consumers. A remaining unsupported reachable effect blocks
   activation; it cannot be relabelled a successful bounded result.

No root architecture, runtime, protobuf or simulator behavior changes in this packet. The proposed
work stays in Umpire/Core and the existing Exercise/Runner modules. Hosted Dispatch, real prisoner
upkeep, broader combat and a whole playable campaign retain separate acceptance boundaries.
