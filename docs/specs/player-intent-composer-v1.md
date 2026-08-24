# Player Intent Composer v1 Specification

**Status:** Proposed; product direction approved; independent planning review passed; implementation not authorized

**Date:** 2026-08-24

**Proposed capability:** `INTENT-001`

**Research:**
[Player Intent Input and Needle Feasibility](../research/player-intent-input-and-needle-feasibility.md)

**Technical design:** [Player Intent Composer v1](../design/player-intent-composer-v1.md)

**Predecessors:** [Campaign Observation v1](campaign-observation-v1.md),
[Legal Actions v1](legal-actions-v1.md)

**Roadmap placement:** select the representative decision after the Sprint 5 combat skeleton;
validate the no-model interaction before Sprint 8; integrate one deterministic slice with Minimal
Maproom; evaluate a parser only after the deterministic six-turn MVP.

## Objective

Give a human commander a compact way to express a bounded but combinatorial decision without
turning Maproom into either a wall of controls or an opaque chatbot.

For a complex strategic decision, Maproom presents a few contextual **suggested approaches**, a
natural-language entry field, and direct map/form editing. Every input mode edits one visible,
private, typed intent draft. The application may ask at most two automatically surfaced,
high-value clarification questions. The player must inspect and confirm the interpretation before
deterministic Staff logic can propose operational plans, and must separately confirm a current
server-issued legal action before the Umpire changes authoritative state.

The optional Needle model is only a replaceable parser that populates the draft from short explicit
language. The complete experience works without Needle or any other model.

### Intended player

- A new player who benefits from recognizable strategic starting points and guided clarification.
- An experienced player who can state a complete intent quickly without opening many panels.
- A keyboard, screen-reader, touch, or pointer user who needs equivalent semantic controls.

### Observable success

- A player can understand what the game permits without learning a prompt dialect.
- A multi-field strategic intent can be entered faster than through a full control matrix.
- The interpreted result is visible, correctable, and never confused with an issued order.
- Model absence, refusal, or error cannot prevent play or weaken authority and fog boundaries.

## User-visible demonstration

At a decision to defend Tobruk, Maproom presents:

```text
WHAT DO YOU WANT TO ACCOMPLISH?

Suggested approaches
[Hold Tobruk] [Attack the eastern force]
[Withdraw and regroup] [Conserve forces]

Or describe another approach
[                                                       ]
```

The labels say **Suggested approaches**, not **Available moves**. They are useful starting points,
not an exhaustive legal-action list.

The player selects **Hold Tobruk**. Maproom seeds an editable statement:

> Hold Tobruk and organize a defensive posture.

The player extends it:

> Hold Tobruk and organize a defensive posture. Keep the armored brigade available for a
> counterattack and conserve water.

The parser, when available, proposes fields. Deterministic validation finds one material omission
and Maproom asks:

```text
When may the defending force withdraw?

[If Tobruk becomes untenable]
[When major armor losses become likely]
[Never without another order]
[Describe another condition]
```

After the answer, Maproom shows the complete private draft:

```text
Objective:             Hold Tobruk
Posture:               Defensive
Armor role:            Mobile reserve
Supply priority:       Conserve water
Counterattack area:    Tobruk defensive area
Withdrawal condition: If Tobruk becomes untenable

[Edit on map] [Clear interpretation] [Confirm intent and ask Staff]
```

Staff then returns deterministic, state-bound plan alternatives. The player chooses or refines one,
reviews the exact order, and explicitly submits it. Submission revalidates current exact-audience
legal-action membership. Only the Umpire commits the resulting authoritative event.

## Approved product decisions

1. **Hybrid, prompt-forward interaction.** Language may lead strategic decisions, but language,
   starters, map gestures, lists, and structured fields edit the same typed draft.
2. **Decision-scale routing.** Small closed decisions use direct controls. Spatially precise work
   uses map/list interaction. Language is most useful for coherent multi-field Command intent.
