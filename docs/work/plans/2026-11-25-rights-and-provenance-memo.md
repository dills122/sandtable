# Rights and provenance memo: source-derived material in Sandtable

**Date:** 2026-11-25. **Task:** T8 (RC critical-path analysis), retargeted by the 2026-11-25 owner decision that the engine ships without source-derived data and players supply a local content pack.

**Nature of this memo.** This is a factual inventory and an option list for the project owner and any counsel they retain. It is **not legal advice and reaches no legal conclusion** about copyright, trademark, licensing, fair use, or whether any use is lawful or authorized. Where it says "reproduces", "restates" or "derived", it describes what files contain, not whether any use is permitted. No data was moved or deleted to produce it.

**Labels.** VERIFIED = checked against the repository at this branch point (`cc9c4d3d`). DOC = taken from a named project document or the 2026-11-24 IP audit without re-checking. ASSUMED = inference.

## 1. What this memo covers

1. An inventory of what the repository holds that reproduces, restates or is derived from the published game (the 1979 SPI *Campaign for North Africa*), and what provenance is recorded for each item.
2. The options for how Sandtable could be distributed, stated neutrally, with what each requires technically and what each leaves open.
3. The questions the owner or counsel would need to answer. The technical design for the first option is in `docs/work/plans/2026-11-25-content-pack-separation-design.md`.

It does not decide which option to pursue. The owner's 2026-11-25 decision is that the engine ships without source-derived data and players supply a local content pack. This memo treats that as the working direction and keeps the other options visible, because they are not mutually exclusive.

## 2. Current stated position

- VERIFIED: `LICENSE` is Apache-2.0 and the README section "Material not licensed under Apache-2.0" states a provisional, conservative position: Apache-2.0 covers original code and original engineering writing; it does **not** cover material that reproduces or closely restates the published game's charts, tables, rosters, scenario setups or map, nor related research notes. The README says rights-holder review has not been completed, that no license is granted to view, copy, redistribute or reuse that material, and that public visibility in the repository does not change this. The README itself calls this "not legal advice".
- DOC: The project's own rights notes say ownership chain is unverified and that no inquiry has been confirmed sent ([source rights status](2026-11-17-graziani-source-rights-status.md), [contact status](2026-11-17-graziani-rights-contact.md)). A draft permission inquiry exists and is explicitly "not sent".
- DOC: The repository has no scans, page images, box art, logos, counter art, fonts or VASSAL files in the current tree or in any ref in history (IP audit section 3, section 4.1, section 7).

## 3. Inventory of source-derived material

Class key (from the IP audit): (a) cited by section number only; (b) short factual parameter; (c) full or near-full reproduction of a table, chart, roster, list or map; (d) near-verbatim or close paraphrase prose. Sizes are VERIFIED from the working tree unless marked DOC.

### 3.1 Production code and data in `src/`

| Item | Paths | Size | Class | In README carve-out? |
| --- | --- | ---: | --- | --- |
| Rules tables (close assault results table, weather chart, breakdown bands, movement costs, initiative ratings, terrain enterability) | `src/Cna.Core/Rules/Cna1979{CombatAdjudication,Weather,Breakdown,Movement,InitiativeRatings,HistoricalTerrainEffects}.cs` | 6 files, 57.6 KB | (c)/(b) | Yes (all six named) |
| Rosters, weapon profiles, placements, arrival schedules | `src/Cna.Core/Content/Cna1979GrazianiSourceFacts.cs` (1,541 lines, 99.9 KB), `Cna1979GrazianiCounterFacts.cs` (6.4 KB), `Cna1979GrazianiCairoSourceExcerpt.cs` (5.8 KB) | 112 KB | (c) | Yes |
| Hex map transcription: 7,073 hexes and 4,365 hexside records | `src/Cna.Core/Content/HistoricalMap/{hexes,hexsides}.v1.json` (781 KB + 595 KB), loader `Cna1979HistoricalMap.cs` (11 KB) | 1.37 MB data | (c) | Yes (data and loader) |
| Source-backed campaign setups and turn authority | `src/Cna.Core/Campaigns/CampaignSource*.cs` | 67 files, 994 KB | (c) for data literals, (a)/(b) for logic | Yes (conservative glob) |
| Source pack artifact builders | `src/Cna.Core/Content/ContentSource*PackArtifact.cs` | 30 files, 146 KB | builders over the data above | **Not named** (README names only `CampaignSource*.cs` under `Campaigns/`) |
| Source contact/visibility helpers | `src/Cna.Core/Visibility/CampaignSource*.cs` | 3 files | mixed | **Not named** (ASSUMED: matched only if the `CampaignSource*.cs` glob is read to cover `src/`) |

VERIFIED: the three `Cna1979{Breakdown,Combat,Reaction}ContentCatalog.cs` loaders and `src/Cna.Core/Content/Fixtures/*.json` are synthetic (`rules-lab.*`, origin `Synthetic`, source `sandtable-rules-lab`). The README places them under Apache-2.0, consistent with the audit.

### 3.2 Tests and fixtures

