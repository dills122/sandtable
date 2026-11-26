# Load harness L1: Stage 1 opening discovery and ordering

> **2026-11-26 follow-up:** L1 measurements below are the Stage 1 discovery baseline.
> The standalone [load harness](../../tools/Cna.LoadHarness/README.md) now has both drivers,
> PostgreSQL, open Turn 2 Reserve checkpoint, full legal-choice policy and verified state fingerprints.

## Scope and result

Measured against the existing Development, loopback-only v2 HTTP fixture on the
`rules-lab.breakdown.reaction.combat-infantry.v1` board-game lab, seed 12345,
SQLite journal, Release, .NET 10.0.400, Apple Silicon. Each of two sequential
runs created a fresh journal and two seats for the same campaign. The probe
used a fixed journal clock and stopped at the Stage 2 initiative declaration
without submitting that declaration. No Core or Host behavior changed.

`LoadHarnessSpikeDiscoversStageOneAndRepeatsOrderedChoices` uses
`POST /api/v2/campaigns/{id}/openings/open` (#628) separately for each seat.
It opens each advertised stage/element, repeats that opening, and submits the
last index of the last available opening in discovery order (Axis seat first,
Commonwealth second). This is a coverage trace, not a representative strategy
or a proof of every legal path. When both discovery answers are empty, the
probe uses the prior-stage fallback data below, trying both authenticated seats.

Observed on 2026-11-25:

- Two runs reached the Stage 2 boundary with 21 accepted owner submissions each;
  each journal reconstructed successfully. System steps are separate from this
  owner-submission count.
- 23 opened stage/element cuts per run matched byte-for-byte in the same order,
  including revisions and choice counts. Repeating each opening with the same
  seat gave identical refs and revision. First refs from fresh journals differed.
- 44 read-only discovery requests, 46 repeated opening requests, 26 fallback
  opening requests (10 successful, 16 conflicts), and 21 submissions per run,
  excluding creation and seat setup. Ten of 22 discovery iterations yielded
  empty lists for both seats despite a valid direct owner opening.
- Focused test: 1 passed, 0 failed, 35.603 s test duration (36.310 s runner wall
  time). No Postgres or concurrency measurements in L1.

The Host constructs public refs from positional indices in the draft's
`ActionIds` array (`QueryOwnerOpeningCore` in
`CampaignDurableAuthorityJournal.Openings.cs`). Refs depend on the journal key,
seat epoch, revision, visible digest and index, not on lexicographic order of
the ref string. The repeated visible cuts and accepted index sequence establish
stability on this trace. They do not prove canonical ordering for unseen
branches, every possible index, every seed or a different lab. The harness
should use index selection and exclude opaque refs from deterministic identities.

## Observed next-opening ladder

Each row is data from the probe, with zero-based owner-submission ordinal.
`—` in a discovery column means that seat returned an empty list. A direct
fallback was successful for the specified audience when both lists were empty;
the other seat's request conflicted. All other listed openings came from discovery. Stage names are literal HTTP values; singleton element IDs
are `land.<stage>` unless otherwise noted.

| Ordinal | Axis discovery | Commonwealth discovery | Successful fallback (seat / stage / element) |
|---:|---|---|---|
| 0 | initiative-order | — | — |
| 1–2 | — | reserve-designation | — |
| 3 | — | movement (commonwealth-truck), movement-segment-completion | — |
| 4 | — | combat-selection (commonwealth-infantry, commonwealth-truck) | — |
| 5 | — | reserve-release (commonwealth-infantry) | — |
| 6 | — | cycle-control | — |
| 7 | — | truck-convoy-pass | — |
| 8 | — | — | Commonwealth / post-combat-pass / land.post-combat-pass |
| 9–11 | — | post-combat-pass | — |
| 12 | — | — | Axis / second-reserve / land.second-reserve |
| 13 | — | — | Axis / second-movement-completion / land.second-movement-completion |
| 14 | — | — | Axis / combat-selection / axis-infantry |
| 15 | cycle-control | — | — |
| 16 | — | — | Axis / truck-convoy-pass / land.truck-convoy-pass |
| 17–20 | — | — | Axis / post-combat-pass / land.post-combat-pass |
| 21 (stop) | — | — | Axis / stage-entry-initiative-declaration / land.stage-entry-initiative-declaration |

At ordinal 12, both direct `post-combat-pass` probes conflicted before the
second-reserve fallback. At ordinal 21, both `post-combat-pass` and
`second-reserve` probes conflicted before the successful Stage 2 declaration
probe. An earlier exploratory run that omitted the repeated-pass fallback
received a Stage 2 opening at ordinal 18 while the verified checkpoint was
still in Stage 1. The final test explicitly checks the reconstructed Stage 2
initiative-declaration checkpoint, not only opening availability. No unsupported-rule discovery entry
was observed in this trace. The separate `TwoRealHttpPlayersConductAnEntireOperationStageOne`
fixture proves additional explicit route paths; this test does not infer that
unobserved stages are unavailable.

## Fallback candidate table

The table in the test is keyed by the previously accepted action stage and
ordered as shown. These are candidates, not a rules engine or a guarantee
that a stage is open. `{seat}` is the lower-case name of the requesting seat
in this lab. Discovery remains primary and carries both `openingStage` and
`actionStage`; submit using the returned action stage, not an assumed alias.

| Previous action stage | Candidate opening stage | Acting element | Evidence |
|---|---|---|---|
| truck-convoy-pass | post-combat-pass | land.post-combat-pass | Observed |
| post-combat-pass | post-combat-pass | land.post-combat-pass | Observed |
| post-combat-pass | second-reserve | land.second-reserve | Observed |
| post-combat-pass | stage-entry-initiative-declaration | land.stage-entry-initiative-declaration | Observed |
| second-reserve | second-movement-completion | land.second-movement-completion | Observed |
| second-movement-completion | combat-selection | {seat}-infantry | Observed |
| combat-selection | cycle-control | land.cycle-control | Candidate; discovery succeeded in this trace |
| cycle-control | truck-convoy-pass | land.truck-convoy-pass | Observed |

**Implication for L2/L4:** an empty open-seat list is not sufficient terminal
evidence on the current Host. Measure direct probes separately and classify an
unresolved opening explicitly rather than reporting a finished game. The
fallback table is limited to this trace; branches such as Repeat and alternate
rounds need additional evidence before claiming coverage. No new route or gate
relaxation is necessary for this spike.

Reproduce locally:

```sh
just test-method host '*LoadHarnessSpike*'
```
