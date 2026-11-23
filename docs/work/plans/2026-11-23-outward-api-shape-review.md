# Outward HTTP API shape review (2026-11-23)

**Status:** direction document for owner review and sync — no code in this pass.
Checked directly against `src/Cna.OrleansHost/Api/CampaignDurableOutwardApi.cs` (957
lines) and `CampaignDurableAuthorityJournal.Openings.cs`/`.Submissions.cs` on current
main, cross-referenced against
[`2026-11-22-outward-http-api-reference.md`](2026-11-22-outward-http-api-reference.md)
(PR #506's endpoint-by-endpoint reference, which this doc does not duplicate).

## Headline verdict

**The core shape is sound. One real structural problem exists, and it's not the one
you'd guess from the enum size.** The 43-member `CampaignOwnerOpeningKind` enum and
the generic `/openings`+`/actions` RPC pair are *not* the shape problem — switching to
per-resource routes would make things worse, not better, given this project's actual
growth rate (one new kind per MTS slice, heading toward Stage 3 and Turns 2-6). The
real problem is the **error model**: dozens of genuinely distinct internal rejection
reasons collapse into one indistinguishable HTTP code, and the codebase's own comments
already admit this. That's Recommendation 1 below, and it's worth fixing before the
kind count doubles again.

Also worth naming up front: **this API has no external clients yet.** Every route
under `/api/v2/campaigns` is gated to `Environment.IsDevelopment()` plus
loopback/same-origin (`CampaignDurableOutwardApi.cs:23-48`). "Real HTTP player" this
session has always meant "a real HTTP request through this code path," never "reachable
over the public internet." This matters for prioritization: versioning strategy, rate
limiting, and public-API observability are *not* urgent — there is no third-party
consumer to protect against or support yet, only this project's own future UI. Several
things a generic "review your API" pass would flag don't apply here, and this doc says
so rather than inventing urgency for them.

## 1. Error model — the real problem, ranked first

**Finding, directly confirmed in code:** `CampaignOwnerSubmissionResult` has exactly
three states — `Accepted(receipt)`, `Rejected()` (no code), and `RuleNotImplemented()`
(`CampaignDurableAuthorityJournal.Submissions.cs:875-898`). Every validation failure
inside `SubmitOwnerActionCore` — wrong seat epoch, wrong audience, stale
`openingRevision`, already-applied action, schema-version mismatch, malformed public
reference, wrong `actingElementId`, and more — calls the same parameterless
`Rejected()` (confirmed by direct grep: 20+ call sites across
`Submissions.cs:255-582`, every one identical). The outward API then turns that into
HTTP 409 `{contractVersion: 2, code: "unavailable"}}` regardless of which of those 20+
reasons actually fired (`CampaignDurableOutwardApi.cs:826`). **The codebase's own
comment at `Submissions.cs:318` already names this**: "falls through to the generic,
indistinguishable `CampaignOwnerSubmissionResult.Rejected()`." This is not a
theoretical concern — it is a documented, acknowledged collapse point.

**Why this matters concretely:** a real client (the project's own future UI, or a bot)
cannot distinguish, from the wire response alone:
- "Resubmit with a fresh `openingRevision`" (stale revision — recoverable, re-query
  and retry) vs.
- "Your credential is wrong or expired" (should re-authenticate, not retry) vs.
- "This game state will never accept this action" (a real rule violation — show the
  player an error, don't retry) vs.
- "You already submitted this, here's effectively a no-op" (idempotent replay case —
  arguably should be 200, not 409, but currently indistinguishable from the above)

All four currently look identical: HTTP 409, `code: "unavailable"`. A client has no
way to build correct retry/UI-feedback logic without reading `Submissions.cs`'s
internals directly, which defeats the point of having a stable wire contract.

**Recommendation:** give `CampaignOwnerSubmissionResult.Rejected()` an optional reason
code, the same shape `RuleNotImplemented()` already has
(`RuleNotImplementedCode = "rule-not-implemented"` is the existing precedent — just
generalize it). Concretely:

```csharp
internal static CampaignOwnerSubmissionResult Rejected(string reasonCode) =>
    new(null, reasonCode);

// at each of the 20+ call sites, name the actual reason instead of calling the
// parameterless overload:
internal const string StaleRevisionCode = "stale-revision";
internal const string AlreadyAppliedCode = "already-applied";
internal const string WrongAudienceCode = "wrong-audience";
internal const string InvalidCredentialCode = "invalid-credential";
// etc. — audit each of the 20+ current Rejected() call sites in Submissions.cs
// and assign the real reason; don't invent new categories speculatively, only name
// what's actually there today.
```

This is additive and backward-compatible: existing clients checking `code ===
"unavailable"` as their catch-all keep working (the new codes are more specific
siblings, "unavailable" can remain the fallback for anything not yet categorized), and
no wire shape changes (still `{contractVersion, code}`). **Zero risk to `EVT-001`** —
this is HTTP response shaping, not event serialization. Low effort (an afternoon's
mechanical pass through 20+ call sites, each with an obvious answer from its
surrounding `if`), high leverage (every future client interaction benefits), and gets
*more* valuable as the kind count grows, not less. This is the single highest-priority
item in this review.

## 2. A real, immediately-actionable duplication at this exact layer

While reading the API layer directly (not assumed from the doc's prior summary): the
`/openings` handler (`CampaignDurableOutwardApi.cs:668-713`) and the `/actions` handler
(`:745-790`) each contain the **same 23-branch, copy-pasted
`if (stage == "x" && element != "y") throw new JsonException(...)` block**, verifying
pregame/source opening keys. Confirmed by direct line-by-line comparison — the two
blocks are identical except for which handler they sit in. This is the exact same
disease class this session already fixed at the Host-layer dispatch (PR #525) and the
`CampaignExercises`/`ExerciseSession` layer (the overhaul doc's Section 0) — just one
layer further out, at the HTTP boundary itself, and apparently missed by the earlier
registry refactor because it's a validation block, not a dispatch branch.

**Recommendation:** extract one shared `RequireKnownSourceElement(string stage, string
element)` helper, called from both handlers. Mechanical, zero behavior change,
~46 lines collapse to ~25. Small, but worth doing in the same pass as Recommendation 1
since it's the same file and the same kind of fix.

## 3. Resource shape: generic RPC pair is the right call, not a problem

Investigated directly: is "one generic `/openings`+`/actions` pair keyed by a
43-member enum" the wrong shape, versus dedicated routes per decision type (e.g.
`POST /{campaignId}/movement`, `POST /{campaignId}/reserve-designation`, ...)? **No —
the generic shape is correct for this project's actual trajectory, and switching would
make things worse.** Evidence: the enum has grown by one member almost every MTS slice
this session (Reserve, Movement, Breakdown, now Combat Decision — 4 new kinds in one
day), heading toward Stage 3 and Turns 2-6, each turn potentially repeating the same
mechanic set. Dedicated per-resource routes would mean a new route registration *and*
wiring to add alongside every new kind, instead of one enum member plus one registry
entry (already collapsed to this shape by `#525`). The generic pair is the right
abstraction for "the set of decision types grows continuously and roughly linearly
with game-content work" — resource-oriented routing is the right default for APIs
whose resource set is small and stable, which this is explicitly not, by design, for
the foreseeable future of this project.

**What *is* a real gap, and the right fix for the actual underlying complaint**
(not "wrong shape," but "how does a client know what exists"): there is **no
discovery mechanism at all**. `stage` is validated only by exclusion — a hardcoded
disjunction of ~45 known strings (`CampaignDurableOutwardApi.cs:799-814`); there is no
endpoint that lists valid stages, describes what shape each one's payload/response
takes, or tells a client "given this seat, what's the next legal action." A new UI or
bot integrator today has exactly one way to learn the contract: read source or the
hand-maintained reference doc (`2026-11-22-outward-http-api-reference.md`). That's
workable for this team right now (one UI, built by people with repo access) but is a
real gap the moment a second consumer (an external bot, a contributor without full
context) needs to integrate.

**Recommendation, scoped honestly to what's actually needed:** not a full OpenAPI spec
(no `Swashbuckle`/`Microsoft.AspNetCore.OpenApi` reference exists anywhere in
`src/Cna.OrleansHost` today — confirmed by grep — and generating one for 45+
hand-rolled minimal-JSON endpoints would be a large, separate effort for a benefit this
project doesn't need yet, given zero external consumers). Instead: a single new
read-only endpoint, `GET /{campaignId}/openings/{stage}/schema` or similar, that
returns which audience(s) can currently reach that stage and whether it's
Stable/Experimental/Omit per the existing #506 classification — i.e., make the
*existing, hand-maintained reference doc's own table* queryable at runtime instead of
only living in a markdown file. This is a small, scoped, genuinely useful piece of
expansion — not "build a generic API discovery framework."

## 4. Idempotency contract: mechanism is sound, documentation is missing

The retry/idempotency key (`openingRevision` + `publicActionRef`, echoed on
`/actions`) is real and tested — proven by this session's own `#515` fix and its test
suite, plus the pre-existing cold-replay tests cited throughout the #506 reference doc.
**No new mechanism is needed.** The gap is purely documentation: nothing tells an
external client "it is safe to retry the exact same `/actions` body on a network
timeout" as part of the API contract — that behavior is only knowable by reading
`Submissions.cs`'s retry predicate. Low-priority, low-effort fix: add one paragraph to
the existing outward-API reference doc stating the retry contract explicitly. Not
worth its own implementation effort; fold into whichever PR next touches that
reference doc.

## 5. Versioning: no real problem yet, correctly not urgent

`contractVersion` is checked for hard equality to `2`
(`CampaignDurableOutwardApi.cs:878-881`); there is no negotiation, no deprecation
path, no parallel-version routing. **This is fine as-is.** With zero external
consumers and the entire surface gated to dev-only/loopback (Headline Verdict above),
a `v3` migration today would mean editing this one file's route registrations — there
is nothing yet to migrate *away from* in a way that breaks anyone. Revisit this
question only when the project actually ships a non-loopback deployment or a non-owned
client starts depending on `v2` — not before. Flagging this as "checked, genuinely not
a problem" rather than skipping it, per the review's own instruction not to invent
issues.

## 6. Observability: one real, cheap gap

**Finding:** the `Error(status, code)` helper (`CampaignDurableOutwardApi.cs:955-956`)
returns `{contractVersion, code}` with no correlation/request identifier of any kind —
confirmed by grep across the whole file for `TraceIdentifier`/`correlationId`/
`X-Request-Id`/similar: zero hits. If a client reports "my request failed," there is
nothing in the response to correlate against server-side logs.

**Recommendation:** add `HttpContext.TraceIdentifier` (ASP.NET Core already generates
one per request, no new infrastructure) to the error envelope:
`{contractVersion, code, requestId = context.TraceIdentifier}`. One-line change per
error path, genuinely free (the value already exists), real support/debugging value.
Small enough to bundle with Recommendation 1's pass through the same error-handling
code.

No rate limiting exists anywhere in this file — also genuinely fine for the same
reason as Section 5 (no external consumers to protect against yet).

## 7. Missing capabilities for real external/UI use — ground-truthed against actual need

Checked each candidate against whether this project's *stated* goals (two real HTTP
players now, a full 6-turn playable game eventually) actually need it, not generic API
best-practice:

- **Campaign listing (`GET /campaigns` or similar) — real, scoped gap.** Confirmed: the
  only campaign-creation route is `POST /` (`:578`); there is no `GET` that lists
  existing campaigns. A human player opening a UI has no way to see "my games" without
  the UI maintaining its own out-of-band list of campaign IDs. This is a genuine,
  small, well-justified expansion once a real UI exists — not urgent today (no UI
  exists yet either), but worth having on the list for whenever UI work starts, rather
  than discovered as a surprise gap then.
- **Spectator/replay export — not needed yet, correctly out of scope.** Nothing in this
  project's stated goals mentions spectating or replay sharing as a near-term need;
  the replay/determinism machinery exists for *verification* (cold-replay tests), not
  as a product feature. Don't build this speculatively.
- **Batch operations — not needed.** Every gameplay action is already one decision at
  a time, matching this project's own UX-001 invariant ("automation removes
  bookkeeping, never a real decision point") — batching real decisions would cut
  against that invariant, not serve it.
- **WebSocket/SSE for push notifications — not needed, poll is the right fit.** This is
  a turn-based wargame with human decision points on both sides, not a real-time
  game; the existing poll-the-opening-endpoint pattern is already how both players'
  HTTP clients work in every test this session has built, and nothing about latency
  tolerance here resembles a real-time-push use case. Don't build this.

## Sequencing

**Can start now, zero collision with any in-flight work:** Recommendations 1
(error-code granularity), 2 (the `/openings`/`/actions` validation-block dedup), and 6
(`requestId` in error responses) are all scoped to
`CampaignDurableOutwardApi.cs`/`CampaignDurableAuthorityJournal.Submissions.cs`'s
error-handling paths specifically — distinct from both the in-flight MTS Core work
(`CampaignExercises.cs`/`ExerciseSession.cs`) and the just-landed Host-layer registry
refactor (`#525`, which touched the *dispatch* logic in these same files, not the
*rejection-reason* logic this touches). Worth doing together as one PR — same files,
same theme, genuinely complementary.

**Small, low-priority, can go whenever:** Recommendation 4's one-paragraph
documentation addition to the existing reference doc.

**Worth scoping later, not urgent, no implementation yet:** Recommendation 3's
schema/discovery endpoint — real value, but genuinely not blocking anything today
(this team is still the only consumer) and deserves its own short design pass (what
exactly does "schema" mean per stage — payload shape? legal-audience set? both?)
before implementation, the same way other pieces of this session's work got a design
doc before code.

**Correctly not scheduled at all:** versioning strategy (Section 5), rate
limiting/public observability beyond `requestId` (Section 6), spectator export, batch
operations, push notifications (Section 7) — each checked directly against this
project's actual stated goals and found to be premature or inapplicable, not simply
unexamined.
