# User-Space Declassification Boundary Enforcement

**Status:** Accepted; ZOC/Reaction boundary implemented, repository-wide rollout remains staged

**Date:** 2026-08-31

**Decision owner:** Project owner / architecture maintainer

**Affected capabilities:** Campaign Observation, Legal Actions, projected history, ZOC/Reaction,
Maproom, hosting, and every later player- or model-facing adapter

## Executive conclusion

Sandtable should treat every authority-to-user transition as an explicit declassification operation,
not as ordinary DTO mapping. The security property applies to the complete transcript available to a
persistent audience, not to one serialized object in isolation.

The recommended enforcement model has five mutually reinforcing gates:

1. a machine-readable disclosure manifest for every outward contract and audience;
2. a separate outward-contract assembly that cannot reference authoritative Core types;
3. audience-specific value shapes whose constructors and readers make forbidden combinations
   unrepresentable;
4. build-breaking analyzers and API/reference-graph checks; and
5. multi-state, multi-output noninterference tests over observations, legal actions, receipts,
   projected history, and rejection results.

No one gate is sufficient. Assembly and analyzer rules prevent direct structural leaks. Strict
admission prevents crafted invalid combinations. Transcript tests are the required backstop for
semantic, inferential, and cross-state leaks that type systems and analyzers cannot prove absent.

For ZOC/Reaction, the owner selected binding secrecy. `ZOR-TASK-004C` replaces the exact
element-derived Movement tuple beside a stable representation identity with closed current
move-option/cost capabilities. It also adds strict reacting semantic admission, a versioned
disclosure manifest, retained-transcript regression evidence, and a mandatory boundary gate before
Task 005.

## Decision question and scope

> How should Sandtable make it mechanically difficult to disclose any fact, identity, correlation,
> or inference that has not been explicitly approved for a user audience?

This matters now because the third independent `ZOR-TASK-004B` review reproduced a class of leak that
ordinary strict serialization and same-object fog tests did not catch: two individually approved-
looking payloads formed a prohibited join across states.

In scope:

- Core observation, legal-action, receipt, projected-history, and rejection outputs;
- public API and assembly dependency direction;
- construction, canonical serialization, and strict readback;
- player, System, and later Intelligence/Maproom audience separation;
- compile-time, test-time, and CI enforcement; and
- the immediate ZOC/Reaction consequence.

Originally out of scope for the investigation packet:

- activating dormant Observation 6 or the ZOC/Reaction runtime;
- completing the repository-wide contract-assembly and Roslyn-analyzer migration in the bounded
  ZOR remediation checkpoint;
- defining authentication, transport encryption, or hosted authorization;
- logging/telemetry implementation beyond identifying it as an outward sink; and
- relaxing any existing fog requirement without explicit owner approval.

## Required threat model

The default player-space threat model should assume that one audience can:

- retain every byte it has ever received;
- correlate observations, action sets, receipts, histories, rejection codes, and visible timing
  across the whole campaign;
- know the rules and all previously disclosed own/public facts;
- choose submissions adaptively based on prior outputs;
- compare canonical and semantic values; and
- perform arbitrary joins over stable identifiers and exact/quasi-identifying tuples.

System-authority output is not a player audience. Cross-player collusion needs an explicit policy:
the recommended default is to prove noninterference per authenticated principal and never expose the
System audience to a player principal. If the product must resist two opposing players pooling their
transcripts, that stronger property must be declared separately because physical play normally gives
each side different legitimate knowledge.

### Formal security property

For audience `A`, let `Visible_A` be the approved declassification policy and let `Out_A(trace)` be
the complete ordered byte transcript delivered to that audience.

Two authoritative traces are audience-equivalent when every difference between them is outside
`Visible_A`, including identities and correlations not expressly approved. For the same sequence of
audience-visible choices:

```text
trace1 ≡Visible_A trace2  =>  Out_A(trace1) == Out_A(trace2)
```

Equality is exact canonical-byte equality over the complete transcript. A policy may deliberately
allow a delta, but the manifest must name the exact field, identity scope, lifetime, and inference
that is approved. There is no implicit exemption for values that are individually owner-visible.

## Evidence and current repository baseline

### Documented facts

