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

## Open questions — resolved against real campaigns, not theorized

Both items below were resolved by actually running the connector against real
campaigns over real HTTP (PR #542, and the follow-up Reserve Designation work),
not by further discussion. The resolutions turned out different from what was
originally guessed.

1. **Context tier for the per-request game-state suffix — RESOLVED, and
   differently than expected: there is no richer tier to pick between.**
   Checked directly against a real campaign: the Initiative Order opening's
   real visible cut is exactly `{contractVersion, campaignId, positionId,
   choices}` — no force counts, no positions, no enemy visibility. Checked a
   second decision type (Reserve Designation) and found the same shape
   (`choices` as `{kind, elementId}` objects, still no tactical data). Then
   checked whether any *other* real endpoint exposes a broader map/forces/
   contacts view — grepped every route in `CampaignDurableOutwardApi.cs`;
   the only other state-adjacent endpoint found (`source-counter-contacts`)
   is a pregame-setup admission endpoint, not a live map query. **There is
   currently no endpoint anywhere in the real outward API that exposes
   richer tactical state to any client, human or automated.** This was not
   a `FOW-001` scoping choice — it's that the outward API, as built and
   proven this session, was scoped narrowly to "can two players submit and
   receive the exact legal next decision," never "render a tactical picture
   of the battlefield." A real human player's own UI would hit the exact
   same wall today.

   Consequence for the harness: the original few-shot examples (written
   before this was checked) implied force-ratio data would always be
   present in the input. It won't be, for either decision type checked so
   far. That mismatch — not model size, not JSON formatting — is what
   caused the originally-reported hallucination (a 1.5B model, taught by
   example to expect comparable numbers, fabricated some when none were
   given). Fixed by rewriting both advisors' system prompts to state
   plainly that no situational force/position data will be present and to
   reason about the general structural tradeoff instead, with worked
   examples matching the real (minimal) input shape rather than a
   hypothetical richer one. Confirmed against three fresh real campaigns
   each for both decision types: zero fabricated facts in any of the six
   runs, a real, qualitative improvement over the original hallucination.

2. **Model quality bar — RESOLVED as "workable but genuinely fragile," not
   "good," at the 1.5B tier, and the harder finding is about the
   verification step's own reliability, not just the generator's.** Across
   3 fresh real Initiative Order runs: all 3 avoided fabricating facts
   (the original bug), but only 1 of 3 passed its own verification pass
   cleanly — the other 2 were flagged `Grounded: false` by the *verifier*,
   and reading them, at least one of those two flags looks like verifier
   noise rather than a real problem in the candidate. Across 3 fresh real
   Reserve Designation runs (a structurally harder decision — a
   variable-length choice list, not a fixed binary, with only one worked
   example instead of two): only 1 of 3 was internally coherent; the other
   2 showed genuine self-contradiction (one run picked "complete" while its
   own stated rationale argued for holding something in reserve — the
   opposite action). All 3 were correctly flagged `Grounded: false`.

   Reading across both decision types: **the generator's reliability
   degrades with decision complexity** (binary choice, well-anchored by two
   worked examples → mostly coherent; variable-length choice, anchored by
   only one example → frequently incoherent), and **the verifier itself
   (same 1.5B model) is noisy in both directions** — it caught every real
   problem in both batches (true positives), but also flagged at least one
   candidate that reads as fine (a plausible false positive). The system
   behaved safely either way — nothing incoherent would have reached a
   player unflagged — but "safe because it rejects a lot" and "actually
   reliable" are different claims. Concrete next steps, not yet done: (a)
   more worked examples for decision types with more structure (Reserve
   Designation's variable-length shape needs more than one anchor, the same
   lesson Initiative Order's two-example prompt already demonstrated); (b)
   consider an asymmetric model size — keep the cheap generator, use a
   larger model only for the verification call, since that call runs once
   per advice request either way and a false-positive-prone verifier
   defeats the point of having one; (c) escalate the generator's own model
   size only if (a) doesn't close the gap, per the design's own stated
   order.

## Live-session prototype — run for real, not just in xunit

Every verification so far ran inside xunit's own process, through a hand-built
`HttpClient` the test itself constructed. That client added a header
(`Sec-Fetch-Site: same-origin`) the real outward API's same-origin gate (#319)
requires from every caller. The connector's own shipped `Program.cs` never
added that header to its real `HttpClient` registration — meaning the actual
standalone service, as it would run in any real deployment, got a bare `403
Forbidden` on its very first call. This was never caught because nothing had
ever actually run the real service outside a test. Fixed by adding the same
header where the connector's own `OutwardApi` `HttpClient` is registered —
the connector doing, in its own runtime wiring, exactly what its test harness
already simulated it doing.

Caught by building `Cna.LocalCampaignSimulator` (new `src/` project): a small
dev-only tool that boots one real campaign — real file-backed journal, real
Orleans silo, real `MapDurableCampaignApi()` routes, the same production
classes the test harness already used, just packaged as a long-running
process instead of disposed at the end of a test — and keeps it listening on
a real local port. Pointing the real `Cna.RecommendationConnector` service
(via `dotnet run`, no test runner involved) at that address, then driving
both decision types with plain `curl`, is the first time this connector
actually ran as the standalone service it's meant to be, against a live
campaign, end to end:

- `POST /advise/initiative-order` → real `act-last` recommendation, grounded
  rationale text, `Grounded: false` (verifier-noise, same documented pattern).
- Submitted the real `act-first` action over the real outward API to advance
  the campaign (this connector itself never does this — a human/UI would).
- `POST /advise/reserve-designation` → real `complete` recommendation against
  a genuinely different opening.

Also added a second few-shot example to `ReserveDesignationAdvisor`'s system
prompt (a 3-option case, distinct answer, explicit "the real list varies,
re-read it" instruction) — the concrete next step (a) from the section above.
Re-verified 3x against fresh real campaigns: 3/3 internally coherent (up from
1/3 before), closing the self-contradiction gap without needing a larger
model. Verifier noise (2/3 flagged `Grounded: false` on reads that look
fine) persists and is unaffected by this change, as expected — it's a
property of the single-model verifier, not the generator's examples.
Deliberately did **not** add a second example to `InitiativeOrderAdvisor`:
its real input never varies (always the same four-field shape), so a second
example with a different answer for an identical input would teach
inconsistency, not generalization — one example per genuinely-constant input
is the right size, not under-provisioned.

Not done: the asymmetric-verifier-model idea (b) from the section above —
still a real next step, just not this increment's.

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
