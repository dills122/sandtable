# Lightweight Persona Models Spike

**Status:** Owner-approved evaluation direction; no model approved for product support

**Date:** 2026-08-16

**Decision owner:** Project owner

## Executive conclusion

Sandtable should not present an unrestricted model picker or make any model part of the Umpire. It
should launch with a small, capability-tested Intelligence support matrix:

1. **Scripted Command policy** remains the default, deterministic baseline and mandatory fallback.
2. **Ministral 3 3B Instruct 2512, Q4_K_M GGUF** is the leading local-balanced candidate for plan
   selection and short persona narrative.
3. **Ministral 3 8B Instruct 2512, Q4_K_M GGUF** is the leading local-quality candidate for machines
   with more memory.
4. A **custom OpenAI-compatible endpoint** supports a LAN model server or an explicitly configured
   hosted provider, but only after a capability probe and the same War College evaluation gates.
5. **Qwen3.5 2B/4B and 9B** should be retained as the principal challenger family in the evaluation
   spike. **Phi-4-mini-instruct** is a useful permissive, text-only 3.8B control.
6. Sub-1B models such as **Qwen3.5-0.8B** should remain experimental and narrative-only until they
   prove they can meet Sandtable's semantic plan-selection gates. Their small footprint does not by
   itself make them reliable commanders.

This is a candidate decision, not approval to ship weights or add a provider. The model cards do not
measure persona fidelity, selection among Sandtable legal plans, resistance to prompt injection in
observations, or quantized performance on Sandtable's target hardware. A bounded evaluation must
answer those questions before any named model becomes a supported product option.

**Owner decision, 2026-08-16:** Approve the support shape and bounded evaluation direction as part
of the combined persona/web-play research. This does not approve a model download, runtime
dependency, hosted provider, minimum hardware claim, or player-facing named model.

## Decision frame

### Question

Which lightweight model families, sizes, serving modes, and initial support levels should Sandtable
consider for optional commander personas?

### Why this matters now

The repository already defines the non-authoritative Intelligence boundary and the shape of a
commander-persona policy. A model shortlist is useful for keeping the eventual gateway and player
settings narrow, but premature model integration would compete with the legal-plan, scripted-policy,
fog-of-war, and replay foundations on which safe model use depends.

### Authorized scope

- Compare credible current open-weight models in approximately sub-1B, 1-4B, and 7-9B tiers.
- Compare local, LAN, and hosted delivery.
- Identify licensing, distribution, privacy, structured-output, context, and hardware consequences.
- Propose a future evaluation with measurable gates.

### Prohibited scope

- No model downloads, execution, fine-tuning, dependency changes, or provider accounts.
- No protobuf, gateway, architecture, roadmap, or product behavior changes.
- No inference that a general benchmark score proves Sandtable persona quality.

### Stop condition

This packet stops at a decision-ready shortlist, support matrix, and evaluation plan. Named support
remains conditional on measured product fit and a separate implementation decision.

## Decision criteria

Criteria are ordered by importance for Sandtable rather than by general model popularity.

| Criterion | What good looks like |
| --- | --- |
| Authority safety | Output is always an untrusted proposal; Umpire legality and adjudication are unchanged |
| Fog-of-war safety | Only the side-safe observation reaches the provider; no hidden state, broad retrieval, or silent memory |
| Semantic selection | Selects an existing legal `plan_id`, respects bounded parameters, and avoids dominated choices |
| Structured output | Runtime can constrain a small versioned JSON schema; application validates again after decoding |
| Persona quality | Distinct profiles produce explainable, bounded differences without caricature or invented facts |
| Fallback and replay | Timeout/failure uses the same effective persona in scripted policy; accepted commands, not regeneration, drive replay |
| Local feasibility | A useful tier runs on ordinary CPU, Apple Silicon, or consumer GPU hardware with a bounded context |
| Distribution | License and notices permit the intended commercial/non-commercial delivery model without bespoke negotiation |
| Operational simplicity | One adapter and one local runtime cover several platforms; models remain replaceable and version-pinned |
| Privacy and consent | Local mode keeps observations on-device; remote mode is explicit, disclosed, and configurable |

