# Player Intent Composer v1 Technical Design And Delivery Plan

**Status:** Proposed; independent planning review passed; implementation not authorized

**Date:** 2026-08-24

**Specification:** [Player Intent Composer v1](../specs/player-intent-composer-v1.md)

**Research:**
[Player Intent Input and Needle Feasibility](../research/player-intent-input-and-needle-feasibility.md)

## Delivery goal

Deliver a prompt-forward interaction that compresses a large combination of bounded strategic
choices without hiding interpretation or weakening Sandtable's deterministic authority path.

The stable product asset is the typed private draft and its deterministic validation—not any
particular prompt layout or model. Suggested approaches, natural language, map manipulation, lists,
and fields are interchangeable producers. Needle is a late, optional adapter evaluated after the
no-model interaction and real semantic contracts prove useful.

## End-to-end control flow

```text
authenticated player + current campaign authority
                         |
                         v
          side-safe observation + current legal inputs
                         |
                         v
          deterministic IntentOpportunity projection
             |                           |
             v                           v
   suggested approaches            parse context
             |                           |
             |                 optional local parser
             |                           |
             +-----------+---------------+
                         |
                         v
              private typed IntentDraft
              ^          |             ^
              |          |             |
         map/list     direct fields   free text
                         |
                         v
             deterministic client validation
                         |
              0..2 clarification prompts
                         |
                         v
              visible complete draft
                         |
                 explicit confirmation
                         |
                         v
              trusted server revalidation
                         |
                         v
             deterministic Staff planning
                         |
                 plan review/refinement
                         |
                 explicit final submit
                         |
                         v
          current exact-audience legal membership
                         |
                         v
                       Umpire
```

There is no parser-to-Staff, parser-to-legal-action, or parser-to-Umpire edge.

## Component ownership

| Component | Owns | Must not own |
| --- | --- | --- |
| Maproom web client | Private drafts, input synchronization, starter rendering, local parser adapter, client validation feedback, clarification presentation, accessibility, final review | Side authorization, hidden legality, Staff rules, authority handles, adjudication |
| Maproom BFF/application boundary | Authenticated player-to-side derivation, opportunity delivery, server validation, cancellation/deadlines, Staff orchestration, stale responses | Model inference inside authoritative turns, Core rule duplication, trusting browser side/state |
| Intent opportunity projector | Side-safe semantic schema, allowed presentation values, starter templates, clarification metadata | Legal action invention, hidden state, provider prompts from arbitrary content |
| Player-intent contracts/validator | Closed payloads, provenance, required/optional fields, contradictions, deterministic clarification ordering | Rules adjudication or model-specific types |
| Deterministic Staff module | Confirmed-intent-to-plan expansion, intent-fit/tradeoff explanations, current legal planning inputs | Remote/model I/O, final authority, hidden opponent truth beyond explicitly authorized Staff view |
| Optional parser adapter | Short utterance-to-proposed-fields extraction, refusal/failure, private diagnostics | Strategy, defaults, validation authority, planning, execution, submission |
| Cna.Core Umpire | Legal action derivation/revalidation, rules, RNG, events, authoritative transition | Drafts, prompt interaction, parser/provider state |
| Chronicle/Archives | Accepted authoritative history and eventual persistence | Abandoned drafts, raw utterances, parser reasoning by default |

## Interaction state machine

```text
Empty
  -> Drafting
       -> NeedsClarification
       -> ReadyToConfirm
       -> Stale
  -> ConfirmingIntent
       -> ReadyToConfirm      (server validation returned correctable issues)
       -> Planning            (accepted non-authoritative confirmed intent)
       -> Stale
  -> PlanSelection
       -> Drafting            (revise Command intent)
       -> PlanEditing
       -> FinalReview
       -> Stale
  -> Submitting
       -> Accepted
       -> FinalReview         (correctable rejection)
       -> Stale
```

`NeedsClarification` is ordinary draft state, not a separate chatbot transcript. A follow-up answer
updates fields in the same draft. At most two questions are automatically promoted per parse
attempt; direct editing remains available at every point.

## Proposed contracts

The sketches below define responsibility and information flow. Names and serialization are not
approved production APIs.

### Intent opportunity

