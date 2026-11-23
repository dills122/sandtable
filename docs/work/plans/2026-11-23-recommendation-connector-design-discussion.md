# Smart recommendation connector — design discussion (2026-11-23)

**Status:** hashed-out-so-far record, not a final spec. Captures a design
conversation, not an implementation plan — nothing below is built yet.
Two items are explicitly unresolved and need a small empirical check before
committing to a direction (marked below).

## The idea

A connector that uses a local LLM to give a Sandtable player advice during
a real game session — e.g. "given my current legal actions, what looks
strongest and why." Purely advisory: it must never become part of the
authoritative game state.

## Hard constraints this design leans on

Sandtable is a deterministic-replay engine with hard invariants (`DET-001`,
`DET-002`, `EVT-001`, `REL-001`, `FOW-001`, `FID-002`, `UX-001`). The
connector's entire safety story rests on being a **pure, read-only consumer**
of the real outward HTTP API — the same one a third-party client would use —
with zero new authoritative surface:

- It never writes to the journal. No event it produces is ever accepted as
  canonical. This makes it zero-risk to `DET-001`/`DET-002`/`EVT-001` by
  construction, not by discipline.
- It only ever sees what the player's own client already legitimately sees
  (their own visible state, via the same query endpoints a real client
  calls). This keeps `FOW-001` intact by construction, not by review.
- The LLM's own output is non-deterministic, and that's fine — `DET-001`/
  `DET-002` govern the *game's* state evolution, not advisory tooling that
  never writes back. Worth stating explicitly so it isn't mistaken for a
  gap later.

## Decisions made

1. **Architecture: standalone microservice.** Not a sidecar with direct
   DB/journal access, not baked into the game host. It talks to the real
   outward HTTP API over its own communication channel, exactly like any
   other client would (and incidentally stress-tests the Stable/Experimental
   API reference from #506 and the discovery endpoint from #534 as real
   external-consumer surface).
2. **UX: pull-only, user-requested.** An explicit "ask for advice" action
   the player triggers. No proactive/push surfacing for now, until the
   functionality itself is hashed out further. This sidesteps a real
   `UX-001` tension rather than resolving it by review each time: `UX-001`
   isn't just "don't add bookkeeping," it's "don't remove a real decision
   point" — an always-on advisor risks automating the decision itself, not
   just the tedium around it. Pull-only avoids that question outright.
