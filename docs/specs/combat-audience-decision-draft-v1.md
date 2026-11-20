# Combat audience decision — draft CMB-CON-005 successor artifacts (draft v1)

**Status: DRAFT for owner review. Not frozen.** No independent review has run. Freezing is the
owner's own action; nothing in this file, its schema, fixture, verifier or tests performs or implies
it. Task 020B, per the [Task 020 scoping doc](../work/plans/2026-11-20-task-020-combat-legal-actions-scoping.md)
(T020-Q1 ruling, 2026-11-19: draft now, owner freezes).

Artifacts:

- [ordered schema](combat-audience-decision-draft-v1.schema.json) (field/type descriptor, not JSON Schema)
- [goldens and rejection vectors](fixtures/combat-audience-decision-draft-v1.json)
- verifier: `src/Cna.Core/Actions/CampaignCombatAudienceDraftVerifier.cs` (internal)
- oracle tests: `tests/Cna.Core.Tests/Actions/CombatCycleDisclosureDraftContractTests.cs`

These pin the bytes the dormant Core audience surface
(`src/Cna.Core/Actions/CampaignCombatAudienceDecisions.cs`, 020A0–020A2) already emits. They add no
behavior, change no event, snapshot or canonical byte, and keep every type `internal`. The prose
contract stays [Combat contact side projection v2](combat-contact-side-projection-v2.md).

## What is covered

Five owner-visible round kinds, plus the shared decision and outcome envelopes:

| Stage | Visible cut | Revision | Offers | Golden |
| --- | --- | --- | --- | --- |
| `combat-selection` | `CampaignContactCombatOwnerVisibleCutV2` (v2) | side contact audience revision (≥ 1) | assault offers + optional finish | `selection-axis-first-cut` |
| `force-assignment` | `CampaignContactForceAssignmentOwnerVisibleCutV1` (v1) | side contact audience revision (≥ 1) | one seal | `force-assignment-axis`, `force-assignment-commonwealth` (same cut) |
| `reserve-release` | `CampaignReserveReleaseOwnerCutV27` (v27) | 0 | `choices` | `reserve-release-axis` |
| `cycle-control` | `CampaignCycleControlOwnerCutV28` (v28) | 0 | `choices` | `cycle-control-axis` |
| `rba-decline` | `CampaignContactRbaDeclineOwnerVisibleCutV1` (v1, new in 020A4) | side contact audience revision (≥ 1) | one decline | `rba-decline-commonwealth` |

`rba-decline` is the bounded decline-only Retreat Before Assault decision (`CMB-POL-001`; T020-Q7
ruled 2026-11-20, option b). Its codec is separate from the full
`CampaignContactRetreatBeforeAssaultOwnerVisibleCutV1`. It keeps campaign, side, position, defender
formation, contact revision, contact observation, the defender's own element and the decline offer.
It drops `moveOffers`, `locations`, `edges` and `initiallyAdjacentToKnownEnemy`, which only serve
RBA movement, so every listed offer is takeable. Core's RBA move derivation is unchanged; its move
offers are never published or mapped. The field selection is an author choice for owner review.

Decision envelope: `contractVersion:1`, `campaignId`, `audience` (`axis`/`commonwealth`), `stage`,
`actingElementId` (`cycle-control` for Cycle Control), `audienceRevision`, `visibleDigest`
(`sha256:` over the exact visible value bytes), `visible` (raw cut bytes), `offerRefs`. Each ref is
`cmbref.v1.` + SHA-256 of a compact JSON preimage of domain, campaign, audience, stage, acting
element, visible digest and ordinal. Outcome envelope: exactly
`{"contractVersion":1,"outcome":"rejected"}` for every rejection cause, or the accepted form naming
the submitted ref. Submissions are an in-process record; this draft defines no submission wire bytes
(the Host route has its own v2 body).