## Repository observations

The following are observations from the repository at the start of this spike, not external claims.

- **Observation:** `Cna.Intelligence.Gateway` deliberately reports that no provider is configured.
- **Observation:** `DecisionRequest` carries `decision_id`, `state_version`, `ruleset_hash`, a compact
  commander projection, a strategic observation, and Umpire-provided plan candidates.
- **Observation:** `DecisionResponse` returns one selected plan ID, bounded parameters, commentary,
  and a provider/model/prompt/persona trace.
- **Observation:** the current transport uses free-form strings and a parameter map; it does not yet
  express the complete domain persona, condition, or a strict response schema.
- **Observation:** the approved persona direction makes the model an optional projection of a
  versioned Command policy. Scripted and model-backed paths must use the same effective profile.
- **Observation:** model I/O must remain outside authoritative Orleans grain turns, and every timeout
  or unavailable provider must fall back to deterministic scripted selection.

These boundaries make the task materially easier than open-ended game playing. The model does not
need to learn the board game, calculate combat, search paths, or invent orders. It compares a compact
set of legal strategic candidates and optionally writes short presentation text.

## Evidence and analysis

### The useful context is small and deliberately bounded

- **Documented fact:** current candidate models advertise maximum contexts from 32K to 256K tokens.
  Ministral 3 advertises 256K, Qwen3.5 advertises 262,144, Phi-4-mini advertises 128K, and the smaller
  Gemma 3 variants advertise 32K.
- **Inference:** those maxima are not a product requirement. A redacted profile, condition summary,
  recent events, and tens of compact candidate records should fit comfortably within an initial
  8K-token input budget and a 256-token decision output budget.
- **Inference:** allowing the advertised maximum by default would waste KV-cache memory, enlarge the
  prompt-injection surface, encourage advisory memory to become an unofficial history store, and
  make latency less predictable.
- **Recommendation:** make context and output caps part of a versioned provider capability profile.
  Start with 8K input for plan selection, 16K only for bounded narrative/after-action work, and no
  automatic truncation of required candidate data. An oversized request must fall back explicitly.

### Structured syntax is necessary but not semantic correctness

- **Documented fact:** `llama.cpp` exposes an OpenAI-compatible local server, supports JSON Schema to
  grammar conversion for a subset of JSON Schema, and supports Metal, CUDA, CPU, Vulkan, and other
  backends. Its documentation warns that the schema constrains output but is not automatically
  visible to the model prompt.
- **Documented fact:** vLLM's OpenAI-compatible server supports structured outputs using JSON schema,
  regex, choices, or grammar. Its function-calling guidance explicitly distinguishes guaranteed
  parseability from output quality.
- **Documented fact:** Mistral documents JSON mode and custom structured outputs, recommending custom
  schemas when possible and still recommending explicit prompt instructions.
- **Inference:** the decision schema should dynamically enumerate the legal `selected_plan_id`
  values and allowed parameter names/ranges where the runtime's supported schema subset permits it.
  This prevents many malformed answers but does not prove that the selected legal plan is sensible.
- **Recommendation:** enforce four gates in sequence: constrained decoding, strict transport parsing,
  gateway semantic validation, and Umpire validation against the current decision ID/state/hash and
  still-legal plan. A repair attempt must be bounded and deduplicated; otherwise use scripted fallback.

### Quantization makes local delivery plausible, not automatically equivalent

- **Documented fact:** Mistral's official Ministral 3 3B GGUF repository provides Q4_K_M at 2.15 GB,
  Q5_K_M at 2.47 GB, Q8_0 at 3.65 GB, and BF16 at 6.87 GB. Its model card says the family targets edge
  deployment and the quantized model fits below the FP8 memory requirement.