| Item | Paths | Notes |
| --- | --- | --- |
| Tests that reference source-derived symbols | 85 files: 80 in `tests/Cna.Core.Tests`, 4 in `tests/Cna.OrleansHost.Tests`, 1 in `tests/Shared` (VERIFIED by grep for `Cna1979Graziani*`, `Cna1979HistoricalMap`, `spi-1979`, `SourceDerived`, `CampaignSource*`, `ContentSource*Pack`, source rules-table classes) | Counts files that mention the symbols, not files that embed whole tables. |
| Golden JSON restating tables | `tests/Cna.Core.Tests/Rules/Fixtures/*.golden.json` (4 files: breakdown x2, movement, weather) | DOC for contents: audit F-4, F-7. |
| Research and evidence fixtures | `docs/research/fixtures/combat-selected-source-v1.json`; `docs/specs/fixtures/combat-content-v7.{canonical,vectors}.json`; three `combat-evidence-*.zip` bundles (0.4 / 13.3 / 13.5 MB, archives of older copies of `src/`, `docs/`, `tests/` files) | DOC: audit section 3.1. The v7 canonical pack is synthetic; the vectors expand the close assault table. |

### 3.3 Documentation

| Item | Paths | Class | Notes |
| --- | --- | --- | --- |
| Double-entry transcriptions of scenario data | `docs/work/plans/graziani-*-review/*.json`, `graziani-distance-*.json` | (c) | DOC: about 352 KB plus distance files; includes a 190-cell distance chart (audit F-5). |
| Rule-by-rule analyses with section citations and close restatement | `docs/work/plans/*graziani*` (DOC: 1,194 of 1,315 top-level entries), `docs/research/*source*`, `docs/specs/graziani-*.md`, `docs/specs/weather-determination-v1.md`, `docs/specs/combat-weather-v1.md` | (d) paraphrase | DOC: audit F-6 says the paraphrase level was not judged. |

### 3.4 Third-party data and provenance

