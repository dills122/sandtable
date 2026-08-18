# Turn-Preamble Action Boundary Spike

**Status:** Decision-ready; recommended scope adopted for specification correction

**Date:** 2026-08-17

**Rules target:** `cna-1979.1`

**Roadmap capability:** `ACTION-001`

## Decision

Correct Legal Actions v1 so it reaches and exposes the first practical, source-backed side choice
without skipping either Naval Convoy phase.

For the two existing synthetic rules-laboratory setups only:

1. add an explicit, versioned setup policy stating that the opening Naval Convoy Schedule and
   Tactical Shipping phases have no obligations;
2. resolve each mandatory phase through its own trusted system action and authoritative event;
3. enter Operation Stage 1 Initiative Declaration;
4. offer the initiative holder exactly two side actions: act first or act last;
5. record the resulting first- and second-acting sides for Operation Stage 1; and
6. stop honestly at Weather Determination.

The no-obligation policy is setup data, not an inference from missing logistics fields. It is
covered by setup identity, campaign creation history, replay validation, and an adopted ruling that
requires explicit resolution of an admitted empty phase. Any convoy checkpoint without that exact
policy remains unsupported.

Side legal-action generation for this slice derives only from the already approved Campaign
Observation v1 boundary. Trusted system actions remain a separate authority path. The membership
guarantee applies to outward player and Intelligence submissions; campaign creation, replay, and
trusted system orchestration are not player submissions.

## Question and scope

This spike asks:

> What is the earliest source-backed non-empty side choice in the mandatory turn preamble, and
> should Legal Actions v1 include it so the capability proves acting-side membership and
> non-vacuous fog safety without skipping Naval Convoy or unrelated preamble rules?

The review covers the sequence of play, Initiative Declaration, Weather Determination, the
Land-only abstract logistics and simplified convoy rules, the September 1979 errata, the relevant
charts, and the current Content Pack, setup, campaign, and observation boundaries. It does not
design full logistics, ports, production, replacements, tactical shipping, weather resolution, or
published-scenario ingestion.

## Method

- Rendered and visually inspected the image-only Land Rules and chart scans retained outside Git.
- Text-searched the OCR-enabled September 1979 errata for the relevant rule locators.
- Compared mandatory sequence positions with current campaign and observation contracts.
- Distinguished source facts, repository policy, and implementation consequences.
- Compared four scope options against authority, fog, determinism, replay, and roadmap evidence.

## Source findings

### Exact sequence order

| Documented fact | Stable source reference |
| --- | --- |
| Initiative Determination begins the Game Turn. | `spi-1979-land-rules:5.2` |
| Naval Convoy follows and contains Naval Convoy Schedule and Tactical Shipping. | `spi-1979-land-rules:5.2` |
| Operation Stage 1 begins only after Naval Convoy and starts with Initiative Declaration, then Weather Determination. | `spi-1979-land-rules:5.2` |
| Naval Convoy Schedule includes cargo, route, and future-arrival planning; Tactical Shipping includes cargo planning between North African ports. | `spi-1979-land-rules:5.2.naval-convoy-stage` |

The sequence is mandatory. A legal-action implementation cannot jump directly from Initiative
Determination to Initiative Declaration.

### Earliest ordinary side choice

Published play can require side decisions during Naval Convoy Schedule before Initiative
Declaration. Under the Land-only abstract rules, players plan simplified supply arrivals at the
start of the Game Turn; the Axis later divides scheduled Supply Units, Motorization Points, and
Replacement Points among available convoy lanes. Lane capacities and arriving tonnage are then
resolved under the convoy rules and charts.

