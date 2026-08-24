# Player Intent Input And Needle Feasibility

**Status:** Product direction approved; independent planning review passed; implementation not authorized

**Date:** 2026-08-24

**Decision owner:** Project owner

**Research work item:** `RSH-INTENT-001`

**Outcome:** [Player Intent Composer v1 specification](../specs/player-intent-composer-v1.md)

## Executive conclusion

Sandtable should pursue a **hybrid intent composer**, not a globally rigid UI and not a prompt-only
game. Maproom may lead strategic decisions with a prominent prompt-like invitation—“What do you
want to accomplish?”—but language, map gestures, lists, and structured controls must all edit the
same visible private typed draft. Staff can expand only a confirmed intent into operational plans;
the player previews and explicitly submits a current server-issued legal action; the Umpire remains
the only authority.

This preserves what is appealing about prompt-first play: players can state coherent goals in their
own words instead of reverse-engineering a form. It avoids the central prompt-only failure: an
opaque probabilistic interpretation becoming indistinguishable from the player's actual order.

Needle is a credible **experimental local parser** for that narrow draft-population task. Its small
footprint, constrained JSON, explicit refusal, and WebAssembly/desktop targets are attractive. It is
not approved as a dependency or supported provider. Its 256-token window, base-only confidence,
newness, and unproven Sandtable grounding make a corpus-backed spike mandatory. If Needle fails,
the hybrid product remains intact: structured controls still edit the same draft, and a larger or
future parser can use the same adapter boundary.

## Decision resolution

On 2026-08-24 the owner approved these planning consequences:

1. Maproom's stable interaction concept is one typed private intent draft with several input modes.
2. Strategic decisions may be prompt-forward; small closed choices and precise map work remain
   direct-manipulation first.
3. No parser may submit, execute, generate legal actions, see authoritative state, or silently fill
   unstated preferences.
4. Needle proceeds only to a later bounded evaluation after representative Staff/intent schemas
   exist; no package or model is added now.

The owner additionally approved contextual, non-exhaustive approach starters and at most one or two
high-value clarification questions before the player reviews the typed draft. The starters and
clarification requirements are deterministic; the optional parser only extracts what the player
said.

Approval records a product/research direction, not permission to implement Maproom, Staff, a new
contract, or a model provider ahead of the roadmap.

## Decision question and scope

> Which player-input architecture should Sandtable pursue, and where—if anywhere—should a tiny
> constrained local model translate player language into typed intent?

In scope:

- rigid, prompt-first, and hybrid interaction shapes;
- player agency, discoverability, correction, accessibility, privacy, fog, and no-model behavior;
- a model-independent private-draft boundary;
- a narrow Needle feasibility assessment; and
- a future prototype and measurement plan.

Out of scope:

- production UI, Staff, Core, protobuf, or provider implementation;
- installing or downloading Needle;
- choosing final movement/combat order schemas before those rules exist;
- approving remote processing of player text;
- generated narrative, autonomous commanders, speech recognition, or general chat; and
- changing the accepted Maproom stack or gameplay roadmap.

## Method and evidence labels

The research inspected the current roadmap, web-play decisions, frontend validation gate, Command /
Staff / Umpire model, Campaign Observation, Legal Actions v1, and current action types. It then
reviewed primary human-AI interaction and accessibility guidance, comparative interactive natural-
language research, and current Needle documentation. Two project issues are retained only as
clearly labelled community observations.

- **Documented fact:** directly stated by a primary standard, publication, or publisher source.
- **Repository observation:** directly visible in Sandtable's current repository.
- **Community observation:** reported by a named third party with limitations retained.
- **Inference:** Sandtable-specific conclusion drawn from facts and observations.
- **Unknown:** material uncertainty requiring a decision, prototype, corpus, or playtest.

No package, model, or runtime was installed, and no empirical Needle test was run in this research
task.

## Existing Sandtable constraints

### Product and client

