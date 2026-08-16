# Commander Personas Spike

**Status:** Approved feature direction; implementation not scheduled

**Date:** 2026-08-15

**Decision owner:** Project owner

## Executive conclusion

Sandtable should model a commander persona as a versioned Command policy, not as an Umpire rule and
not merely as an LLM role prompt. A persona should provide an evidence-backed, game-scaled base
profile; a bounded condition response to campaign pressure; deterministic candidate-plan scoring;
and a separate narrative presentation. Intelligence may use the same projection to recommend a
legal plan or write commentary, but the Umpire continues to adjudicate only submitted commands.

The campaign engine should expose one sequence of explicit decision barriers. Configuration decides
who controls each command slot and which barriers pause for a human. A fully autonomous simulation
resolves every barrier through a commander policy. A hybrid campaign pauses at selected barriers and
lets the human decide, optionally with persona-shaped advice. This avoids separate "simulation" and
"player" game loops.

Begin mechanics work with synthetic test profiles, then publish a small, sourced North Africa pilot
and expand it across the ground campaigns fought in Africa. Deliver the African collection in
campaign-sized waves rather than holding it for one oversized release. Expand beyond Africa only
after War College evaluation shows that profiles are distinct, bounded, competent, reproducible
under scripted play, and safe across fog-of-war boundaries. "All WWII generals" should mean a
substantial catalog of notable operational commanders, delivered theater by theater, rather than a
claim of literal completeness.

## Product intent captured

The current direction is:

- Personas apply to autonomous commanders and can shape advice and commentary in human-led play.
- Both scripted fallback and model-backed Intelligence should express the selected persona.
- Historical profiles should capture recognizable, well-supported tendencies without attempting a
  psychological reconstruction.
- The initial collection should cover notable ground commanders across the WWII campaigns fought on
  the African continent and Madagascar, delivered in campaign-sized waves.
- A game rule may restrict commanders to historically valid sides, roles, and dates; sandbox play may
  allow counterfactual assignments.
- Commanders should respond dynamically to pressure and setbacks, within explicit limits that can be
  tuned through evaluation.
- The long-term collection should cover a substantial roster of notable commanders across WWII.

## Scope

This spike plans the player experience, domain boundaries, profile model, dynamic condition,
historical catalog workflow, evaluation, and delivery sequence. It does not assign final trait values,
alter the protobuf contract, implement a commander policy, or commit persona work to the pre-alpha
roadmap.

The African collection boundary includes North Africa, East Africa, Free French operations in West
and Equatorial Africa, and Madagascar. North Africa should still be the first implementation wave
because it matches Sandtable's current campaign and Land focus. East Africa, Gabon and related Free
French operations, and Madagascar should follow as separate campaign packs with their own command
roles and research queues. Naval and air-only commanders remain out of scope until Sandtable exposes
compatible command decisions.

## One campaign loop, configurable participation

Persona support does not require distinct full-simulation and hybrid engines.

```text
Umpire reaches a decision barrier
        |
        v
Command receives a redacted observation and legal candidate plans
        |
        +-- human pause policy matches --> human chooses, with optional persona advice
        |
        +-- otherwise -------------------> commander policy proposes a choice
        |
        v
Umpire validates the command against current authoritative state
        |
        v
Umpire adjudicates and emits events
```

Recommended campaign configuration concepts:

- `CommandSlotAssignment`: the persona, side, role, and controller assigned to a command slot.
- `ControllerKind`: `Human`, `Scripted`, `ModelAssisted`, or `Replay`.
- `HumanPausePolicy`: `EveryDecision`, `MajorDecisions`, `SelectedDecisionTypes`, or `Never`.
- `HistoricalAssignmentRule`: `Strict`, `SideOnly`, or `Sandbox`.
- `IntelligencePolicy`: whether persona-shaped advice and narrative are enabled for a human.
- `PersonaAdherenceRule`: initially `Off` or `Informational`; a later `Scored` option requires its own
  game-design and evaluation gate.

`MajorDecisions` must be based on typed decision metadata, not an LLM's opinion. A key event may flag
the next legal decision barrier as significant, but it must not manufacture a choice outside the
Umpire's legal-action sequence.