- The approved observation policy already requires whole-payload noninterference for paired
  authorities and forbids real bindings (`OBS-006`, `OBS-010`, `OBS-013`, and `OBS-AC-004` in
  [Campaign Observation v1](../specs/campaign-observation-v1.md)).
- The Legal Actions design already defines an opaque authority handle, observation-derived side
  actions, payload-free submissions, and exact current-membership revalidation
  ([Legal Actions v1](../design/legal-actions-v1.md#delivery-shape)).
- .NET assemblies enforce `internal` visibility, while `InternalsVisibleTo` grants the named friend
  assembly access to all ordinary internal members. This makes assembly separation useful, but a
  broad production friend relationship should remain exceptional
  ([Microsoft documentation](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.internalsvisibletoattribute)).
- Roslyn supports custom source analyzers that emit diagnostics during development and build
  ([Microsoft tutorial](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix)).
- Roslyn's banned-API analyzer provides build diagnostics for forbidden symbols, and its restricted-
  internals rule demonstrates namespace-scoped friend enforcement
  ([Roslyn analyzer rules](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers.md)).
- Analyzer severities can be made build-breaking; this repository already sets
  `TreatWarningsAsErrors=true` and `AnalysisLevel=latest-recommended`
  ([.NET code-analysis overview](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview)).

### Repository observations

- `CampaignAuthorityHandle` is a public opaque shell whose snapshot and context are internal.
- `CampaignSnapshot`, commands, events, replay machinery, action execution, and exact context are
  internal. `AuthorityBoundaryTests` checks this public surface and permits only tests as Core's
  friend assembly.
- Only `Cna.OrleansHost` and `Cna.ExerciseRunner` production projects reference `Cna.Core`.
- Public observations, actions, and their serializers currently live in the same assembly as
  authority. This permits disciplined code but gives the compiler no module-level distinction
  between a declassified scalar and a copied authoritative scalar.
- Existing privacy tests are strong against direct leaks: paired authorities, hidden canaries,
  byte equality, approved-delta comparisons, prohibited-type graph inspection, and reference-graph
  checks are already present.
- Existing tests primarily compare isolated observations or the same checkpoint across paired
  authorities. They do not define one registry of all outward sinks or compare complete adaptive
  audience transcripts across state transitions.
- Canonical readers prove exact shape/order and reserialization, but audience-specific semantic
  invariants remain distributed among constructors and projectors. A projector can omit a forbidden
  collection while the value constructor and reader still admit it.
- No custom repository analyzer or machine-readable disclosure registry currently exists.
- `just check` runs format, build, and tests, so a boundary-specific analyzer and test project can be
  made mandatory without a parallel CI mechanism.

### Inferences

- The current boundary prevents accidental authority-object exposure better than most ordinary DTO
  layers, but it relies too heavily on expert review to recognize semantic correlations.
- A separate contract assembly will prevent authority-type dependencies and accidental object-graph
  retention, but it cannot determine whether two strings or numeric tuples jointly identify a
  secret.
- Attributes and analyzers can require every outward member to carry a reviewed disclosure record,
  but they cannot prove noninterference of arbitrary program semantics.
- Exact transcript comparison over paired authority traces is the strongest practical repository
  evidence for temporal noninterference. Carefully chosen canaries and quasi-identifier fixtures are
  still needed because no finite test suite proves universal information-flow security.
- Publishing already-derived choices is often safer than publishing all inputs needed to rederive
  those choices in user space. Submission safety remains authoritative because action IDs are
  revalidated against exact current membership.

### Unknowns requiring owner decisions

- Whether an own map representation's binding to an own element is actually intended to remain
  secret. Current ZOC requirements say real bindings remain internal, while the older observation
  policy declares exact own-element state visible. This must be reconciled explicitly.
- Whether any principal may access multiple player audiences or the System audience.
- Which rejection timing, diagnostics, logs, traces, and support artifacts will cross the hosted
  boundary.
- Whether public ruleset/scenario identifiers may themselves fingerprint hidden scenario content in
  future non-synthetic packages.

## Failure classes the boundary must stop

| Failure class | Example | Required gate |
| --- | --- | --- |
| Direct authority leak | Snapshot, event, binding object, hidden ID | Assembly graph, prohibited-type analyzer, API test |
| Structural field leak | `elementId` added to a reacting opportunity | Manifest, audience-specific type, schema/API golden |
| Invalid cross-field shape | Reacting state plus identity-bearing root owner rows | Constructor/admission invariant, negative readback test |
| Same-payload correlation | Representation tuple joined to element tuple | Manifest correlation rule, paired fixture |
| Cross-state correlation | State-N element fingerprint joined to state-N+1 representation | Transcript noninterference and temporal join fixture |
| Cross-output correlation | Observation joined to action, receipt, history, or error | Complete output registry and transcript comparison |
| Hidden-state oracle | Membership/rejection/count changes reveal an excluded fact | Paired adaptive traces and stable coarse rejection |
| Future adapter bypass | Host, Maproom, model prompt, log, or telemetry serializes authority | Contracts-only dependency, banned APIs, sink registry |

## Options compared

| Option | Structural enforcement | Semantic/temporal enforcement | Cost | Decision |
| --- | --- | --- | --- | --- |
| Continue conventions plus review | Low | Low | Low | Reject; this produced the repeated review loop |
| Add more isolated serializer/privacy tests | Medium | Low-medium | Low | Necessary but insufficient |
| Attributes and analyzer in the existing Core assembly | Medium-high | Low | Medium | Useful transition state, not the final boundary |
| Separate public-contract assembly plus analyzer | High | Low-medium | Medium-high | Required structural foundation |
| Separate contracts plus strict admission plus transcript noninterference | High | High practical assurance | High | **Recommend** |
| Runtime dynamic taint tracking for production values | Medium | Medium and transformation-sensitive | Very high | Reject for production; retain targeted test canaries |
| External formal information-flow verification | Potentially highest | Potentially highest | Very high/uncertain | Revisit only for a narrower verified kernel |

## Recommended enforcement architecture

### 1. Make declassification policy machine-readable

Create one registry covering every outward root:

- observation;
- player legal-action set;
- submission rejection;
- acceptance receipt;
- projected history;
- Exercise/Maproom/Intelligence view; and
- later logs, diagnostics, notifications, and transport messages.

Each contract and member must declare:

- contract/policy identity and version;
- eligible audiences;
- source classification (`public`, `own-exact`, `apparent`, `derived-choice`, or another closed
  reviewed class);
- stable-identity domain and scope (`campaign`, `audience`, `window`, `opportunity`, or ephemeral);
- lifetime across observations;
- approved inference and rationale/decision ID;
- prohibited joins/correlations; and
- serializer and semantic-admission entry points.

The registry must be code- or data-backed and enumerated by tests. Markdown remains explanatory,
not the only enforcement source. Adding an outward member without a registry entry must fail the
build.

### 2. Split outward contracts from authority

Introduce a small contract module (exact name to be chosen, such as
`Cna.Core.PublicContracts`) with no reference to `Cna.Core`, Content, Rules authority values,
Randomness, hosting, or Intelligence. Core may reference this module and construct its values; the
dependency must never point back toward authority.

Contract types should be sealed, immutable, non-inheritable, explicitly constructed, and expose no
generic object bags or reflection-based serialization. A public contract assembly does not need to
become a service and does not violate the repository's no-microservice rule.

Keep the opaque authority facade. Do not grant production friend access to authority merely to make
projection convenient. If later physical assembly separation requires a friend relationship, limit
it with a restricted-internals analyzer and a narrow namespace, then retain a public-API test that
fails on any newly exposed authority type.

### 3. Make audience-specific invalid states unrepresentable

Do not use one broad root whose collections are conditionally empty by projector convention.
Prefer closed audience/decision variants with different types:

```text
NormalPlayerView
PhasingWaitingView
ReactingDecisionView
SystemDecisionView
```

For example, `ReactingDecisionView` should have no `OwnElements` member at all if owner-element rows
are forbidden in that state. Its strict reader cannot inject a property that the type and canonical
schema do not admit.

Every canonical reader must perform both:

1. syntactic/canonical validation; and
2. audience-specific semantic admission.

Canonical reserialization alone is not semantic admission. Constructors/factories must validate
identity domains, topology membership, audience, discriminator-specific required/forbidden fields,
and cross-field coherence.

### 4. Publish choices instead of sensitive derivation ingredients

Observation-only action derivation means action membership must be explainable by declassified
facts; it does not require every exact authoritative ingredient to be copied into a general user
observation.

When the minimum raw inputs form a prohibited fingerprint, the Umpire boundary should publish a
closed, side-safe candidate or scope-bounded capability instead. A user submission remains only:

- campaign/audience-visible state;
- expected position/window;
- audience; and
- action ID.

The Umpire resolves internal bindings, regenerates exact current membership, recomputes cost and
legality from authority, and rejects stale or forged actions without detailed hidden-state
diagnostics. Candidate presentation may include only independently approved facts. An opaque or
ephemeral opportunity ID is preferable to a long-lived stable representation ID when no stable join
is required by the user experience.

### 5. Add build-breaking analyzers

Create a small Roslyn analyzer project and configure its diagnostics as errors. Initial rules should
enforce:

- outward contract types exist only in the contract assembly/namespace;
- every outward root and member is registered in the disclosure manifest;
- the contract project does not reference authority assemblies or prohibited namespaces;
- public façade methods do not accept or return authority, command, event, RNG, Content, or generic
  object/document types;
- serializers accept only registered contract roots;
- only the named boundary namespace may call authoritative projector primitives;
- generic object bags, ambient serializers, and post-copy redaction helpers are forbidden in the
  boundary; and
- suppressions require a checked-in rationale tied to an approved decision ID.

Use banned-symbol enforcement for broad forbidden APIs/namespaces and a custom analyzer for
Sandtable-specific registration, dependency, and attribute rules. Because warnings are already
errors, violations will fail ordinary `dotnet build` and `just check`.

### 6. Promote transcript noninterference to a first-class test harness

Build a reusable boundary test harness that executes two valid authoritative traces under the same
audience-visible choices and captures every outward byte sequence. It must compare, without field
normalization:

- observations;
- legal-action sets;
- submissions or their public equivalents;
- receipts;
- projected history;
- rejection results;
- Exercise/controller output; and
- later transport/log/notification outputs registered as user-visible sinks.

Required fixture families:

1. hidden identity, binding, count, ordering, content, strength, mobility, and state mutations;
2. identical public state with unique secret canary strings and numeric canaries;
3. cross-state quasi-identifiers designed to make joins uniquely easy;
4. hidden changes before, during, and after an interrupt;
5. same audience choices applied adaptively across both traces;
6. permitted visible deltas with an exact declared delta surface; and
7. invalid/rejected submissions that must not become a hidden-state oracle.

Add a dedicated temporal test for every new stable outward identifier. The test must establish its
scope and prove it cannot be joined to a prohibited identity using any other registered outward
field across the retained transcript.

### 7. Make the boundary gate mandatory and discoverable

Add a `boundary-check` recipe and include it in `just check`. It should run:

- analyzer/build enforcement;
- contract/reference/public-API graph tests;
- canonical and semantic-admission matrices;
- transcript noninterference suites; and
- outward contract/schema goldens.

Any PR that changes a registered outward contract, projector, serializer, legal-action candidate,
receipt, history, rejection, public façade, or adapter must update the disclosure manifest and its
paired transcript evidence. Review should treat missing evidence as a failed gate, not a request for
reviewer intuition.

## Immediate ZOC/Reaction consequence

The current reacting projection copies this tuple from an identity-bearing own element and places it
beside a representation ID:

```text
organization, allowance, location, reserve, mobility,
ledger turn/stage, CP expended, Cohesion
```

Removing root `OwnElements` only from the reacting payload does not remove the same tuple from the
player's retained prior observations. The boundary redesign therefore required one of these
explicit policies:

1. **Preserve binding secrecy (recommended under current requirements):** do not publish the stable
   representation plus the element-derived fingerprint. Publish an approved opportunity/candidate
   capability with the narrowest display facts and re-resolve authority on submission.
2. **Approve owner binding disclosure:** amend the governing specification to say the own
   representation-to-element binding is player-visible, define its lifetime, and test that no
   opponent binding or additional hidden fact follows. This is a privacy-policy change, not a code
   cleanup.

The owner selected option 1. `ZOR-TASK-004C` now publishes exact current move-option/cost
capabilities without the raw tuple and keeps authority-side binding resolution and current
membership validation internal. Task 005 remains dependency-gated only by completion of the new
boundary gate, not by an unresolved disclosure policy.

The separate strict-readback defect has no policy ambiguity: reacting contract admission must reject
identity-bearing root owner rows and owner-ID-keyed Movement-ended rows whenever the selected
reacting shape forbids them.

## Rollout plan after approval

### Gate 0 — Freeze the threat model

- Decide whether own representation-to-element binding is disclosed.
- Decide audience-collusion and outward-sink scope.
- Add the transcript noninterference property to the governing observation/action specifications.

### Gate 1 — Establish a complete outward-sink registry

- Inventory every public/outward root and serializer.
- Add the machine-readable disclosure manifest and reflection/meta-tests.
- Add current schema/API goldens before moving types.

### Gate 2 — Add compile-time enforcement

- Create the contract assembly and one-way dependency.
- Add banned-symbol rules and Sandtable boundary analyzer diagnostics.
- Move contract values/codecs without changing bytes.

### Gate 3 — Add semantic and temporal verification

- Centralize semantic admission for every outward root.
- Add complete transcript capture and paired trace generators.
- Add temporal join, canary, adaptive rejection, and approved-delta suites.

### Gate 4 — Redesign and resume ZOC/Reaction

- Replace the linkable reacting tuple shape according to the approved policy.
- Prove observation/action/history/rejection transcript noninterference.
- Resume Task 005 only after the new boundary gate passes.

### Gate 5 — Extend the gate to adapters

- Require OrleansHost, Maproom, Intelligence, Exercise exports, diagnostics, and notifications to
  depend only on registered contracts.
- Register and test every new outward sink before it becomes public.

## Adopted decision

The owner accepted these statements as the architecture gate. The bounded ZOC/Reaction checkpoint
implements statements 1, 2, 5, and 6 plus semantic admission from statement 4. Audience-specific
root extraction, the repository-wide isolated-contract assembly, and the custom analyzer portion of
statement 3 remain staged follow-ups:

1. Fog safety is a complete persistent-audience transcript property.
2. Every outward root/member must be registered in a machine-readable disclosure manifest.
3. Outward contracts move to a dependency-isolated assembly and are enforced by build-breaking
   analyzers.
4. Audience-specific types and semantic admission must make forbidden shapes unrepresentable.
5. Candidate/capability publication is allowed when publishing raw derivation inputs would violate
   the disclosure policy; authority still revalidates exact membership and adjudication.
6. ZOC/Reaction does not proceed to Task 005 until the own-binding policy and reacting output shape
   are explicitly resolved.

For the adopted Observation 6 successor, semantic admission is centralized at aggregate
construction: capability handles are recomputed from public bytes, projected history repeats the
handle check, published movement costs must exactly match their selected public edge for a
supported mobility, and the decision variant must agree with the observer/active-side relationship.
These checks make canonical-but-forged combinations fail before action derivation.

## Confidence, limits, and evidence that would change the recommendation

**Confidence: high** that the existing same-object checks are insufficient for a persistent audience
and that transcript noninterference would have caught the current ZOC correlation before review.

**Confidence: high** that assembly separation, strict audience types, and analyzers materially reduce
direct and structural leaks.

**Confidence: moderate-high** that the full recommended stack is proportionate for Sandtable. It adds
up-front infrastructure and test-fixture cost, but fog-of-war is a core game invariant and leaked
data cannot be revoked from clients, logs, or model prompts.

**Limitation:** no static analyzer or finite test suite can prove absence of all possible semantic
inference. The recommendation deliberately combines structural prevention, explicit policy, hostile
fixtures, and transcript equivalence rather than claiming formal proof.

The recommendation should be revisited if a practical formal information-flow tool can model the
chosen C# kernel and canonical output semantics at acceptable maintenance cost, or if the owner
explicitly changes the disclosure policy so that the currently prohibited own binding becomes an
approved fact.

## Next gate

Run and retain the complete `just check` evidence for `ZOR-TASK-004C`, then proceed to Task 005 only
with the manifest, semantic-admission, retained-transcript, and boundary tests still green. The
repository-wide follow-up remains dependency-isolated outward contracts, banned-symbol/custom
analyzer enforcement, and extension of transcript capture to receipts, rejections, hosting,
Maproom, Intelligence, diagnostics, and notifications.