- **Documented fact:** the official Ministral 3 8B GGUF repository provides Q4_K_M at 5.2 GB,
  Q5_K_M at 6.06 GB, and Q8_0 at 9.03 GB. The optional vision projector is a separate file.
- **Documented fact:** `llama.cpp` supports integer quantization and CPU/GPU hybrid inference; Apple
  Silicon is a first-class target using ARM, Accelerate, and Metal.
- **Inference:** model-file size is a lower bound, not working-set memory. Runtime state, KV cache,
  context, concurrency, and platform buffers must be measured. Sandtable should not advertise a RAM
  minimum derived only from the weight file.
- **Unknown:** the plan-selection and persona-quality loss from Q4_K_M versus Q5_K_M for these models.
- **Recommendation:** use Q4_K_M only as the first evaluation configuration, compare Q5_K_M on the
  same corpus, and ship the smallest configuration that clears quality and hardware gates. Do not
  load the unused vision projector for text-only persona work.

### General benchmarks are screening evidence only

- **Documented fact:** the Qwen3/Qwen3.5 model cards report instruction-following, reasoning, agent,
  multilingual, role-playing, and long-context benchmarks. Mistral and Microsoft publish similar
  general comparisons for their small models.
- **Inference:** instruction-following and function-calling claims make these models plausible
  candidates, but none of the published suites tests Sandtable's redacted observations, historical
  profiles, candidate-plan schema, or competence floor.
- **Unknown:** whether a 3B model can express meaningfully distinct personas without changing the
  quality of plan selection, and whether an 8B model's improvement is noticeable enough to justify
  its memory and latency.
- **Recommendation:** do not rank finalists using vendor benchmark tables. Rank them using one
  versioned Sandtable corpus, identical prompts, identical schema constraints, and measured hardware.

## Candidate comparison

All facts in this table were observed from official model cards or licenses on 2026-08-16. Product-fit
judgments are explicitly labeled as inferences.

| Candidate | Documented facts | Sandtable inference | Disposition |
| --- | --- | --- | --- |
| Qwen3.5-0.8B | Apache 2.0; 0.8B language model; 262K native context; card positions it for prototyping, task-specific fine-tuning, and research/development | Excellent footprint and useful lower-bound control, but likely too brittle for supported autonomous selection without specialization | Experimental; narrative/routing only unless evaluation proves otherwise |
| Gemma 3 270M/1B | 32K context; small local sizes; Gemma-specific terms require agreement, downstream restrictions/notices, and license propagation for distribution | Attractive extreme-low tier, but limited capacity and extra distribution obligations bring no clear launch advantage | Defer |
| Qwen3.5-2B/4B | Apache 2.0; non-thinking by default; 262K context; OpenAI-compatible vLLM/SGLang guidance; text-only serving can omit vision profiling | Strong current challenger; 2B tests the footprint boundary and 4B may offer quality near the balanced tier, but the newer hybrid architecture/runtime path needs direct operational validation | Evaluate as challenger, not initial support promise |
| Ministral 3 3B Instruct 2512 | Apache 2.0; 3.4B language model plus separate 0.4B vision encoder; 256K context; native function calling/JSON claim; official GGUF and `llama.cpp` instructions; Q4_K_M is 2.15 GB | Best current combination of permissive distribution, first-party quantization, cross-platform serving, and task-relevant structured output | Leading local-balanced candidate |
| Phi-4-mini-instruct | MIT; 3.8B text model; 128K context; card emphasizes instruction following/function calling and constrained environments; official vLLM and ONNX paths, community quantizations | Valuable text-only control with simple licensing; less coherent as the primary two-tier family and first-party GGUF story than Ministral | Evaluate as 1-4B control |
| Ministral 3 8B Instruct 2512 | Apache 2.0; approximately 9B total parameters; 256K context; same family capabilities; official GGUF; Q4_K_M is 5.2 GB | Natural quality tier sharing templates/runtime/license with 3B; likely reasonable on 16 GB unified-memory or consumer-GPU systems, but working set and benefit must be measured | Leading local-quality candidate |
| Qwen3.5-9B | Apache 2.0; 9B language model; 262K native and advertised longer extension; strong vendor-reported instruction-following results | Strong quality challenger, but its extra modalities and newer serving requirements do not justify a second launch family without a measured win | Evaluate as 7-9B challenger |
| Llama 3.2 1B/3B | 128K context, wide ecosystem, Llama 3.2 community license rather than a standard permissive software license | Mature ecosystem is useful, but current permissive candidates have clearer distribution and more current small-model offerings | Defer |
| Gemma 3 4B | 128K context, broad runtime support, Gemma-specific terms | Credible technical challenger, but accepting and propagating custom terms is unnecessary for the initial matrix | Defer unless it materially wins evaluation |