3. **Contextual starters.** Scenario/state-aware suggested approaches reduce blank-prompt anxiety
   but never claim to enumerate every legal or sensible choice.
4. **Bounded clarification.** Maproom automatically asks at most two material questions. Additional
   omissions appear as editable fields rather than an unbounded chat loop.
5. **Visible interpretation.** No parse result bypasses the private draft, field-level validation,
   player correction, and explicit confirmation.
6. **Deterministic authority path.** Staff constructs plans; the current legal-action boundary
   establishes legality; the Umpire alone adjudicates.
7. **Optional parsing.** Needle is evaluated only after real intent schemas and a retained corpus
   exist. No-model play remains complete.

## Terminology

| Term | Meaning |
| --- | --- |
| Intent opportunity | A side-safe, state-bound description of the current decision's semantic fields, allowed presentation values, starters, and clarification policy. It is not a legal-action set. |
| Suggested approach | A deterministic scenario/state-aware template that seeds a draft. It is non-authoritative and non-exhaustive. |
| Parse context | The smallest presentation-safe subset of an intent opportunity provided to an optional parser. |
| Intent draft | Mutable private client/platform state containing one closed typed payload plus field provenance and unresolved text. |
| Clarification need | A deterministic validator result identifying a material missing, ambiguous, or contradictory field and its bounded answers. |
| Confirmed intent | A complete state-bound semantic payload explicitly confirmed by the player and accepted by server-side validation. It is still not an authoritative game command. |
| Staff plan | A deterministic, side-safe operational proposal derived from a confirmed intent and current legal planning inputs. |
| Issued order | A current exact-audience legal action explicitly submitted and accepted by the Umpire boundary. |

## Scope

### In scope for the capability

- decision-specific intent opportunities and contextual suggested approaches;
- natural-language, starter, map, list, and form producers of one typed draft;
- field provenance, unresolved text, deterministic validation, and bounded clarification;
- explicit intent confirmation and deterministic Staff plan generation;
- exact final-order review and current legal-action submission;
- no-model behavior, privacy, stale-state handling, accessibility, onboarding, and evaluation;
- an optional local parser adapter and a separately gated Needle experiment; and
- versioned fixtures and a retained utterance corpus.

### Explicitly out of scope for version 1 planning

- implementing movement, combat, supply, Staff, Maproom, or multi-action legal plans before their
  roadmap predecessors;
- replacing Campaign Observation or Legal Actions v1;
- allowing a parser to recommend strategy, infer hidden preferences, generate legal actions, or
  submit commands;
- sending raw player language to a remote provider without separately approved consent and privacy
  policy;
- general chat, narrative generation, autonomous commander proposals, or long conversational
  memory;
- speech recognition, except as a future producer of text through the same draft boundary; and
- declaring Needle a production dependency before it passes the evaluation gates.

## Functional requirements