```text
IntentOpportunity
  contractVersion
  campaignId
  stateVersion
  positionId
  audienceBinding        # delivered from trusted session; not a browser assertion
  opportunityId
  schemaId
  interactionScale       # closed-choice | strategic | spatial | plan-board
  fields[]
    fieldId
    valueType             # closed enum/reference/bounded scalar/region/policy
    required
    allowNoPreference
    presentationLabel
    helpText
    allowedValues[]       # presentation-safe IDs and labels
    clarificationPriority
    clarificationTemplateId?
  suggestedApproaches[]
    approachId
    label
    description
    seedValues[]
    seedText?
  parsePolicy
    enabledForSchema
    maxUtteranceBytes
    maxAutomaticQuestions = 2
```

The opportunity is a presentation contract. It does not prove an action is legal, and its suggested
approaches do not map one-to-one to Core action IDs.

### Private draft

```text
IntentDraft
  localDraftVersion
  draftId
  opportunityBinding
  payload                 # one closed schema-specific variant
  fieldStates[]
    fieldId
    semanticValue?
    provenance            # manual | starter | parsed-local | parsed-remote | rules-default
    evidenceSpan?
    validationState
  sourceUtterance?        # private and locally retained by default
  unresolvedText[]
  clarificationCount
  lastParserTrace?        # private provider/version/latency/confidence, no reasoning required
  status
```

Campaign/state/position/audience/schema binding originates from the opportunity. Parser output can
only propose payload fields and evidence; the client cannot use parser-supplied binding values.

### Confirmed intent request

```text
ConfirmedIntentRequest
  contractVersion
  clientRequestId
  campaignId
  expectedStateVersion
  expectedPositionId
  opportunityId
  schemaId
  payload
```

The trusted host derives audience from the authenticated campaign seat. The request omits raw
utterance, parser trace, parser confidence, and client-provided audience. Server validation is
idempotent by `clientRequestId` and returns either field-level rejection/stale status or an accepted
non-authoritative planning handle bound to the same state.

### Staff plan set

```text
StaffPlanSet
  contractVersion
  campaignId
  stateVersion
  positionId
  audience
  confirmedIntentId
  plans[]
    planId
    title
    intentFit[]
    tradeoffs[]
    sideSafePreview
    legalActionBinding   # exact future shape depends on Legal Actions evolution
```

Plan IDs are opaque state-bound identities. They are not model tool names or long-lived capability
tokens. Core still re-derives current legal membership at final submission.

## Suggested-approach projection

### Source and safety

Suggested approaches must come from reviewed structured presentation data and deterministic policy.
Version 1 should test two sources behind one closed precedence rule:

1. scenario-authored presentation templates eligible for the current intent schema; and
2. deterministic Staff policy that filters/templates those approaches from the current side-safe
   opportunity.

Arbitrary scenario prose, reports, generated narrative, model output, and hidden opponent state may
not become executable starter definitions or parser instructions.

The projector:

1. validates the authenticated audience and admitted current authority;
2. obtains the exact side-safe observation and current legal planning inputs;
3. chooses one supported intent schema for the decision;
4. derives allowed values and eligible starters;
5. rejects starters containing unavailable or non-side-safe references;
6. canonically orders them by authored priority then stable ID; and
7. emits immutable presentation data bound to the current state.

### UX rules

- Show three or four starters at most before an **Other approach** affordance.
- Use outcome-oriented labels such as **Hold Tobruk**, not low-level command names.
- Explain the starter's seed in plain language before selection where ambiguity exists.
- Never style starters as mandatory or exhaustive.
- Preserve player edits when switching starter only after warning about fields that will change.
- Record field provenance so seeded choices remain distinguishable from manual ones.

## Parser boundary

### Request construction

The client constructs a parser request from:

- the player's current short utterance;
- exactly one `schemaId`;
- allowed field/value IDs plus short presentation aliases; and
- a versioned instruction owned by application code.

It excludes reports, narrative, help text, complete observation, Content Pack, Staff plans,
Chronicle, hidden state, authority handles, action hashes, cookies/tokens, and untrusted scenario
text. Request construction enforces byte/token budgets before invocation.

### Response handling

The adapter returns data but never executes a selected tool/function:

```text
ParseResult = ProposedFields | Refused | Unavailable | InvalidOutput | TimedOut
```

`ProposedFields` is grammar/schema checked and then passed through the ordinary deterministic draft
validator. Model reasoning is ignored. Confidence, if present, can be retained as private evaluation
telemetry but cannot accept a field or suppress confirmation.

