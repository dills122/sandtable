# Task 020 — Combat audience-safe actions: scoping (2026-11-20)

**Status:** author scoping and a first dormant slice. No independent review has run. Nothing
here claims Task 020/021 is complete, certified, or "Activated and witnessed". The owner
authorizes and counts review passes.

## 1. What Task 020 actually requires

Source: [combined plan, Checkpoint I](../../design/combat-cycle-implementation-plan.md#checkpoint-i--side-safe-public-activation).

| Item | Plan text (abridged) |
| --- | --- |
| Output | Implement the **frozen `CMB-CON-005` successor** observation/action projection and authenticated-audience submission mapping. Enforce allowlisted custody/replacement inferences; hide raw authority history/counters and unapproved opponent facts. |
| Verification | `CombatCycleDisclosure`: equal-owner-visible-history byte/action/opening/notification/rejection pairs across hidden forks, seal order and terminal reasons; forced-action and capability-closure negatives. |
| Dependencies | 004 / 019 / **successor freeze**. Paths: Core Observations, Core Actions, focused Core tests. |
| Hard gate | "Task 020 cannot start its outward implementation against the historical World-derived projection." Freeze a versioned `CMB-CON-005` successor first (contact transitions, loss/rediscovery, public revision distinct from authority version, same-cut offers, owner opening/submission, source-backed forced and surprise Combat, closed capability manifest validated at activation and on restore). |

Task 021 (separate): activate the certified capability in ordinary Core authority; reject
unsupported profiles/terminals before decisions; keep old readers and recovery. Unlike
017–019, the plan has **no sub-task split** (no 020A/B…) for 020 or 021; this doc proposes one.

## 2. Corrections to the dispatch premise

1. **The outward surface is not "missing" — it exists at the Host, outside `CampaignLegalActions`.**
   [Contact side projection v2](../../specs/combat-contact-side-projection-v2.md) is the
   `CMB-CON-005` successor draft. Core already projects owner-visible cuts for every Combat round
   kind (`src/Cna.Core/Visibility/*Cut*.cs`, `CampaignReserveReleaseOwnerCutV27`,
   `CampaignCycleControlOwnerCutV28`, `CampaignRepeatCombatOwnerCutV31`), and
   `CampaignHostAdapter.Query*OwnerDraft` hands visible bytes plus private Core action IDs to the
   Host. The Host (`CampaignDurableAuthorityJournal.Openings.cs` / `.Submissions.cs`) mints seat-bound
   openings with HMAC public refs, re-projects on submit, requires byte-equal visible cuts, and maps
   ordinal → current Core action. `CampaignOwnerOpeningKind` covers `CombatSelection`,
   `RetreatBeforeAssault`, `ForceAssignment`, `PostAssaultRetreat`, `Custody`, `ReserveRelease`,
   `CycleControl`, `RepeatMovement`, `RepeatCombatSelection`. It is reachable only via the
   loopback Development `/api/v2/campaigns` route and is explicitly "not public Combat activation".
2. **What is genuinely missing for Exercise/Runner (#448):** a *Core-owned* audience-safe
   decision/submission surface. `CampaignLegalActions.Query` / `CampaignExercises.Query` stop at
   Combat entry (`CampaignHostAdapter.QueryAudience` rejects whenever `CurrentCombatDecision` is set),
   and every Combat step in `CampaignExercises` is `internal` and keyed by private Core action IDs.
3. **"Extend `CampaignLegalActionSet`" conflicts with `CMB-CON-005`.** CON-005 requires "own
   revision/action set separate from authority version". `CampaignLegalActionSet.StateVersion` is
   bound to the exact authority revision used by submission; at Combat cuts a hidden opponent seal or
   private settlement advances that version. Reusing it would leak hidden progress and make
   equal-visible histories distinguishable. The successor needs a parallel type keyed by the side
   **contact audience revision**, not an extension of the legal-action set.
4. **The successor is not contract-frozen** by the roadmap's own definition (spec + schema/fixtures +
   failure-sensitive oracle). v2 has prose only; no schema, golden bytes, vectors, or verifier.
   Freezing is an owner decision (`User review precedes moving beyond a checkpoint when its
   contract boundary changes`).

## 3. Open items the frozen docs do not resolve (stop-and-report)

| ID | Gap | Why it needs a ruling |
| --- | --- | --- |
| T020-Q1 | Successor freeze of `CMB-CON-005` (v2) | Plan gate. Agent can draft schema/goldens, cannot declare frozen. |
| T020-Q2 | Forced/surprise Combat | Land 10.3 mandatory attack in opposing ZOC. Current Core **throws** (`"Positive ZOC needs a contact-safe Combat projection."`) at runtime instead of rejecting at activation. v2 §"Forced and surprise contact" states the requirement but pins no source-backed detection/forced-choice/interruption behavior. Needs a source ruling before implementation. |
| T020-Q3 | Closed capability manifest at activation | No `CapabilityManifest` exists in Core. Shape, family list and restore revalidation are unspecified beyond the fog spike's prose. Probably 021-owned, but the plan lists "capability-closure negatives" under 020 verification. Owner should assign. |
| T020-Q4 | Core public-ref derivation | v2: refs "derive only from approved owner-visible values and Host-issued opening identity". Exercise has no Host. Slice 1 uses a visible-values-only derivation (digest + ordinal + stage + element + audience + campaign) and keeps the type `internal`. Owner should confirm this is the in-process subset, with Host HMAC layered on top, before it becomes public. |
| T020-Q5 | Custody/replacement inference allowlist | Plan requires "allowlisted custody/replacement inferences". No allowlist document exists; custody cut bytes currently carry whatever `CampaignContactCustodyOwnerVisibleCutV1` emits. Needs an explicit list. |
| T020-Q6 | Host/Core mapping duplication | Slice 1 duplicates the Host's re-project/byte-compare/ordinal-map rule in Core. Long-term the Host should consume the Core mapping (021 or a follow-up), or the two can diverge. |

## 4. Proposed breakdown

Each child keeps the plan's five-primary-file cap and needs owner-authorized review before
acceptance. Children marked *dormant* stay `internal` until T020-Q1 is resolved.

| Child | Output | Depends on |
| --- | --- | --- |
| **020A0** (this slice, dormant) | Core audience-safe decision/submission surface for **Selection** (first-side initial cut): typed decision keyed by contact audience revision, visible-only refs, same-cut submit mapping, one generic rejection envelope; `CombatCycleDisclosure` hidden-fork tests. | 019 path present |
| 020A1 (dormant) | Same surface for RBA (move/decline) and Force Assignment (both seal orders: first seal must not change the other owner's decision bytes). | A0 |
| 020A2 (dormant) | Post-assault Retreat, Custody (after Q5 allowlist), Reserve Release, Cycle Control; repeat-combat V31 and second-side V47 selection. | A1, Q5 |
| 020B | Draft frozen artifacts for the successor: schema + canonical goldens + rejection vectors + verifier for every owner-visible cut and the decision/rejection envelopes. Owner freezes. | A0–A2, Q1 |
| 020C | Forced/surprise Combat transitions per ruling; replace runtime throw with activation rejection. | Q2 ruling |
| 020D | Make the A-surface public; Host consumes Core mapping (Q6). Terminal-reason and seal-order disclosure pairs end-to-end. | B, C, Q4 |
| 021 | Capability manifest admission and ordinary activation (per plan), if Q3 assigns it there. | 020 |

### Why Selection first

It is the first owner decision in every Combat sequence, has one acting side and no paired
seal, already has a hidden-fork precedent (`HiddenEnemyPositionsKeepFinishOfferAndVisibleBytesEqual`)
and a synthetic fixture with an adjacent visible contact. It exercises every surface property
(revision, refs, same-cut acceptance across hidden advance, generic rejection) without touching
timing rounds, seals or settlement.

## 5. Slice 020A0 — what was built

- `src/Cna.Core/Actions/CampaignCombatAudienceDecisions.cs` (internal): `CampaignCombatAudienceDecision`,
  `CampaignCombatAudienceSubmission`, `CampaignCombatAudienceSubmissionResult`, and
  `CampaignCombatAudienceDecisions.QuerySelection` / `SubmitSelection`.
  - Decision carries contract version, campaign, audience, stage `combat-selection`, acting element,
    contact audience revision, visible digest, the existing V2 owner-visible bytes, and ordered refs.
    No authority version, Core action ID or private binding enters it.
  - Ref = `cmbref.v1.` + SHA-256 over a canonical JSON preimage of (domain, campaign, audience, stage,
    acting element, visible digest, ordinal).
  - Submit re-projects the current cut, requires exact visible-digest/revision/identity match, maps
    the ordinal to the current private action, and delegates to the existing
    `CampaignExercises.SubmitContactOwnerCombatSelection` with the session checkpoint. Adjudication
    is unchanged. Every failure returns one byte-identical rejection envelope.
- `tests/Cna.Core.Tests/Actions/CombatCycleDisclosureTests.cs`: see §6.

Not touched: Host, `CampaignLegalActions`, event contracts, canonical bytes, replay. No new event
version, no change to DET-001/002 or EVT-001 surfaces.

## 6. Slice 020A0 test evidence

`CombatCycleDisclosureTests`, 7 tests, all passing locally (Debug, 2026-11-20). Neighbor suites
`CampaignContactCombatSelectionCutTests` and `CampaignLegalActionsTests` still pass (29 total with the
new class); `Boundary=UserSpace` 82/82; `dotnet format --verify-no-changes` clean on both new files.
Full repository gate not run.

| Test | Evidence |
| --- | --- |
| `SelectionDecisionMatchesHostDraftAndCarriesNoPrivateFields` | Creation-rooted Exercise path. Visible bytes equal the Host draft; ref count equals Host private IDs; revision equals contact audience revision; no Core action ID, `boundRepresentationId`, `authorityVersion`, `stateVersion` or `actionId` in outward bytes; deterministic re-query. |
| `AcceptedAudienceSelectionUsesTheUnchangedPrivateAdjudication` | For assault and finish: event bytes, snapshot checkpoint and receipt equal the private-ID path; `Reconstruct` verifies; retry after acceptance rejected; no decision after the cut closes. |
| `EveryRejectionCauseSharesOneOutwardEnvelope` | 12 invalid submissions (forged ref, raw Core action ID as ref, wrong/System audience, wrong/opponent element, revision, digest, stage, campaign, contract version, empty ref) plus post-acceptance replay: byte-identical rejection, no trusted step. |
| `OpponentSeatReceivesNoDecisionAtTheActingSidesCut` | Commonwealth/System/unknown element get the same null result. |
| `EqualVisibleHiddenForksYieldIdenticalAudienceDecisions` | Four synthetic fork pairs: unseen enemy hex; visible contact with hidden opponent CP spend; private RNG seed; hidden authority-version advance. Byte-identical decision, refs, revision; opponent rejected on both. |
| `VisibleDifferenceChangesTheDecisionAndStalesTheOpenedRef` | Sensitivity control: an observed contact changes bytes, digest and all refs. |
| `RefOpenedOnOneHiddenForkSelectsTheSameOwnerChoiceOnTheOther` | Two creation-rooted campaigns differing only in seed: byte-equal decisions; a ref opened on one accepted on the other with the same choice/handle/action; reconstruction verifies. |

Notes from building the tests:

- Synthetic snapshots cannot submit: `CampaignSnapshotV11Admission` requires a breakdown-catalog
  setup. Cross-fork acceptance therefore uses two real creation-rooted sessions (seed fork). This is
  the admission gate working as intended.
- A first attempt at a "hidden authority advance" on a real session rebuilt the contact journal from
  scratch, which reset the audience revision (9 → 1) and correctly produced different bytes. That
  was a test-construction error, not a leak; the fork is now built synthetically with both journals
  initialized identically.

## 7. Still pending after 020A0

All other round kinds (A1/A2), successor freeze artifacts (B), forced/surprise (C), publication
and Host consolidation (D), seal-order/terminal-reason disclosure pairs, capability manifest,
custody inference allowlist, Exercise/Runner adoption (022–024), and all independent review.