### Why Ministral leads without being pre-approved

The recommendation is driven by delivery coherence rather than a claim that Ministral is the
smartest model:

- one Apache-2.0 family spans balanced and quality tiers;
- Mistral publishes the exact GGUF variants and direct `llama.cpp` launch path;
- its cards explicitly claim system-prompt adherence, function calling, and JSON output;
- the optional vision projector can remain outside the text-only persona path;
- the same models are available through Mistral's hosted API, which could simplify controlled
  local-versus-hosted comparison without coupling the Sandtable contract to that provider.

The unresolved issue is the one that matters most: no official result demonstrates good Sandtable
commander behavior. A Qwen or Phi finalist should replace Ministral if it clearly wins the retained
evaluation corpus while meeting the same license, hardware, and runtime gates.

## Recommended launch support matrix

“Supported” must mean a pinned model artifact/runtime/configuration combination that passed War
College evaluation, not merely any endpoint that responds to an OpenAI-shaped request.

| Player-facing mode | Initial status | Candidate/runtime | Intended use | Failure behavior |
| --- | --- | --- | --- | --- |
| Scripted | Required and default | Versioned Command policy in Sandtable | All decisions, headless simulation, test baseline | No external failure mode |
| Local Balanced | Candidate for first model support | Ministral 3 3B Instruct 2512 Q4_K_M, pinned `llama.cpp`, text only, 8K decision context | Normal plan selection and short persona commentary | Deadline/schema/semantic failure -> same-persona scripted policy |
| Local Quality | Candidate, optional download | Ministral 3 8B Instruct 2512 Q4_K_M, same runtime and caps | Major decisions and richer short narrative | Same fallback; never block campaign progress |
| Custom Local/LAN | Supported protocol after capability probe | Authenticated OpenAI-compatible endpoint, exact provider/model supplied by owner | Shared GPU machine or advanced user runtime | Fail closed to scripted; no silent provider substitution |
| Hosted | Post-MVP, explicit opt-in | One pinned provider adapter plus provider/model allowlist | Convenience and comparison where user accepts remote processing | Disclose remote data path; deadline and fallback mandatory |
| Experimental | Developer/War College only | Qwen3.5 0.8B/2B/4B/9B, Phi-4-mini, later candidates | Evaluation, not normal campaign selection | Never shown as supported until gates pass |

### Product presentation

The application should offer capability-oriented choices rather than a long model catalog:

- `Scripted (recommended for reproducible simulation)`
- `Local Balanced`
- `Local Quality`
- `Custom Intelligence Endpoint`

Advanced/War College views may show the exact model, quantization, runtime, prompt version, context
caps, and evaluation status. Normal players should not need to understand parameter counts. A local
model download must be separate, consented, checksum-verified, removable, and accompanied by its
license and notices; weights should not be hidden inside the game installer.

## Serving and delivery recommendation

### Local desktop

- **Recommendation:** use a pinned `llama.cpp` server as a separate child process or separately
  managed local process. Talk to it through a narrow loopback-only adapter.