| Documented fact | Stable source reference |
| --- | --- |
| Abstract supply arrivals are planned at the beginning of each Game Turn, with different planning horizons for Axis and Commonwealth. | `spi-1979-land-rules:32.43` |
| Axis supply availability uses a table and random procedure. | `spi-1979-land-rules:32.44`; `spi-1979-axis-charts:32.46` |
| Commonwealth supply availability also uses a table and random procedure. | `spi-1979-land-rules:32.45` |
| Simplified Axis convoys allocate scheduled supply, motorization, and replacement quantities among available lanes. | `spi-1979-land-rules:32.61` |
| Convoy lanes have stage capacity limits and arriving convoys can face Commonwealth bombing. | `spi-1979-land-rules:32.62-32.65`; `spi-1979-axis-charts:32.66` |
| Axis convoy level and capacity charts determine available tonnage. | `spi-1979-axis-charts:56.4-56.5` |
| The Axis may allocate arriving tonnage among Operation Stages unless already designated. | `spi-1979-errata:56.25` |

Therefore the earliest *ordinary* side choices are convoy choices. Implementing them faithfully
would require logistics quantities, eligible cargo, ports, routes, lanes, capacity, planning
horizons, production/replacement facts, and random procedures that the current Content Pack and
Campaign World do not represent.

The errata also warns that all abstract rules were untested and may be confusing
(`spi-1979-errata:32.0`). That warning does not authorize omission or invention; it strengthens the
need to bound any temporary repository ruling narrowly.

### First practical non-empty side action

The initiative holder chooses separately for every Operation Stage whether to act first or last.
The holder remains the initiative holder for the whole turn, while Player A and Player B denote the
first- and second-acting sides for a particular stage.

| Documented fact | Stable source reference |
| --- | --- |
| The initiative holder may elect to act first or last in an Operation Stage. | `spi-1979-land-rules:7.11` |
| The holder keeps initiative throughout the three Operation Stages. | `spi-1979-land-rules:7.12` |
| The first/last election is made in Initiative Declaration for each Operation Stage. | `spi-1979-land-rules:7.14` |
| Player A is the first mover and Player B is the last mover. | `spi-1979-land-rules:7.16` |

This choice requires only public campaign facts already present in Campaign Observation v1:
current position, Operation Stage, initiative holder, and observer. It requires no opposing-force
truth. Once the mandatory convoy checkpoints are resolved legitimately, Initiative Declaration is
the smallest non-empty side-action slice that can prove membership and fog non-interference.

### Weather is the honest cutoff

The initiative holder determines Weather for every Operation Stage by rolling two dice and reading
the applicable season row sequentially. Sandstorm or rainstorm then invokes the Foul Weather
Location Table and another die.

| Documented fact | Stable source reference |
| --- | --- |
| Weather is determined for every Operation Stage. | `spi-1979-land-rules:29.0` |
| The initiative holder rolls two dice and consults the season row. | `spi-1979-land-rules:29.1`; `spi-1979-common-charts:29.61` |
| Foul weather invokes a location table and one additional die. | `spi-1979-land-rules:29.1`; `spi-1979-common-charts:29.7` |

Weather is a mandatory authoritative random/table procedure, not another base player choice.
Legal Actions v1 must stop there rather than inventing a generic advance action.

## Repository findings

- Content Pack v1 currently models topology, formations, and initial deployment only. It has no
  convoy, port, cargo, supply, replacement, production, route, lane, or planning-horizon contract.
- Absence of those fields cannot mean zero convoy obligations. Doing so would convert missing
  authoritative data into a rules conclusion and violate content fidelity.
- Existing rules-laboratory setups already own synthetic initiative/admission policy outside
  published scenario content. A similarly explicit opening-preamble policy is the smallest honest
  fixture extension while scenario contracts remain narrow.
- Campaign Observation v1 already exposes the viewer, current source-free position, active side,
  and initiative holder and proves opponent-only non-interference. It is sufficient input for the
  two Initiative Declaration candidates.
- The current campaign validator admits only Initiative Determination and the post-Initiative Naval
  Convoy cutoff. Implementation will have to admit the two convoy checkpoints, Initiative
  Declaration, and the post-declaration Weather cutoff through event-history validation.

