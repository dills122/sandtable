# Graziani victory and End of Turn: ruling-ready packet and evaluator sketch

**Date:** 2026-11-25. **Task:** T4 Phase 1 of the RC critical-path analysis. **Baseline:** `origin/main` at `02280697`.
**Status:** planning document. It decides nothing. It adds no code, no ruling and no runtime contract.
It consolidates what the existing victory documents already say, separates what is settled for planning
from what still needs an owner ruling, and gives Phase 2 (a pure evaluator) a clean API to build against
once the rulings exist.

**Evidence labels.** **VERIFIED** means I read the code or document at the baseline SHA. **DOC** means a
prior Sandtable document says it and I did not re-open the original scan. **ASSUMED** means my inference.
I did not re-inspect the original scenario or Land PDFs; every source claim below is DOC, inherited from the
documents linked in section 8. I ran no tests.

---

## 1. What exists today

- **No victory or End of Turn code exists in the native chain.** VERIFIED: `grep -rli victory src` finds only
  combat Retreat Point (RP) arithmetic and Runner artifact fields. `Cna1979LandSequence.CreateTurn` emits an
  `end-of-turn` position for every turn 1-6 (`Cna1979LandSequence.cs:44`), and `GetNext` wraps from it to
  turn N+1's first position (`:50-69`). Nothing consumes the End of Turn position.
- **The world model already carries what a location predicate needs, but not side ownership.** VERIFIED:
  `CampaignElementState` holds `ElementId`, `CurrentLocationId`, `ReserveStatus` and `OperationalState`
  (`CampaignElementState.cs:12-34`). Side comes from content (`ContentForces.cs` `SideId`), not from the
  snapshot. `CampaignWorldSnapshotV8` wraps `ContentIdentity`, `ScenarioId` and the V6 world
  (`CampaignWorldV8.cs:33-38`). `LandSide` is `Axis | Commonwealth`.
- **Map identity exists as pinned hashes.** VERIFIED: `Cna1979HistoricalMap` pins `HexHash`, `EdgeHash` and
  `CdSeamPolicyHash` and throws if they change (`Cna1979HistoricalMap.cs:16-19,81-82`). Location IDs are
  lowercase hex IDs such as `c4131` (`Cna1979GrazianiSourceFacts.cs:286`). The map JSON has a `settlement`
  field per hex (`hexes.v1.json`), but in the first rows I read it is `null`; I did not check whether
  Alexandria, Cairo, Matruh or Siwa have settlement data. ASSUMED: it is incomplete (see Q-L1).
- **Source identity and the six printed rows are settled as transcription, not as rules.** DOC: victory
  matrix, blind reading comparison, and the 2026-11-17/18 follow-up notes.

## 2. Settled for planning (RUL-GRA-02, -07, -22, -25)

These are owner-visible planning choices already recorded in `2026-11-12-graziani-ruling-decisions.md`.
They are project interpretations, not errata. Phase 2 may implement them as-is once the owner confirms
D-0 below.

| ID | Planning choice | Source |
| --- | --- | --- |
| RUL-GRA-02 | §60.22's pointer to §60.71 is a mistaken cross-reference. Use the Graziani list in **§60.81**. | DOC |
| RUL-GRA-07 `gra-victory-result-v1` | Evaluate all six rows independently on one terminal snapshot, each yielding `match`, `no-match` or `unknown`. A known-false required conjunct makes a row `no-match` even if another conjunct is unknown. Any `unknown` row makes the whole result `unresolved` (no winner, no level). If all rows are known: each side's highest matching label (strategic > decisive > tactical); one side matching gives that side and level; neither gives `no qualifying victory`; both gives `simultaneous qualifying victories` with no sole winner. Lower rows are **not** prerequisites (the location sets are not nested). | DOC |
| RUL-GRA-22 | Italian strategic: choose Alexandria or Cairo and one qualifying hex of that city. The same qualifying combat unit that holds that hex is the convoy-capability witness. | DOC |
| RUL-GRA-25 | Commonwealth supply trace is applied separately to each objective in the row. Each objective may trace to a different certified friendly-controlled Alexandria/Cairo hex. | DOC |

