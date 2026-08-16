# Initiative Determination Spike

**Status:** Implemented and independently reviewed

**Date:** 2026-08-15

**Rules target:** `cna-1979.1`

## Decision

Implement Initiative Determination as the first random, authoritative campaign mechanic. A single
typed command will resolve either a scenario-predetermined holder or an opposed one-die contest,
record the complete explanation as an event, and stop at Naval Convoy.

Use a repository-owned, versioned SHA-256 counter stream for random draws. Do not use
`System.Random` as a persisted replay contract and do not let callers supply ratings, dice, or the
winner.

Before implementation, correct the current sequence model: the side holding initiative and the
side acting first in an Operation Stage are separate facts. The holder chooses first or last for
each of the three stages. The currently encoded first/last/first pattern is an example permitted by
the rules, not a fixed actor schedule.

## Question and scope

This spike asks:

> What is the smallest source-faithful, deterministic design that resolves Game Turn Initiative
> Determination, replays exactly, preserves later player choices, and stops honestly at the next
> unsupported phase?

The work covers Land Rules 7.1 and 7.2, the Initiative Ratings Chart, the September 1979 errata,
the current Umpire contracts, and deterministic .NET implementation options. The spike itself did
not implement the mechanic, ingest a published scenario, or redistribute source scans or rules
prose; the linked specification and design now govern the completed implementation.

## Method

- Rendered and visually inspected the image-only Land Rules and common chart scans retained
  outside Git.
- Text-searched the OCR-enabled September 1979 errata for relevant locators and initiative terms.
- Compared source behavior with the merged sequence and campaign contracts.
- Checked official Microsoft documentation before selecting a persistent random-stream contract.

## Source findings

### Documented facts

| Fact | Stable source reference |
| --- | --- |
| Initiative is determined at the beginning of each Game Turn. | `spi-1979-land-rules:7.12` |
| Each side has an Initiative Rating that may vary by date. | `spi-1979-land-rules:7.13` |
| Each side rolls one die and adds its Initiative Rating; the higher total wins and tied totals are rerolled. | `spi-1979-land-rules:7.14` |
| The holder keeps initiative through all three Operation Stages. | `spi-1979-land-rules:7.12` |
| The initiative side is the first player unless it elects otherwise, and that election is made separately for each Operation Stage. | `spi-1979-land-rules:7.11`, `7.14` |
| First-Game-Turn initiative is normally assigned by the scenario rather than rolled. | `spi-1979-land-rules:7.15` |
| Commonwealth rating is 3 on Game Turns 1-42, 4 on 43-90, and 5 on 91-111. | `spi-1979-common-charts:initiative-ratings` |
| Axis rating is 6 with Rommel on a Game Map, 3 with German land combat units but no Rommel on a Game Map, and 1 with neither. | `spi-1979-common-charts:initiative-ratings` |
| Tripoli/Tunisia Holding Boxes on Game Map A do not count as a Game Map for the Axis rating test. | `spi-1979-common-charts:initiative-ratings-note` |

The September 1979 errata search found no initiative-specific correction. This is a recorded
observation, not a claim that the errata can never affect future initiative-adjacent scenario data.

### Consequences

- `initiativeHolder` must not be named or interpreted as `firstPlayer`.
- Stage order is a later player declaration and cannot be precomputed from the initiative result.
- The current code's `spi-1979-land-rules:7.12` actor-order reference is stale: the implementation
  migration must replace it with separate `7.11` and `7.14` references while retaining `7.12` for
  timing and holder duration.
- Commonwealth ratings derive only from Game Turn; Axis ratings derive from authoritative map
  presence facts.
- Ratings are normalized rules data and must participate in the canonical ruleset hash.
- A published scenario can assign the first holder without consuming randomness.
- Synthetic rules-laboratory setups may exercise predetermined and contested paths, but they must
  be labeled as synthetic and use the same resolver as published content.

## Randomness options