Golden paths are the existing 020A fixtures: `rules-lab.breakdown.reaction.combat-assault.v1`
(Selection and Force; the contact is bound to infantry, so Selection is not exercised on a
non-combat-unit contact) and `rules-lab.breakdown.reaction.combat-double-fresh.v1` (Reserve Release
and Cycle Control). Each golden row records setup, campaign ID, seed, audience and prefix.

Rejection vectors reuse the existing per-round-kind enumerations verbatim (each vector names its
source test): 13 Selection, 9 Force Assignment, 9 Reserve Release, 9 Cycle Control, 14 decline-only
RBA (including the raw Core decline and move action IDs used as refs). Cycle Control has
no enumeration of its own in 020A2; it uses the Reserve/Cycle list with the cross-stage vector
pointing at `reserve-release` instead of itself.

## Verifier

`CampaignCombatAudienceDraftVerifier.VerifyDecision(bytes)` returns ordered failure codes; empty
means conforming. It checks, in one pass: strict JSON; exact ordered decision fields; contract
version; audience; covered stage (any other stage, including `custody`, fails `stage-uncovered`);
revision rule per stage; digest over the raw visible bytes; compact visible encoding; exact ordered
visible top-level fields and visible contract version; visible bindings (campaign, side, acting
element, contact observation revision); per-stage offer shape and count against `offerRefs`; for
Selection, every assault targets a current contact in `currentTargets`; a recursive denylist of
private keys (`authorityVersion`, `stateVersion`, `actionId`, ...); recomputation of every ref from
decision-visible values only; finally byte equality with the production serializer.
`VerifyOutcome(bytes, decision?)` checks the two outcome shapes and, given a decision, that an
accepted ref was offered.

Procedure: run
`dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class Cna.Core.Tests.Actions.CombatCycleDisclosureDraftContractTests`.
It (1) checks the schema file against the verifier's rules, (2) regenerates every golden from current
Core and requires byte equality, (3) applies every rejection vector to a live cut and requires the
golden rejection bytes, (4) applies a shared tamper set plus stage-specific tampers to each round
kind's golden, consistently re-signed where needed so the deeper check (not just the digest fence)
must catch it.

## What is not covered, and why

Each gap is an unresolved owner ruling or unbuilt surface. No placeholder content is supplied.

| Not covered | Reason |
| --- | --- |
| Custody cut | T020-Q5 resolved 2026-11-20 (the V1 captor cut is the allowlist). The custody stage is built in the Core audience surface (#457) but not yet modeled here; the verifier still rejects `custody` as uncovered. |
| Forced and surprise Combat transitions | T020-Q2 ruled 2026-11-20: out of the bounded profile and rejected at content admission (`BRK-CERT-003`); no transition is modeled. |
| Contact bound to a non-combat unit | T020-Q8 ruled 2026-11-20 (option c): the Selection offer stays and the downstream V19 `force-unavailable` System cancellation handles it, so no visible-byte rule applies. Goldens still use an infantry-bound contact. |
| RBA movement, post-assault Retreat, repeat Movement/Selection (V31), second-side Selection (V47) | RBA movement is outside `CMB-POL-001` (T020-Q7); the others are not built in the Core audience surface. |
| Capability manifest | T020-Q3 ruled to Task 021. |
| Host HMAC refs and Host/Core mapping consolidation | T020-Q4 (Core visible-values-only refs accepted as the in-process subset; Host HMAC precedes 020D) and T020-Q6 (deferred to 020D). |
| Notification bytes, terminal-reason disclosure pairs | No outward notification exists yet; terminal-reason pairs are 020D. |

## Observations for the owner (not changed here)

- Reserve Release and Cycle Control cuts spell `side` as `Axis`/`Commonwealth` (enum `ToString`);
  Selection and Force use lowercase. The draft pins the bytes as emitted. Unifying them would change
  V27/V28 visible bytes and is an owner decision.
- Reserve Release and Cycle Control report `audienceRevision: 0` and fence on the visible digest
  only (author interpretation recorded in 020A2, within the T020-Q4 in-process subset).
- The goldens pin current Core output. Any change to these visible cuts or the ref preimage fails the
  golden test; that is intended for a freeze candidate.