- **Reasoning:** official support covers Apple Metal, NVIDIA CUDA, CPU, and other backends; its server
  is OpenAI-compatible and supports schema-constrained output. A separate process isolates crashes,
  memory pressure, warmup, and lifecycle from the .NET Umpire and gateway.
- **Security consequence:** bind to loopback by default, generate an ephemeral credential if the
  runtime supports it, never log complete observations by default, and never let model endpoints
  address arbitrary URLs supplied by campaign content.

### Shared LAN or self-hosted GPU

- **Recommendation:** accept an explicitly configured HTTPS/OpenAI-compatible endpoint only through
  Intelligence Gateway. Require authentication, a capability probe, deadline support, and an owner
  allowlist for model IDs.
- **Unknown:** OpenAI compatibility is not a complete behavioral standard. Structured-output fields,
  chat templates, token counting, cancellation, and error behavior vary between runtimes.
- **Consequence:** provider adapters still need conformance tests; the Gateway cannot simply pass
  arbitrary payloads through and call the result supported.

### Hosted provider

- **Recommendation:** defer a first-party hosted choice until the local evaluation corpus exists.
  Then test one cost-efficient pinned model and retain provider/model identifiers rather than a
  floating `latest` alias for recorded decisions.
- **Privacy consequence:** the UI must say that the side-safe observation, commander projection, and
  candidates leave the player's/server's machine. API keys belong in server-side secret storage,
  never campaign state or the browser. Provider retention and regional processing are deployment
  decisions, not properties of the protobuf contract.
- **Reliability consequence:** hosted availability or policy changes must never make a campaign
  unplayable. The deterministic fallback remains mandatory.

### Runtimes deferred from the initial product surface

- **Ollama:** useful user-managed compatibility target, but a second managed runtime would enlarge
  packaging, lifecycle, schema-conformance, and support work. Its endpoint can be evaluated through
  the custom mode before Sandtable manages it.
- **MLX LM:** attractive Apple-specific optimization and fine-tuning environment, but `llama.cpp`
  already treats Apple Silicon as a first-class backend. Add MLX only if measured latency or memory
  gains justify another adapter/runtime.
- **vLLM:** the right candidate for a shared GPU server and evaluation lab, not the minimum desktop
  runtime. Its structured-output and OpenAI-compatible features make it a strong custom/LAN target.
- **Browser/WebGPU inference:** defer. It would move model weights, memory pressure, compatibility,
  and potentially side-safe observations into the browser, complicating the trust and support model.

## Safety, fog-of-war, and replay consequences

### Authority

- A model receives legal candidates and returns a proposal. It never generates executable hex moves,
  supply arithmetic, combat results, legal actions, or authoritative events.
- Schema-constrained output is not trusted output. The Gateway and Umpire validate independently.
- A semantically bad but legal proposal is allowed to be a bad command decision; an illegal proposal
  is rejected and falls back.

### Fog of war and prompt injection

- Redaction occurs before Dispatch. A provider never receives the opponent's hidden state, even when
  hosted beside the authoritative service.
- Candidate and event text are untrusted data, not instructions. Prompt rendering must delimit them,
  cap their length, and prevent content packs or player-authored names from injecting new system rules.
- Provider-visible traces must avoid raw hidden state, secrets, and full prompt logging by default.
- Advisory memory, if later enabled, must be derived only from the same side-visible event projection,
  versioned, bounded, inspectable, and disposable. It is not a source of game truth.

### Personas and historical presentation

- The model sees the versioned effective profile and condition projection; it does not invent trait
  values or independently infer historical psychology.
- Narrative must be labeled generated, must not fabricate quotations, and must not turn profile
  tendencies into authoritative facts.
- Models and provider safety layers may flatten or refuse historical military personas. That is a
  quality/fallback condition, not a reason to weaken provider safeguards or the Umpire boundary.

### Replay and deduplication