## Options considered

| Option | Result | Decision |
| --- | --- | --- |
| Keep system-only initiative resolution and empty side sets | Preserves the current cutoff but does not deliver the roadmap's first side legal action; fog proof is vacuous. | Reject |
| Implement the first ordinary convoy planning choices | Source-order correct but expands into an unmodeled logistics subsystem and unstable abstract-rule surface. | Defer |
| Add explicit no-obligation fixture policy, resolve both convoy phases, then expose Initiative Declaration | Preserves sequence, records every advance, adds a genuine side choice, and stays within public observation facts. | Adopt |
| Skip or silently auto-complete Naval Convoy | Deletes mandatory phases and derives behavior from missing data. | Reject |

## Adopted boundary and ruling

The setup policy is an immutable contract-version-1 value whose initial version permits only:

`no-opening-naval-convoy-obligations`

It means exactly that the supported synthetic scenario has no Naval Convoy Schedule cargo,
replacement, or arrival-planning obligations and no Tactical Shipping cargo or eligible port-to-port
shipment in the opening turn. It does not mean that the repository has implemented the general
convoy rules, that all scenarios have empty convoy phases, or that later turns are empty.

The policy carries its own stable repository-synthetic source reference, distinct from each setup's
Initiative sources:

`sandtable-rules-lab:opening-preamble.no-naval-convoy-obligations.v1`

The policy and that source participate in setup canonical bytes and hash and are embedded in
campaign creation history and snapshots. Projection revalidates both before either convoy phase can
resolve. Initiative resolution continues to consume only the setup's Initiative-specific sources,
so the unchanged `InitiativeDetermined` contract and canonical evidence do not acquire an unrelated
convoy source.

Record the adopted ruleset ruling with these stable identities:

- ruling: `cna-1979.1.ruling.explicit-empty-opening-convoy-resolution`;
- conflict: `cna-1979.1.conflict.empty-opening-convoy-phase`;
- alternative: `reject-empty-opening-convoy-as-unsupported`;
- alternative: `resolve-explicitly-admitted-empty-opening-convoy`; and
- selected behavior: `resolve-explicitly-admitted-empty-opening-convoy`.

The ruling sources are `spi-1979-land-rules:5.2`, `spi-1979-land-rules:32.43`,
`spi-1979-land-rules:32.61`, and the repository-synthetic policy reference above. Its two behavioral
alternatives are:

- reject any empty mandatory convoy phase as unsupported; or
- when and only when the admitted setup explicitly declares no obligations, resolve the named
  phase with a mechanic-specific event and advance once.

Select the second behavior. Protect it with setup-hash, no-policy rejection, phase-order, event,
projection, replay, and ruleset-hash tests. This ruling fills a repository procedure gap; it does
not change any published positive convoy obligation.

## Legal-action and authority consequences

Legal Actions v1 now needs five concrete candidates:

- system: resolve Initiative Determination;
- system: resolve the admitted no-obligation Naval Convoy Schedule;
- system: resolve the admitted no-obligation Tactical Shipping phase;
- initiative-holder side: act first in Operation Stage 1; and
- initiative-holder side: act last in Operation Stage 1.

The opposite side receives an empty set at Initiative Declaration. Both side sets derive from the
Campaign Observation v1 projection for their audience; paired valid states that alter any
opponent-only force fact must produce structurally and byte-identical side action sets. System
generation may use admitted authority directly but never exposes its candidates as belonging to a
side.

Accepted Initiative Declaration records the chosen first- and second-acting sides for Operation
Stage 1 and advances to Weather. The choice must not overwrite `initiativeHolder`.

The current-action membership requirement applies to untrusted outward player and Intelligence
submissions. Host and Intelligence adapters must depend on the legal-action submission boundary,
not construct player commands, player-choice events, or call direct decision/projection seams.