3. **Model direction: as small/cheap as possible, escalate only if bad.**
   No GPU — quantized (GGUF, 4-bit) small models run fine on CPU-only
   containers for this use case (human-paced, "ask for advice" button, a
   few seconds of latency is fine). Rough sizing: a 3B quantized model wants
   ~4-6GB RAM; 1-1.5B wants ~2-3GB. A small CPU container (2-4 vCPU) is
   plausible for low request volume — **this assumption needs confirming
   against real expected concurrent usage before locking in hosting cost.**
   - Candidate ladder, smallest first: Qwen2.5 0.5B (quick sanity check,
     honestly expected to be too weak) → **Qwen2.5 1.5B or Llama 3.2 1B
     (the real starting candidate)** → Gemma 2 2B / Phi-3.5-mini / Qwen2.5
     3B (escalate only if the 1-1.5B class genuinely fails).
   - Structural lever that matters more than model size: **constrain the
     output space.** Don't let the model free-form suggest actions — have
     it rank/annotate the literal legal-action list the API already
     returned. This removes the main small-model failure mode (hallucinating
     an action that doesn't exist) without needing a bigger model to
     compensate.
   - Tuning order: prompt engineering first (cheap, fast to iterate) before
     any fine-tuning investment (real infra/data-generation cost). Only
     reach for fine-tuning if prompt-only quality is genuinely bad.
4. **Static harness context, dynamic game-state suffix.** The game's own
   rules/mechanics never change per request, so they belong in one
   well-tuned static system prompt built once (move-legality shapes, how to
   read the state format, output constraints like "only reference actions
   by their exact id string") — not re-derived or re-taught every call.
   Per-request content is then just the live game-state parts. This also
   lowers the bar for a small model: it doesn't need to understand wargames
   cold each request, just follow a fixed instruction set it's effectively
   already been taught. (Future optimization, not needed yet: llama.cpp/
   Ollama can reuse the KV-cache for a repeated static prefix, so a fixed
   system prompt + varying suffix costs less compute than reprocessing the
   whole prompt fresh each time.)

5. **Harness/eval split: separate the runtime harness from offline tuning
   tooling.** Pushback worth recording: Ollama's native schema-constrained
   `format` field and a typed client (OllamaSharp/`Microsoft.Extensions.AI`)
   only guarantee output *shape*, not that the advice is actually good —
   that's a lesson already learned the hard way on a prior OpenRouter-based
   code-reviewer project, where real usefulness only showed up once a real
   harness was built around the raw model calls. Locking in the split that
   avoids re-learning that lesson here:
   - **Runtime harness (ships in production, stays in C#):** no framework
     needed. A real harness still means more than one call — generate
     candidate(s), then a separate verification pass that checks the
     recommendation against the real legal-action list and state for *this*
     specific request, not just schema validity. Plus: curated/templated
     context (a compact summary, not a raw JSON dump — small models parse
     templated text better than verbose structured JSON) and concrete
     few-shot examples baked into the static system prompt, not just rules
     explained once. This is plain sequential OllamaSharp calls, no new
     dependency.
   - **Offline tuning/eval tooling (development-time only, never shipped,
     doesn't need to be .NET):** this is where the actual "does the advice
     work" signal comes from — a small set of real decision-point scenarios
     with a reference for what a good answer looks like, run against the
     harness, iterated on. Real options found, not yet chosen between:
     [Promptfoo](https://braintrust.dev/articles/best-promptfoo-alternatives-2026)
     (CLI/YAML, low setup, straightforward test-case-against-model
     comparison) or [DSPy](https://statsig.com/perspectives/dspy-compilers-prompt-optimization)
     (goes further — automatically searches for better prompts/few-shot
     examples against a metric, e.g. MIPROv2/GEPA; real production results
     cited, but Python with its own signature/module abstraction, a real
     learning curve). A third option that needs no new tool at all: write
     the eval set as fixtures in the existing xunit suite, same shape as
     everything else in this repo's test culture — lower adoption cost,
     no automatic prompt-search.
   - Explicitly rejected for this: Outlines (constrained-decoding library)
     — Ollama's native `format` field already covers the one thing it would
     add, and its own Ollama backend support is disputed/unclear across
     sources; LangChain/Semantic Kernel/other heavy agent frameworks — none
     buy anything for a task this narrow that a two-call harness doesn't
     already cover.

6. **v1 scenario/decision scope: Initiative Order only.** Deliberately the
   simplest decision in the game, not just "a small handful" — prove the
   whole harness/eval loop on one binary decision before asking it to
   reason about anything spatial (Movement) or combat-adjacent (which this
   session already found splits into real sub-complexity). Scoped to Stage
   1 specifically (not Stage 2), since Stage 1's chain is the one part of
   this system that's fully proven and not a moving target — Stage 2's own
   generalization work was still landing in the main session as this
   decision was made.

   Mechanically verified against the real ruleset (not assumed) before
   locking this in, because the obvious worry — "is this just a fixed
   doctrinal answer with no real per-state differentiation" — turned out to
   have a real answer:
   - The dice contest that decides *who holds initiative* is fully
     automatic (no player choice) and genuinely varies: the seed is
     caller-supplied per campaign (`CampaignDurableOutwardApi.cs`'s create
     endpoints require a `seed` field, not a fixed constant), and even
     within one seed, each side's roll gets a rating bonus that moves as
     the campaign plays out — Axis's bonus depends on their actual force
     disposition (`Cna1979InitiativeRatings.ClassifyAxisPresence`),
     Commonwealth's depends purely on game turn
     (`Cna1979InitiativeRatings.GetCommonwealth(gameTurn)`). Real, structural
     per-side asymmetry, not a coin flip.
   - The actual decision — Act First vs. Act Last — belongs only to
     whichever side *wins* that contest (`InitiativeResolver.Resolve`'s
     `holder`). The loser has no choice at this point at all. This is the
     advisor's entire scope for this slice: given the state at the moment
     the winner is asked to choose (their own position, whatever is
     visible of the opponent, however the roll happened to land), which of
     the two legal actions is the better call. The advisor never touches
     or predicts the roll itself — that's already a resolved, committed
     fact by the time there's anything to query.
   - The tradeoff is the classic tempo-vs-information one: Act First commits
     your movement/combat before the opponent acts, trading information for
     tempo; Act Last lets you react to everything the opponent commits to,
     trading tempo for information. Real per-state differentiation (strong
     local position → press with tempo; uncertain disposition → wait for
     information), not a fixed maxim.

## Open questions — need an empirical check, not more theorizing

1. **Context tier for the per-request game-state suffix.** Three tiers
   considered:
   - Minimal: just the current `QueryOpening` response (legal actions only).
   - Current full visible state + pending decision (everything the
     player's own client already renders — still 100% `FOW-001`-safe since
     it's identical to what they already see).
   - Historical/replay trend context (attrition patterns, momentum) — needs
     its own `FOW-001`-scoped history query surface, which may not exist
     yet; **not investigated**.

   This tension cuts against decision 3 above (cheap/small model, low
   compute): richer context costs more tokens per request even on cheap
   hardware, and a small model may not meaningfully use extra context
   anyway. **Proposed next step (not yet run):** a small empirical test —
   same model, a handful of real decision points, tier-1 vs. tier-2 context
   — to see whether advice quality actually improves or whether the small
   model can't use the extra context well regardless. Defer tier-3 until
   there's a concrete reason to build a new history-exposure endpoint for it.

2. **Model quality bar.** Not yet tested against anything. The ladder above
   is a starting plan, not a result.

## Boundary set now, not deferred

No "admin/spectator" mode that lets the connector see hidden state for
debugging purposes, ever — even an operator-only backdoor erodes the "pure
consumer of the player's own visible API" guarantee the whole design leans
on. If an admin debugging need comes up later, it gets its own separate
tooling, not a privileged path through this connector.

## Not yet decided / not discussed

- Exact deployment/hosting provider and real cost figures (depends on
  confirming the concurrency assumption above).
- Whether advice responses are cached, and on what key, if at all (purely
  a cost/latency optimization, not a correctness question, given the LLM's
  non-determinism is already a non-issue per the constraints section).
- Fairness implications if only some players use the advisor (a game-design
  question, not an architecture one — flagged, not resolved).