- Fresh model generation is not deterministic and is never required for replay. Chronicle retains
  the accepted command plus the decision trace and response hash; replay resubmits recorded commands.
- A retry for the same `decision_id` returns the stored result when available. It must not sample a
  second answer after an accepted proposal.
- Record exact provider, model artifact/revision, quantization, runtime version, prompt-template
  version, persona version, sampling configuration, context caps, latency, validation/fallback reason,
  and response hash. Do not retain hidden chain-of-thought.

## Rejected or deferred approaches

| Approach | Decision | Reason |
| --- | --- | --- |
| Bundle a model inside the base installer | Reject initially | Large installer, license/notice coupling, stale weights, platform-specific runtime, and no consented removal path |
| Make a sub-1B model the default commander | Defer | Footprint is attractive; semantic and persona competence are unproven and vendor positioning is experimental |
| Support every Ollama/Hugging Face model | Reject | “Loads successfully” is not evidence of schema, safety, prompt-template, or persona conformance |
| Fine-tune historical personas first | Reject | Profiles and policy projection should prove themselves with prompting and scripted controls before creating weight provenance and distribution obligations |
| Let a large context hold campaign memory | Reject | Duplicates Chronicle, increases leaks/cost, and undermines explicit versioned advisory memory |
| Use a hosted model by default | Reject | Introduces accounts, cost, privacy, availability, and policy dependencies into an optional feature |
| Route by “routine/major” to different models immediately | Defer | Adds nondeterministic operational complexity before either tier has passed a common corpus |
| Expose model choice inside campaign rules | Reject | Model/provider is Intelligence configuration and trace metadata, not Umpire ruleset authority |
| Treat temperature zero as deterministic replay | Reject | Runtime/model/hardware changes can alter generation; replay relies on recorded accepted commands |

## Bounded evaluation spike

### Objective

Decide whether any lightweight model qualifies for `Local Balanced` or `Local Quality`, and select the
smallest quantization that meets Sandtable quality and hardware gates.

### Candidates and configurations

- Ministral 3 3B Instruct 2512: Q4_K_M and Q5_K_M.
- Ministral 3 8B Instruct 2512: Q4_K_M and Q5_K_M.
- Qwen3.5 2B and 4B: one supported 4/5-bit text-only local configuration each.
- Qwen3.5 9B: one supported 4/5-bit text-only configuration.
- Phi-4-mini-instruct: one 4/5-bit configuration as a text-only control.
- Scripted Command policy as the deterministic competence and persona baseline.

Model revisions, runtime builds, hashes, prompt versions, hardware, commands, and raw structured
results must be retained in an ignored or rights-safe evaluation workspace; aggregate results and the
decision belong in `docs/research/`.

### Corpus

Build 120 synthetic, rights-safe, side-redacted decision cases before evaluating models:

- 40 routine posture/logistics choices;
- 40 normal operational choices with meaningful trait trade-offs;
- 20 pressure/setback cases;
- 10 deliberately dominated-candidate traps; and
- 10 adversarial cases containing instruction-like text in names, facts, or recent events.

Project four synthetic personas (aggressive, deliberate, logistics-focused, pressure-sensitive) and a
neutral control. Mark a subset with expected preference direction rather than one supposedly perfect
historical answer. Include metamorphic pairs where hidden authoritative state changes but the side-safe
request must remain byte-identical.

### Run design

- Use the same versioned prompt, schema, context/output caps, candidates, and runtime adapter.
- Run plan selection with the model card's low-temperature recommendation, plus three repeated trials
  per case to characterize stability. Deduplication is disabled only inside the evaluation harness.
- Evaluate commentary separately from plan selection so creative sampling cannot affect decisions.
- Test at least one 16 GB Apple Silicon reference machine and one x86-64 CPU-only reference machine;
  add an 8-12 GB consumer GPU if that becomes a supported target.
- Run no network retrieval, no hidden state, no free-form tools, and no model-authored memory.