### Expected play styles

| Experience | Configuration | Result |
|------------|---------------|--------|
| Full deterministic simulation | Scripted controllers; no pauses | Repeatable run for the same rules, inputs, profiles, and seed |
| Model-assisted simulation | Model-assisted controllers; no pauses | Proposals are deduplicated and recorded; replay is deterministic |
| Strategic hybrid | Human controller; major-decision pauses | Human supplies intent at meaningful checkpoints; routine choices are delegated |
| Detailed human play | Human controller; every-decision pauses | Human chooses at every exposed command barrier |
| Historical replay | Replay controllers | Recorded commands are resubmitted and validated against the matching state |

A model-assisted run is not assumed to regenerate the same prose or proposal in a fresh run. Its
authoritative event replay remains deterministic because accepted commands are recorded. Strict
same-input/same-output simulation uses the scripted controller.

## Domain model

Separate stable historical interpretation, transient campaign condition, effective decision
weights, and presentation.

### Commander profile definition

The immutable, versioned catalog definition should contain:

- stable commander ID, display name, catalog version, and profile version;
- service, historical sides, eligible command roles, theater, and active date ranges;
- a short historical summary and explicit source references;
- ordinal base traits rather than falsely precise psychological scores;
- doctrine or decision tags that describe preferences not captured by a scalar;
- a pressure-response definition;
- a narrative brief that describes tone without imitating quotations or claiming inner thoughts;
- content warnings or context notes where the historical record requires them.

Recommended initial base traits, each on a small scale such as `-2..+2`:

| Trait | Decision meaning |
|-------|------------------|
| Initiative | Preference for acting before the situation is fully resolved |
| Aggression | Willingness to accept operational risk for opportunity |
| Preparation | Preference for deliberate buildup and planning |
| Logistics discipline | Penalty applied to plans that strain supply |
| Force preservation | Penalty applied to casualty and encirclement risk |
| Adaptability | Willingness to leave a failing posture or doctrine |
| Pressure resilience | Resistance to condition-driven decision degradation |
| Coalition coordination | Preference for plans that preserve allied alignment |

These are game behaviors, not clinical or moral judgments. Profile authors must be able to point to
campaign evidence for a non-neutral rating. Traits should not encode combat bonuses, movement
exceptions, extra information, or different rules.

### Commander condition

The dynamic component should initially be transient and bounded:

- `pressure`: current operational demands and threat;
- `confidence`: recent success or failure response;
- `fatigue`: sustained decision load without recovery;
- `shock`: short-lived response to an abrupt adverse event.

Condition inputs must be derived from versioned authoritative events and only from facts visible to
that side. Candidate inputs include threatened encirclement, supply crisis, rapid losses, loss of an
objective, repeated failed plans, unexpected enemy contact, sustained tempo, and recent success.
Quiet periods, stabilization, resupply, successful plans, and commander relief may provide recovery.

The initial implementation should not include permanent personality growth or an unconstrained model
memory. A later campaign-development system may add bounded learning after its rules can be stated,
replayed, and evaluated.

### Effective behavior

At a decision barrier, Command derives an effective profile from:

```text
base traits
+ pressure-response lookup
+ bounded transient condition
= effective decision weights, clamped to the allowed range
```

A commander known for performing well under pressure might resist degradation or receive a small,
capped improvement in a relevant trait. Another might become less consistent, more cautious, or more
impulsive. The effect must be explicitly authored per profile; Sandtable should not assume that
stress always causes one particular response. Research finds an overall adverse effect in many
uncertain decisions, but also meaningful dependence on the task, stressor, and individual context.

Every transition should emit or persist enough typed data to explain:

- which visible events changed condition;
- the old and new bounded values;
- which profile response rule applied; and
- how the effective weights affected candidate scoring.

### Narrative persona

Narrative presentation is a projection of the profile, observation, and selected plan. It may affect
briefings, advice, commander commentary, and War Diary prose. It must never change legal plans,
authoritative facts, or condition state. Narrative prompts should request original period-appropriate
prose, not impersonation or fabricated quotations.