### Needle disposition

Needle's 45M model, constrained JSON, local targets, and small footprint justify a spike for one
short extraction schema. Its 256-token window, base-only confidence, and unproven domain grounding
prohibit making it a design dependency. The adapter must also support a deterministic alias parser,
fake parser, and no parser so comparative testing does not hard-code provider behavior.

## Deterministic validation and clarification

Validation is schema-specific and produces stable ordered issues:

1. contract/binding/version failure;
2. unknown field or value outside current allowlist;
3. contradictory fields;
4. missing required field;
5. material ambiguity or unresolved evidence;
6. optional incomplete preference; and
7. advisory interaction help.

Each material issue can reference a schema-authored clarification template with:

- stable question ID and wording key;
- affected field(s);
- bounded answer IDs and display labels;
- optional manual/free-text route;
- priority; and
- whether the issue blocks intent confirmation.

The UI asks the first unresolved blocking/material questions, never more than two automatically.
Answering a bounded choice updates the field directly. Free text runs through the same parser if
available, otherwise opens the relevant structured field. Contradictions are never resolved by a
silent “most recent wins” rule unless the player explicitly replaces the prior value.

Client validation provides immediate feedback. Server validation re-derives the current opportunity
and validates the submitted payload; it never trusts the client's issue list, completeness flag,
starter identity, parser provenance, or confidence.

## Staff planning boundary

Confirmed intent is a request for planning, not an authoritative command. Deterministic Staff:

- accepts only a server-validated current intent;
- reads the authorized side-safe planning view and current legal planning inputs;
- expands Command goals into zero or more closed operational plans;
- explains which confirmed constraints each plan satisfies or compromises;
- never invents unavailable entities or action IDs;
- returns explicit no-plan/conflict results when the intent cannot be realized; and
- performs no model, network, database, clock, or unseeded random I/O inside pure plan generation.

The future legal-action contract must define whether a Staff plan binds one action, an atomic batch,
or a versioned multi-step plan. This design does not route around that unresolved authority work.
The first prototype therefore uses synthetic plan fixtures and fake legal bindings.

## Final review and submission

The final review is visually and semantically distinct from intent confirmation. It shows:

- the exact selected plan/order;
- participating own formations and visible areas/routes;
- disclosed costs, constraints, contingencies, and unresolved warnings;
- the state/version to which it is bound;
- whether the order changed since the last review; and
- **Return to draft** and **Confirm and issue order** controls.

Submission reaches the existing/future Legal Actions boundary with only trusted session audience and
current opaque legal identity. Core re-derives membership and applies one accepted transition or a
typed zero-event rejection. Chronicle records the accepted command/events, not the composition
history.

## Stale state and idempotency

- Any opportunity-binding change marks the draft stale before confirmation.
- An accepted confirmed intent and each Staff plan retain exact campaign/state/position/audience.
- Host requests carry idempotent client request IDs and explicit deadlines.
- Duplicate confirmation/planning requests return the same safe result or a stable conflict.
- A stale response cannot overwrite a newer local draft.
- Refetch preserves locally authored text/fields only as an unbound copy; the player must review
  differences and explicitly rebind through current opportunity validation.
- Final submission follows Legal Actions v1 rejection precedence and never retries with a newly
  inferred action.

## Privacy, security, and fog controls

| Threat | Control |
| --- | --- |
| Hidden-state leakage to parser | Construct parse context only from the presentation-safe intent opportunity; paired opponent-variation tests protect bytes/semantics. |
| Prompt injection from reports/content | Do not concatenate reports, narrative, arbitrary scenario prose, or help content into parser instructions. |
| Parser invents value/action | Grammar constraints where available plus deterministic current-allowlist validation; parser never receives or returns legal action IDs. |
| Browser forges side/state | Host derives seat/audience and re-derives opportunity/current state; client binding is concurrency input, not authority. |
| Raw strategic intent leaks through telemetry | No raw utterance/draft in logs; opt-in research store is separate, minimized, access-controlled, and deletable. |
| Silent remote processing | Provider class and locality shown; remote parsing disabled by default and separately consented/approved. |
| Local model supply-chain risk | Pin model/runtime digest, deliberate provisioning, CSP/caching review, offline network test, and corpus gate before upgrade. |
| Cross-player hot-seat exposure | Define a mandatory seat-transition epoch that aborts/invalidates in-flight work and clears or isolates all outgoing private composition state before the next seat can interact. Do not assume a privacy curtain already exists. |

