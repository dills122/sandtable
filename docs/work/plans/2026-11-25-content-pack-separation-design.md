# Content pack separation: design for shipping the engine without source-derived data

**Date:** 2026-11-25. **Task:** T8 / G2.7 follow-up, retargeted to the 2026-11-25 owner decisions. **Status:** design for review. No production code, data or test was changed to write it.

**Labels.** VERIFIED = checked in the tree at `cc9c4d3d` by file read or grep. DOC = taken from a named project document without re-checking. ASSUMED = inference or estimate. Work-hour (wh) figures are all ASSUMED.

**Not legal advice.** This document describes which files contain source-derived data and how to separate them technically. It does not decide whether any use is permitted. See `docs/work/plans/2026-11-25-rights-and-provenance-memo.md` for the inventory and options.

## 1. Decisions this design applies

From the owner decisions of 2026-11-25 (and the RC analysis):

1. The engine RC is reached first on synthetic content; the real-content RC follows.
2. The engine ships **without** source-derived data. Players supply a local content pack.
3. Embedded local store for local play, Postgres for server (not affected by this design, except that a campaign must pin and re-find its pack).
4. The `/api/v2` Development-plus-loopback gate is not relaxed (not affected).
5. Nothing is moved or deleted by this task. The phases below move data only when implemented, one reviewable PR at a time.

## 2. What is source-derived, exactly

### 2.1 Four kinds of coupling

The engine does not merely hold source data beside the code; it is coupled to it in four different ways. Each needs a different separation technique.

| Coupling | Where (VERIFIED) | Consequence |
| --- | --- | --- |
| **A. Rules tables compiled into the engine as C# statics** | `src/Cna.Core/Rules/Cna1979CombatAdjudication.cs` (175 lines), `Cna1979Weather.cs` (220), `Cna1979Breakdown.cs` (428), `Cna1979Movement.cs` (264), `Cna1979InitiativeRatings.cs` (218), `Cna1979HistoricalTerrainEffects.cs` (51); 57.6 KB total | 71 other `src/` files name these classes (union count; 0 of them are `ExerciseSession.cs` or `CampaignExercises.cs`). The synthetic scenarios already run against the **real** tables, so removing them breaks the synthetic path too. |
| **B. Source data embedded as resources or literals** | Map: `src/Cna.Core/Content/HistoricalMap/{hexes,hexsides}.v1.json` (781 KB + 595 KB) read via `GetManifestResourceStream` at `Cna1979HistoricalMap.cs:170` and hash-checked at `:176-179`; csproj `EmbeddedResource` at `src/Cna.Core/Cna.Core.csproj:17`. Rosters and facts: `Cna1979GrazianiSourceFacts.cs` (1,541 lines, 99.9 KB), `Cna1979GrazianiCounterFacts.cs`, `Cna1979GrazianiCairoSourceExcerpt.cs` | Referenced from about 46 `src/` files (SourceFacts), 10 (CairoExcerpt), 7 (HistoricalMap), 4 (CounterFacts). |
| **C. Source-track logic interleaved with source data** | `src/Cna.Core/Campaigns/CampaignSource*.cs` (67 files, 994 KB; 28 reference `Cna1979GrazianiSourceFacts` or `"oa-` locators), `src/Cna.Core/Visibility/CampaignSource*.cs` (3), `src/Cna.Core/Content/ContentSource*PackArtifact.cs` (30 files, 146 KB) | The state machines (`CampaignSourceAuthorityState`, `CampaignSourceTurnAuthority`, `CampaignSourceTurnAction`, `CampaignSourceSetupBridge`) are named 369 + 61 + 35 + 15 times in `CampaignExercises.cs` and 5 times in `ExerciseSession.cs`. Some files are logic keyed to named units (for example `CampaignSourceCireneMarmaricaGroupedDefinition`, `CampaignSourceFourthIndianRoad`). Whether such a file is "logic" or "data in disguise" is unclassified (see phase P0). |
| **D. Validators that admit only synthetic content** | `ContentPackV6Validator.cs:131` ("Only synthetic scenarios are supported."), `ContentPackV7Validator.cs:29-36` (exactly one source `sandtable-rules-lab`, kind `RepositorySynthetic`, origins `Synthetic`), `ContentPackV7Models.cs:188` (`SupportedPackId = "rules-lab.content.close-assault.v1"`), `ContentPackV6Models.cs:7` (`SupportedCapabilityProfileId`) | `ContentPackV6`/`V7` cannot carry real content today. Real content travels in the V5-derived "source scheduled combat pack" format (`ContentSourceScheduledCombatPackArtifact`, `ContentScheduledCombatPackSerializer.SourceFormat`). The base validators (`ContentPackValidator.cs:116`, `ContentPackV5Validator.cs:342`) do accept `SourceDerived` origins. |