## 3. The six outcome categories

Printed predicates, from §60.81 (printed p. 28), as transcribed in the victory matrix. "Supply" is the
unresolved concept; section 5 treats it.

| Category | Location predicate | Printed supply reference |
| --- | --- | --- |
| Italian strategic | Hold Alexandria **or** Cairo (any hex) with a combat unit | That unit can be supplied by convoy from any hex on map D |
| Italian decisive | Take and hold (occupy) **both** Mersa Matruh and Siwa with a combat unit | Supplied by convoy from Tobruk |
| Italian tactical | Take and hold Sidi Barrani (printed "Sidi Birani", C4131); **retain** Sollum (C4021), Fort Maddalena (C3019), Giarabub | In supply, as above, to Tobruk |
| Commonwealth tactical | Take and hold Sollum, Halfaya Pass (C3922) and Siwa (C0127) | Truck-convoy trace back to Alexandria or Cairo |
| Commonwealth decisive | Take and hold Bardia, Fort Maddalena, Sidi Omar, Sollum, Siwa | "Supply as above" |
| Commonwealth strategic | Take and hold Tobruk | "Supply as above" |

### 3.1 Evaluation instant

**Source (DOC):** §60.22 ends the scenario "at the conclusion of Game Turn 6, Operations Stage 3". Land
§5.2's outline names nothing to do at "End of Game-Turn" except that the turn is complete. §60.81 states no
separate scoring moment.

**Catalogue facts (VERIFIED):** the last catalogued position of a stage is the second-player `patrol`
(`Cna1979LandSequence.cs:113`). Turn 6's last two positions are therefore
`operation-3.second-player.patrol` then `end-of-turn`. Today `GetNext` would roll turn 6 over to a turn 7
that the scenario does not contain.

**Proposed pin (ASSUMED, for ruling):** the evaluation snapshot is the world at the moment turn 6 leaves
`operation-3.second-player.patrol`, which is the entry to turn 6's `end-of-turn` position. Evaluation
happens exactly once, there. Turn 6's `end-of-turn` position then performs terminalization only and mutates
no element state (section 7). With that spec, "after patrol" and "after End of Turn" are the same snapshot,
so the choice between them is invisible. The ruling only matters if the owner later adds End of Turn
bookkeeping that changes location or supply state (D-1).

**Must-hold properties** (all DOC from the 2026-11-12 matrix and the 2026-11-18 publication note):
no mutation after terminalization; verdict computed from one immutable snapshot; exactly-once emission;
evaluation never earlier than the end of turn 6 stage 3 (the source does not authorize early termination).

### 3.2 Precedence and tie policy

Settled for planning by RUL-GRA-07 (section 2). Concrete behavior the evaluator must have:

1. **Per-side highest label only after all rows are known.** Strategic beats decisive beats tactical within
   a side. No cross-side comparison of labels is made.
2. **Both sides qualify** returns `SimultaneousQualifyingVictories` carrying each side's highest label and
   every matched row. It is not a draw and not a win. The policy does not invent a downstream consequence.
3. **Neither qualifies** returns `NoQualifyingVictory`. It is not a draw or a default win.
4. **Any unknown row** returns `Unresolved`, even when a known row already matches. This is conservative by
   design and is the single most consequential policy fact for RC: **until the supply and location
   predicates in sections 4 and 5 are ruled, every real run returns `Unresolved`.**
5. **Conditional exclusions.** Italian tactical and Commonwealth tactical/decisive share Sollum; Italian
   tactical and Commonwealth decisive share Fort Maddalena; Italian decisive and Commonwealth tactical/decisive
   share Siwa. If, and only if, "hold/retain" means exclusive current possession of one single hex, those
   row pairs cannot both match (DOC, result-order follow-up). §10.27 lets both sides "control" a hex through
   opposing ZOCs, so exclusivity is an assumption about the predicate, not a derivable fact (D-4). The
   evaluator must not hard-code the exclusion; it should detect and report a violation as an internal
   consistency error if possession is defined exclusively.

