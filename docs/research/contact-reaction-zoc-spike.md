# ZOC and Reaction Interruption Spike

**Status:** Decision-ready post-Movement package; owner rulings required before task authorization

**Date:** 2026-08-25

**Decision owner:** Project owner

**Research work item:** `CONTACT-001`

## Executive conclusion

The next post-Foundation capability should be a ZOC/Reaction interruption package, not a monolithic
Contact/Reaction feature. Enemy-ZOC entry ends ordinary movement and may open an optional
non-phasing Reaction decision. Contact is created by Close Assault adjacency, while Engaged is a
combat result; their participant-scoped lifecycle belongs with the Combat-cycle package.

Nothing in this packet blocks the deliberately non-contact Movement Foundation. Reaction mutation
does depend on the completed Movement vertical, positive ZOC inputs, and the Breakdown continuity
decision because Reaction spends movement resources and accumulates BP.

## Source index

| Source | Locator | Use |
| --- | --- | --- |
| [1979 Land Game rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) | 8.14-8.25; 8.5-8.68; 10.1-10.3; 21.2-21.3 | Enemy-ZOC stopping, Reaction, Contact/Engaged distinction, ZOC qualification, and BP coupling |
| [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) | 10.3 | Non-phasing ZOC-combat clarification |
| [Reconnaissance/contact research](recon-contact-knowledge-spike.md) | future knowledge boundary | Existing representation/knowledge constraints |

## Evidence and recommended architecture

**Documented fact:** Entering enemy ZOC stops the mover. Reaction is optional movement by the
non-phasing side after qualifying adjacency, spends CP, can repeat subject to restrictions, cannot
enter enemy ZOC, and contributes to BP. Contact/Engaged are later combat-produced states.

**Repository observation:** Current authority has no suspended phasing action, pending opponent
window, movement-ended eligibility, Contact/Engaged relationship, positive ZOC fixture, or BP
continuity. The current synthetic isolated battalions cannot supply the normal positive ZOC
threshold and lack the defensive-combat qualification fact.

**Inference:** ZOC/Reaction is a persisted audience handoff, not a branch inside `MoveElement`.
Commit the triggering phasing move first; if it opens a window, only the non-phasing side owns
Reaction/decline actions. Each accepted event increments state version, invalidates old candidates,
and eventually closes the window before phasing Movement resumes. No authoritative turn waits on a
human, worker, or model.

Recommended minimum state:

- stable Reaction window/trigger identity and source state version;
- phasing/reacting sides plus suspended Movement position;
- apparent/public trigger representation and authoritative real binding;
- eligible/resolved/declined participant basis and window state;
- per-element stage movement-ended/repeat eligibility; and
- CP plus the BP vocabulary selected by `BREAKDOWN-001`.

Do not add a generic per-element `contactStatus`. Future Contact/Engaged authority needs a stable
relationship ID, fixed participant sets, state, creation truth, and lifecycle history.

## Fog and fallback boundary

- The reacting side sees exact own candidates and only the triggering opponent's apparent
  representation.
- The phasing side sees a generic waiting state, never the hidden eligible-reactor list.
- Decline reveals no hidden list or reason; accepted Reaction reveals only already approved
  apparent route/location facts.
- Authoritative Chronicle may retain real bindings and rule evidence; player history is a separate
  redacted projection.
- Deterministic unavailable/timeout fallback is an explicit decline that changes no CP/BP/RNG and
  resumes the phasing side. No automatic route is inferred.

## Positive ZOC fixture gate

Before public ZOC-sensitive movement, add a source-faithful fixture with either two qualifying
battalion-equivalent elements stacked together or one normalized larger organization, explicit
combat/recon/HQ classification, minimum defensive Close Assault capability, admitted Cohesion, and
a permitted adjacent edge. Include negative vectors for every supported exclusion. Organization or
base CPA must not stand in for combat qualification.

Task 005 may still freeze the apparent `exertsZoc` field shape and current false/negative cases;
positive ZOC behavior belongs to this later package.

## Proposed task graph

1. `ZOR-TASK-001` — source/ruling lock: multiple reactors, repeat eligibility, decline scope,
   waiting visibility, and fallback.
2. `ZOR-TASK-002` — ZOC rules/content/positive fixture and canonical identity migration.
3. `ZOR-TASK-003` — movement-ended and pending-window world/snapshot/replay contracts.
4. `ZOR-TASK-004` — dormant observation/action contracts and fog-equivalence tests.
5. `ZOR-TASK-005` — internal move-to-window trigger; public membership remains dormant.
6. `ZOR-TASK-006` — Reaction CP/BP resolution, decline/close, resumption, and atomic publication.
7. `ZOR-TASK-007` — Exercise/Maneuver evidence, synchronization, and independent review.

These tasks are proposed, not authorized. They begin only after Movement Foundation and an approved
`BREAKDOWN-001` continuity boundary.

## Owner decisions and deferrals

Still required before contracts freeze:

- ordering when multiple units can react;
- whether one unit may react more than once to one trigger;
- per-unit decline versus closing the entire window;
- exact public waiting-state wording/shape; and
- the minimum positive-ZOC content vocabulary.

Contact/Engaged participant identity, Break Contact/Break Off, and cycle persistence are deferred to
`COMBAT-CYCLE-001`. They must not be inferred inside the ZOC/Reaction package.