| Data | Recorded provenance | Recorded terms | Unverified |
| --- | --- | --- | --- |
| Hex and hexside data | DOC: third-hand. SPI printed map, then a VASSAL module redraw (v2.1.0), then extraction into the CNA Living Rules repository (`basmith7/cna`, revision `c8769224721feb252cef530a2099cb44efbd6f28`), then copied into Sandtable with some checks against the scan. See [map transcription spec](../../specs/graziani-historical-map-transcription.md). | DOC: the upstream states its `data/` directory is CC0-1.0 and that values are "believed to be uncopyrightable facts and game parameters". | Whether the CC0 dedication reaches beyond the upstream maintainer's own contribution; the VASSAL module's own terms; rights in the redraw; rights in underlying game artwork. The project's own note says a third-party license cannot grant rights its author does not hold. |
| Charts, tables and rosters | DOC: hand-entered from public scans hosted on spigames.net (URLs and SHA-256 per source in the [review status doc](2026-11-13-graziani-review-status.md#reacquiring-source-files)), cross-checked by OCR and, for charts, against the VASSAL module's embedded image. | DOC: the scans carry a 1979 SPI copyright notice; no redistribution license has been identified. | Present rights holder; whether the host site's public availability implies any permission (the project's note says it does not establish a license). |
| Living Rules text | DOC: consulted as a lead and oracle only. Upstream `rules/` and `rulings/` are CC-BY-SA-4.0. | Share-alike and attribution would attach if text were copied. | Not found copied (audit section 4.2); not re-checked here. |

### 3.5 Git history

DOC (audit section 7): everything in the inventory above has been publicly visible in every commit since it landed (earliest map transcription commit `f34e3e9f`), plus 182 remote branches and the zip bundles' 269 MB of older copies. A change to the current tree, including every option below, does not by itself change what history, forks, clones or caches already contain. Whether to rewrite history is an owner and counsel decision; this memo does not recommend it.

### 3.6 Naming and statements (for completeness)

DOC: the audit also flagged wording in `NOTICE`/README ("Avalon Hill/Hasbro lineage" is not supported by the repo's own rights research, which names SPI and Decision Games and calls the chain unverified) and a README sentence about "rules text". These are statement-accuracy items, not data inventory; they are listed in the audit's open questions and are not re-litigated here. ASSUMED: PR #601 and later edits may have changed some of them; check current `README.md` and `NOTICE` before relying on the audit's line references.

## 4. Options

The three options below are stated so each can be evaluated on facts. None is recommended on legal grounds. Cost figures are engineering estimates (ASSUMED) from the design doc.

### Option A: ship the engine only; players supply a local content pack

**What it means.** The distributed repository and builds contain the engine and a synthetic default content set only. Source-derived data (section 3) is not part of the distributed artifact. A player points the engine at a content pack on their own machine.

**What must be true technically** (detail in the design doc):

- The engine runs end to end on synthetic content alone, including original-valued rules tables (today the synthetic scenarios run against the real tables compiled into `src/Cna.Core/Rules/`).
- Source-derived data is loaded from a user-supplied path, hash-pinned, and validated; a campaign records the pack hash and refuses to resume without the exact pack.
- A guard in CI fails when source-derived markers appear outside the allowed paths.
- Tests that embed source-derived data move to synthetic fixtures, or to a conformance suite that runs only when a pack is present.

**What it leaves open.**

- Already-published history and forks (section 3.5).
- How a player obtains a pack. A pack builder tool or format specification can be original code, but someone must produce the data. The routes are: the player transcribes from a copy they hold, someone else distributes a pack, or the owner distributes one under some permission. Each route has its own rights questions that this option does not answer. This is a product gap as much as a legal one.
- Whether documentation that restates rules (section 3.3) stays in the public repository.
- Whether distributing a tool that knows the pack format and the names of source-derived identifiers is itself acceptable. This memo does not judge that.

**Effort and reversibility.** Largest engineering cost of the three (ASSUMED 190-410 wh across phases; see the design doc). The separation is reversible: data can be re-embedded later if permission is obtained.

### Option B: seek rights-holder permission

**What it means.** Identify the rights holder(s), request written permission for specific uses, and keep the evidence with the release record. The project's draft inquiry (source rights status doc, "Permission-review packet") lists four scopes: original or cropped scans, transcribed data, a digitized map and derived topology, and source-based rules and scenario implementations.

**What must be true.** A rights holder (or holders) must be identified. The project's notes list SPI, a later commercial publisher and the VASSAL and Living Rules contributors as leads, with chain of title unverified. The map data may have a different grantor than the charts.

**What it leaves open.** Outcome, timing, cost and scope are unknown. Permission may be partial (for example, data but not scans), conditional, or refused. No inquiry is confirmed sent (DOC). Silence is not permission.

**Effort and reversibility.** Low engineering cost, high calendar uncertainty. Does not require waiting: it can run in parallel with Option A. A grant would let the project re-embed data selectively later.

### Option C: ship derived data

**What it means.** Distribute the transcribed and derived data (tables, rosters, map, setups) with the engine, in whole or in part. Variants a reviewer could consider: as it stands today; a narrowed subset (for example only the map data that has an upstream CC0 statement); or data reworked so that it is a smaller or more abstracted derivative.

**What must be true.** The owner or counsel must be satisfied about the rights position for each shipped item. Nothing in this repository establishes that. The README currently states the opposite provisional position.

**What it leaves open.** Everything in section 3.4 "Unverified" column. A narrowed subset requires deciding which items qualify and on what basis; the audit proposes a finer-grained carve-out by content type for adoption, adjustment or rejection (audit section 6.3).

**Effort and reversibility.** Lowest engineering cost (nothing moves). Hardest to reverse once builds and packages are in third-party hands.

### Side-by-side

| | A: engine + user pack | B: seek permission | C: ship derived data |
| --- | --- | --- | --- |
| Engineering cost | High (ASSUMED 190-410 wh) | Low | Lowest |
| Depends on third party | No | Yes | No (but depends on owner/counsel comfort) |
| Real-content RC reachable | Yes, with a local pack | Possibly with grant | Yes |
| Engine RC on synthetic content reachable | Yes (prerequisite) | Not affected | Not affected |
| Changes what is already public | No | No | No |
| Open question it creates | How players get a pack | Who to ask, and the answer | Basis for each shipped item |

## 5. Questions for the owner and counsel

These restate and extend the audit's section 8. They are questions, not findings.

1. Do you want the carved-out data to remain public in this repository, move to a private or separate repository, or be removed? Is history rewriting in scope? (Audit Q1; irreversible actions need explicit owner approval.)
2. Should the close-paraphrase research notes (section 3.3) stay public under Option A?
3. Which routes for obtaining a pack (player transcription, third-party distribution, owner distribution) is the project willing to document or support?
4. Is a pack-builder or pack-validator tool, which names source identifiers but holds no tables, acceptable to ship?
5. Who is the correct rights holder per material class (rules and charts, counters and map artwork, the VASSAL redraw, the Living Rules data)? Has anyone sent the draft inquiry or received a reply?
6. For the map data: does the upstream CC0 statement, together with the VASSAL module's terms, suit counsel's purposes? Should `NOTICE` credit the Living Rules project and the VASSAL module author? (Audit Q4.)
7. Contributor policy: contributions are accepted under Apache-2.0 including into carved-out paths. Should contributors be told not to add source-derived data without review? (Audit Q7.)
8. The README names items by hand. A manifest plus CI guard (design doc, phase 0) would keep the list current. Does the owner want that manifest to be public?

## 6. What was not verified

- No scan, VASSAL module or upstream repository was re-read for this memo. Upstream license statements and the ownership chain are DOC from earlier notes.
- The 85-file test count is a symbol grep, not a review of how much data each test embeds.
- Documentation paraphrase level (section 3.3) was not judged, consistent with the audit.
- Counts were taken at `cc9c4d3d`; later merges (for example the in-flight pregame wiring in PR #615) may add references.
- No trademark search was run. No contact with any third party was made.