### 3.3 Hold, take and retain predicates

What is known (DOC): §60.81 uses take, hold, occupy and retain in different rows. The Italian decisive row
alone says "(occupy)". Land §10.27 says a hex can be "controlled by both players". Land §30.56 defines port
control as "the last player to occupy the hex". Scenario §62.8 (Crusader, a different scenario) defines city
control as current occupation by a combat unit of any strength, with supply. None is expressly the Graziani
predicate.

**Proposed minimum for Phase 2 (ASSUMED, requires D-2):** a side **holds** a hex if, in the terminal
snapshot, at least one qualifying element of that side has `CurrentLocationId` equal to that hex, no
qualifying element of the opposing side is in the hex, and the element is not in a state excluded by D-3.
**Take** and **retain** reduce to the same snapshot test (D-2 asks whether history is required). A hex
satisfying only ZOC control and having no occupant is **not** held.

### 3.4 Combat-unit qualification

Land §3.23 lists infantry, tanks, recce, artillery, antitank and antiaircraft as combat units. §3.31 makes an
HQ a combat unit if combat units are attached; §3.33 says an HQ with none is not. §3.34 lets armed
regimental HQs use their own strength. Engineers (§23.11) are noncombat and project no ZOC. §60.82's
parenthesis test is for the longer campaign and must not be inherited. DOC. The existing HQ and engineer
notes (2026-11-18) give concrete counters that depend on the answer (1st Libyan Regiment HQ, 2nd Libyan
Division HQ with `E`).

**Evaluator consequence:** classification must be a data-driven input (a `Func`/table keyed by element class
and attachment), returning `Unknown` for any element class the classification table does not mention.

## 4. Location identity and map identity

- **Location identity (D-5).** Each named place needs a certified set of eligible hexes, with source IDs for
  the ones the scenario prints (Sollum C4021, Halfaya C3922, Siwa C0127, Fort Maddalena C3019, Sidi Barrani
  C4131). Matruh, Tobruk, Bardia, Sidi Omar, Giarabub, Alexandria, Cairo have no recorded hex set in the
  documents I read. Alexandria and Cairo are multi-hex ("any hex"); the Air/Logistics summary lists
  Alexandria as E3613/E3714 and Cairo as five hexes (DOC) and conflicts exist between that summary and
  §60.81 for Halfaya (C3922 vs C3922/C3821). **Do not enlarge a printed single-hex objective** without a
  ruling.
- **Spelling.** Source prints `Sidi Birani`; the location list prints `Sidi Barrani` C4131. The planning
  assumption is identity; the independent blind reading differs in spelling. Needs explicit acceptance
  (D-5) before the evaluator treats them as one place.
- **Evaluator input contract.** The evaluator takes a `CampaignVictoryMapIdentity` carrying the three
  pinned hashes (hex, hexside, seam policy) and a place-to-hexes table version. It rejects any snapshot
  whose bound map identity differs. Unknown place means row `unknown`, never `no-match`.
- **Benghazi/setup coordinates** (A4827 vs B4827) are a setup question, resolved for planning by RUL-GRA-05;
  they do not feed a victory row.

## 5. Supply-route semantics

This is the largest open area. The source states a supply condition on every row, and the Land-only rules
(§32) replace ordinary supply with a motorization procedure without saying how §60.81's trace works.

**Settled (DOC):** a local §32.16 supply-unit draw is **not** the victory test; §60.82's trace length and
point scoring are not imported; directions differ (Italian: supply **from** map D or Tobruk; Commonwealth:
trace **back to** Alexandria/Cairo).

**Candidate policy, not ruled: `gra-victory-route-v2-proposed`** (2026-11-17 policy note, with open
findings from its review):

- Hypothetical, directed medium-truck connectivity: no actual shipment, no truck availability, no
  motorization, stock, CP budget or length limit.