### 2.2 Source-derived data, by path

This repeats the memo's inventory with the engine-side facts that matter for separation.

| Data | Paths | Size | Kind of coupling |
| --- | --- | ---: | --- |
| Close assault table, weather chart, breakdown bands, movement costs, initiative ratings, terrain enterability | `src/Cna.Core/Rules/Cna1979{CombatAdjudication,Weather,Breakdown,Movement,InitiativeRatings,HistoricalTerrainEffects}.cs` | 57.6 KB | A |
| Weapon profiles, rosters, placements, arrivals, counter facts | `src/Cna.Core/Content/Cna1979GrazianiSourceFacts.cs`, `...CounterFacts.cs`, `...CairoSourceExcerpt.cs` | 112 KB | B, C |
| Hex map | `src/Cna.Core/Content/HistoricalMap/*.json`, loader `Cna1979HistoricalMap.cs` (187 lines; asserts 7,073 hexes at `:163` and a C/D seam check) | 1.37 MB + 11 KB | B |
| Source campaign track | `src/Cna.Core/Campaigns/CampaignSource*.cs`, `Visibility/CampaignSource*.cs`, `Content/ContentSource*PackArtifact.cs` | about 1.14 MB | C |
| Tests and goldens | 85 test files reference these symbols (80 `Cna.Core.Tests`, 4 `Cna.OrleansHost.Tests`, 1 `tests/Shared`); 4 `tests/Cna.Core.Tests/Rules/Fixtures/*.golden.json` | | see section 6 |
| Research fixtures and notes | `docs/research/fixtures/`, `docs/specs/fixtures/` (incl. three `combat-evidence-*.zip`), `docs/work/plans/*graziani*`, `docs/specs/graziani-*.md` | | documentation, outside this engine design (owner question in the memo) |

### 2.3 What is already synthetic and stays

VERIFIED: `src/Cna.Core/Content/Fixtures/*.json` (25 files; `rules-lab.*`), `Cna1979SyntheticContentCatalog.cs` (pack `rules-lab.content.movement-contact.v1` as legacy and V5 artifacts; V6 packs come from the breakdown catalog; resolvers `Resolve`, `ResolveV5`, `ResolveV6`), `Cna1979{Breakdown,Combat,Reaction}ContentCatalog.cs` loaders, `Cna1979SetupCatalog.cs`, `Cna1979BreakdownSetupCatalog.cs`, and the 29 files under `scenarios/` (`exercises/`, `maneuvers/`). The README already places these under Apache-2.0.

## 3. Target architecture

```
                      distributed artifact (Apache-2.0 + synthetic)            on the player's machine
  +--------------------------------------------------------------+      +---------------------------------+
  | Engine (Cna.Core, Host, Maproom)                              |      | Local content pack (data only)   |
  |  - logic, codecs, validators, resolvers                       |      |  pack-index.json  (hash-pinned)  |
  |  - synthetic default pack (embedded, original values)         | <--- |  rules/*.json   (rules tables)   |
  |  - ContentPackResolver chain: [local pack dir] -> [synthetic] |      |  map/*.json     (hexes, sides)   |
  +--------------------------------------------------------------+      |  content/*.json (V5/V6/V7 packs) |
                                                                          |  facts/*.json   (rosters, setups)|
                                                                          +---------------------------------+
```

Design rules:

1. **Data only.** A pack contains JSON (and optionally JSON sidecars). The engine never loads assemblies, scripts or any executable from a pack path. This removes a code-execution surface and keeps the loader reviewable. (Alternative considered and rejected as default: a compiled content assembly. It would be a mostly mechanical move of today's files, but it executes foreign code and cannot cleanly separate the source-track state machines, which `CampaignExercises.cs` names directly.)
2. **Hash-pinned everywhere.** Every file in a pack has a SHA-256 in the pack index; a campaign records the pack identity it was created with; a resume or replay that cannot find bytes with the same hash fails closed. This extends today's mechanism (`IContentPackV6Resolver.ResolveV6(packId, expectedHash)` returning `UnknownPackId` or `HashMismatch`, `CampaignContentContext.cs:28-38`, `ContentCatalogRejectionReason` at `Cna1979SyntheticContentCatalog.cs:5-10`).
3. **The default is synthetic and complete.** With no pack configured the engine is fully demonstrable (section 5).
4. **Fail closed, name what is missing.** A campaign that needs a pack the machine does not have reports "content pack `<packId>` `<hash>` required" and does not run, rather than silently substituting synthetic content.
5. **No judgement of correctness.** The loader proves bytes and structure, not that a transcription is faithful. Faithfulness belongs to the review kit and a conformance suite (section 6).

## 4. Loader design

### 4.1 Pack layout and index

Directory (or zip of the same layout, hashed as the directory index):

```
<pack-root>/pack-index.json         canonical JSON, sha256 = pack identity
<pack-root>/rules/<artifact>.json   one file per rules artifact
<pack-root>/map/hexes.v1.json, hexsides.v1.json
<pack-root>/content/<packId>.json   V5 source / V6 / V7 content packs and sidecars (combat-source-v1 etc.)
<pack-root>/facts/<name>.json       roster, setup, arrival data now literal in C#
```

`pack-index.json` (new format `sandtable.content-pack-index.v1`, ASSUMED name) carries: `packSetId`, `rulesetId`, minimum engine contract version, a table of `{path, sha256, role, id}` entries for every file, and a free-text `provenance` block written by the pack author (what the data was derived from; it is carried and displayed, never interpreted). Canonical serialization follows the existing pattern (`ContentPackV6Artifact.Create` → `SerializeCanonical` → `sha256:<hex>`; VERIFIED in `ContentPackV6Artifact.cs`).

### 4.2 Resolution

A `LocalContentPackResolver` implements the three existing interfaces (`IContentPackResolver`, `IContentPackV5Resolver`, `IContentPackV6Resolver`; VERIFIED in `CampaignContentContext.cs:5-18`) and is composed in front of `Cna1979SyntheticContentResolver.Instance`:

1. Configured via host setting (for example `Sandtable:ContentPackDirectory`) or CLI/env for the runner and local simulator. The Host wires resolvers at `src/Cna.OrleansHost/Program.cs:12` and `:42` only (VERIFIED), so the Host change is two lines plus configuration.
2. Startup scan reads `pack-index.json`, checks its hash against a file-level pin if supplied, and builds an in-memory map `packId → file`. No file is read twice; no network; no directory walking beyond the index entries (path traversal and symlinks rejected).
3. Lazy parse with the existing deserializers (`ContentPackV6Serializer.Deserialize`, `ContentPackV7Serializer.Deserialize`, `ContentScheduledCombatPackSerializer.Deserialize*`; VERIFIED present). Existing size and depth caps apply (for example `ContentCombatSourceV1.Parse` rejects over 65,536 bytes and depth over 16); per-file caps for map and facts files are new (ASSUMED 4 MB).
4. **Byte identity:** re-serialize with the canonical serializer and require equality with file bytes, so a file cannot carry content that hashes differently from what was validated.
5. Validate with an admission policy (section 4.3), compute identity, compare with `expectedHash`, return `Resolved` or `Rejected(UnknownPackId | HashMismatch)`; add `Invalid` as a new rejection reason carrying diagnostics.
6. Cache the parsed artifact per hash for the life of the silo. This composes with the grain-resident verified working set (L10b): the pack is loaded once per silo, not per grain activation. (ASSUMED: L10b's working set keys on content hash; not re-checked.)

### 4.3 Admission (the G2.7 gap)

Replace the hard-coded synthetic gates with a policy object, default behaviour unchanged:

```
ContentAdmissionPolicy { AllowedOrigins: Synthetic | SourceDerived, AllowedSourceKinds, SupportedPackIds, CapabilityProfiles }
```

- `ContentPackV6Validator.cs:131` and `ContentPackV7Validator.cs:29-36` consult the policy instead of constants. The built-in policy used by the synthetic catalog keeps today's strictness, so every existing synthetic test keeps passing unchanged.
- `ContentPackV7Models.SupportedPackId` stops being a single constant: a pack index lists which pack ids it supplies, and the policy for a local pack allows exactly those ids.
- The policy for a local pack allows `SourceDerived` origins only when `pack-index.json` declares them, so a mislabelled synthetic pack cannot masquerade and a source-derived pack cannot claim `Synthetic`.
- Reference validation (every origin reference resolves in the pack's `sourceIndex`), stable-ID rules (`ContentContractGuards.RequireStableId`) and duplicate-ID checks already exist in the base validators and are reused.

This is the work the RC analysis called "content pack format admission" (G2.7, 15-30 wh); the local resolver adds to it.

### 4.4 Rules tables from a pack

Rules tables are the hardest part because they are compiled statics (coupling A) **and** part of the pinned ruleset identity: `Cna1979Ruleset.Manifest` is the canonical ruleset hash, tested by `IsCanonicalHash` (`Cna1979Ruleset.cs`), and 63 `src/` files mention `RulesetId`. Replacing table values changes that hash by design.

The codecs needed already exist as internal artifacts: `WeatherRulesArtifactCodec`, `MovementRulesArtifactCodec`, `BreakdownRulesArtifactCodec`/`V2`, `CombatRulesInputArtifactCodec`, `ReserveRulesArtifactCodec`, with `RulesetArtifact` carrying `ContentHash` (VERIFIED in `src/Cna.Core/Rules/`). The design is:

1. Introduce `IRulesTables` (immutable, hash-identified) with the lookups the static classes expose today (for example `Cna1979Weather.Definition.Seasons`, `Cna1979Movement.LookupTerrain/LookupRoute/LookupHexside`, `Cna1979CombatAdjudication`'s table, initiative ratings, terrain effects).
2. Keep the six static classes as thin facades over a default `IRulesTables` (the synthetic one) during migration, so the 71 consumers can move in small PRs rather than one.
3. Pin the **rules-table-set hash** into the ruleset manifest and the campaign setup, so a replay selects the same tables. The ruleset hash becomes a function of table-set hash plus engine contract. This is design fork F1 (section 9).
4. Real tables are admitted from `rules/*.json` through the existing codecs; the pack index names which artifact fills which slot.

## 5. The synthetic default pack

### 5.1 What exists (VERIFIED)

Content pack `rules-lab.content.movement-contact.v1` (legacy and V5), the V6 breakdown packs, and the breakdown and combat families under `src/Cna.Core/Content/Fixtures/` (25 JSON files, including `combat-source-v1` and `retreat-source-v1` sidecars bound by pack id and hash); setups in `Cna1979SetupCatalog.cs` and `Cna1979BreakdownSetupCatalog.cs`; exercise and maneuver manifests in `scenarios/`; the Host default catalog (`Program.cs:12,42`); the Stage 1 HTTP end-to-end test fixture (`CombatInfantryRequest()`).

### 5.2 What is missing for "demonstrable without source data"

1. **Original-valued rules tables.** Today the synthetic scenarios exercise the real close assault table, weather chart, breakdown, movement and initiative values. A synthetic table set must be authored from design intent (for example plausible band shapes) and must not be seeded from the source charts; its provenance file should say how it was authored. Whether synthetic tables can stay structurally identical to the real ones (same bands and keys, different numbers) is a design choice; identical structure keeps every test meaningful. ASSUMED 15-25 wh.
2. **A map.** No synthetic hex map exists: synthetic content uses `ContentTopology` locations, while the 7,073-hex map is source data used only by the source track. Engine RC needs a small synthetic map (ASSUMED 40-120 hexes, generated by a script from a seed so no data file is derived from the real map) for movement, ZOC and terrain effects on the native chain.
3. **A six-turn synthetic scenario** with two sides, formations, arrivals, supply, weather, victory conditions (depends on the victory evaluator work) so a complete six-turn playthrough runs on public data. ASSUMED 20-40 wh, and **may already be planned under the engine RC on synthetic content**; if so, this phase is shared and not extra.
4. A `SYNTHETIC-PROVENANCE.md` stating origin and authoring method of every synthetic table and the map generator seed.

### 5.3 Demonstrable-without-source acceptance test

Run the runner and the Host with no pack configured; create a campaign; play to turn 6 and victory; save and resume; assert the repository passes the guard in section 7 with an empty allowlist (after phase P8).

## 6. Moving tests and fixtures

Tests that reference source-derived symbols fall into four groups. Counts are file-level and VERIFIED unless marked.

| Group | Examples | Count (VERIFIED by grep) | Treatment |
| --- | --- | --- | --- |
| **T1: generic engine logic using the source excerpt only as convenient data** | Tests that call `GrazianiSourceExcerpt.Build()` / `Cna1979GrazianiCairoSourceExcerpt` | 33 files reference the excerpt | One indirection point: `tests/Shared/GrazianiSourceExcerpt.cs` is linked into two test projects (`Cna.Core.Tests.csproj:150`, `Cna.OrleansHost.Tests.csproj:11`). Repoint it at a synthetic excerpt of the same shape; most T1 tests then need no edit. |
| **T2: tests that assert source facts** | `GrazianiSourceAuthorityTests`, `Cna1979HistoricalMapTests`, `CampaignSourceSeamCandidateTests`, `GrazianiHostStressTests`, `GrazianiSourceJournalTests` | 16 files reference `Cna1979GrazianiSourceFacts`; 4 reference `Cna1979HistoricalMap` | Move to a **conformance suite** that runs only when a pack path is supplied (for example env `SANDTABLE_CONTENT_PACK`) and reports skipped otherwise. The suite lives with the pack, not in public CI. |
| **T3: golden JSON restating tables** | `tests/Cna.Core.Tests/Rules/Fixtures/cna-1979.1.{breakdown-tables.v1,breakdown-tables.v2,movement-tables.v1,weather-tables.v1}.golden.json` | 4 | Regenerate against the synthetic table set; real goldens ship with the pack. |
| **T4: tests that name real units or places but only incidentally** | remaining of the 85 | about 85 minus the above (ASSUMED 20-30) | Rename identifiers to synthetic names; mechanical. |

Impact on the CI timing manifest: moving tests to skipped changes measured rows. Follow `AGENTS.md` "Registering New Test Rows" as of the branch point: each PR that adds or renames test rows registers a new file in `.github/scripts/long-suite-weights/`. The weights design change in #617/#618 may make this automatic. The conformance suite is a separate test assembly outside the public solution filter if it needs to stay out of `Sandtable.slnx` (as the load harness already is).

## 7. Guard against regression

Add `SOURCE-DERIVED.manifest` (path globs) and a script `.github/scripts/source_derived_guard.py` run in CI:

- Fail if any tracked file outside the manifest contains the markers `ContentOriginKind.SourceDerived`, `spi-1979`, `Cna1979GrazianiSourceFacts`, `"oa-` followed by a locator pattern, or any known source identifier list.
- Fail if a manifest path no longer exists (keeps the list honest).
- The manifest shrinks to empty as phases P2-P6 land; the empty manifest is the machine-checkable definition of "engine ships without source-derived data".
- This is the audit's own suggestion (audit section 6.3, last paragraph). The manifest is public by construction; whether that is acceptable is a memo question.

## 8. Phased task list

All wh values are ASSUMED. "Hot files" means `ExerciseSession.cs`, `CampaignExercises.cs`, `PendingDecision.cs` (exclusive to sandtable-32). "#615" means the in-flight pregame wiring in `CampaignHostAdapter.cs`, `CampaignDurableAuthorityJournal*`, API endpoints (also touches `CampaignExercises.cs`; VERIFIED from the PR file list).

| Phase | Task | wh | Files touched (new or changed) | Hot files | #615 | Depends on |
| --- | --- | ---: | --- | --- | --- | --- |
| **P0** | Classification and guard: classify each of the 67 + 3 + 30 source-track files as data, logic, or logic-keyed-to-named-units; add `SOURCE-DERIVED.manifest` and the guard script (report-only first) | 5-10 | new files under `.github/scripts/`, one doc; no `src/` change | none | none | none |
| **P1** | Admission policy and local resolver: `ContentAdmissionPolicy`, relax V6/V7 gates through it, `LocalContentPackResolver`, pack index format, rejection reason `Invalid`, Host/runner configuration, missing-pack error surface | 25-45 | `src/Cna.Core/Content/ContentPackV6Validator.cs`, `ContentPackV7Validator.cs`, `ContentPackV7Models.cs`, new `Content/LocalContentPack*.cs`, `Campaigns/CampaignContentContext.cs` (interfaces), `src/Cna.OrleansHost/Program.cs` | none | `Program.cs` only; no overlap with `CampaignHostAdapter.cs` or Journal | none |
| **P2** | Rules-table seam: `IRulesTables`, facades over the six static classes, migrate the 71 consumers in slices, pin table-set hash in the ruleset (fork F1) | 40-80 | `src/Cna.Core/Rules/*`, plus 71 consumers in `Campaigns/`, `Setups/`, `Observations/`, `Rules/` | none (VERIFIED: no consumer is a hot file) | none (`CampaignHostAdapter.cs` not a consumer) | P1 for loading; seam itself can start earlier |
| **P3** | Map from pack: move `HistoricalMap` resource loading behind the resolver; keep the 7,073-hex and seam assertions as pack conformance, not engine constants | 10-20 | `Cna1979HistoricalMap.cs` and its 7 consumers (`CampaignSourcePlayableWorld`, `CampaignSourceTurnAuthority`, ...), csproj | none | none | P1 |
| **P4** | Facts to data: extract the contents of `Cna1979GrazianiSourceFacts.cs`, `CounterFacts.cs`, `CairoSourceExcerpt.cs` into pack JSON with typed readers; delete literals from the engine | 40-80 | the three files; about 46 consumers (mostly `Content/ContentSource*` and `Campaigns/CampaignSource*`) | none | `CampaignDurableOutwardApi.cs` references source symbols: avoid it; adapt via the facade | P1, P0 |
| **P5** | Source-track data literals in the 28 data-bearing `CampaignSource*.cs` files: extract data, keep logic; generalize any file classified "logic keyed to named units" | 60-120 | the 28 files plus `Visibility/CampaignSource*.cs` | none, provided public/internal signatures of `CampaignSourceAuthorityState`, `CampaignSourceTurnAuthority`, `CampaignSourceTurnAction`, `CampaignSourceSetupBridge` do not change (they are named 480 times in `CampaignExercises.cs`) | none | P0, P4 |
| **P6** | Synthetic default content: original-valued rules tables, generated small map, six-turn scenario, provenance file (shared with engine RC synthetic content if planned there) | 35-65 | new `Content/Fixtures/*`, new generator under `tools/` or `scenarios/` | none | none | P2 for tables |
| **P7** | Test migration: repoint `GrazianiSourceExcerpt`, rename incidental source names (T4), regenerate goldens (T3), build conformance suite (T2); timing weights need no registration for ordinary rows | 40-90 | the 85 test files, `tests/Shared/GrazianiSourceExcerpt.cs`, new conformance project outside `Sandtable.slnx` | none | `tests/Cna.OrleansHost.Tests/GrazianiPregameJournalTests.cs` is in #615: do that file last, after #615 merges | P4-P6 |
| **P8** | Pack author documentation and validator CLI (`pack validate <path>`: hashes, schema, admission, reference check; prints which campaigns the pack can host); switch the guard to enforcing with an empty manifest; update README | 15-30 | docs, a small CLI project, `.github/` | none | none | P1-P7 |

Total ASSUMED 270-540 wh; 235-475 if P6 is already counted under the synthetic engine RC. The critical path is P0 → P1 → P2 → P6 (engine runs on synthetic tables and map without the real ones in the binary) with P3-P5 parallelizable behind P1 by file ownership.

### 8.1 File-disjointness summary

- **Hot files:** no phase edits them. VERIFIED: none of the 71 rules-table consumers and none of the `Content/` files are `ExerciseSession.cs`, `CampaignExercises.cs` or `PendingDecision.cs`. The only coupling to the hot files is through the source-track state types, and phases P4-P5 are constrained not to change those types' signatures. If a phase must, it waits for the sandtable-32 chain.
- **#615:** avoid `CampaignHostAdapter.cs`, `CampaignDurableAuthorityJournal*.cs`, `CampaignDurableOutwardApi.cs` and `GrazianiPregameJournalTests.cs`. P1 touches `Program.cs` only; rebase on #615 after it merges before P4 and P7.
- **Sandtable-38 (Sequencing, new files):** no overlap.
- **Within this design:** P2, P3, P4, P5 touch disjoint source files except shared consumers in `Campaigns/` (for example `CampaignSourceTurnAuthority.cs` consumes both the map and rules tables); serialize those slices.

## 9. Design forks for the owner

Each of these changes what is built; recommended default in bold.

- **F1. Ruleset hash and table sets.** Pin a rules-table-set hash inside the ruleset identity so a replay selects the same tables (**recommended**), or keep one canonical `cna-1979.1` hash and treat tables as ambient configuration. Ambient is less work but a campaign could replay differently if the wrong tables are loaded, which breaks the determinism the engine is built on.
- **F2. Pack contents: data only or also code.** **Data only.** A code-bearing pack is cheaper to produce from today's files but executes foreign code and cannot be reviewed as data.
- **F3. Missing pack at resume.** **Campaign is listed as "locked: content pack required" and cannot proceed**; alternative is to refuse to list it. Silent fallback to synthetic is not offered.
- **F4. Who authors the synthetic tables.** An owner decision, because it is a design-balance task that affects how convincing the public demo is.
- **F5. Source-track logic keyed to named units.** If P0 finds files whose logic is specific to real unit names and places, they either become data-driven (more work, P5 upper end) or are treated as source-derived and move to the pack side as data, losing code reuse.
- **F6. Pack format family.** Extend the existing V5/V6/V7 families with an index (**recommended**), or define one new unified format. The first reuses codecs, validators and hash identity; the second is cleaner but is a larger change to a frozen-bytes ecosystem.

## 10. Risks and what was not verified

- Estimates are ASSUMED, not derived from a task-by-task bottom-up. P2 and P5 carry most of the uncertainty (71 and 28 files).
- The "logic keyed to named units" classification (P0) has not been done; section 2.1 coupling C may be larger than the 28-file count suggests.
- Not verified: whether L10b's working set keys on content hash; whether any other consumer reads `HistoricalMap` resources by name; how `CampaignSnapshotValidator` handles a ruleset hash other than the canonical one (it references `Cna1979Weather`, `RulesetId`).
- Separation does not change what history, forks and caches already contain (memo section 3.5).
- A pack with no authoring route is a product gap (memo section 4, option A). This design provides the format and validator, not a source of pack data.

## 11. Reproducing the counts

```sh
grep -rlwE "Cna1979(CombatAdjudication|Weather|Breakdown|Movement|InitiativeRatings|HistoricalTerrainEffects)" src --include='*.cs'
grep -rlwE "Cna1979(GrazianiSourceFacts|GrazianiCounterFacts|GrazianiCairoSourceExcerpt|HistoricalMap)" src --include='*.cs'
grep -o "CampaignSource[A-Za-z0-9]*" src/Cna.Core/Exercises/CampaignExercises.cs | sort | uniq -c | sort -rn
grep -rlE "Cna1979Graziani|Cna1979HistoricalMap|spi-1979|SourceDerived|GrazianiSourceExcerpt|CampaignSource|ContentSource[A-Za-z]*Pack|Cna1979CombatAdjudication|Cna1979Weather\b|GrazianiSource" tests --include='*.cs'
```