- **Repository observation:** the accepted mature Maproom is a responsive Vue 3 + Vite TypeScript
  SPA behind an ASP.NET Core player boundary. Local hot-seat remains the first playable mode, before
  hosted asynchronous friend play. See the
  [web-play shape](sandtable-web-play-shape-spike.md) and
  [research synthesis](persona-models-and-web-play-synthesis.md).
- **Repository observation:** the accepted renderer seam consumes an immutable side-safe view model
  and emits semantic player intent. It cannot own rules or invent legal actions. The later rules-lab
  prototype must already test map, keyboard, non-drag, private-draft, stale-state, and reconnect
  behavior. See the [frontend deep dive](frontend-and-webdiplomacy-success-deep-dive.md).
- **Repository observation:** Maproom is not yet implemented. Movement, combat, persistence, and the
  player UI remain beyond the current authority skeleton, so this direction does not require a UI
  migration. See the [pre-alpha roadmap](../roadmap/pre-alpha-roadmap.md).

### Authority and fog

- **Repository observation:** Maproom receives an authorized side-safe observation and current
  server-issued legal-action set. Final submission is bound to campaign, state, position, audience,
  and an opaque action ID; current membership is revalidated. See
  [Legal Actions v1](../specs/legal-actions-v1.md).
- **Repository observation:** Campaign Observation is a derived query result, never a command. Its
  spec explicitly rejects casual mapping of the whole value into free-form Intelligence strings;
  a future mapping must be typed and allowlisted. See
  [Campaign Observation v1](../specs/campaign-observation-v1.md).
- **Repository observation:** private drafts are mutable platform/client data. Only an accepted
  version-bound Umpire submission enters authoritative history. Intelligence failure cannot make
  ordinary campaign commands unavailable.
- **Documented project requirement:** `UX-001` permits automation to remove bookkeeping but not an
  original decision point.

### Domain meaning

The [naming model](../../naming-overview.md) already describes the correct separation:

```text
Command: “What should we accomplish?”
       |
       v
Staff: “How can we accomplish that?”
       |
       v
Umpire: “What actually happens?”
```

Language input belongs at the first boundary. “Hold Tobruk, preserve armor, and avoid exhausting
our water reserves” is player intent. Selecting formations, routes, allocations, and engagement
details is Staff planning. Checking legality, resolving supply/combat, and changing state is Umpire
authority.

## External interaction evidence