- Terminal weather, certified directional terrain and hexside traversability, road crossings only over
  certified edges.
- Every path vertex passes occupancy and incoming-ZOC checks, including endpoints; a friendly unit in a
  hex negates enemy ZOC (§10.26); a zero-edge path is allowed only if endpoint and source-control checks
  pass on the same hex.
- Commonwealth western movement limit (§8.19) applied to route vertices as an **extrapolation**.
- Quantifiers: Italian strategic = one witness tuple; Italian decisive = a witness at each of Matruh and
  Siwa, which may differ; Italian tactical = a route from Tobruk to a subject at each of the four places;
  each Commonwealth objective traces separately (RUL-GRA-25).
- Witness binding (from the policy review): one tuple `(objective, subject hex, unit, source, path)` per
  location, all checked on the same snapshot.
- Three-valued logic: one fully certified path makes the existential `true`; `false` requires every
  eligible subject/source/path combination to be certified false or another conjunct known false; unknown
  membership, unknown incoming ZOC or an uncertified edge yields `unknown`.

**Open policy review findings carried forward:** the candidate leaks timing (verdict appears only when all
rows are known); the finite-search-domain rule needs a certified boundary; weather and the §8.19 extension
need acceptance.

**What the evaluator can do without a route ruling:** model the route as an injected
`ISupplyRouteOracle` (section 9) whose only built-in implementation returns `Unsupported`. Rows whose
required conjuncts include a route then resolve to `no-match` (if another conjunct is known false) or
`Unknown`. This keeps the evaluator honest and lets the location logic be tested now.

**Reachability caveat (ASSUMED):** no document I read proves that any category is reachable in six turns.
If, for example, the Italians cannot trace from Tobruk once the Commonwealth holds it, three rows are
unreachable by construction. The owner may want a reachability check before spending effort on routes
(see D-8).

## 6. Owner decisions needed

Numbered so Phase 2 and the PR can reference them. Each lists what the evaluator does **if unanswered**.
The recommended answer is mine (ASSUMED); the owner decides.

| ID | Question | Recommended | If unanswered |
| --- | --- | --- | --- |
| **D-0** | Confirm RUL-GRA-02, -07, -22, -25 as the working policy for the evaluator (not as official rules). | Yes. | Evaluator ships behind `Unsupported` for any policy not confirmed. |
| **D-1** | Evaluation instant: snapshot at entry to turn 6 `end-of-turn`, taken after turn 6 stage 3 second-player `patrol`; End of Turn mutates nothing. Or is any turn-end bookkeeping to occur before the verdict? | Entry to turn 6 `end-of-turn`; no bookkeeping. | Evaluator takes the snapshot as input and documents the instant as unratified. |
| **D-2** | Take / hold / retain: snapshot-only current occupation, or history required (for example "took it and held it through a whole stage", "retain" meaning never lost)? Does ZOC-only control count? | Snapshot-only current occupation by a qualifying element; ZOC-only does not count; "take" and "retain" check the same instant (no history). | All location conjuncts return `Unknown`. |
| **D-3** | Which element states disqualify a holder: `ReserveStatus`, disrupted, in-transit, mid-retreat, captured? | Exclude anything not on-map and in normal state; needs content check. | Holder qualification returns `Unknown` for any state not in a table. |
| **D-4** | Is possession exclusive per hex (one side at a time)? If both sides have units in one hex, who holds? | Exclusive; mixed occupation means neither side holds. | Mixed occupation returns `Unknown`. |
| **D-5** | Place-to-hex sets for Alexandria, Cairo, Matruh, Tobruk, Bardia, Sidi Omar, Giarabub, and acceptance of `Sidi Birani` = `Sidi Barrani` C4131; single hex versus broader place for Halfaya. | Printed single hex where §60.81 prints one; city sets from the certified map; blind second reading of the packet first. | Any place without a certified set yields `Unknown`. |
| **D-6** | Combat-unit classification for victory: general §3.23/§3.31 or a stricter test; status of HQ-with-gun and engineer-capable HQ. | General §3.23 with §3.31 (HQ counts only with attached combat units); parenthesized-rating test not applied. | Unlisted element classes return `Unknown`. |
| **D-7** | Supply abstraction: confirm hypothetical directed medium-truck connectivity (`gra-victory-route-v2-proposed`), or choose actual/feasible shipment, local §32.16 draw, or defer. Sub-decisions: length limit, weather, §8.19 western limit, ZOC and occupancy rules, source control, map-D origin quantifier ("any hex": one eligible origin or something stronger), and who must trace under "as above". | Candidate v2 with the policy review's witness binding. | Route oracle returns `Unsupported`; rows with a route conjunct are `Unknown` unless another conjunct is known false. |
| **D-8** | Reachability evidence: does the owner want a reachability study (can each of six categories be satisfied in six turns under D-7?) before investing in routes? | Yes, but only after D-7; cheap graph check. | n/a |
| **D-9** | Verdict publication: audience, timing and disclosure of an `Unresolved` result; does the intelligence plane learn anything? The 2026-11-17 review shows publish-when-known leaks that all rows were decidable. | Publish a terminal result to the authority and both players only after turn 6; treat `Unresolved` as a single indistinguishable "adjudication pending" until evidence is supplied. | Evaluator returns authoritative evidence only; no player-view filtering is implemented. |
| **D-10** | What happens when the result is `Unresolved` at the end of turn 6: block terminalization (game stays open), terminalize as "unresolved", or require an owner/adjudicator command? | Block terminalization and surface a rule-not-implemented rejection; do not guess. | Same as recommended (fail closed). |
| **D-11** | Policy/version binding: where do `gra-victory-result-v1` and the route/predicate version IDs live (ruleset identity, snapshot, event)? | In the ruleset manifest and copied into the terminal event. | Evaluator returns the version strings it was constructed with. |

