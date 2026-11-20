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
   contract boundary changes`). Update 2026-11-19: a *draft* schema/goldens/vectors/verifier now
   exists for the four built round kinds (§7b); it is still not frozen.

## 3. Open items the frozen docs do not resolve (stop-and-report)

| ID | Gap | Why it needs a ruling | Status |
| --- | --- | --- | --- |
| T020-Q1 | Successor freeze of `CMB-CON-005` (v2) | Plan gate. Agent can draft schema/goldens, cannot declare frozen. | **Resolved 2026-11-19 (owner, relayed):** draft the freeze artifacts now for owner review; the owner performs the freeze. Draft in §7b. Not frozen. |
| T020-Q2 | Forced/surprise Combat | Land 10.3 mandatory attack in opposing ZOC. Current Core **throws** (`"Positive ZOC needs a contact-safe Combat projection."`) at runtime instead of rejecting at activation. v2 §"Forced and surprise contact" states the requirement but pins no source-backed detection/forced-choice/interruption behavior. Needs a source ruling before implementation. | **Resolved 2026-11-20 (owner, relayed):** out of scope for the bounded certified content. Per `combat-p01-ac003-bounded-profile-decision-proposal.md` ("Mandatory ZOC assault, Holding Off and forced retreat \| Source condition not met by current one-stacking-point occupied hexes") it does not arise in singleton-infantry content. 020C replaces the runtime throw with a clean activation-time "unsupported, exceeds bounded profile" rejection, after verifying the throw is unreachable in that content. No forced/surprise mechanic is designed. |
| T020-Q3 | Closed capability manifest at activation | No `CapabilityManifest` exists in Core. Shape, family list and restore revalidation are unspecified beyond the fog spike's prose. Probably 021-owned, but the plan lists "capability-closure negatives" under 020 verification. Owner should assign. | **Resolved 2026-11-19 (owner, relayed):** assigned to Task 021, not 020. |
| T020-Q4 | Core public-ref derivation | v2: refs "derive only from approved owner-visible values and Host-issued opening identity". Exercise has no Host. Slice 1 uses a visible-values-only derivation (digest + ordinal + stage + element + audience + campaign) and keeps the type `internal`. Owner should confirm this is the in-process subset, with Host HMAC layered on top, before it becomes public. | **Resolved 2026-11-19 (owner, relayed):** confirmed acceptable as the in-process subset for now; Host HMAC layering is required before anything goes public (020D). |
| T020-Q5 | Custody/replacement inference allowlist | Plan requires "allowlisted custody/replacement inferences". No allowlist document exists; custody cut bytes currently carry whatever `CampaignContactCustodyOwnerVisibleCutV1` emits. Needs an explicit list. | **Resolved 2026-11-20 (owner, relayed):** the existing `CampaignContactCustodyOwnerVisibleCutV1` field set (`campaignId`, `captorElementId`, `captiveQuantity`, `offers[kind, route]`) is the allowlist; no change. Custody built in 020A3 (§7c). The 020B draft verifier still treats `custody` as uncovered; the draft has not been extended. |
| T020-Q7 | RBA unseen-encounter policy (added in 020A1) | v2 requires it "before an outward opening"; RBA moves into hexes with unseen enemies have no defined outcome. See §7. | **Resolved 2026-11-20 (owner, relayed):** RBA movement stays excluded (outside `CMB-POL-001`, which adopts decline-only RBA). Build RBA decline-only; do not build movement. Building decline-only raised a codec question; see §7c. |
| T020-Q8 | Contact bound to a non-combat unit (added in 020A1) | Selection offers an assault that has no Force slot; v2 requires a source-backed disposition before outward issuance. See §7. | **Resolved 2026-11-20 (owner, relayed), revised the same day:** the first ruling (exclude the offer) conflicted with the fog rule (§7e); the owner then chose option (c): keep the Selection offer unchanged and rely on the existing V19 `force-unavailable` System cancellation downstream. Confirmed by tests in §7e. |
| T020-Q6 | Host/Core mapping duplication | Slice 1 duplicates the Host's re-project/byte-compare/ordinal-map rule in Core. Long-term the Host should consume the Core mapping (021 or a follow-up), or the two can diverge. | **Resolved 2026-11-19 (owner, relayed):** deferred to 020D; not fixed now. |

## 4. Proposed breakdown

Each child keeps the plan's five-primary-file cap and needs owner-authorized review before
acceptance. Children marked *dormant* stay `internal` until T020-Q1 is resolved.

| Child | Output | Depends on |
| --- | --- | --- |
| **020A0** (this slice, dormant) | Core audience-safe decision/submission surface for **Selection** (first-side initial cut): typed decision keyed by contact audience revision, visible-only refs, same-cut submit mapping, one generic rejection envelope; `CombatCycleDisclosure` hidden-fork tests. | 019 path present |
| 020A1 (dormant) | Same surface for RBA (move/decline) and Force Assignment (both seal orders: first seal must not change the other owner's decision bytes). | A0 |
| 020A2 (dormant) | Post-assault Retreat, Custody (after Q5 allowlist), Reserve Release, Cycle Control; repeat-combat V31 and second-side V47 selection. | A1, Q5 |
| 020B | Draft frozen artifacts for the successor: schema + canonical goldens + rejection vectors + verifier for every owner-visible cut and the decision/rejection envelopes. Owner freezes. Draft for the four built round kinds landed (§7b); Custody pending Q5. | A0–A2, Q1 (resolved) |
| 020C | Forced/surprise Combat transitions per ruling; replace runtime throw with activation rejection. | Q2 ruling |
| 020D | Make the A-surface public; layer Host HMAC refs over the Core subset (Q4 ruling); Host consumes Core mapping (Q6, deferred here). Terminal-reason and seal-order disclosure pairs end-to-end. | B, C |
| 021 | Capability manifest admission and ordinary activation (per plan); Q3 assigns the manifest here. | 020 |

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

## 7. Slice 020A1 — Force Assignment built; RBA stopped

**Force Assignment (built, dormant).** `CampaignCombatAudienceDecisions` now dispatches by stage
(`combat-selection`, `force-assignment`); `QueryForceAssignment`/`SubmitForceAssignment` reuse the
same decision, ref and rejection rules over `CampaignContactForceAssignmentOwnerVisibleCutV1` and
delegate to the unchanged `SubmitContactOwnerForceSeal`. Selection behavior is unchanged (020A0 tests
pass unmodified). `CombatCycleDisclosureForceTests`, 6 rows, local Debug pass:

| Test | Evidence |
| --- | --- |
| `ForceDecisionsMatchHostDraftsAndCarryNoPrivateFields` | Both owners: bytes equal Host draft, one ref, no private fields; cross-owner element/audience, System, and Selection-stage queries all unavailable. |
| `FirstSealLeavesTheOtherOwnersDecisionAndRefUnchanged(attackerFirst: true/false)` | Both seal orders: the other owner's decision bytes/revision/ref are byte-identical after the first seal advances private authority; the pre-seal ref is accepted afterward; sealed owner's retry is the generic rejection; two seals reconstruct and System completion is accepted. |
| `AcceptedAudienceSealUsesTheUnchangedPrivateAdjudication` | Event, snapshot checkpoint and receipt equal the private-ID path for both owners. |
| `EveryForceRejectionCauseSharesOneOutwardEnvelope` | 8 invalid submissions via both `SubmitForceAssignment` and `Submit`, plus a Force submission through `SubmitSelection`: identical rejection. |
| `ForceDecisionsAreEqualAcrossAHiddenRandomFork` | Seed-forked campaigns: both owners' decisions byte-equal; cross-fork ref accepted. |

Fixture note: the Truck setup's adjacent contact is bound to `commonwealth-truck`, which has no
combat component and therefore no Force slot. Force tests use `rules-lab.breakdown.reaction.combat-assault.v1`.
This is the v2 spec's own open item ("a contact bound to a unit with no combat component has no
Force Assignment candidate; normal contact selection must gain a source-backed non-infantry
disposition before outward Combat issuance"). It also bears on Selection: the Selection surface
currently offers an assault on such a contact. Logged as **T020-Q8**.

**RBA (stopped, not built).** RBA decline alone is mechanically simple, but the RBA decision must
expose the full Core offer set (moves + decline), and RBA moves hit an unresolved ruling:

- v2 spec: "An unseen encounter policy remains to complete before an outward opening."
- Code: `CampaignContactRetreatBeforeAssaultCut.DeriveOneEdgeMoves` excludes only *known* contact
  hexes (by design: "Hidden opposing occupancy never controls offer presence"), and
  `CampaignNativeCombatRetreatMovement` has no hidden-occupancy branch. So a defender can be offered,
  and accepted into, a hex holding an unseen enemy, with no defined encounter/interruption outcome.
- What happens then (surprise contact, interruption, rejection) is a source/rules ruling of the same
  family as Q2. Logged as **T020-Q7**. It is not Q5 (custody allowlist).

Options for the owner: (a) rule the unseen-encounter policy, then build RBA; (b) authorize a dormant
RBA surface that mirrors today's Core offers with an explicit "unseen encounter unresolved" caveat;
(c) defer RBA and continue with post-assault Retreat/Reserve Release/Cycle Control (post-assault
Retreat likely raises the same encounter question — not yet checked).

Owner disposition (2026-11-20, relayed): RBA **deferred**; Q7 not pursued; no internal-mirror RBA.

## 7a. Slice 020A2 (partial) — Reserve Release and Cycle Control built

Scope per owner: 020A2 minus RBA/Force (done or deferred) and minus Custody (Q5). Post-assault
Retreat, repeat V31 and second-side V47 selection are not in this slice.

- New stages `reserve-release` and `cycle-control` reuse `CampaignHostAdapter.QueryNativeReserveReleaseOwnerDraft` /
  `QueryNativeCycleControlOwnerDraft` for visible bytes and private IDs (so their existing guards apply),
  and delegate to the unchanged `SubmitNativeReserveReleaseChoice` / `SubmitNativeCycleControl`.
- Cycle Control is side-owned; it uses the fixed subject `cycle-control` as its acting identity.
- These two cuts carry no contact observation. Author choice: the decision reports
  `audienceRevision: 0` and relies on the visible digest as its only fence. This is an
  interpretation under **T020-Q4**, not a ruling.
- Side-safety check by reading: the V28 Repeat witness (`CampaignCycleContinuationMovementWitnessV28`)
  reads own elements and *known* contacts only; V27 pending members derive from own settled World.

`CombatCycleDisclosureReserveCycleTests`, 4 rows, local Debug + Release pass:

| Test | Evidence |
| --- | --- |
| `ReserveReleaseDecisionMatchesHostDraftAndCarriesNoPrivateFields` | Bytes equal Host draft; refs = choices; no private IDs/authority/hash fields; opponent, wrong member, System unavailable; Cycle Control unavailable before completion. |
| `AcceptedReserveAndCycleChoicesUseTheUnchangedPrivateAdjudication` | Every Reserve choice and every Cycle choice (Repeat and Finish): event/snapshot/receipt equal the private path; retry rejected; reconstruction verifies; opponent and wrong-subject Cycle queries unavailable. |
| `EveryReserveAndCycleRejectionCauseSharesOneOutwardEnvelope` | 8 invalid submissions plus cross-entry-point submission: identical rejection. |
| `ReserveAndCycleDecisionsEncodeNoSettlementOrOpponentFacts` | Seed fork (seeds 6/7): byte-equal Reserve and Cycle decisions; cross-fork acceptance. A non-leak check, not an equal-visible-history proof. |

The fixture path uses `rules-lab.breakdown.reaction.combat-double-fresh.v1` (same trusted path as
`CampaignNativeSelectionTimeoutTests`); pre-Release Combat steps go through the existing trusted API.

## 7b. Slice 020B (draft) — successor freeze artifacts for owner review

Per the Q1 ruling (2026-11-19). **Draft only: not frozen, not reviewed.** The owner freezes.

- [Draft spec](../../specs/combat-audience-decision-draft-v1.md),
  [ordered schema](../../specs/combat-audience-decision-draft-v1.schema.json),
  [goldens + rejection vectors](../../specs/fixtures/combat-audience-decision-draft-v1.json).
- Verifier `src/Cna.Core/Actions/CampaignCombatAudienceDraftVerifier.cs` (internal; no runtime caller).
- Covers Selection, Force Assignment (both owners), Reserve Release, Cycle Control and the
  decision/outcome envelopes: five goldens, 40 rejection vectors reused from the 020A tests.
- Not covered, by design: Custody (Q5), forced/surprise transitions (Q2), non-combat-unit
  disposition (Q8), unbuilt round kinds (RBA, post-assault Retreat, V31/V47), Host HMAC (Q4, 020D),
  capability manifest (Q3, 021). The verifier fails `custody` as an uncovered stage.

`CombatCycleDisclosureDraftContractTests`, 7 rows, local Debug pass:

| Test | Evidence |
| --- | --- |
| `DraftSchemaMatchesTheVerifierRulesAndStaysUnfrozen` | Schema decision fields, per-stage visible fields/versions and private-key denylist equal the verifier's; `custody` is listed only as a gap; schema and fixture status say DRAFT / Not frozen. |
| `GoldensReproduceFromCurrentCoreAndConform` | Every golden is reproduced byte-for-byte from current Core (length + SHA-256) and verifies; rejection and accepted envelopes verify; accepted ref unknown to another decision, extra outcome field, malformed ref are caught. |
| `RejectionVectorsYieldTheGoldenRejectionEnvelopeForEveryCoveredStage` | All 40 vectors applied to a live cut return the golden rejection bytes with no trusted step; the valid submission is then accepted and its outcome verifies. |
| `VerifierCatchesTampered{Selection,ForceAssignment,ReserveRelease,CycleControl}Cuts` | Per round kind: 18 shared tampers (digest fence, forged/raw/missing/duplicate refs, field order, injected private field, version, audience, uncovered `custody` stage, campaign/revision/visible-version/visible-field drift re-signed, nested private field re-signed, indented bytes, truncated bytes) plus stage-specific re-signed tampers (stale assault target, offer binding, offer count, seal shape, side, choices). |

Mutation check (local, not committed): disabling the verifier's ref-derivation and offer-target
checks fails all four tamper tests.

## 7c. Slice 020A3 — Custody built; RBA decline-only stopped on a codec fork

**Custody (built, dormant).** New `custody` stage over `CampaignHostAdapter.QueryContactCustodyOwnerDraft`
(the confirmed Q5 allowlist bytes, unchanged), delegating to the unchanged
`SubmitNativeCombatCustody`. The cut carries no contact observation, so revision is 0 and the
visible digest is the fence (same rule as Reserve Release / Cycle Control).
`CombatCycleDisclosureCustodyTests`, 5 rows, local Debug + Release pass:

| Test | Evidence |
| --- | --- |
| `CustodyDecisionMatchesHostDraftAndCarriesOnlyTheAllowlistedFields` | Bytes equal Host draft; visible top-level fields are exactly `contractVersion, campaignId, captorElementId, captiveQuantity, offers` and each offer exactly `kind, route`; no victim element, lot, settlement hash or private ID in outward bytes; opponent/victim/wrong element/System unavailable. |
| `NoCaptureYieldsNoCustodyDecision` | A no-capture history gives no Custody decision for either side. |
| `AcceptedCustodyChoiceUsesTheUnchangedPrivateAdjudication` | Both choices (relocate-and-guard, leave-unguarded): event/snapshot/receipt equal the private path; reconstruction verifies; retry rejected; decision closed after acceptance. |
| `EveryCustodyRejectionCauseSharesOneOutwardEnvelope` | 11 invalid submissions plus cross-entry-point: identical rejection. |
| `CustodyDecisionsEncodeNoSettlementPrivateFacts` | Two capture histories (seeds 5/20) with different private streams and settlement hashes: byte-equal decisions; cross-fork acceptance. Non-leak check only. |

Fixture: `rules-lab.breakdown.reaction.combat-double-fresh.v1`; seeds 5 and 20 capture with Axis
as captor (found by a one-off local probe over seeds 1–40; only those two captured).

**RBA decline-only (stopped).** The existing defender cut
`CampaignContactRetreatBeforeAssaultOwnerVisibleCutV1` always serializes a `moveOffers` array, and in
the bounded fixture that array is non-empty (Core derives one-edge moves). "Same pattern as the
other four" would publish those bytes, which then advertise move offers that have no ref. The
alternatives each need an owner call:

- (a) reuse V1 bytes unchanged and issue a ref only for the decline (outward bytes list
  unselectable moves);
- (b) add a decline-only visible codec (new outward shape; would also need the 020B draft extended);
- (c) change Core's bounded-profile RBA cut to derive no move offers under `CMB-POL-001` (changes
  existing V1 bytes and the Host's dev-route RBA opening).

Nothing for RBA is built.

## 7d. Slice 020C — bounded-profile ZOC rejection (Q2)

**Reachability check (done before changing anything).** The ruling holds for every admitted
V6 contact campaign, not just the singleton fixtures:

- A ZOC source needs more than one stacking point in a hex
  (`CampaignElementMovedV3Factory.DeriveZocAuthority`: `if (stacking <= 1) continue;`).
- Only `land.organization.battalion` has stacking authority (value 1); any other organization is
  unsupported.
- **Activation-time rejection already exists:** `ContentPackV6Validator` raises `BRK-CERT-003`
  ("Same-side co-located combat and headquarters leaves may total at most one battalion") and
  treats unsupported organizations as stacking 2, so such content is never admitted.
- `CampaignWorldV6Validator` rejects any later World with two same-side non-truck elements in one
  hex, and `DeriveZocAuthority` refuses an invalid World before deriving anything.
- Therefore the selection cut's positive-ZOC branch is unreachable for admitted content; nothing
  is silently swallowed.

**Change.** Both throw sites in `CampaignContactCombatSelectionCut` now use one named invariant,
`"Unsupported: positive opposing ZOC exceeds the bounded Combat profile."`, with a comment citing
the activation gates. It stays a defensive `InvalidOperationException`: the audience surface maps it
to "no decision", Exercise submission to `InvalidAuthority`, and the Host to "No owner opening"; no
outward boundary leaks it. No forced/surprise mechanic, no new rejection category, no Host change.

`CombatBoundedProfileZocTests`, 4 rows, local Debug + Release pass:

| Test | Evidence |
| --- | --- |
| `EveryAdmittedBreakdownSetupStartsWithoutAnyZocSource` | All 18 breakdown setups, both sides: zero ZOC sources and controlled hexes at creation. |
| `ContentAdmissionRejectsCoLocatedSameSideBattalions` | For double-fresh, double and battalion-matrix content: co-locating two same-side battalions fails `ContentPackV6Validator` with `BRK-CERT-003`, and canonical serialization refuses it. |
| `NoAdmittedWorldCanStackTwoSameSideCombatElements` | The same three stacks in a live World fail `CampaignWorldV6Validator`, and ZOC derivation refuses them. |
| `SelectionInvariantNamesTheBoundedProfile` | Pins the invariant message. |

## 7e. Q8 (non-combat-unit contact) — option (c) confirmed

**Outcome (2026-11-20).** The owner chose option (c) below. No Selection change and no new
activation rejection. `CombatNonCombatContactForceUnavailableTests` (2 rows) confirms the existing
path on the Truck fixture, where the adjacent contact is bound to `commonwealth-truck`:

- Selection offers assault + finish; offer presence does not inspect the hidden defender.
- After Barrage and RBA decline, `CanCompleteNativeCombatForceAssignment` is false and the
  defender has no Force decision.
- The System step the Host appends before opening a Force round,
  `CancelNativeCombatForceAssignment(..., -1, -1, -1, null, "force-unavailable")`, is accepted with
  no seals. Anti Armor and the cancelled-assault successor follow into same-slot Reserve Release,
  and reconstruction verifies at the cancellation and at Reserve Release.
- Recorded for Exercise/Runner adopters (022): before that System step, the attacker's own Force
  cut still projects. The Host appends the step before opening any Force round; an in-process
  driver must do the same.

**Gap found and fixed.** After any Force cancellation (V19 `force-unavailable` or timeout), Core's
private Force cut still projects and only submission rejects it, so the 020A1 audience Force stage
offered an unsubmittable seal. The Force stage now returns no decision once its round is cancelled
(repeat-assault rounds checked against their own cancellation). Core adjudication, the private cut,
events and the Host are unchanged.

**Original analysis (kept for the record).** The first ruling said to exclude contacts with no
Force slot from Selection's assault offers. Building it would have broken the fog rule the v2 spec
states for offers:

- Every contact the current Core issues is at `CampaignContactIdentification.Presence` (e.g.
  `CampaignContactAdjacencySource`). The observer knows only that something is in that hex.
- Whether the bound unit has a combat component is hidden. v2: "Contact offer presence remains
  side-visible and does not inspect the hidden defender resources."
- Dropping the offer for a truck-bound contact (Truck fixture: `commonwealth-truck`) tells the
  attacker that contact is non-combat.
- It also fits neither codec choice: filtering the V2 owner-visible bytes changes them by hidden
  state; keeping the bytes publishes an assault offer with no ref.
- Fixing it in Core offer generation instead would change V12 decision semantics that retained
  histories replay (e.g. the Truck-setup selection in `CampaignContactCombatSelectionCutTests`).

Options for the owner:

- (a) exclude only when the contact's identification already reveals the bound unit type (needs a
  detection rule that does not exist yet);
- (b) make non-combat contacts a bounded-profile activation rejection, like 020C (singleton-infantry
  content has no truck-bound reachable contacts, but several rules-lab fixtures do);
- (c) keep the offer and the existing V19 `force-unavailable` System cancellation. **(chosen)**

## 7f. 020D status

Not unblocked. The plan's hard gate for outward Task 020 work is a **frozen** `CMB-CON-005`
successor. The 020B artifacts are a draft for owner review, not frozen. Q4's ruling also requires
Host HMAC layering before anything goes public. The draft does not yet cover Custody, and RBA
decline-only (§7c) and Q8 (§7e) are unresolved.

## 8. Still pending after 020A0/020A1/020A2/020B-draft

Covered (dormant, internal): Selection (first-side initial cut), Force Assignment (both seal
orders), Reserve Release, Cycle Control. These are every round kind not blocked on an owner ruling.
Draft freeze artifacts exist for these four (§7b); freezing is the owner's action.

Waiting on owner rulings: RBA (deferred, Q7), Custody (Q5), post-assault Retreat (likely Q7-family,
unchecked), repeat V31 and second-side V47 selection (Q8 applies to Selection generally),
forced/surprise (C, Q2). Assigned elsewhere: capability manifest (021, Q3), Host HMAC and Host
consolidation (020D, Q4/Q6). Also open: owner freeze of the 020B draft, terminal-reason disclosure
pairs, Exercise/Runner adoption (022–024), and all independent review.