## Decision behavior

### Scripted controller

The deterministic controller should rank only Umpire-provided legal candidates. A transparent first
model can combine normalized plan attributes with effective traits:

```text
score = objective value
      + initiative/opportunity preference
      - logistics-discipline * supply risk
      - force-preservation * casualty risk
      - pressure-adjusted encirclement risk
      + authored doctrine matches
```

The exact formula is a versioned Command policy, not part of a historical profile. Tie-breaking uses
the campaign's seeded RNG or a stable candidate order according to the policy contract. The selected
plan still returns as a proposal and must pass the normal decision ID, state version, ruleset hash,
and legal-plan validation.

### Model-assisted controller

Intelligence receives the same redacted observation, legal candidates, profile version, effective
traits, condition summary, and policy/prompt versions. It may reason about the candidates but cannot
invent actions or see hidden opposing state. Existing decision-ID deduplication and stale-proposal
rejection remain mandatory. Timeout, malformed output, or provider unavailability invokes the
scripted controller with the same effective profile.

### Human controller

For human play, the persona should initially advise rather than constrain. The player may request:

- a persona-shaped recommendation with reasons;
- a neutral staff comparison of candidate plans; or
- no Intelligence assistance.

The UI should distinguish historical profile, current condition, staff facts, and generated
commentary. A human choice that disagrees with the assigned persona remains legal. Future role-play
rules could reward adherence, but should not be smuggled into the first implementation. Under the
recommended `Informational` default, Maproom may describe a choice as aligned, mixed, or divergent
after it is submitted. That label does not alter adjudication, victory, unit performance, or future
condition.

This preserves human agency and avoids turning an approximate historical interpretation into a
combat modifier. It also keeps role-play quality separate from military effectiveness: a divergent
choice can still be strategically sound, while an in-character choice can still fail.

## Historical profile policy

### Evidence standard

Each non-neutral trait or authored pressure response needs:

1. at least one campaign-specific source;
2. preferably corroboration from a second independent source;
3. a short evidence note separating documented action from designer inference; and
4. a confidence rating such as `low`, `moderate`, or `high`.

Use official histories, archives, scholarly biographies, contemporary orders or correspondence, and
reputable military museums ahead of popular summaries. Memoirs are useful but interested sources.
Enemy assessments and famous reputations require corroboration. Absence of evidence should produce a
neutral trait, not an invented differentiator.

Historical people should not become national stereotypes or collections of flattering myths. The
catalog must retain context for documented atrocities, collaboration, or political allegiance where
omitting it would materially misrepresent the person. Inclusion as a playable profile is not an
endorsement.

### Inclusion criteria

A commander belongs in a theater catalog when all of the following are true:

- they held a meaningful ground command during the covered campaign;
- Sandtable exposes decisions at a compatible command scale;
- they had enough campaign evidence to author at least one defensible differentiator; and
- they add historical coverage or a behavior pattern not already overrepresented.

Notability alone is insufficient. A famous commander with no decisions at the modeled scale should
wait for the appropriate game layer.

## Proposed North Africa catalog

This is a research queue, not a final rating table. Command role, dates, evidence, and playable
distinctiveness must be verified before a profile is published.

### Wave A: Western Desert pilot and coverage

| Side | Candidate | Why research them |
|------|-----------|-------------------|
| British/Commonwealth | Archibald Wavell | Theater command during the opening campaign and competing commitments |
| British/Commonwealth | Richard O'Connor | Mobile operational command during Operation Compass |
| British/Commonwealth | Claude Auchinleck | Theater and Eighth Army command through the 1941-42 reverses |
| British/Commonwealth | Bernard Montgomery | Preparation, morale restoration, supply buildup, and set-piece operations |
| Australian | Leslie Morshead | Tobruk defense and later divisional command at El Alamein |
| New Zealand | Bernard Freyberg | Divisional command across repeated North African operations |
| Free French | Marie-Pierre Koenig | Bir Hakeim defense and breakout |
| German | Erwin Rommel | High-tempo maneuver, operational opportunity, and supply risk |
| Italian | Rodolfo Graziani | Opening invasion and response to Operation Compass |
| Italian | Ettore Bastico | Senior Axis command and coalition-command friction |
| German | Ludwig Crüwell | Afrika Korps command during Operation Crusader and Gazala preparation |