## 7. End of Turn specification (answers multi-turn doc Open Question 3)

**Facts.** VERIFIED: the position exists and wraps to the next turn; no code acts on it. DOC: Land §5.2's
outline gives End of Game-Turn no named procedure; the crosswalk says admit rollover "and any separately
specified turn-end obligations" and does not list any.

**Proposed spec (ASSUMED, requires D-1 and D-12 below):** End of Turn is a **System position** (no player
choice) with one deterministic transition:

1. **Turns 1-5 (rollover).** The System step emits one `TurnEnded(gameTurn)` event and advances via
   `GetNext` to the next turn's first position (`initiative-determination`). It mutates **no** element
   state. Per-stage CP ledgers are keyed by `LedgerGameTurn`/`LedgerOperationStage` and so need no reset
   (VERIFIED: `CampaignCombatCycleMovement.cs:108` compares those fields to the current cycle; ASSUMED that
   this holds for all consumers when Turn 2 begins, which is a test obligation, not a finding).
2. **Turn 6 (terminal).** The System step takes the snapshot, calls the victory evaluator once, and
   - if the result is a known result (`Victory`, `Simultaneous`, `None`), emits one `ScenarioTerminated`
     event carrying the result, policy versions and map identity, and moves the session to a terminal
     state that rejects every later command with a stable terminal rejection;
   - if the result is `Unresolved`, fails closed per D-10.
   `GetNext` must **not** be called past turn 6; the terminal state replaces it. The catalogue itself
   remains turn-generic (its `CreateTurn(7)` is harmless data, but nothing may enter it).
3. **Exactly-once and replay.** The terminal event is derived from the journal; replay of the same
   journal yields the identical event; a second evaluation attempt is a no-op that returns the recorded
   event (idempotency key = scenario ID + turn 6 + policy version).
4. **Unsupported turn-end obligations.** Every obligation that the source may attach to the turn boundary
   (examples to be enumerated by a source pass, not assumed here: monthly or turn-keyed supply and
   motorization receipts, cohesion or readiness recovery, repair facility adjustments, arrivals and
   replacements) is either (a) already handled at the start of the next turn by the naval-convoy and
   initiative preambles, (b) explicitly ruled out of scope, or (c) rejected as `rule-not-implemented` at
   the boundary. The End of Turn step must not silently skip an obligation that a ruling later adds.