### Local hot-seat isolation boundary

Local hot-seat is the first playable mode, so seat transition is a security/privacy boundary rather
than presentation polish. Before the incoming player can see or operate Maproom, the transition must:

1. increment a trusted seat/session epoch and revoke the outgoing audience binding;
2. abort parser workers and pending parse, confirmation, Staff-planning, and submission requests
   where cancellation is available;
3. make every late response carry/match the old epoch so reducers discard it without rendering;
4. clear or cryptographically isolate source utterances, unresolved text, typed drafts, field
   provenance, parser traces/sessions, plan previews, diagnostics containing content, and undo/back
   history;
5. clear relevant DOM and assistive-technology live-region text plus in-memory stores, session/local
   storage, IndexedDB, Cache Storage entries, and service-worker messages created by the composer;
   the composer never writes strategic content to the system clipboard without explicit player
   action; and
6. render a neutral privacy transition screen until the incoming seat is authenticated and its
   authorized observation/opportunity has loaded.

The implementation must inventory the actual storage and worker surfaces introduced by Maproom and
the selected parser. Tests seed each surface, delay responses across the transition, exercise back
navigation, and prove that the incoming seat cannot recover the outgoing player's strategic data.
If persistent per-seat drafts are later approved, isolation keys and access control replace clearing
only after a separate storage/privacy design passes review.

## Accessibility and onboarding design

### First-use lesson

The first representative turn should teach by completing a real bounded decision:

1. Highlight one starter and state that suggestions are not the only choices.
2. Seed a draft and show which fields changed.
3. Invite one plain-language addition.
4. Show the interpretation beside the player's words.
5. Ask one bounded clarification and explain why it matters.
6. Require correction/review before Staff planning.
7. Distinguish intent confirmation from final order submission.

Contextual examples are generated from the current schema, not a global prompt cookbook. Advanced
players can hide examples without losing fields or validation.

### Equivalent interaction

- Starters are semantic buttons with descriptions and predictable focus.
- Every map edit has a synchronized list/table/form alternative.
- Parser status and changed fields use text and programmatic announcements, not color alone.
- Clarification moves focus to its heading and returns focus to the changed field/draft summary.
- Errors identify the field, problem, and known allowed correction.
- The final confirmation cannot be triggered by pressing Enter inside the prompt.
- Reduced motion disables animated field-diff/map transitions without hiding changes.

## Observability without strategic-content logging

Allowed operational telemetry:

- feature/schema/provider/runtime version;
- parser locality, availability, refusal/error category, latency, and request/response byte counts;
- field count and aggregate correction count without values;
- clarification count and stable question IDs;
- stale/rejection reason codes;
- time to confirmed intent/final order; and
- manual-only versus parser-assisted mode when consent/policy permits.

Disallowed by default:

- raw parser request/response bodies, utterance text, unresolved text, field values, draft payload,
  model reasoning, map selections, complete observation, plan body, or hidden action semantics.

Raw request/response bodies belong only in a separately authorized, explicitly consented evaluation
corpus with purpose limitation, access control, minimization, and deletion—not operational telemetry.

## Placement in the pre-alpha roadmap

Player Intent Composer is a cross-cutting future interaction track, not a new current sprint. Its
implementation gates attach to proven gameplay and Maproom milestones:

| Roadmap point | Intent phase | Authorized outcome | Must not block |
| --- | --- | --- | --- |
| Current work through Sprint 3 | Retained design only | Keep the reviewed direction synchronized while Serial Maneuvers, Organization/stage entry, and Reserve advance | Current developer instrumentation or authoritative preamble work |
| After Sprint 5 combat skeleton | Phase 0 | Select one real multi-field decision and approve its Command/Staff/Umpire field classification, starter policy, and plan-binding dependency | Sprint 5 completion or replay checkpoint |
| Between Sprint 5 and Sprint 8 | Phase 1 | Isolated no-model rules-lab composer and hot-seat privacy prototype against synthetic Staff plans | Scenario ingestion and remaining Land-rule work |
| Sprint 8 Minimal Maproom | Phases 2-3 | Closed contracts, validator, trusted host/Staff seam, and one deterministic end-to-end composer slice, only after real legal-plan binding is approved | Other deterministic Maproom decisions when the Staff-plan dependency is unresolved |
| After deterministic six-turn MVP | Phases 4-6 | Corpus/baseline, `INTENT-PARSER-EVAL-001`, optional Needle adoption, and production hardening | MVP completion, campaign availability, or deterministic composer use |