### Wave B: Torch and Tunisia coverage

| Side | Candidate | Why research them |
|------|-----------|-------------------|
| Allied | Dwight Eisenhower | Coalition and theater command during Torch and Tunisia |
| British | Harold Alexander | Senior land coordination from the eastern advance through Tunisia |
| British | Kenneth Anderson | First Army command in the Tunisian campaign |
| United States | Lloyd Fredendall | II Corps command through the Kasserine defeat and a needed failure case |
| United States | George S. Patton | II Corps discipline and recovery after Kasserine |
| United States | Omar Bradley | II Corps command in the final Tunisian operations |
| German | Hans-Jürgen von Arnim | Fifth Panzer Army and final Axis command in Tunisia |
| Italian | Giovanni Messe | First Italian Army's final defensive campaign and surrender |

The first playable `Graziani's Offensive` scenario predates many candidates. Under `Strict`
historical assignment, it should expose only date- and role-eligible profiles. Later commanders can
remain available under `SideOnly` or `Sandbox` rules and for later scenarios.

## Wider African campaign coverage

The initial African collection covers all relevant ground campaigns, but not in one release. Keep a
coverage ledger that records each campaign, eligible command roles, candidate commanders, evidence
status, and whether Sandtable has a compatible decision surface.

| Campaign pack | Geographic and operational coverage | Delivery note |
|---------------|--------------------------------------|---------------|
| North Africa | Western Desert, Torch, and Tunisia, 1940-43 | First pack; aligned with current game direction |
| East Africa | British Somaliland, Sudan/Eritrea, Kenya/Italian Somaliland, Ethiopia, and Gondar, 1940-41 | Research commanders such as William Platt, Alan Cunningham, the Duke of Aosta, and theater-level Wavell |
| Free French Africa | Dakar, Gabon, Kufra, and connected West/Equatorial African ground operations | Include only roles with meaningful ground decisions; combined operations need careful command-slot modeling |
| Madagascar | Operation Ironclad and the subsequent 1942 land campaign | Ground commanders only; naval and air command remain contextual |

"All" means every in-scope African ground campaign appears in the coverage ledger and receives an
explicit include, defer, or incompatible decision. It does not mean every officer receives a persona.
The normal notability, evidence, command-scale, and distinctiveness gates still apply.

## Contract and ownership consequences

- Command owns persona definitions, condition derivation, controller policy, and recommendations.
- The Umpire owns legal candidates, authoritative state, seeded RNG, rules, adjudication, and events.
- Intelligence Gateway owns prompt/persona rendering, provider routing, structured-output validation,
  and narrative generation.
- Chronicle records accepted commands and authoritative results; persona traces remain explanatory
  metadata rather than adjudication authority.

Prefer a Command domain library/module rather than a new service. A future `Cna.Command` library is a
reasonable home if implementation proves that the behavior does not belong in an existing domain
module. Do not place persona behavior in `Cna.Intelligence.Gateway`, because scripted fallback and
human advice must work without a provider.

The existing protobuf `CommanderProfile` is only a small transport projection. Before adding fields,
define the domain schemas for profile, assignment, condition, evidence, and policy trace. Then update
`intelligence.proto` first, preserve existing field numbers, and add new fields with new numbers. At
minimum, a future request projection likely needs effective traits and a bounded condition summary.

Campaign configuration must pin catalog, profile, Command-policy, and ruleset versions. A save must
either retain those versions or fail clearly if required content is unavailable. Replays consume the
accepted command log and must not silently re-evaluate a newer persona version.

## War College evaluation

Historical plausibility cannot be established by anecdote or a single successful game. Evaluate
personas against a versioned corpus of redacted decision points and paired seeded campaigns.

Required gates:

- **Determinism:** scripted policy gives the same selection and trace for the same inputs.
- **Distinctiveness:** materially different profiles do not collapse to the same choices everywhere.
- **Competence floor:** a persona is not a caricature that routinely selects dominated plans.
- **Bounds:** maximum condition cannot overwhelm hard legality or configured scoring limits.
- **Pressure response:** authored profiles respond differently only where their definitions say so.
- **Fallback equivalence:** provider failure retains the selected persona and current condition.
- **Fog of war:** profile and condition derivation never reveal or consume hidden enemy state.
- **Replay:** accepted decisions reproduce the same authoritative campaign result.
- **Historical rubric:** reviewers can trace claimed tendencies to sources and distinguish inference.
- **Balance visibility:** report win rate and plan-selection distribution without forcing equal win rate.

Run profiles on both historical and counterfactual sides during evaluation. This finds behavior defects;
it does not imply that counterfactual outcomes validate historical claims.

## Delivery sequence

### Phase 0 - Adopt product rules

- Record the all-Africa coverage boundary, campaign-pack sequence, and ground-command scale.
- Adopt the single decision-barrier loop and configurable pause policies.
- Decide how strict historical assignments appear in scenario defaults.
- Define what profile and condition information players can inspect.

### Phase 1 - Prove Command mechanics with synthetic fixtures

- Define versioned profile, condition, assignment, evidence, and policy-trace schemas.
- Implement event-derived bounded condition and a transparent scripted scorer.
- Add aggressive, deliberate, logistics-focused, and pressure-sensitive test profiles without
  historical names.
- Prove deterministic fallback, replay, and fog-of-war behavior.

### Phase 2 - North Africa pilot

- Research and review a small set spanning both sides and several behavior patterns.
- Recommended pilot: Wavell, O'Connor, Montgomery, Morshead, Rommel, and Graziani.
- Add source-backed trait notes and confidence ratings.
- Run War College decision-corpus and paired-campaign evaluation before tuning profile versions.

### Phase 3 - Player participation and Intelligence projection

- Add controller assignment, human pause policy, and historical-assignment rules to setup.
- Show recommendations, current condition, and explanation traces in Maproom.
- Extend the protobuf contract and prompt renderer only after the domain schemas stabilize.
- Verify that unavailable Intelligence produces the same valid scripted path.

### Phase 4 - Complete the North Africa catalog

- Add remaining Western Desert candidates, then Torch/Tunisia candidates.
- Review rank, role, date eligibility, source quality, and duplicate behavior coverage.
- Version profiles independently so evidence and tuning changes do not rewrite old saves.

### Phase 5 - Complete the African ground collection

- Add East Africa, Free French African operations, and Madagascar as separate campaign packs.
- Maintain an African campaign coverage ledger with no silently omitted ground campaigns.
- Research each pack against its own operational records instead of projecting North African traits
  into a different command context.
- Defer naval and air-only commanders until legal decisions exist at those command scales.

### Phase 6 - WWII theater packs

- Expand by theater and campaign rather than nationality or fame alone.
- Reuse the schema while allowing theater-specific doctrine tags and command roles.
- Add ground commanders first; introduce naval and air command only with compatible decision systems.
- Maintain a catalog coverage ledger instead of claiming exhaustive completeness.

Persona delivery should follow, not precede, a stable legal-plan surface. The current pre-alpha roadmap
appropriately keeps model-backed Intelligence disabled and places deterministic policy after legal
actions and fallback behavior exist.

## Risks and mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Historical myth becomes game truth | Caricatures and misleading behavior | Source notes, corroboration, confidence, neutral defaults |
| Trait labels imply false psychological precision | Unjustified ratings | Small ordinal scale and campaign-specific evidence |
| Dynamic condition creates runaway drift | Persona overwhelms strategy | Hard bounds, transient state, recovery, versioned traces |
| Model and fallback act like different commanders | Broken identity and testing | Same effective profile projection and fallback tests |
| Persona leaks hidden state | Invalid decisions and security failure | Derive condition only from side-visible events; negative tests |
| Human persona feels restrictive | Player loses agency | Advice by default; restrictions require an explicit future rule |
| Historical validity blocks fun combinations | Reduced sandbox value | `Strict`, `SideOnly`, and `Sandbox` assignment rules |
| Catalog grows faster than evidence review | Shallow or duplicate profiles | Theater waves, inclusion gates, coverage ledger |
| Famous commanders dominate selection | Narrow player experience | Surface role/date fit and scenario-recommended profiles |
| Controversial figures are sanitized | Misleading presentation | Context notes and explicit inclusion-not-endorsement policy |