5. **Host and view.** End of Turn is auto-drained like other System steps (as for Stage 1 preamble in
   #508). Players see only turn-ended and, on turn 6, the filtered terminal result per D-9.

**Extra owner questions specific to End of Turn:**

| ID | Question | Recommended | If unanswered |
| --- | --- | --- | --- |
| **D-12** | Is any turn-end bookkeeping in scope for RC beyond rollover (supply/motorization, cohesion or readiness recovery, repairs, replacements, arrivals)? Please list which, or rule "none; handled at next turn's preamble". | None for Turns 1-5 (rollover only); the source outline lists nothing. Source pass to confirm per obligation. | Rollover only; document as a no-op with a regression test that fails when a listed obligation lands. |
| **D-13** | Is the native turn-6 terminal state a new `PendingDecision`-free terminal, or modeled as a position past `end-of-turn`? This touches `ExerciseSession.cs`, `CampaignExercises.cs` and `PendingDecision.cs`, owned by sandtable-32. | Terminal state in the session wrapper; coordinate with sandtable-32 before any wiring. | No wiring is attempted in Phase 2. |

## 8. Provenance

Documents read in full or in substantial part: `2026-11-12-graziani-victory-matrix.md`,
`2026-11-16-graziani-victory-supply-source-review.md`, `graziani-victory-review/README.md`,
`2026-11-17-graziani-victory-independent-decision.md`, `-victory-policy-next.md`, `-victory-policy-review.md`,
`-victory-city-policy-proposal.md`, `2026-11-18-graziani-victory-publication-source.md`,
`-victory-control-terms.md`, `-victory-control-policy-audit.md`, `-victory-remaining-source.md`,
`-victory-result-order-followup.md`, `-victory-hq-combat-class.md`, `-victory-engineer-class.md`,
`2026-11-12-graziani-ruling-decisions.md` (RUL-GRA-02, -07, -22, -25),
`2026-11-13-graziani-land-sequence-crosswalk.md` (End of game turn row),
`2026-11-22-multi-turn-multi-stage-scoping.md` (Open Question 3, MTS-TASK-011).

**Not read:** the remaining 2026-11-17/18 victory follow-ups (convoy-direction review, route review, city-hex
review, additional-source-search), the two JSON readings, and every original PDF. Their conclusions reach
this document only through the notes above. The rights status of the source-derived place tables is
unresolved (README carve-out); the evaluator's tables must not ship under Apache-2.0 until that is settled.

## 9. Phase 2 sketch: `CampaignVictoryEvaluator`

New files only. No edits to `ExerciseSession.cs`, `CampaignExercises.cs` or `PendingDecision.cs`.
`internal` types in `Cna.Core`, like the neighboring `Campaign*` types. The names are a proposal.

```text
src/Cna.Core/Campaigns/Victory/
  CampaignVictoryPolicy.cs        // PolicyId ("gra-victory-result-v1"), PredicateVersion, RouteVersion
  CampaignVictoryRows.cs          // static table of the six §60.81 rows: side, level, location conjuncts, supply conjunct
  CampaignVictoryInput.cs         // immutable evaluator input (below)
  CampaignVictoryResult.cs        // typed result (below)
  CampaignVictoryEvaluator.cs     // static pure Evaluate(input) -> result
  ICampaignVictoryRouteOracle.cs  // route question; default oracle returns Unsupported
  CampaignVictoryEvidence.cs      // witness tuples, reasons, deterministic ordering, codec hash
```

```csharp
internal enum VictoryTruth { True, False, Unknown }
internal enum VictoryLevel { Strategic, Decisive, Tactical }

internal sealed record CampaignVictoryInput(
    CampaignWorldSnapshotV8 World,                 // immutable; carries ContentIdentity + ScenarioId
    CampaignVictoryMapIdentity Map,                // hex/edge/seam hashes + place table version
    CampaignVictoryPolicy Policy,                  // policy and predicate versions bound to ruleset
    CampaignVictoryPlaceTable Places,              // place -> certified hex set; absent place => Unknown
    CampaignVictoryUnitClassifier Classifier,      // element -> qualifying combat unit? (True/False/Unknown)
    int GameTurn, string PositionId,               // must equal turn 6 end-of-turn entry
    string SnapshotHash);                          // proof the World equals the journaled snapshot

internal sealed record CampaignVictoryRowResult(
    string RowId, LandSide Side, VictoryLevel Level, VictoryTruth Outcome,
    IReadOnlyList<CampaignVictoryReason> Reasons,  // one per conjunct, ordinal order
    IReadOnlyList<CampaignVictoryWitness> Witnesses);

internal abstract record CampaignVictoryResult(IReadOnlyList<CampaignVictoryRowResult> Rows);
internal sealed record VictoryUnsupported(string Code, string Detail, ...);      // unratified policy, unknown map, wrong instant
internal sealed record VictoryRejected(string Code, string Detail, ...);         // forged/mismatched proof
internal sealed record VictoryUnresolved(...);                                   // any row Unknown
internal sealed record VictoryNone(...);                                         // all rows known, none match
internal sealed record VictorySingleSide(LandSide Side, VictoryLevel Level, ...);
internal sealed record VictorySimultaneous(VictoryLevel Axis, VictoryLevel Commonwealth, ...);
```

**Evaluation algorithm.**

1. Validate: `World` snapshot hash equals `input.SnapshotHash`; map identity equals the pinned
   `Cna1979HistoricalMap` hashes; `Policy` is a registered version; `(GameTurn, PositionId)` equals the
   turn 6 end-of-turn entry. Any mismatch returns `VictoryRejected` (forged or stale proof) with no
   partial rows; an unratified policy returns `VictoryUnsupported`.
2. For each of the six rows in fixed table order, evaluate each conjunct to `VictoryTruth`. Combine with
   false-dominates-conjunction (RUL-GRA-07). Existential witnesses use true-dominates-existential; witness
   lists are sorted by ordinal ID so output is order-independent.
3. Compose per section 3.2.
4. The result carries the policy, predicate and route versions, map identity and a canonical-bytes hash of
   rows plus witnesses, so replay stability is a byte-equality test.

**Purity and determinism (DET-001/EVT-001):** no clock, `Guid`, RNG, `Dictionary` iteration order, floating
point or culture-sensitive string compare; all collections sorted ordinally before use; no I/O.

**Branch tests for Phase 2** (all synthetic worlds; no claim of historical reachability):
six single-row positives; remove each conjunct in turn; both sides qualify; neither qualifies; one side
qualifies with another row `Unknown` (must be `Unresolved`); known-false row dominating an unknown conjunct;
Sollum, Siwa and Fort Maddalena shared-objective consistency checks; mixed-occupation hex; ZOC-only hex;
unlisted element class; unknown place; forged snapshot hash, mismatched map hash, wrong turn/position,
unregistered policy version; identical input evaluated twice and in reordered-element worlds gives
byte-identical results; policy-version change does not alter a stored result; route oracle `Unsupported`
versus injected-true/false/unknown.

**Phase 2 prerequisites.** At least D-0, D-2, D-4, D-5, D-6 answered for location rows; D-7 for route
rows (otherwise those rows stay `Unknown` and the evaluator returns `Unresolved`, which is a valid, tested
outcome). D-9, D-10, D-12, D-13 are needed only before any wiring, which is out of scope here.

## 10. Non-goals of this document

- No hex sets, ruling text or route algorithm is invented. Every item the sources leave open appears as a
  numbered decision with a fail-closed default.
- No claim that any victory category is reachable in six turns.
- No wiring into `ExerciseSession`, no Host, API or Maproom surface, no CI manifest rows.