The first playable MVP requires the reviewed no-model interaction and hot-seat isolation where the
representative slice is present. It does not require Needle, another parser, remote inference, or a
model asset. A failed parser evaluation removes only parser integration from the release plan.

## Delivery phases and gates

The phases are sequential at their authority boundaries. UX fixtures, accessibility harness work,
and corpus design can proceed in parallel where they do not invent unresolved rules contracts.

### Phase 0 — Requirements and representative decision

- Begin only after the Sprint 5 movement/contact/combat skeleton and replay checkpoint expose a real
  action vocabulary.
- Select one real future multi-field Command decision and define the Command-versus-Staff semantic
  cut.
- Resolve starter source/precedence and optional/default-field policy.
- Update roadmap authorization and exact project ownership before implementation.

**Gate:** owner-approved spec/design with no unresolved question that changes the first contract.

### Phase 1 — No-model rules-lab interaction prototype

- Run as a bounded cross-cutting product-validation lane between Sprint 5 and Sprint 8; do not turn
  it into production Maproom or block scenario/rules delivery.
- Build an isolated low-fidelity composer with synthetic side-safe opportunity and Staff plan
  fixtures.
- Implement starters, custom text retained as unresolved, direct fields, map/list synchronization,
  two-question cap, stale state, intent/final confirmation separation, and parser-unavailable mode.
- Prototype the complete hot-seat transition boundary, including worker/request cancellation,
  seat/session epoch rejection, storage/DOM/live-region clearing, and neutral privacy screen.
- Run formative keyboard, screen-reader, desktop/tablet, and new-player walkthroughs.

**Gate:** every task completes without a model; users understand suggestions are non-exhaustive and
can accurately restate the interpreted order; correction burden is acceptable; the next hot-seat
player cannot recover any seeded outgoing-seat composition content or receive a late response.

### Phase 2 — Closed contracts and pure validator

- Begin for production only when Sprint 8 ownership exists and Phase 0/1 gates pass.
- Define experimental opportunity, payload variants, confirmed-intent, clarification, and Staff-plan
  contracts outside Core authority.
- Implement shared conformance fixtures and pure client/server validation.
- Add paired fog, culture/order, contradiction, state-binding, and provenance tests.

**Gate:** one real intent schema stays at Command level, fits the parser context budget, and carries
no hidden state, generic property bag, executable command, or opaque action identity.

### Phase 3 — Deterministic Staff and trusted host seam

- Integrate during Sprint 8 only after the future plan/batch Legal Actions binding is approved; an
  unresolved binding defers this slice without blocking other deterministic Maproom work.
- Implement authenticated opportunity delivery and confirmed-intent validation.
- Implement deterministic Staff plan generation against synthetic then real current legal planning
  inputs.
- Define cancellation, deadline, idempotency, stale response, and zero-plan behavior.
- Complete deterministic onboarding and accessibility for the representative composer slice.
- Integrate final review only after the Legal Actions plan/batch shape is approved.

**Gate:** end-to-end no-model flow reaches current legal revalidation; replay/authority bytes are
independent of private draft and parser provenance.

### Phase 4 — Parser baseline and corpus

- Begin after the deterministic MVP composer path works; it does not gate MVP completion.
- Add fake and deterministic alias parsers behind the adapter.
- Build several hundred consent-safe reviewed evaluation utterances across explicit, ambiguous,
  contradictory, off-topic, typo, speech-like, and adversarial cases.
- Establish manual-only and deterministic-parser interaction baselines.

**Gate:** corpus coverage and scoring are reviewable; product value is not dependent on a model.

### Phase 5 — Needle feasibility spike

- Execute `INTENT-PARSER-EVAL-001` after Phase 4; no production dependency or model asset is added by
  the spike.