| Option | Strength | Failure mode | Decision |
| --- | --- | --- | --- |
| Seeded `System.Random` | Minimal code | Microsoft does not guarantee identical sequences across major .NET versions | Reject as replay contract |
| Caller-supplied dice | Easy testing | Moves authoritative outcomes outside the Umpire and makes forged results valid commands | Reject |
| Store only winner and rolls in event | Replay is possible | Same seed and command stream cannot independently reproduce the event; cursor continuity is absent | Reject |
| Versioned SHA-256 counter stream | Portable, explicit, dependency-free, random-access validation | Requires a small byte-level specification | Adopt |

Microsoft documents that the `System.Random` implementation is not guaranteed to remain the same
across major .NET versions. .NET 10 exposes `SHA256.HashData`, allowing the project to own a stable
algorithm without adding a dependency. SHA-256 is used here as a deterministic mixing function,
not for secrets or cryptographic authentication.

## Recommended random-stream contract

The initial algorithm identifier is `sandtable.sha256-counter.v1`.

1. Encode the 19-byte ASCII domain `sandtable.random.v1`, followed by one zero byte, the unsigned
   64-bit campaign seed in big-endian form, and an unsigned 64-bit block index in big-endian form.
2. SHA-256 of that 36-byte input is the corresponding 32-byte stream block.
3. Random cursor `n` selects block `n / 32` and byte `n % 32`.
4. A d6 examines bytes until one is below 252, then returns `(byte % 6) + 1`. Every examined byte,
   including a rejected byte, advances the cursor.
5. Initiative consumes one Axis d6 and then one Commonwealth d6 per round. A tied modified total
   starts another round in the same order.

This fixed encoding and rejection sampling avoid modulo bias and runtime-dependent behavior. Seed,
algorithm identifier, and next-byte cursor are authoritative state. They must not appear in a
side-specific observation or public log before their future results are no longer actionable.

## Setup boundary

The resolution command must carry only optimistic-concurrency and expected-position fields. It
must not carry a rating, presence flag, roll, or holder.

Those facts come from validated campaign setup/state:

- resolution mode: `predetermined` or `contested`;
- predetermined holder when applicable;
- Game Turn from the sequence position;
- Rommel's typed initiative location; and
- the typed initiative locations of German land combat units.

The minimal location vocabulary distinguishes `qualifying-game-map`,
`tripoli-tunisia-holding-box`, and `off-map-or-unavailable`. A pure classifier derives the three
Axis rating cases from those facts. This keeps the published Holding Box exclusion testable without
introducing map topology or unit movement into the slice.

Until versioned scenario content exists, integration demonstrations should use two stable, clearly
synthetic rules-laboratory setups selected by identifier: one predetermined and one contested. The
setup registry, not a free-form create command, supplies the typed facts. Published scenario
initialization remains a separate content-ingestion task.

## Alternatives rejected

- Advancing generically and rolling later would cross a mandatory rule without an event.
- Treating the initiative holder as first actor would delete the three stage-order decisions.
- Hard-coding chart values inside the campaign engine would bypass source provenance and ruleset
  hashing.
- Implementing a general dice-expression language now would add abstraction unsupported by a
  second use case.
- Allowing the Intelligence plane to roll or select initiative would violate the authority
  boundary and make deterministic fallback irrelevant.

## Resulting implementation boundary

The accepted command emits one `InitiativeDetermined` event and projects to Naval Convoy. Generic
sequence completion remains rejected. Naval Convoy, Initiative Declaration, stage execution,
weather, full scenario ingestion, Chronicle persistence, Orleans scheduling, and UI are out of
scope.

The detailed acceptance contract is in the
[Initiative Determination specification](../specs/initiative-determination.md), and the proposed
types and task sequence are in the
[Initiative Determination technical design](../design/initiative-determination.md).

## Sources

- [Original Land Rules scan](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
- [Original common charts scan](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf)
- [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
- [Microsoft `System.Random` implementation notes](https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-random)
- [Microsoft .NET 10 `SHA256.HashData`](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.sha256.hashdata?view=net-10.0)