| ID | Requirement |
| --- | --- |
| `INT-001` | Maproom receives one versioned intent opportunity bound to campaign, state version, position, exact player side/audience, and one closed intent schema. A future authenticated host derives the side; no browser-supplied side is trusted. |
| `INT-002` | The opportunity exposes only side-safe presentation data required for the current decision: stable field/value identifiers, player labels/help, zero or more suggested approaches, clarification templates, and decision-scale metadata. It contains no hidden opposing truth, complete authority state, executable command, or parser instruction from scenario prose. |
| `INT-003` | Suggested approaches are deterministic for the same admitted opportunity, canonically ordered, accessibility-labelled, and explicitly presented as non-exhaustive. Selecting one seeds fields and/or editable text but causes no Staff, legal-action, or Umpire side effect. |
| `INT-004` | Maproom maintains one private typed draft per campaign/decision/audience. Language, starters, map gestures, lists, and structured controls update that same draft rather than parallel representations. |
| `INT-005` | Every field value carries provenance identifying manual entry, starter, local parse, remote parse if separately authorized, or an explicit disclosed rules default. Parser output cannot supply trusted campaign, concurrency, audience, or schema identity. |
| `INT-006` | An optional parser receives only a short player utterance, one schema identifier, and compact presentation-safe allowed values. It returns a proposed closed payload, evidence references where supported, unresolved text, refusal, or typed failure. It cannot see an authority handle, complete Campaign Observation, complete Content Pack, Chronicle, reports concatenated as instructions, or hidden legality. |
| `INT-007` | A deterministic validator checks contract version, state binding, schema, allowed-value membership, evidence, completeness, ambiguity, and contradiction. The browser may run an equivalent validator for feedback, but the server independently validates any confirmed intent before Staff consumes it. |
| `INT-008` | Validation produces field-level errors and an ordered collection of material clarification needs. Ordering is deterministic and based on schema-authored priority, not model judgment. |
| `INT-009` | Maproom automatically presents no more than two clarification questions for one interpretation attempt. Each uses bounded semantic answers plus a free-text/manual-edit escape. After the cap, remaining issues stay visible in the draft and required issues block confirmation. |
| `INT-010` | The parser never fills an unstated preference silently. An optional field can remain unset or carry an explicit player-selected `no-preference` value. A rules default is allowed only when authored by the game, visibly disclosed, and distinguishable from parsed player intent. |
| `INT-011` | The player can inspect, edit, clear, or replace every parsed/starter-derived field and can return to the original utterance while the draft is retained. Parse confidence is telemetry only and never an authority or auto-accept threshold. |
| `INT-012` | Intent confirmation requires an explicit player action on a currently valid, complete draft. It creates a non-authoritative confirmed-intent request; it does not itself submit a legal action or append Chronicle history. |
| `INT-013` | Deterministic Staff consumes only a server-validated confirmed intent, the exact current side-safe planning view, and current legal planning inputs. It produces zero or more state-bound plan candidates with explainable intent-fit and tradeoff data. Optional model I/O is outside this path. |
| `INT-014` | A player may choose a Staff plan, refine supported details through the same semantic controls, or use an advanced manual-order path. Staff never removes an original player decision point. |
| `INT-015` | Before authoritative submission, Maproom displays the exact order/plan being issued, important consequences knowable to the side, state binding, validation status, and a distinct final confirmation control. |
| `INT-016` | Final submission uses current exact-audience Legal Actions semantics and is re-derived/revalidated by the Umpire boundary. Stale, wrong-audience, unavailable, malformed, or no-longer-legal submissions produce typed rejection and zero authoritative events. |
| `INT-017` | Any state/position/audience/schema change marks affected drafts and plans stale. Maproom blocks confirmation/submission, refetches authorized data, preserves safe private edits where possible, and requires revalidation. It never silently rebinds an old order to new authority. |
| `INT-018` | Parser refusal, timeout, crash, unsupported runtime, missing model, or failed validation leaves the structured composer fully usable. No unavailable local parser silently falls back to a remote provider. |
| `INT-019` | Raw utterances, unresolved text, drafts, parser traces, and abandoned plans are private non-authoritative data. They do not enter Chronicle or ordinary logs. Research retention requires explicit opt-in, purpose, minimization, and deletion policy. |
| `INT-020` | Small closed decisions can omit the prompt and parser entirely. The opportunity declares its interaction scale so Maproom can choose direct controls, prompt-forward composition, precise map/list manipulation, or a combination without changing authority semantics. |
| `INT-021` | Onboarding teaches supported semantic concepts through guided starters, live field interpretation, contextual examples, correction, and an explanation that nothing is issued before review. It does not require memorizing exact prompt wording. |
| `INT-022` | The same decision can be completed with keyboard and programmatically labelled structured controls without a parser, pointer, drag action, or voice input. Parse status, provenance, errors, clarifications, and confirmation state are exposed to assistive technology. |
| `INT-023` | Every local hot-seat transition aborts or invalidates in-flight parse/planning work and clears or cryptographically isolates the outgoing seat's utterance, unresolved text, draft payload, parser trace/session/worker state, pending responses, DOM/live-region content, and relevant browser memory/storage/cache before the incoming seat can interact. A new seat/session epoch rejects late responses from the prior player. No transition relies on a Maproom privacy mechanism that has not yet been specified and tested. |