- **Documented fact:** the CHI 2019
  [Guidelines for Human-AI Interaction](https://doi.org/10.1145/3290605.3300233) were validated
  through multiple rounds including 49 practitioners evaluating 20 AI-infused products. Relevant
  guidance is to disclose capability and likely error; support efficient invocation, dismissal,
  and correction; disambiguate or degrade gracefully when uncertain; and make reasons accessible.
- **Documented fact:** Microsoft's
  [interactive NL-to-API study](https://www.microsoft.com/en-us/research/uploads/prod/2018/04/sigir18_nl2api.pdf)
  decomposed interpretations into correctable components. Compared with a non-interactive NLI, the
  interactive version improved task success, completion time, and satisfaction in simulation and
  human experiments.
- **Documented fact:** Microsoft Research's
  [conversational data program](https://www.microsoft.com/en-us/research/project/conversational-data-analytics/)
  explicitly treats language as ambiguous and imperfect and visual controls as efficient for some
  tasks; its design goal is to integrate both.
- **Documented fact:** [WCAG 2.2](https://www.w3.org/TR/WCAG22/) Level AA requires textual error
  identification, labels/instructions, and known correction suggestions. Its error-prevention
  guidance names review, confirmation, and correction before final submission. Sandtable already
  targets WCAG 2.2 AA.
- **Documented observation:** [DirectGPT](https://arxiv.org/abs/2310.03691) reported faster work and
  fewer/shorter prompts when direct-manipulation controls were layered over an LLM. Those editing
  tasks are not strategy play; the result supports testing a hybrid, not predicting Sandtable
  outcomes.
- **Documented fact:** the 2025
  [Magentic-UI report](https://www.microsoft.com/en-us/research/publication/magentic-ui-report/)
  studies editable co-planning and action guards. It is broader than this task, but its separation
  between generated proposal, editable plan, and guarded execution is structurally relevant.

**Inference:** a prompt-only submission path would be a poor fit. Language should populate visible,
editable components; uncertainty should become a clarification or explicit unresolved field; the
player should confirm the exact interpretation before Staff expansion or Umpire submission.

## Interaction option comparison

| Criterion | Rigid structured UI | Prompt-first / free text | Hybrid intent composer |
| --- | --- | --- | --- |
| Expression | Exact but can expose implementation-shaped forms | Broad and natural | Broad goals plus precise map/form editing |
| Discoverability | Visible choices | Hidden vocabulary and capability boundary | Visible fields teach what language can express |
| Error recovery | Strong field-level validation | Often devolves into repeated prompting | Field-level correction without losing original text |
| Authority and fog | Straightforward | Easy to blur proposal and command | Straightforward behind private draft + allowlist |
| Accessibility | Good with semantic non-map controls; dense forms can burden users | Voice/text helps some users but ambiguity and prompt literacy burden others | Best potential: interchangeable voice/text/map/list/form entry |
| No-model completeness | Complete | Poor if parsing is required | Complete |
| Expert efficiency | Strong for repeated exact choices | Strong for coherent multi-field goals | Strong in both cases |
| Testability | Highest | Lowest | High at draft/validator boundary; parser measured separately |
| Initial cost | Lowest | High once correction/fallback are honest | Moderate |
| Reversibility | Presentation can change; forms may harden around low-level mechanics | Provider behavior becomes product behavior | Parsers and controls remain replaceable draft producers |

**Recommendation:** choose the hybrid. “Prompt-forward” remains a presentation choice for strategic
decisions; it must never mean “prompt-only.”

## Proposed interaction model

### Decision-scale routing

| Decision type | Primary interaction | Language role |
| --- | --- | --- |
| Binary or small closed choice | Buttons/radio/keyboard shortcut | Optional alias; a model adds little value |
| Strategic Command intent | Prompt-like composer plus visible goal/preferences | Populate an editable semantic draft |
| Precise placement or route | Map/list/table manipulation | Modify selected objects or bounded fields; never infer legality |
| Multi-formation operational plan | Staff plan board and preview | Seed goals/constraints; refine graphically |
| Read-only query/help | Search, filters, contextual help | Separate future capability, not the command path |

### Player experience sketch

```text
+------------------------------------------------------------------+
| COMMAND — What do you want to accomplish?                        |
| [ Hold Tobruk, preserve armor, conserve water.                 ] |
|                                                    [Interpret]    |
+------------------------------------------------------------------+
| INTERPRETED ORDER — private draft                                |
| Objective       [Tobruk             v]  from “Tobruk”            |
| Posture         [Hold               v]  from “Hold”              |
| Force policy    [Conserve armor     v]  from “preserve armor”    |
| Logistics       [Conserve water     v]  from “conserve water”    |
| Unresolved      None                                             |
|                                                                  |
| [Edit on map] [Clear interpretation] [Preview Staff plan]        |
+------------------------------------------------------------------+
| Nothing here is an issued order until the later explicit submit. |
+------------------------------------------------------------------+
```

The UI may feel conversational, but it is not required to maintain a chatbot conversation. The
valuable state is the typed draft. Follow-up text edits that same object; direct manipulation edits
it too.

### Model-independent draft contract

The eventual specification should define a trusted envelope and closed decision-specific payloads,
not a generic dictionary. This is a sketch, not an approved schema:

```text
IntentDraftEnvelope
  contractVersion
  draftId
  campaignId
  expectedStateVersion
  expectedPositionId
  audience
  intentSchemaId
  payload: one closed typed intent variant
  provenance: manual | parsed-local | parsed-remote
  parserTrace?: private provider/version/latency/confidence data

StrategicPostureIntent
  objectiveId?
  posture?: hold | advance | withdraw | screen
  forcePreservation?: conserve | balanced | expend
  logisticsPriority?: conserve | balanced | surge
  engagementThreshold?: bounded policy value
  unparsedText[]
```

Maproom stamps campaign, concurrency, position, and audience identity. A parser cannot supply or
alter that envelope. It receives only:

- the short player utterance;
- one `intentSchemaId`; and
- a small presentation-safe list of allowed objective/unit/policy values.

A deterministic validator checks contract shape, allowlist membership, completeness, contradictions,
evidence, and current-state binding. It returns field-level errors and allowed corrections. Staff
consumes only a player-confirmed valid payload. Final action membership is still revalidated by the
Umpire.

### Trust flow

```text
side-safe view + decision-specific presentation vocabulary
                         |
              +----------+----------+
              |                     |
       direct controls       optional parser
              |                     |
              +----------+----------+
                         |
               private typed intent draft
                         |
             deterministic validation
                         |
             visible edit / clarification
                         |
                explicit confirmation
                         |
             deterministic Staff planning
                         |
               preview / final submit
                         |
          current legal-action revalidation
                         |
                       Umpire
```

The parser has no edge to the authority handle, complete Campaign Observation, complete Content
Pack, opposing state, Chronicle, Staff execution, or submission.

## Needle assessment

### Documented capabilities and constraints

- **Documented fact:** [Needle 2's model card](https://huggingface.co/Cactus-Compute/needle2)
  describes a 45M-parameter Apache-2.0 model, a 14 MB binary, roughly 28 MB session memory, a
  256-token sliding window, and desktop/mobile/server/WebAssembly builds.
- **Documented fact:** the [API documentation](https://github.com/cactus-compute/needle/blob/main/doc/apis.md)
  says enums, ranges, patterns, lengths, and collection constraints compile into the decoding
  grammar. One declared schema can force one structurally valid extraction call; unsupported input
  can return no call; inference can run offline after the engine is present.
- **Documented fact:** only the call is grammar-constrained. Structural validity does not establish
  semantic correctness. The reasoning string is unconstrained.
- **Documented fact:** more than five tools trigger retrieval of only five. One
  `capture_player_intent` schema is safer than dynamically modeling every order as a tool.
- **Documented fact:** confidence is calibrated only for the base model. Fine-tuned weights return no
  confidence score. The [fine-tuning guide](https://github.com/cactus-compute/needle/blob/main/doc/finetuning.md)
  says argument grounding can require thousands of varied examples, and tuned archives are tied to
  an engine version.
- **Documented fact:** publisher-documented deployment options include localhost HTTP and browser
  WebAssembly. Browser compatibility, cold load, caching, cross-origin isolation, and assistive-
  technology behavior still require a Sandtable prototype.

As of 2026-08-24, [PyPI package metadata](https://pypi.org/project/cactus-needle/) reports version
2.0.9, released 2026-08-21.

### Suitable use

- short explicit English Command statements;
- one small decision-specific extraction schema;
- values constrained to presentation-safe objective/unit/policy lists;
- private on-device draft assistance;
- field-level interpretations that the player visibly confirms; and
- optional voice input after a separately evaluated speech-to-text layer.

### Unsuitable use

- deciding which strategy is good;
- filling unstated goals or preferences;
- consuming the complete observation or a long order sheet;
- long conversational reference resolution;
- routing and coordinating many formations;
- narrative generation;
- using hidden opponent truth;
- direct legal-action submission; or
- replacing deterministic Staff or fallback.

### Maturity observations

- **Community observation:** Needle issue
  [#61](https://github.com/cactus-compute/needle/issues/61) reports excellent 0.1–0.8 second speed but
  poor argument grounding and weak confidence separation in eight Spanish-language test cases with
  16 overlapping tools and only about 300 fine-tuning examples. The author explicitly identifies
  the tiny sample, language, overlap, and training-size confounds.
- **Community observation:** closed issue
  [#51](https://github.com/cactus-compute/needle/issues/51) reproduced a README failure on several
  platforms with version 2.0.1. The current package is newer. This is release-cadence evidence, not
  a known 2.0.9 defect.

**Inference:** Needle deserves a controlled spike, not trust. A schema-valid result and publisher
confidence value are insufficient product gates.

### Deployment choices

| Shape | Advantages | Risks | Initial disposition |
| --- | --- | --- | --- |
| Browser WebAssembly | On-device privacy; follows hosted Maproom; no localhost service | Browser/runtime compatibility, cold load, caching, 14 MB delivery, upgrade and CSP work | Preferred feasibility target for mature hosted Maproom |
| Localhost process | Simple documented HTTP API; good for desktop-hosted/local hot-seat | Lifecycle/install/support; browser-to-localhost security; unavailable when player's device is closed | Useful desktop prototype or local mode |
| Server-side Needle | Simple BFF adapter and controlled version | Sends private intent to server; loses on-device/offline value; hosted availability/support | Defer; a larger server model may offer more value |
| Native library binding | Lowest protocol overhead | C# binding/ABI/lifecycle burden and static-library packaging | Do not start here |

No remote fallback may happen silently. If local parsing is unavailable, Maproom keeps the text as
an unprocessed private note and exposes the complete structured composer.

## Failure model and required controls

| Failure mode | Required control |
| --- | --- |
| Structurally valid but wrong field | Editable field-level draft, original utterance retained, corpus scoring, explicit confirmation |
| Invented or unavailable entity | Dynamic allowlist plus deterministic membership validation and suggested allowed values |
| Omitted, ambiguous, or contradictory preference | Leave unresolved and ask one bounded clarification; never silently default through the parser |
| Stale draft | Bind to state/position; block finalization; refetch authorized view; revalidate |
| Prompt injection through reports/content | Parser receives player utterance plus typed presentation vocabulary only; no concatenated reports, raw content, or hidden state |
| Provider unavailable | Manual draft controls remain complete; no submission side effect |
| Fine-tuned confidence absent or base confidence misleading | Treat confidence as telemetry, not authority; calibrate empirically if useful |
| Upgrade behavior drift | Pin engine/model/schema; run the complete golden utterance corpus before upgrade |
| Raw intent leaks strategy | Keep draft/parser telemetry private; do not log raw utterances; use explicit opt-in and delayed release for any research corpus |

## Prototype and evaluation plan

This sequence starts only when the owner authorizes it. It deliberately separates product value
from model value.

### `INTENT-P0` — No-model interaction prototype

Build a low-fidelity or isolated Maproom order composer for three representative decision scales:

1. one small closed choice;
2. one strategic multi-field intent; and
3. one precise map/list order.

Use deterministic fake parse results. Test prompt-forward entry, visible fields, direct edits,
unresolved text, clearing, stale drafts, and final confirmation. Do not integrate a model.

**Gate:** users understand the interpreted order and can complete every task through keyboard and
structured controls without the parser.

### `INTENT-P1` — Typed contract and validator spike

Define one experimental envelope and two or three closed semantic payloads outside Core authority.
Generate compact presentation-safe vocabularies from synthetic decision fixtures. Implement a pure
validator for membership, contradictions, completeness, evidence, and state binding. Use manual and
fake-parser producers against the same contract.

**Gate:** the schema expresses real Command intent without embedding Staff mechanics, opaque action
hashes, hidden state, or generic property bags, and one rendered request fits comfortably within
Needle's 256-token window.

### `INTENT-P2` — Corpus

Create a versioned consent-safe evaluation corpus containing:

- concise explicit orders and paraphrases;
- optional and missing fields;
- contradictions and ambiguity;
- off-topic text and requests outside the current decision;
- spelling variants, abbreviations, historical place/unit labels, and speech-transcription-like
  errors; and
- adversarial attempts to inject unsupported entities or bypass confirmation.

Start with several hundred reviewed examples across the experimental schemas for evaluation only.
Do not fine-tune from that initial set. If base performance is promising but grounding is the only
gap, expand into the thousands before a separately approved fine-tuning experiment, consistent with
the publisher's guidance.

### `INTENT-P3` — Parser comparison

Pin exact versions and compare:

1. manual structured entry (product control);
2. deterministic keyword/alias parsing where reasonable;
3. base Needle through one schema; and
4. one larger constrained model only if needed to distinguish model-size failure from interaction-
   design failure.

Run from files/CLI first. Evaluate browser WebAssembly only after semantic quality justifies product
integration work. Never call `.run()` or execute model-selected functions; use one-turn extraction.

### `INTENT-P4` — Formative Maproom test

Place the best parser behind the same isolated composer and compare manual-only with hybrid entry.
A small formative study can expose confusion and correction burden; it is not a statistical proof.
Include keyboard-only and screen-reader review, stale-state recovery, model-unavailable behavior,
and desktop/tablet form factors.

### `INTENT-P5` — Decision

Adopt a parser only if it improves the interaction without weakening comprehension, correction,
accessibility, privacy, or the no-model path. Otherwise retain the hybrid draft UI with language
disabled, deterministic aliases only, or a future replaceable provider.

## Proposed measurement and gates

These are decision thresholds for the spike, not claims about current Needle performance.

### Semantic quality

- 100% structurally valid responses or explicit refusal/error;
- zero validator-admitted out-of-allowlist values;
- at least 95% macro field accuracy on explicit in-domain held-out utterances;
- at least 90% exact whole-draft match on that same set;
- zero false acceptance in the reviewed high-severity adversarial set;
- at least 95% correct abstain/clarify behavior on deliberately ambiguous or unsupported cases; and
- every mismatch remains visibly correctable before Staff or submission.

Fine-tuned Needle receives no lower gate merely because confidence is unavailable. The application
does not auto-accept based on confidence at any threshold.

### Interaction value

- 100% task completion without any model;
- no authoritative transition without explicit final confirmation;
- median correction count no worse than manual-only entry;
- a meaningful time or effort reduction on multi-field strategic tasks, with a proposed initial
  target of 20%;
- players can accurately restate what will happen after reviewing the draft; and
- prompt entry does not reduce discovery of valid options or help.

### Runtime and privacy

- representative target hardware, exact engine/model build, and request/response byte **counts** are
  recorded; raw request/response bodies are excluded from production logs and telemetry;
- proposed initial targets: warm p95 below 500 ms, cold readiness below 3 seconds, and process/browser
  memory below 64 MB on the minimum target device;
- no inference network access after an intentionally provisioned model is present;
- no raw utterance, observation, or draft body in ordinary logs/telemetry; and
- unavailable or rejected parsing leaves the campaign and manual composer fully functional.

### Accessibility

- all draft fields and parse status expose programmatic names, roles, values, and textual errors;
- keyboard, screen-reader, non-drag, zoom/reflow, and reduced-motion paths complete the same tasks;
- the parser never becomes the sole alternative to precise pointer actions; and
- suggestions name allowed corrections rather than only reporting “I didn't understand.”

## What would change the recommendation

- If real playtesting shows nearly all player decisions are binary or single-field, language adds
  too little value; retain direct structured controls.
- If future Staff cannot define compact typed intent separate from mechanics, Needle is the wrong
  abstraction even if its extraction scores are good.
- If correction burden matches or exceeds manual entry, disable the parser while retaining the
  shared draft model.
- If players cannot discover capabilities from the hybrid UI, make structured controls more
  prominent; do not expand the prompt's apparent freedom.
- If a larger constrained model materially outperforms Needle within acceptable local cost, keep
  the adapter and select from measured evidence.
- If browser WebAssembly cannot meet privacy, accessibility, compatibility, or load targets, keep
  parsing desktop-local only or omit it from hosted Maproom.

## Confidence, limitations, and unresolved questions

**High confidence:** prompt-only submission conflicts with Sandtable's authority, accessibility,
draft, and no-model boundaries; a shared typed private draft is the correct seam.

**High confidence:** Needle's documented shape fits short constrained extraction, not strategic
reasoning or complete order generation.

**Moderate confidence:** prompt-forward hybrid entry will improve multi-field strategic decisions.
External evidence is analogous rather than game-specific, and Sandtable has no player study yet.

**Low confidence:** Needle 2.0.9 will meet Sandtable's semantic-quality or browser-runtime gates.
There is no representative corpus or controlled experiment.

Open owner/product questions:

- At which future decision barriers should humans state high-level Command intent versus select an
  already-expanded Staff plan?
- Should phone Maproom support full strategic composition or only bounded action entry and review?
- Should raw private utterances persist with drafts, disappear after confirmation, or be retained
  only through an explicit research opt-in?
- Which languages beyond English matter for the first hosted product?
- Does voice input belong in the first accessibility prototype or only after text interaction is
  stable?

## Implementation consequences after approval

Approval should later produce, in order:

1. a Maproom product/interaction requirement for one private typed draft with several input modes;
2. a Staff/Command specification defining the semantic intent scale;
3. a presentation-safe per-decision parse-context contract;
4. a pure validator and fake parser in the rules-lab UI prototype;
5. a retained corpus and isolated provider evaluation; and only then
6. a provider/deployment decision and any package/runtime integration.

Do not update the Intelligence protobuf first. Player intent parsing is a different interaction and
privacy path from autonomous commander proposals, even if both later reuse provider infrastructure.

## Source index

### Sandtable

- [Sandtable web-play shape](sandtable-web-play-shape-spike.md)
- [Frontend selection and webDiplomacy success deep dive](frontend-and-webdiplomacy-success-deep-dive.md)
- [Persona models and web-play synthesis](persona-models-and-web-play-synthesis.md)
- [Lightweight persona models](lightweight-persona-models-spike.md)
- [Campaign Observation v1](../specs/campaign-observation-v1.md)
- [Legal Actions v1](../specs/legal-actions-v1.md)
- [Pre-alpha roadmap](../roadmap/pre-alpha-roadmap.md)
- [Naming overview](../../naming-overview.md)

### External primary sources

- Amershi et al., [Guidelines for Human-AI Interaction](https://doi.org/10.1145/3290605.3300233)
- Microsoft Research,
  [Natural Language Interfaces with Fine-Grained User Interaction](https://www.microsoft.com/en-us/research/uploads/prod/2018/04/sigir18_nl2api.pdf)
- Microsoft Research,
  [Natural Language Interface for Data Analytics](https://www.microsoft.com/en-us/research/project/conversational-data-analytics/)
- Microsoft Research, [Magentic-UI report](https://www.microsoft.com/en-us/research/publication/magentic-ui-report/)
- W3C, [Web Content Accessibility Guidelines 2.2](https://www.w3.org/TR/WCAG22/)
- W3C, [Understanding Error Suggestion](https://www.w3.org/WAI/WCAG22/Understanding/error-suggestion)
- [DirectGPT](https://arxiv.org/abs/2310.03691)
- Cactus Compute, [Needle repository and API](https://github.com/cactus-compute/needle)
- Cactus Compute, [Needle 2 model card](https://huggingface.co/Cactus-Compute/needle2)
- Cactus Compute, [Needle package metadata](https://pypi.org/project/cactus-needle/)
- Cactus Compute, [Needle fine-tuning guide](https://github.com/cactus-compute/needle/blob/main/doc/finetuning.md)

### Community observations

- Needle issue [#61: tool-router evaluation](https://github.com/cactus-compute/needle/issues/61)
- Needle issue [#51: closed cross-platform README failure](https://github.com/cactus-compute/needle/issues/51)