### Proposed pass gates

These are product gates to approve before execution, not claims that any candidate currently passes.

| Gate | Proposed threshold |
| --- | --- |
| Schema conformance | 100% parseable and schema-valid under constrained decoding |
| Legal identifier/parameters | 100% references to supplied plan IDs and allowed parameter domains before Umpire submission |
| Gateway/Umpire acceptance | At least 99.5%; every rejection produces a typed reason and scripted fallback |
| Dominated-plan avoidance | At least 98% across explicit trap cases; no persona is exempt |
| Persona direction | At least 80% of pre-labeled discriminating cases move in the expected direction versus neutral |
| Persona distinctiveness | At least three of four synthetic profiles have materially different aggregate choice distributions without failing the competence floor |
| Repeat stability | At least 90% identical plan selection across three low-temperature trials; variability is reported, never used for replay |
| Adversarial observations | 100% remain within schema and candidate set; no instruction-like observation changes system constraints |
| Fog metamorphic pairs | 100% byte-identical provider requests when only hidden state differs |
| Unsupported factual claims | No invented authoritative fact in decision rationale; generated flavor is separately labeled and reviewed |
| Fallback equivalence | 100% timeout/malformed/unavailable cases invoke the same effective-persona scripted policy |
| Latency | On the agreed minimum hardware, 3B-class p95 completes a normal decision within 10 seconds; 8/9B quality tier within 20 seconds |
| Memory/robustness | No OOM or process crash at 8K input, 256 output, one decision in flight on the advertised minimum hardware |

### Human review

Two reviewers should blind-rate a balanced sample for:

- adherence to the supplied profile rather than stereotypes;
- explanation grounded only in supplied facts and candidate attributes;
- useful differentiation without theatrical excess;
- concise, original, non-quotation narrative; and
- whether the 8/9B tier provides a player-visible improvement over 3/4B.

Disagreements and inter-reviewer agreement should be retained. A benchmark win cannot override a
systematic historical-persona or privacy defect.

### Stop conditions

- Stop evaluating a configuration after a confirmed schema/runtime incompatibility or repeated OOM
  on its intended tier; retain the failure evidence.
- Stop adding candidates when one balanced and one quality configuration pass all gates and the next
  candidate would not change license, hardware, or delivery decisions.
- If no model passes, ship scripted personas only. That is a complete and supported product state.

## Owner decisions requested

1. Approve or reject Ministral 3 3B/8B as the leading evaluation family, with Qwen3.5 and Phi-4-mini
   retained as challengers rather than launch promises.
2. Approve or reject `llama.cpp` as the sole first managed local runtime, while treating vLLM/Ollama/
   MLX as custom endpoint targets until evidence justifies more adapters.
3. Decide the minimum advertised local hardware, especially whether 16 GB system/unified memory is
   the baseline for `Local Balanced` and whether CPU-only latency is a support requirement.
4. Decide whether Sandtable will download verified weights on demand or only connect to a runtime the
   player installed. Do not bundle weights in the base installer under this recommendation.
5. Approve or defer a post-MVP hosted provider. If approved later, choose its privacy/retention regions,
   account/cost model, and exact version-pinning policy before implementation.
6. Approve the evaluation corpus and numeric gates before any named model appears in normal settings.

## Confidence, limitations, and unknowns

**Confidence: moderate** in the support-shape recommendation and **low-to-moderate** in the named
model ranking until execution.

High-confidence conclusions:

- the model must remain optional, non-authoritative, and replaceable;
- constrained output plus application/Umpire validation is required;
- local, LAN, and hosted delivery can share one narrow provider abstraction;
- long advertised contexts are unnecessary for plan selection;
- permissive licensing and first-party quantized artifacts materially simplify distribution;
- Sandtable-specific evaluation is the only defensible support gate.

Material limitations and unknowns:

- no candidate was downloaded or run;
- model cards and vendor benchmarks are interested sources;
- official cards do not evaluate Sandtable personas or redacted game decisions;
- exact throughput and working-set memory depend on runtime build, hardware, context, concurrency,
  and quantization;
- quantization may change persona consistency or plan competence in ways general benchmarks miss;
- hosted model names, availability, price, retention, and policies are time-sensitive;
- local model licenses and model cards can change, so the exact accepted artifact and license must be
  archived and reviewed at distribution time;
- the current protobuf projection is not the final strict persona/decision schema.

Evidence that would change the recommendation includes a challenger materially beating both
Ministral tiers on the retained corpus, a first-party runtime incompatibility, unacceptable historical
persona behavior, failure on common 16 GB machines, or new license/distribution obligations.

## Source index

All web sources were observed on 2026-08-16. Model and service facts are time-sensitive.

### Leading family and challengers

- [Mistral: Ministral 3 3B model card](https://docs.mistral.ai/models/model-cards/ministral-3-3b-25-12)
- [Mistral: Ministral 3 3B Instruct GGUF model card and files](https://huggingface.co/mistralai/Ministral-3-3B-Instruct-2512-GGUF)
- [Mistral: Ministral 3 8B Instruct GGUF files](https://huggingface.co/mistralai/Ministral-3-8B-Instruct-2512-GGUF/tree/main)
- [Mistral: Ministral 3 8B Instruct model card](https://huggingface.co/mistralai/Ministral-3-8B-Instruct-2512)
- [Qwen: Qwen3.5 0.8B model card](https://huggingface.co/Qwen/Qwen3.5-0.8B)
- [Qwen: Qwen3.5 2B model card](https://huggingface.co/Qwen/Qwen3.5-2B)
- [Qwen: Qwen3.5 4B model card](https://huggingface.co/Qwen/Qwen3.5-4B)
- [Qwen: Qwen3.5 9B model card](https://huggingface.co/Qwen/Qwen3.5-9B)
- [Microsoft: Phi-4-mini-instruct model card](https://huggingface.co/microsoft/Phi-4-mini-instruct)
- [Microsoft: Phi-4-mini-instruct MIT license](https://huggingface.co/microsoft/Phi-4-mini-instruct/blob/main/LICENSE)

### Deferred families and licenses

- [Google: Gemma 3 model card](https://ai.google.dev/gemma/docs/core/model_card_3)
- [Google: Gemma Terms of Use](https://ai.google.dev/gemma/terms)
- [Meta: Llama 3.2 3B Instruct model card and license](https://huggingface.co/meta-llama/Llama-3.2-3B-Instruct)

### Serving and structured output

- [`llama.cpp` repository, hardware backends, quantization, server, and grammar overview](https://github.com/ggml-org/llama.cpp)
- [`llama.cpp` JSON Schema and grammar documentation](https://github.com/ggml-org/llama.cpp/blob/master/grammars/README.md)
- [vLLM OpenAI-compatible server documentation](https://github.com/vllm-project/vllm/blob/main/docs/serving/online_serving/openai_compatible_server.md)
- [vLLM structured-output documentation](https://github.com/vllm-project/vllm/blob/main/docs/features/structured_outputs.md)
- [vLLM function-calling quality warning and strict schema guidance](https://github.com/vllm-project/vllm/blob/main/docs/features/tool_calling.md)
- [Mistral custom structured-output documentation](https://docs.mistral.ai/studio-api/conversations/structured-output)
- [Mistral deployment overview](https://docs.mistral.ai/inference/deployment)
- [MLX LM official repository](https://github.com/ml-explore/mlx-lm)

## Recommended next action

After owner approval, schedule the evaluation only after the versioned persona domain projection,
side-safe observation, legal candidate-plan surface, and deterministic scripted controller exist.
Those artifacts define the corpus and the safe fallback. Until then, keep the Gateway unavailable and
do not let a model integration become a dependency of pre-alpha gameplay.