## Non-functional requirements

| ID | Requirement |
| --- | --- |
| `INT-NFR-001` | Core remains deterministic and model-free. Parser and remote I/O occur outside authoritative grain turns and outside the final legal-action decision. |
| `INT-NFR-002` | Contracts are closed, versioned, immutable at server boundaries, and use stable identifiers rather than labels or generic property dictionaries. Removed serialized fields reserve their numbers/identities where applicable. |
| `INT-NFR-003` | The same opportunity, confirmed intent, and admitted state produce the same validation and Staff plan semantics independent of parser provenance, culture, input collection order, clock, or network availability. |
| `INT-NFR-004` | The mature UI targets WCAG 2.2 AA, including textual labels/errors/suggestions, focus management, keyboard/non-drag alternatives, zoom/reflow, reduced motion, and screen-reader announcement of parse/clarification state. |
| `INT-NFR-005` | The no-model composer has 100% task coverage. Optional parser assets are lazily provisioned and failure-isolated; their availability cannot gate campaign activation or turns. |
| `INT-NFR-006` | Parser/provider/schema versions and request/response byte counts are observable without retaining request/response bodies. Raw bodies may exist only in an explicitly consented, purpose-limited, access-controlled evaluation corpus with a deletion policy; they never enter ordinary production logs or telemetry. Model upgrades rerun the retained golden and held-out corpora before release. |
| `INT-NFR-007` | Proposed initial local-parser targets are warm p95 below 500 ms, cold readiness below 3 seconds, and memory below 64 MB on recorded minimum hardware. These are evaluation gates, not current Needle claims. |
| `INT-NFR-008` | A representative parser request fits comfortably within Needle's documented 256-token window; failure to do so rejects Needle for that schema rather than truncating trusted context. |
| `INT-NFR-009` | Client and server validation use shared fixtures and contract vectors. Client validation improves interaction but never substitutes for server validation or current Core legal-action membership. |
| `INT-NFR-010` | New runtime dependencies, model assets, remote processing, telemetry retention, and governing contract changes each require separate owner approval after the relevant prototype gate. |

## Acceptance scenarios

| ID | Scenario | Expected evidence |
| --- | --- | --- |
| `INT-AC-001` | Open a strategic decision with no prior draft | Side-safe suggested approaches and a free-text/custom path appear; labels state that starters are suggestions, not the complete move set. |
| `INT-AC-002` | Select **Hold Tobruk** and add armor/water constraints in text | Starter and parser update one visible draft; each field exposes source/provenance; nothing is submitted. |
| `INT-AC-003` | Omit a material withdrawal policy | Deterministic validation surfaces the highest-priority bounded question; the answer updates the draft without creating chat-only state. |
| `INT-AC-004` | Produce three or more material omissions | Maproom asks at most two automatic questions, then shows remaining required fields directly and blocks confirmation until resolved. |
| `INT-AC-005` | Enter an unsupported location, contradictory posture, or prompt-injection-like report text | The parser cannot add an unlisted value; validation reports field-level issues; report/content text was never included as parser instruction. |
| `INT-AC-006` | Disable or crash the parser | The player completes the same draft with starters, map/list/form controls and reaches the same confirmed-intent semantics. |
| `INT-AC-007` | Compare manual and parsed drafts with identical semantic values | Server validation and Staff plan semantics are identical except private provenance/telemetry. |
| `INT-AC-008` | Change campaign state while a draft or plan is open | The old artifact becomes visibly stale; no silent rebinding or authoritative event occurs; authorized state is refreshed. |
| `INT-AC-009` | Confirm intent and inspect Staff plans | Staff returns only current state-bound deterministic plans or an explicit no-plan result, with intent-fit/tradeoff explanations and no hidden opposing truth. |
| `INT-AC-010` | Submit a selected plan after final review | Core revalidates exact current legal membership and either commits one accepted transition or returns a typed zero-event rejection. |
| `INT-AC-011` | Complete a binary initiative choice | Direct controls complete it without showing a prompt or invoking a parser. |
| `INT-AC-012` | Complete the strategic flow keyboard-only and with a screen reader | Every starter, input, draft field, clarification, error, map alternative, plan, and confirmation is reachable and programmatically understandable. |
| `INT-AC-013` | Inspect logs, Chronicle, and default telemetry after drafting and abandonment | No raw utterance, draft body, unresolved text, parser reasoning, or abandoned plan is present. |
| `INT-AC-014` | Upgrade parser/model/schema versions | The complete retained corpus runs; comparison is recorded; release blocks on semantic, refusal, privacy, runtime, and interaction gates. |
| `INT-AC-015` | Hand a local hot-seat campaign from one side to the other while a draft, parser worker, and delayed parse/planning response exist | The outgoing seat's strategic content is absent from the next seat's UI, back navigation, DOM/live regions, in-memory state, relevant browser storage/cache, parser session, and diagnostics. In-flight work is aborted or invalidated, late responses are discarded by seat/session epoch, and the incoming seat sees only its authorized view. |