- Pin exact runtime/model/schema versions without integrating them into production.
- Run base Needle one-turn extraction from files/CLI; never execute selected functions.
- Compare semantic accuracy, abstention, correction burden, latency, memory, and offline behavior.
- Test browser WebAssembly only if semantic quality passes; test localhost only as a desktop/local
  alternative.

**Gate:** all parser-adoption thresholds in the specification pass. Otherwise close the spike and
retain the hybrid composer without Needle.

### Phase 6 — Product integration and release controls

- Begin only after the post-MVP parser evidence gate selects a provider/deployment target.
- Integrate the selected parser only behind lazy optional provisioning and a kill switch.
- Complete parser-specific disclosure/accessibility, privacy, CSP/cache/supply-chain, upgrade corpus,
  and support documentation.
- Roll out to rules-lab/internal play before any user campaign.

**Gate:** full repository gate, interaction/accessibility acceptance, privacy review, no-model chaos
test, version pinning, rollback drill, and independent implementation review pass.

## Delivery task breakdown and refinement obligations

Paths are provisional until Phase 0 authorizes projects. These IDs are delivery work items, not
permission to implement an umbrella item directly. Before execution, every child task must be
refined to at most five material files with its own acceptance criteria and evidence command.

Phase 0 must also add a compact requirement-group → task/checkpoint → evidence matrix covering all
`INT-*`, `INT-NFR-*`, and `INT-AC-*` obligations. In particular, the following broad work items must
be split before their phase is authorized:

| Parent work item | Required bounded child slices |
| --- | --- |
| `INTENT-PLAN-002` | draft/input synchronization; clarification plus stale/dual-confirmation state; hot-seat epoch, cleanup, and late-response isolation |
| `INTENT-PLAN-008` | deterministic first-use/onboarding; keyboard, non-drag, reflow, screen-reader, and assistive-technology hardening |

- [ ] `INTENT-PLAN-001` — Select representative decision and freeze semantic ownership.
  - Acceptance: every field is classified as Command intent, Staff planning choice, or Umpire rule;
    starter/default policy and first client form factors are resolved; the requirement-group →
    task/checkpoint → evidence matrix is complete.
  - Verify: owner review plus traceability to roadmap/rules sources.
  - Files: this spec/design, roadmap, representative mechanic spec/design.
  - Dependencies: completed Sprint 5 movement/contact/combat skeleton and replay checkpoint.
  - Estimated scope: small, documentation/decision only.
- [ ] `INTENT-PLAN-002` — Prototype the no-model composer against fixtures.
  - Acceptance: starter/text/map/form inputs synchronize one draft; two-question cap, clearing,
    stale state, two confirmations, and the complete hot-seat transition boundary work without a
    parser; prior-seat data and late responses never reach the incoming seat.
  - Verify: component tests, storage/worker/late-response/back-navigation hot-seat tests,
    keyboard/manual screen-reader walkthrough, and recorded demo scenarios.
  - Files: future Maproom prototype component, draft reducer/state, fixture, tests.
  - Dependencies: `INTENT-PLAN-001`; may proceed alongside Sprints 6-7 without blocking them.
  - Estimated scope: parent work item; split into the three required bounded slices above before
    Phase 1 authorization.

### Checkpoint A — Interaction value

- The no-model path completes every representative task.
- Players understand starters are non-exhaustive and can accurately restate the typed draft.
- Hot-seat isolation and accessibility walkthroughs pass before production contract work.

- [ ] `INTENT-PLAN-003` — Define experimental closed intent contracts.
  - Acceptance: opportunity, two or three payload variants, confirmed intent, validation issues,
    and Staff plan set are versioned and contain no forbidden authority/provider data.
  - Verify: public-surface/type-graph, serialization, malformed/old-version, and fog-pair tests.
  - Files: future contract module plus focused tests/fixtures.
  - Dependencies: `INTENT-PLAN-001`, Checkpoint A, and authorized Sprint 8 project ownership.
  - Estimated scope: medium after payload variants are split into focused slices.
- [ ] `INTENT-PLAN-004` — Implement deterministic validation and clarification ordering.
  - Acceptance: client/server fixtures agree; all precedence and cap scenarios pass; no parser value
    bypasses allowlist/evidence/contradiction checks.
  - Verify: pure table-driven tests and mutation/failure-sensitivity review.
  - Files: validator, schema definitions, fixtures, client/server tests.
  - Dependencies: `INTENT-PLAN-003`.
  - Estimated scope: medium, at most five material files.