## Acceptance criteria for the first implementation slice

- One authoritative campaign loop supports autonomous and human-paused decisions through
  configuration.
- Versioned synthetic profiles influence only legal candidate-plan selection.
- Profile condition is derived from side-visible authoritative events, bounded, explainable, and
  reproducible.
- The scripted controller is deterministic and is used on every Intelligence failure path.
- Human players may inspect or ignore persona-shaped advice without losing legal options.
- Human choices may receive informational alignment feedback, but no hidden modifier or penalty.
- Historical assignment rules support strict and sandbox play.
- At least four synthetic profiles pass determinism, bounds, replay, and fog-of-war tests.
- No historical profile ships until its non-neutral fields have evidence notes and review status.

## Adopted directions

The project owner selected:

1. all WWII African ground campaigns for the initial theater collection, delivered in waves;
2. ground commanders only for now; and
3. summarized condition and explanations in Maproom, with exact values and scoring available in
   developer and War College views; and
4. consequence-free human overrides with `PersonaAdherenceRule` defaulted to `Informational`.

The informational rule may show alignment feedback after a choice, but must never reject, weaken, or
secretly modify a legal command. `Scored` role-play remains a possible later game rule requiring its
own visible scoring contract, evaluation gate, and opt-in setup control.

## Research sources

These sources establish campaign coverage, command roles, and research leads. They do not by
themselves justify final trait ratings.

- [Imperial War Museums: British victory in the desert](https://www.iwm.org.uk/history/how-the-british-secured-a-victory-in-the-desert-during-the-second-world-war)
- [Imperial War Museums: A short guide to the war in Africa](https://www.iwm.org.uk/history/a-short-guide-to-the-war-in-africa-during-the-second-world-war)
- [Imperial War Museums: Italy's defeat in East Africa](https://www.iwm.org.uk/history/how-italy-was-defeated-in-east-africa-in-1941)
- [National Army Museum: The struggle for North Africa, 1940-43](https://www.nam.ac.uk/explore/struggle-north-africa-1940-43)
- [National Army Museum: Madagascar, 1942](https://collection.nam.ac.uk/detail.php?acc=2005-01-69-25)
- [National Army Museum: Archibald Wavell](https://www.nam.ac.uk/explore/archibald-wavell)
- [U.S. Army Center of Military History: Northwest Africa](https://history.army.mil/portals/143/Images/Publications/catalog/6-1.pdf)
- [U.S. Army Center of Military History: Omar Bradley](https://history.army.mil/Research/Reference-Topics/5-Star/Gen-Omar-N-Bradley/)
- [Australian War Memorial: Australian strategy and command in 1941](https://www.awm.gov.au/visit/events/conference/remembering-1941/horner)
- [Australian War Memorial: Leslie Morshead papers](https://www.awm.gov.au/get-involved/donations-bequests/findingaids/private/morshead)
- [New Zealand History: North African campaign background](https://nzhistory.govt.nz/war/the-north-african-campaign/background)
- [Chemins de mémoire: Marie-Pierre Koenig](https://www.cheminsdememoire.gouv.fr/fr/marie-pierre-koenig)
- [Chemins de mémoire: Free France in Africa, 1940](https://www.cheminsdememoire.gouv.fr/fr/1940-ralliements-de-lempire-la-france-libre)
- [The National Archives: British Army operations in WWII](https://www.nationalarchives.gov.uk/help-with-your-research/research-guides/british-army-operations-second-world-war/)
- [PubMed: Effects of stress on decisions under uncertainty](https://pubmed.ncbi.nlm.nih.gov/27213236/)
- [PubMed: Decision making under stress review](https://pubmed.ncbi.nlm.nih.gov/22342781/)
- [PubMed: Stress, valuation, learning, and risk-taking](https://pubmed.ncbi.nlm.nih.gov/28044144/)