## Parser adoption gate

Needle is not part of the capability definition. It may become one implementation of the parser
adapter only after a separately authorized experiment demonstrates:

- 100% structurally valid result, explicit refusal, or typed failure;
- zero validator-admitted values outside the current allowlist;
- at least 95% macro field accuracy and 90% exact whole-draft match on explicit held-out orders;
- zero reviewed high-severity false acceptances;
- at least 95% correct abstain/clarify behavior on deliberately ambiguous/unsupported cases;
- no worse median correction count than manual entry and a proposed 20% effort reduction on
  multi-field strategic tasks;
- the runtime/privacy/accessibility targets in this specification; and
- complete playability when the model is absent.

Failure leaves the stable intent composer intact with structured controls and, where worthwhile,
deterministic aliases.

## Tech stack and commands

The accepted mature Maproom direction is Vue 3 + Vite + TypeScript behind an ASP.NET Core
same-origin player boundary. Server contracts, Staff application logic, and authoritative adapters
remain C# on .NET 10. Exact project/package choices wait for the no-model prototype and their
roadmap predecessors.

```bash
dotnet restore Sandtable.slnx
dotnet format Sandtable.slnx --verify-no-changes --no-restore
dotnet build Sandtable.slnx --no-restore
dotnet test --solution Sandtable.slnx --no-build
git diff --check
```

Future Maproom commands must be recorded when its project is created; this specification does not
invent package-manager commands before the accepted frontend scaffold exists.

## Proposed repository structure

Exact filenames are gated by project creation, but ownership must follow this shape:

```text
src/Cna.Core/                         authoritative legal actions and adjudication only
src/Cna.OrleansHost/                  campaign activation and trusted side/session boundary
src/<Maproom BFF application>/        intent opportunity, confirmation, Staff-plan orchestration
src/<Maproom web client>/             private draft, starters, parser adapter, accessible composer
src/<Staff domain module>/            deterministic intent-to-plan expansion; no remote I/O
tests/<matching test projects>/       contract, validation, Staff, host, client, accessibility tests
docs/specs/                           product and contract requirements
docs/design/                          technical flow and staged delivery plan
docs/research/                        evidence and provider feasibility
```

No new microservice is implied. Prefer modules in the future Maproom/Staff application boundary
until scale or deployment evidence requires otherwise.

## Contract style

Future C# boundary values should follow the repository's closed typed-contract style. This is
illustrative, not implementation authorization:

```csharp
public sealed record ConfirmedPlayerIntent(
    int ContractVersion,
    string CampaignId,
    long ExpectedStateVersion,
    string ExpectedPositionId,
    string IntentSchemaId,
    PlayerIntentPayload Payload);
```