### Checkpoint B — Contract and fog boundary

- Client/server fixtures agree on contract and validation semantics.
- Paired opponent-only changes do not affect exact side-safe opportunity/parse context.
- No generic property bag, provider type, hidden state, command, or action ID crosses the boundary.

- [ ] `INTENT-PLAN-005` — Add trusted opportunity/confirmation host seam.
  - Acceptance: side comes from authenticated session; state/idempotency/deadline/cancellation
    behavior is explicit; raw text is absent from server request/logs.
  - Verify: in-process host integration tests including forged side and stale responses.
  - Files: future BFF endpoint/application handler, DTO adapter, integration tests.
  - Dependencies: `INTENT-PLAN-003`, `INTENT-PLAN-004`, authenticated Maproom host ownership.
  - Estimated scope: medium, one vertical host slice.
- [ ] `INTENT-PLAN-006` — Implement deterministic Staff plan expansion.
  - Acceptance: same admitted input yields the same plans; impossible intent returns no-plan/conflict;
    no remote/model/clock/unseeded random I/O exists.
  - Verify: deterministic fixtures, fog pairs, plan-to-current-legal binding tests.
  - Files: future Staff module, plan contracts/adapters, focused tests.
  - Dependencies: `INTENT-PLAN-003`, `INTENT-PLAN-004`, and an approved future
    single/batch/multi-step Legal Actions binding.
  - Estimated scope: medium per representative plan variant.
- [ ] `INTENT-PLAN-007` — Integrate exact final review and legal submission.
  - Acceptance: intent confirmation and final submission remain distinct; stale/wrong-audience/legal
    rejection produces zero events; accepted path uses current Core membership.
  - Verify: end-to-end in-process tests and replay/authority-digest comparison.
  - Files: Maproom review component, BFF submission adapter, integration tests.
  - Dependencies: `INTENT-PLAN-005`, `INTENT-PLAN-006`, and current Core legal membership.
  - Estimated scope: medium, one end-to-end representative slice.
- [ ] `INTENT-PLAN-008` — Harden deterministic onboarding and accessibility.
  - Acceptance: the first-use lesson and contextual help pass formative review; semantic/no-model
    paths pass keyboard, non-drag, reflow, and assistive-technology gates.
  - Verify: focused component/e2e/accessibility tests and a manual assistive-technology report.
  - Files: focused Maproom onboarding/accessibility components plus tests/docs.
  - Dependencies: `INTENT-PLAN-002`, `INTENT-PLAN-007`, and Sprint 8 Maproom ownership.
  - Estimated scope: parent work item; split onboarding from accessibility hardening before Sprint 8
    authorization.

### Checkpoint C — Deterministic Maproom slice

- Manual/no-model intent entry reaches exact current legal revalidation end-to-end.
- Intent confirmation and final submission remain distinct and stale-safe.
- First-use guidance and semantic/no-model interaction pass the accessibility gates.
- Private composition/parser provenance does not affect replay or authority bytes.

- [ ] `INTENT-PLAN-009` — Build parser adapter and retained corpus harness.
  - Acceptance: fake, unavailable, deterministic alias, and external local parser share one data-only
    interface; scoring never executes functions and records exact versions.
  - Verify: corpus runner tests, offline/network observation, privacy/log inspection.
  - Files: parser adapter, corpus schema/runner, fixtures, tests.
  - Dependencies: `INTENT-PLAN-007`, `INTENT-PLAN-008`, and the deterministic six-turn MVP
    composer path.
  - Estimated scope: medium, evaluation-only and outside production activation.
- [ ] `INTENT-PLAN-010` — Evaluate Needle without product coupling.
  - Acceptance: evidence report covers every semantic/runtime/privacy gate and explicitly adopts or
    rejects each target deployment.
  - Verify: pinned reproducible commands and held-out corpus results reviewed independently.
  - Files: research report, experimental configuration/scripts outside production paths.
  - Dependencies: `INTENT-PLAN-009` and explicit authorization for `INTENT-PARSER-EVAL-001`.
  - Estimated scope: small research/evidence slice; no product dependency changes.

### Checkpoint D — Parser decision

- The retained corpus, manual/deterministic baselines, and exact versions are reviewable.
- Every semantic, refusal, correction, privacy, accessibility, and runtime threshold is reported.
- The decision explicitly adopts, narrows, or rejects each deployment target.