For `ACTION-001`, snapshot, exact content context, commands, events, serializers, projection, replay
preparation, and replay harnesses become Core-internal. Only `Cna.Core.Tests` receives friend access;
no production assembly does. Public creation returns a sealed non-record
`CampaignAuthorityHandle` with no authority-bearing getters, deconstruction, serialization, or
value-revealing `ToString`. Observation, action query, and action submission unwrap it only inside
Core. Accepted submission returns a successor handle plus side-safe receipt, never a checkpoint,
event, context, artifact, random state, or full authority payload.

OrleansHost is the sole production project that references Core and may store/pass this opaque
in-process handle only to the safe facades; it gains no outward player/Intelligence adapter in this
capability. DecisionWorker's currently unused Core project reference is removed, so it cannot
reference or receive the handle or complete authority. Cross-assembly activation replay remains
unavailable until `HOST-001` defines authenticated Chronicle provenance and a dedicated
activation-only or constrained write-only capability. It may not be added to an outward adapter or
re-publicize authority serialization/projection. Public-surface and real
OrleansHost/DecisionWorker reference tests enforce this boundary.

## Implementation consequences

- Clean-cut to setup schema 3, `CreateCampaign` command 4, `CampaignCreated` event 4, and
  `CampaignSnapshot` 4. Add opening-preamble policy contract 1 and stage-order value contract 1.
  Reject setup schema 2 and creation/snapshot contract 3 in the new executable; the prior Git
  revision remains their historical executable.
- Keep `InitiativeDetermined` event 2, Campaign World 1, Land sequence position 2, Campaign
  Observation 1, and ruleset-manifest contract 2 unchanged. Add the ruling to the existing manifest
  schema, changing the canonical ruleset hash without changing that schema.
- Give the two new convoy commands/events and Initiative Declaration command/event contract version
  1. No old reader exists for those new types.
- Refactor current public campaign authority into an opaque handle plus safe creation, observation,
  query, and submission facades. Internal acceptance tests retain exact event/projection/replay
  access through the existing test-only friend relationship.
- Add mechanic-specific commands/events/resolvers for the two no-obligation convoy checkpoints and
  Initiative Declaration. Do not add generic sequence completion.
- Persist contract-version-1 stage order separately from initiative holder and include it in
  snapshot/event/replay validation. State versions 1 through 4 have no stage order; state version 5
  at Weather has exactly one entry for Operation Stage 1 with two distinct complementary sides.
- Determine Initiative Declaration eligibility from the observation's viewer and public initiative
  holder; no hidden-state access or inferred opponent fact is required.
- Generate side candidates from Campaign Observation v1 only; use authority state solely for
  validation and system candidates.
- Update the ruleset manifest with the adopted ruling and expected hash migration.
- Stop accepted play at Weather Determination; Weather is a later source-driven capability.
- Add metamorphic privacy tests with a non-empty eligible side set and a non-empty opposing hidden
  mutation, comparing complete semantic values and canonical bytes.
- Narrow roadmap wording from every accepted Core command to every accepted outward player or
  Intelligence action submission, internalize every alternate mutation/replay primitive, and test
  the public API plus the real adapter project references.

## Explicit deferrals

- General Naval Convoy Schedule and Tactical Shipping choices or resolution;
- supply, production, replacement, ports, shipping routes, convoy lanes, capacity, arrival timing,
  bombing, and the optional historical rerouting rule;
- Weather resolution and its random/table contract;
- Initiative Declaration for Operation Stages 2 and 3, which cannot be reached in this slice;
- movement, contact, combat, and opposing apparent-contact knowledge;
- host, protobuf, Maproom, persistence, and Intelligence adapters themselves.

## Sources

- [Original Land Rules scan](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
- [Original common charts scan](https://www.spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf)
- [Original Axis charts scan](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf)
- [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
- [Initiative Determination spike](initiative-determination-spike.md)
- [Campaign Observation v1 specification](../specs/campaign-observation-v1.md)