Use dedicated payload variants and stable enums/identifiers. Do not use
`Dictionary<string, object>`, browser-supplied audience claims, model-generated action IDs, or
ambient JSON behavior.

## Testing strategy

- **Pure contract/validator tests:** versions, closed values, validation precedence, deterministic
  clarification ordering, provenance, serialization, and culture/order independence.
- **Paired fog tests:** identical side-safe output under valid opponent-only authority changes.
- **Staff tests:** same confirmed intent/current state yields identical plan candidates; unsupported
  intent yields explicit no-plan/error rather than invention.
- **Host integration tests:** authenticated side derivation, stale state, cancellation, deadlines,
  idempotency, and zero-event rejection.
- **Client component tests:** one-draft synchronization across starter/text/map/form producers,
  question cap, correction, clearing, parser failure, and final confirmation separation.
- **Hot-seat isolation tests:** exercise normal and interrupted seat transitions with populated memory
  and browser storage, parser workers, assistive-technology live regions, back navigation, and late
  responses; prove the next seat cannot recover the prior seat's strategic content.
- **Accessibility tests:** semantic roles/names/errors, focus order, keyboard/non-drag flow, reflow,
  and manual screen-reader checks.
- **Parser evaluation:** versioned corpus separated from deterministic product tests; base parser,
  deterministic baseline, and manual control compared without executing selected functions.
- **End-to-end rules-lab demonstration:** no-model and parser-assisted paths converge on the same
  confirmed-intent and legal-submission semantics.

## Boundaries

### Always

- Preserve Command → Staff → Umpire ownership.
- Bind draft, confirmed intent, plans, and submission to current campaign/state/position/audience.
- Derive the authenticated side at the trusted host boundary.
- Clear or isolate all private composition state at every local hot-seat transition and reject prior
  seat/session responses after the transition.
- Keep validation and complete no-model controls deterministic.
- Show and require correction/confirmation before planning and again before submission.
- Propagate cancellation and deadlines through host/provider calls.
- Add focused deterministic and fog-pair tests before behavior implementation.

### Ask first

- Add or upgrade a parser/model/runtime package.
- Add a protobuf/HTTP contract or create Maproom/Staff projects.
- Persist drafts or raw utterances.
- Enable remote parsing, telemetry, research retention, or voice input.
- Change Legal Actions semantics, Staff authority, or the pre-alpha roadmap.

### Never

- Give a parser hidden opposing state, an authority handle, Chronicle, or submission capability.
- Treat suggested approaches as exhaustive legal actions.
- Silently invent preferences, rebind stale drafts, or fall back from local to remote inference.
- Hold an authoritative grain turn open on model or remote I/O.
- Let a client-side validator or parser establish legality.
- Record private drafting text in authoritative history or ordinary logs.

## Open product and contract questions

- Which first playable movement/combat decision supplies the representative strategic intent schema?
- Which intent fields belong to Command, and which must remain explicit Staff-plan selections?
- Are suggested approaches authored in scenario presentation content, derived by deterministic
  Staff policy, or combined under one versioned precedence rule?
- Which optional fields permit an explicit `no-preference`, and which rules defaults are safe to
  disclose?
- Does the first Maproom target include phone composition or only desktop/tablet plus phone review?
- What is the default private-draft lifetime, and can a player opt into cross-device drafts without
  retaining raw language?
- Which language beyond English is required before parser adoption?

These questions do not block a no-model rules-lab interaction prototype. They do block final
production contracts where noted.

## Specification completion gate

Before implementation begins:

- the owner reviews and accepts this specification after independent review;
- the representative decision and Command/Staff field boundary are selected;
- the governing roadmap records the authorized delivery slice and predecessors;
- exact project ownership and transport contracts are agreed;
- all `Ask first` dependency/privacy decisions required by the first slice are resolved; and
- task-level acceptance criteria in the technical design are updated to real project paths.