- [ ] `INTENT-PLAN-011` — Add optional parser rollout and rollback controls.
  - Acceptance: the selected parser is lazily provisioned behind a kill switch; unavailability and
    rollback preserve the complete deterministic composer; cache/model versions are pinned.
  - Verify: no-model chaos test, privacy/CSP/cache review, upgrade corpus, and rollback drill.
  - Files: parser activation/configuration, deployment/cache policy, focused tests/docs.
  - Dependencies: an adopt/narrow result from `INTENT-PLAN-010`; omit this task after rejection.
  - Estimated scope: medium after target-specific refinement.

### Checkpoint E — Optional parser release

- Deterministic Maproom remains complete with the parser disabled or absent.
- Accessibility/privacy/supply-chain checks and rollback drill pass.
- Full repository gate and independent implementation review pass before user-campaign rollout.

## Verification checkpoints

At each authorized implementation phase run the narrow tests first, then the repository gate:

```bash
dotnet restore Sandtable.slnx
dotnet format Sandtable.slnx --verify-no-changes --no-restore
dotnet build Sandtable.slnx --no-restore
dotnet test --solution Sandtable.slnx --no-build
git diff --check
```

Add exact Maproom build/test/accessibility commands when its scaffold exists. Provider/corpus
experiments must record version/digest, command, target hardware/browser, dataset split, and raw
aggregate results without committing private utterances.

## Rollout and rollback

1. Ship the composer and deterministic controls before any parser.
2. Keep parser activation schema- and environment-specific behind configuration.
3. Provision model/runtime deliberately and lazily; a missing asset renders no broken control.
4. Roll out internally with aggregate privacy-safe correction/refusal telemetry.
5. Disable the parser independently of the draft/Staff/legal path on regression.
6. Pin prior known-good runtime/model/schema and rerun the corpus before rollback or upgrade.
7. Never roll back authoritative campaign contracts to compensate for parser behavior.

## Key risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Prompt hides the actual option space | Contextual starters, visible fields/allowed values, examples, and a structured/manual path. |
| Starter cards become a de facto strategy recommendation | Label as suggestions; deterministic reviewed source; non-exhaustive custom path; explain seeded fields. |
| Clarification becomes tedious chat | Deterministic priority, two-question cap, bounded answers, direct field editing. |
| Typed contract ossifies before gameplay exists | Select a real representative decision first; prototype with experimental versioned schemas; gate governing contract. |
| Staff/parser ownership blurs | Parser extracts explicit text only; validator handles completeness; Staff constructs plans; Core decides legality. |
| Client/server contract duplication drifts | Shared canonical fixtures/vectors and independent server validation; generated transport only after contract approval. |
| Local model is too weak or costly | Provider-independent adapter, retained baselines/corpus, strict adoption gates, no-model product completeness. |
| Draft leaks strategy or hot-seat data | Local-by-default draft, no content logging, explicit seat/session epoch, complete storage/worker/DOM clearing or reviewed isolation, and opt-in retention only. |
| Plan shape conflicts with future Legal Actions evolution | Defer real Staff integration until atomic/batch/multi-step authority contract is approved. |

## Decisions deliberately deferred

- final C#/TypeScript project and transport filenames;
- whether any player-intent values use protobuf;
- exact authoritative binding for multi-order Staff plans;
- Needle browser versus localhost deployment;
- model asset distribution and licensing notices;
- remote parser support;
- voice input and non-English parser scope; and
- persistent/cross-device drafts.

Deferral preserves the approved interaction direction without committing architecture ahead of
Maproom, Staff, movement/combat, and legal-plan predecessors.

## Review challenge points

An independent reviewer should apply extra skepticism to:

- whether the proposed opportunity/confirmed-intent/Staff-plan layering is necessary and correctly
  separated from Legal Actions;
- whether deterministic Staff can plan from only authorized side-safe inputs;
- whether starters can be scenario-aware without leaking or becoming hidden recommendations;
- whether two clarifications is a sound interaction cap and properly separated from completeness;
- whether client-local draft/privacy assumptions fit hosted asynchronous play and hot-seat;
- whether the task ordering waits for the right roadmap predecessors; and
- whether any wording accidentally treats Needle performance or deployment as established fact.
