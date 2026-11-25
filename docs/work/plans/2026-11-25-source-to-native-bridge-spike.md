# Spike T1: how would a real source world seed the native contact chain at Operation Stage 1 Movement? (2026-11-25)

**Status:** spike report. This document is the only change. No production source and no test file is changed or added;
the throwaway hand-off test is pasted in section 7 and was not committed. Baseline: `origin/main` = `02280697`.

**Evidence labels.** **VERIFIED** = I read the cited code, or ran the throwaway test, at the baseline commit.
**DOC** = a repository document says so and I did not re-check it. **ASSUMED** = my inference or estimate, basis stated.
"wh" = active agent-worker hours. I ran one targeted test (section 7) and no other test; nothing here was verified by a full-suite run.

## 0. Verdict

1. **There is no hand-off to seed. The two engines do not share a content type, a world type, or a creation path.**
   The native chain only accepts a *synthetic* `ContentPackV6Artifact` chosen from a closed, embedded list
   (`Cna1979SyntheticContentCatalog.ResolveV6`, `src/Cna.Core/Content/Cna1979SyntheticContentCatalog.cs:93-107`).
   The source track holds a `ContentSourceScheduledCombatPackArtifact` (a V5-shaped source pack) and its own authority state.
   Handing the Cairo excerpt to native creation fails at the first step with `UnknownContent` (section 7, STEP1). VERIFIED.
2. **Even if the pack were admitted, the native V6 validator rejects the Cairo excerpt with 147 issues** (section 7, STEP2),
   and the validator's *design* (not just missing data) forbids real Graziani content: every element must be a lone
   independent battalion, mobility is only non-motorized or Truck, scenarios must be `Synthetic`, one stack value of 1 per hex
   per side (`src/Cna.Core/Content/ContentPackV6Validator.cs:59-70, 72-80, 110-116, 131-135, 148-164`). VERIFIED.
3. **The historical map is not consumed by the native chain at all.** `Cna1979HistoricalMap` (7,073 hexes) is referenced only
   by source-track files (list in section 4). Native movement uses the pack's own `Locations`/`Edges` plus `Cna1979Movement`,
   which knows **2 terrains of 11** (clear, desert), **4 edge kinds of 12** and **one organization** (battalion = 1 stack point).
   31.2% of hexes (2,204 of 7,073) and 3,052 hexside features have no native movement cost (section 7, STEP4). VERIFIED.
4. **`IsGameplayReady` is permanently false by construction, but that gate is the wrong thing to fix.** Two of its 25 blocker strings
   are fixed constants (`battlefield-incomplete`, `control-incomplete`). Source-turn tests bypass the gate by invoking the private
   constructor by reflection (`tests/Cna.Core.Tests/Campaigns/GrazianiSourceAuthorityTests.cs:1745-1767`). Flipping the gate would start
   the *source* turn engine, which implements Stage 1 Movement only for the Fourth Indian opening move
   (`src/Cna.Core/Exercises/CampaignSourceSetupBridge.cs:89-102`) and has no Combat. It would not feed the native chain. VERIFIED.
5. **No existing test drives source content through native Combat selection.** The source track has a separate, ledger-only compulsory-combat
   record that says resolution is out of scope. VERIFIED (section 6).
6. **Estimate.** A bridge that makes the real Graziani Stage 1 Movement-to-Combat playable on the native chain is about
   **70-140 wh** of engineering plus an owner ruling on fidelity (section 8). The three smallest steps that retire most of the
   uncertainty are about 8-14 wh (section 8, items 1-3). ASSUMED (basis: analogues in section 8).

## 1. Scope and method

Goal from the brief: find out exactly how a real source world would seed the native contact chain at Operation Stage 1 Movement;
name the world types; list the per-element data the native Combat/ZOC/Movement code needs that the source track does not provide;
classify `ReadinessBlockers()` as real or constant; check for any test that drives source content through Combat selection;
run one throwaway hand-off test with the Cairo excerpt. Method: code reading with `grep` and line citations, plus one targeted test run
(`dotnet test tests/Cna.Core.Tests --filter-method "*CairoExcerptHandOffRecordsFirstFailure"`, 2 s). I did not read the roughly 1,190
Graziani research documents, did not build the full world, and did not run the Host.

## 2. World types involved

| Name | Type and file | Role | Evidence |
| --- | --- | --- | --- |
| **World6** | `CampaignWorldSnapshotV6`, `src/Cna.Core/Campaigns/CampaignWorldV6.cs:6` | The *live native world*: `Elements` (`CampaignElementStateV5`), `Representations` (one counter per element), `BrokenVehicleLots`. This is what the native chain mutates. | VERIFIED |
| **World7** | `CampaignWorldSnapshotV7`, `CampaignWorldV7.cs:7` | A *Combat projection of the selected pair*: constructor requires exactly 2 elements and 2 representations (`:49-52`), no broken vehicle lots (`:47-48`), and a single settlement. It is derived from World6 when Combat is selected, not a campaign-wide world. | VERIFIED |
| **World8** | `CampaignWorldSnapshotV8`, `CampaignWorldV8.cs:7` | *Scheduled-content initial state*: a World6 plus `PendingArrivals`. Its own comment says "This is not yet an admitted campaign snapshot" (`:6`). Used by the source track as `GroupedWorld.Prior`. | VERIFIED |
| **Grouped world** | `CampaignGroupedWorld`, `CampaignGroupedWorld.cs` (587 lines) | Source-only. Wraps World8 and adds `Groups` (formations with members and a `StackValue`), `Detached` members and `OccupantsAt` (`CampaignGroupedWorld.cs:8-37`). `CampaignSourcePlayableWorld` refuses to flatten it (`CampaignSourcePlayableWorld.cs:8-9`). | VERIFIED |
| **Scheduled World6** | `CampaignScheduledWorld6*`, `CampaignScheduledWorld6Creation.cs:11-39` | A *synthetic* scaffold for the `breakdown-truck-lab` scenario only. Comment: "Synthetic successor creation cut; not yet routed by CampaignCreationExecution" (`:11`). Not a bridge to source content. | VERIFIED |
| **Source authority / source turn** | `CampaignSourceAuthorityState`, `CampaignSourceTurnAuthority`, `CampaignSourcePlayableWorld` | The separate Graziani engine: ~60 setup openings (count is DOC, from the RC analysis), then `Start(...)` gated on `IsGameplayReady` (`CampaignSourceTurnAuthority.cs:94-101`). Creation path is `CampaignExercises.StartSourceTurn` (`CampaignExercises.cs:604-626`). | VERIFIED |

Both engines share one thing: the land sequence catalogue `Cna1979LandSequenceV4` (source `CampaignSourceTurnAuthority.cs:98-100`,
native e.g. `CampaignScheduledWorld6Creation.cs:29`), and the contact journal `CampaignContactJournal`, which already has a source entry
point (`AppendSourceTurnStart`, `src/Cna.Core/Visibility/CampaignContactJournal.cs:64-74`). So a hand-off by *position id* is structurally easy;
the content and world types are the hard part. VERIFIED.

## 3. Why the hand-off fails: the two creation paths

| Step | Native chain | Source track |
| --- | --- | --- |
| Creation | `CampaignExercises.Begin(request, resolver)` (`CampaignExercises.cs:3874-3889`) -> `CampaignCreationExecution.Execute` (`CampaignCreationExecution.cs:61-102`) | `CampaignExercises.Begin` pregame overload (`:783`) / `CampaignPregameSourceCreation.Create` (`CampaignPregameSourceCreation.cs:119-134`) |
| Content type | `IContentPackV6Resolver.ResolveV6(packId, hash)` returning `ContentPackV6Artifact` (`CampaignContentContext.cs:17-20`) | `ContentSourceScheduledCombatPackArtifact` with a pinned hash `SupportedContentHash` (`CampaignPregameSourceCreation.cs:15`) |
| Content source | Closed embedded list: the `rules-lab.*` fixtures (`Cna1979SyntheticContentCatalog.cs:93-107`, `Cna1979BreakdownContentCatalog.cs:5-12`) | Source packs built in code (`Cna1979GrazianiCairoSourceExcerpt.Build`, ...) |
| Genesis state | `CampaignSnapshotV11` + `CampaignContentContext` carrying `ArtifactV6` (`CampaignContactAdjacencySource.cs:239-246` rejects a context without it) | `CampaignSourceAuthorityState`; `ExerciseSession` constructed with `SourceAuthority`/`SourceTurn` |
| Query/submit | `CampaignLegalActions.Query` (`CampaignExercises.cs:3894-3910`) | `CampaignSourceSetupBridge.Query/QueryTurn` (`CampaignExercises.cs:3894-3910`) |
| Persistence | `CampaignDurableAuthorityJournal.CreateCampaign` (`CampaignDurableAuthorityJournal.cs:587`), evidence bound 2 MiB (`:79`) | `CreatePregameSourceCampaign` (`:747`), bound 24 MiB (`:80`) |

There is no function that takes a source world and produces a native snapshot or a `ContentPackV6Artifact`. The only thing that crosses
today is the *contact journal*, and it is computed twice with different code: native from the V6 pack's edge list
(`CampaignContactAdjacencySource.cs:236-237`), source from hard-coded hex path tables
(`CampaignSourceInitialContacts.cs:12-41`, for example the Tobruk and Cairo path distances) and the historical map. VERIFIED.

## 4. What the native chain requires per element that the source track does not provide

| # | Native requirement | Where | Source side today | Gap |
| --- | --- | --- | --- | --- |
| N1 | Pack is a `ContentPackV6Definition` with capability set exactly `{element-mobility, formations, hex-topology, initial-deployment, weather-areas[, breakdown-cohorts]}` and profile `sandtable.capability.breakdown-truck-battalion.v1` | `ContentPackV6Validator.cs:17-41` | Cairo excerpt has 4 capabilities and no weather areas (`Cna1979GrazianiCairoSourceExcerpt.cs:41-42`; section 7 STEP2 first line) | Source packs have no weather-area assignment and no native profile |
| N2 | Every formation is a root battalion with exactly one independent child; every element is an independent battalion | `ContentPackV6Validator.cs:59-80` | Source orgs are brigade/regiment/division/company ids (`Cna1979SourceContentVocabulary.cs:6-9`); grouped formations carry many members | **Structural.** Native cannot represent a brigade or a grouped formation. |
| N3 | Mobility is `non-motorized`, or `motorized` for Trucks only | `ContentPackV6Validator.cs:84-116`, `Cna1979Movement.cs:18-29` | Cairo mobility is `land.mobility.source-unresolved` for every element (section 7 header) | Real mobility classes are unresolved in source content itself |
| N4 | Terrain must be `land.terrain.clear` or `desert` with a cost row | `Cna1979Movement.cs:31-38`, validator `:118-125` | Historical map terrains: clear 4,192, desert 677, rough 655, gravel 463, sea 346, delta 325, salt-marsh 266, mountain 107, swamp 25, major-city 11, heavy-vegetation 6 (STEP4c). Names are bare, not `land.terrain.*`. | 9 terrains / 2,204 hexes (31.2%) have no cost row |
| N5 | Edge features limited to road, track (route) and ridge, slope (hexside) | `Cna1979Movement.cs:40-72, 75-90` | Map hexside features (STEP4d, 40,730 adjacent hex pairs): track 3,476, slope 1,416, wadi 1,398, road 692, minor-river 410, escarpment 404, ridge 392, railroad 358, major-river 172, unfinished-road 162, unfinished-railroad 106, coast 42 | 8 unsupported kinds, 3,052 feature instances. Whether each kind affects Movement is a rules ruling, not data entry. |
| N6 | Stack value: battalion = 1 only; at most 1 point per side per hex in the pack validator | `Cna1979Movement.cs:92-98`, `ContentPackV6Validator.cs:148-164` | Grouped formations hold a `StackValue` (for example 2 for 16th Infantry Brigade, `GrazianiSourceAuthorityTests.cs:1733`); `UnresolvedAt`/`OccupantsAt` model multi-element hexes | No stacking table for brigade/regiment/division/company; real hex stacking limit is not in native |
| N7 | Combat components: only infantry, armor, reconnaissance, anti-tank classes; defensive close-assault rating only | `Cna1979Combat.cs:19-23`; `Cna1979ContentV5CompatibilityValidator.cs:35-61`; `ContentPackV5Validator.cs:55-56` | Source facts carry offensive close-assault, barrage, weapon source profile and artillery components (STEP2: 9 offensive-rating, 2 barrage, 2 weapon-profile, 1 artillery-class errors on 16 Cairo elements) | Offensive/barrage/weapon-profile/artillery are not encodable. This is a Combat rules extension. |
| N8 | Selected Combat world is exactly two elements | `CampaignWorldV7.cs:49-52` | Source battles are grouped formations against grouped formations | A multi-element attacker/defender projection does not exist in World7 |
| N9 | One Truck cohort per side per scenario | `ContentPackV6Validator.cs:138-146` | Source has many motor-pool/truck lots | Hard limit in the validator |
| N10 | Scenario `origin.kind == Synthetic` | `ContentPackV6Validator.cs:131-135` | Source scenarios are `SourceDerived` (STEP2 scenarioOrigin) | Deliberate release-scope guard; must be replaced by a rights/provenance gate |
| N11 | Supply, fuel, ammunition | not present in native Movement/Combat (DOC: RC analysis section 1.2) | Source track has pregame supply, dumps, convoys (`CampaignSourcePlayableWorld.cs:181-196`) | Source supply state has no native consumer |
| N12 | Content hash and RNG continuity | `CampaignCreationExecution.cs:94-107`; native seeds `SandtableRandom.Create(seed)` (`CampaignScheduledWorld6Creation.cs:28`) | Source carries a `RandomStreamState` advanced through pregame (`CampaignSourcePlayableWorld.cs:75`) | Hand-off must carry the stream, not re-seed. |

**Consumers of the historical map** (grep `Cna1979HistoricalMap` over `src/`, VERIFIED): `CampaignSourcePlayableWorld.cs`, `CampaignSourceTurnAuthority.cs`,
`CampaignSourceGroupedZoc.cs`, `CampaignSourceFourthIndianRoad.cs`, `CampaignSourcePolishArrival.cs`, `Visibility/CampaignSourceInitialContacts.cs`,
`Visibility/CampaignSourcePolishArrivalContacts.cs`, and the map file itself. No native file uses it.

**ZOC.** The source track has its own ZOC derivation (`CampaignSourceGroupedZoc.cs`) which only trusts defensive close-assault ratings for
specific verified chart cells and "never guesses, averages, or substitutes" (`:61-65`); it cannot derive ZOC for independent elements
(`CampaignSourcePlayableWorld.cs:207-213`). Native contact/ZOC consumes `Cna1979Zoc` with classification kinds from `Cna1979Combat.cs:44-69`.
These are separate implementations over different data. VERIFIED (read, not run end-to-end).

## 5. Which `ReadinessBlockers()` are real and which are fixed constants

`CampaignSourcePlayableWorld.ReadinessBlockers()` is `CampaignSourcePlayableWorld.cs:131-227`; it can emit 25 distinct strings. VERIFIED by reading.

| Class | Blockers | Evidence |
| --- | --- | --- |
| **Fixed constants (never clear)** | `battlefield-incomplete`, `control-incomplete` | `CampaignSourceBattlefield.cs:1320-1321` writes `battlefieldComplete=false`, `controlCertified=false` unconditionally; `:143-144` reads them back; the code comment at `:134-141` says no completion criteria were ever established and two inference attempts broke existing assertions. Two tests assert `false` (`GrazianiSourceAuthorityTests.cs:722-723`, `GrazianiPregameJournalTests.cs:935-936`). |
| **Binding switches (caller choice, not data)** | `map-missing`, `map-incomplete`, `mobility-incomplete` (only when `!MapIdentityBound`, `:152-161`); `source-movement-map-topology-incomplete` (`:218`); `source-cross-map-seams-unresolved` (`:221`) | Cleared by `Start(source, legacy:false, cdSeam:true)`, which is what `StartSourceTurn(session)` does by default (`CampaignExercises.cs:599`). |
| **State-driven (real setup work; clear when the setup completes)** | `formation-disposition-incomplete`, `seventh-armored-shell-stack-correction-required`, `cw-frontline-initial-loads-unresolved`, two `giarabub-motor-mg-*`, `ccnn-characteristics-incomplete`, `axis-gt1-supply-plan-incomplete`, `commonwealth-supply-plans-incomplete`, `axis-prestart-supply-plan-incomplete`, `initial-cargo-setup-incomplete`, `initial-supply-setup-incomplete`, `initial-unit-setup-incomplete`, `sixth-australian-setup-unavailable`, `central-india-horse-training-state-unresolved`, `owner-setup-finalization-incomplete`, `source-initial-contacts-unavailable`, `source-grouped-movement-domain-incomplete`, `forces-incomplete` | `:162-225`. Tests show individual blockers clearing (`GrazianiSourceAuthorityTests.cs:2046-2048, 2669-2676`). `forces-incomplete` is the one DOC says only #580 clears (memory: owner decision 2026-11-24). |

**Not verified:** which state-driven blockers are still live on the most complete world reachable today. The existing test that builds that
world (`GrazianiSourceAuthorityTests.cs`, roughly lines 1500-1860, one very long method) only asserts that three named blockers are present
(`:1859-1862`). Item 1 in section 8 is the probe that answers it. ASSUMED that at least `forces-incomplete` and `owner-setup-finalization-incomplete` remain.

**Consequence for the design:** the two constants cannot be cleared without inventing a definition of "complete" and "certified". The bridge
should therefore not depend on `IsGameplayReady` at all; it should take a *sealed* source setup (owner-finalized, hash-bound) and project it.

## 6. Does any existing test drive source content through Combat selection?

No. VERIFIED by reading and grep:

- Files that mention both Graziani/`CampaignSource` and the word Combat: `GrazianiSourceAuthorityTests.cs`, `GrazianiZocSourceTests.cs`,
  `CampaignSourceCombatObligationLedgerTests.cs`, `CampaignWorldV8Tests.cs`, `GrazianiSourceTobrukBardiaGarrisonTests.cs`, content tests, and
  `CampaignDurableAuthorityJournalTests.MovementRestartSweep.cs`. None of them calls a native Combat selection query/submit on source-derived state.
  The `Combat` hits in `CampaignWorldV8Tests.cs` are `*CombatFacts` content constants (`:16, 37-39`), not the Combat engine.
- The RC analysis names `CampaignContactJournalTests` as the one test file touching both tracks. I read it: the only source-track reference is
  the version-bump API `AdvanceSourceTurnNonMovement`, called on a **synthetic** rules-lab journal inside
  `AdjacentMovementIsRecordedForBothSidesAndSurvivesColdReplay` (`tests/Cna.Core.Tests/Observations/CampaignContactJournalTests.cs:119-134`).
  It uses no Graziani content and no Combat selection, so the conclusion stands; the RC wording overstates that file.
- The source track's compulsory-combat record, `CampaignSourceCombatObligationLedger.cs:1-38`, states that attack resolution and retreat are
  "out of scope here"; it records obligations only.
- Source-turn tests construct `CampaignSourceTurnAuthority` through reflection on the private constructor, bypassing `IsGameplayReady`
  (`GrazianiSourceAuthorityTests.cs:1745-1767`; the test comments at `:1741-1742` and `:1801` say "without claiming that the campaign-start gate has opened").

## 7. The throwaway hand-off test and its first failure

Local only; not committed; deleted from the worktree after the run. Run with
`dotnet test tests/Cna.Core.Tests --filter-method "*CairoExcerptHandOffRecordsFirstFailure"` (one test, about 2 s). The helper `W` writes to
`ITestOutputHelper` and a temp file because xunit v3 (MTP) does not print test output by default.

```csharp
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

// THROWAWAY spike (T1). Not for merge. Records the first failure at each hand-off step
// from the Graziani Cairo source excerpt into the native (V6 content) Stage chain.
public sealed class ZzSourceToNativeBridgeSpikeTests(ITestOutputHelper output)
{
    private static readonly string Log = "/tmp/t1-spike-output.txt";
    private void W(string s) { output.WriteLine(s); File.AppendAllText(Log, s + "\n"); }

    [Fact]
    public void CairoExcerptHandOffRecordsFirstFailure()
    {
        var cairo = Cna1979GrazianiCairoSourceExcerpt.Build();
        var legacy = cairo.Definition.LegacyDefinition;
        W($"cairo pack={legacy.PackId} hash={cairo.Identity.Hash}");
        W($"capabilities={string.Join(",", legacy.Capabilities)}");
        W($"locations={legacy.Locations.Count} elements={legacy.Elements.Count} formations={legacy.Formations.Count}");
        W($"mobility={string.Join(",", legacy.Elements.Select(e => e.MobilityId).Distinct())}");
        W($"orgs={string.Join(",", legacy.Formations.Select(f => f.OrganizationId).Distinct())}");
        W($"terrain={string.Join(",", legacy.Locations.Select(l => l.TerrainId).Distinct())}");
        W($"scenarioOrigin={string.Join(",", legacy.Scenarios.Select(s => s.Origin.Kind))}");

        // STEP 1: native creation by pack id against the native resolver.
        var request = new CampaignCreationRequest(1, "spike-cairo", Cna1979Ruleset.Manifest.Hash, 12345,
            "rules-lab.breakdown.reaction.combat-infantry.v1", "sha256:" + new string('0', 64),
            legacy.PackId, cairo.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        var begin = CampaignExercises.Begin(request);
        W($"STEP1 Begin(request) started={begin.IsStarted} reason={begin.RejectionReason}");

        // STEP 2: re-express the Cairo pack as a V6 native pack and run the native validator.
        var v6 = new ContentPackV6Definition(legacy, cairo.Definition.ElementCombatFacts,
            cairo.Definition.InitialPlacementCombatFacts, ContentPackV6Definition.SupportedCapabilityProfileId);
        var result = ContentPackV6Validator.Validate(v6);
        W($"STEP2 V6 validator issues={result.Issues.Count}");
        foreach (var issue in result.Issues.Take(25)) W($"  {issue.Code} {issue.Path} :: {issue.Message}");

        // STEP 3: native movement rule lookup for the source vocabulary.
        W($"STEP3 terrain major-city/unresolved supported={Cna1979Movement.LookupTerrain(Cna1979SourceContentVocabulary.MajorCityTerrainId, Cna1979SourceContentVocabulary.UnresolvedMobilityId).IsSupported}");
        W($"STEP3 stacking brigade supported={Cna1979Movement.LookupStackingValue(Cna1979SourceContentVocabulary.BrigadeOrganizationId).IsSupported}");

        // STEP 4: historical map vs native terrain vocabulary.
        var map = Cna1979HistoricalMap.Instance;
        W($"STEP4 historical hexes={map.HexCount}");
        var terrains = new Dictionary<string, int>();
        foreach (var id in HexIds(map))
        {
            var t = map.TerrainAt(id)!;
            terrains[t] = terrains.GetValueOrDefault(t) + 1;
        }
        foreach (var (t, n) in terrains.OrderByDescending(x => x.Value))
            W($"  terrain '{t}' x{n} nativeClearOrDesert={t is "land.terrain.clear" or "land.terrain.desert"}");

        var field = typeof(Cna1979HistoricalMap).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).First(f => f.FieldType.IsGenericType && f.FieldType.GetGenericArguments().Length == 2 && f.FieldType.GetGenericArguments()[1] == typeof(Cna1979HistoricalHex));
        var dict = (System.Collections.IDictionary)field.GetValue(map)!;
        var enumerated = new HashSet<string>(HexIds(map));
        var extra = dict.Keys.Cast<string>().Where(k => !enumerated.Contains(k)).ToArray();
        W($"STEP4b unenumerated ids={extra.Length} sample={string.Join(",", extra.Take(8))}");
        var all = new Dictionary<string, int>();
        foreach (System.Collections.DictionaryEntry e in dict) { var t = ((Cna1979HistoricalHex)e.Value!).Terrain; all[t] = all.GetValueOrDefault(t) + 1; }
        W("STEP4c all terrains: " + string.Join(", ", all.OrderByDescending(x => x.Value).Select(x => $"{x.Key}={x.Value}")));
        var featureCounts = new Dictionary<string, int>();
        var pairs = new HashSet<string>();
        foreach (var k in dict.Keys.Cast<string>())
            foreach (var n in map.NeighborsOf(k))
            {
                var key = string.CompareOrdinal(k, n) < 0 ? k + "|" + n : n + "|" + k;
                if (!pairs.Add(key)) continue;
                var edge = map.EdgeBetween(k, n);
                foreach (var f in edge?.Features ?? []) featureCounts[f] = featureCounts.GetValueOrDefault(f) + 1;
            }
        W($"STEP4d adjacent hex pairs={pairs.Count}; hexside/route feature counts: " + string.Join(", ", featureCounts.OrderByDescending(x => x.Value).Select(x => $"{x.Key}={x.Value}")));
        Assert.False(begin.IsStarted);
    }

    private static IEnumerable<string> HexIds(Cna1979HistoricalMap map)
    {
        for (var section = 'a'; section <= 'e'; section++)
            for (var col = 0; col <= 99; col++)
                for (var row = 0; row <= 99; row++)
                {
                    var id = $"{section}{col:00}{row:00}";
                    if (map.HexAt(id) is not null) yield return id;
                }
    }
}
```

Observed output (every line below was printed by the test; STEP2 is the first 25 of 147 issues):

```text
cairo pack=graziani-cairo-source-excerpt hash=sha256:ae700ad1664ae9706e3f7dce6c58e13ef4d025034eacd6a2bfba664f3c2ced3d
capabilities=land.element-mobility,land.formations,land.hex-topology,land.initial-deployment
locations=5 elements=16 formations=3
mobility=land.mobility.source-unresolved
orgs=land.organization.brigade
terrain=land.terrain.major-city
scenarioOrigin=SourceDerived
STEP1 Begin(request) started=False reason=UnknownContent
STEP2 V6 validator issues=147
  BRK-CERT-001 /capabilityProfileId :: Content requires the supported profile, ruleset and exact predecessor capability set.
  BRK-CERT-004 /elements/1-argyll-sutherland/components/1-argyll-sutherland.infantry/offensiveCloseAssaultRating :: The current combat capability cannot interpret an offensive rating in this profile.
  BRK-CERT-004 /elements/1-argyll-sutherland/components/1-argyll-sutherland.infantry/offensiveCloseAssaultRating :: This capability cannot encode an offensive rating.
  BRK-CERT-004 /elements/1-argyll-sutherland/mobilityId :: Only Trucks use motorized movement in this profile.
  BRK-CERT-004 /elements/1-argyll-sutherland/mobilityId :: Unknown Movement mobility ID 'land.mobility.source-unresolved'.
  ... (same pair repeated per element)
  BRK-CERT-003 /elements/16-infantry-brigade-hq :: Every element must be an independently represented battalion.
  BRK-CERT-004 /elements/16-infantry-brigade-hq/components/16-infantry-brigade-hq.3-7-howitzer/barrageRating :: The current combat capability cannot interpret a barrage rating in this profile.
  BRK-CERT-004 /elements/16-infantry-brigade-hq/components/16-infantry-brigade-hq.3-7-howitzer/componentClassId :: Unknown combat component class ID 'land.combat-component.artillery'.
  BRK-CERT-004 /elements/16-infantry-brigade-hq/components/16-infantry-brigade-hq.3-7-howitzer/weaponSourceProfile :: The current combat capability cannot interpret a weapon source profile.
  BRK-CERT-003 /elements/16-infantry-brigade-hq/organizationId :: Unknown Organization ID 'land.organization.brigade'.
STEP3 terrain major-city/unresolved supported=False
STEP3 stacking brigade supported=False
STEP4 historical hexes=7073
STEP4b unenumerated ids=7073 sample=A0101,A0102,A0103,A0104,A0105,A0106,A0107,A0108
STEP4c all terrains: clear=4192, desert=677, rough=655, gravel=463, sea=346, delta=325, salt-marsh=266, mountain=107, swamp=25, major-city=11, heavy-vegetation=6
STEP4d adjacent hex pairs=40730; hexside/route feature counts: track=3476, slope=1416, wadi=1398, road=692, minor-river=410, escarpment=404, ridge=392, railroad=358, major-river=172, unfinished-road=162, unfinished-railroad=106, coast=42
```

Notes on the output. (a) STEP4 and STEP4b are artefacts of my first enumeration (the map keys are upper-case, `A0101`; `HexAt` normalizes
case), so the per-terrain lines under the first STEP4 count were discarded; STEP4c, taken from the map's own dictionary by reflection, is the
authoritative count and sums to 7,073. (b) STEP2 deliberately re-expresses the excerpt as a `ContentPackV6Definition` with the supported profile id,
which is the most favourable way to present it to the native validator; the issue breakdown by class is: 9 offensive-rating, 8 mobility,
2 weapon-source-profile, 2 barrage, 1 artillery component class, 1 unknown organization, 1 non-battalion element, 1 profile/capability.
**First failure:** STEP1, `CampaignExercises.Begin` returns `RejectionReason = UnknownContent` because `ResolveV6` has no pack with that id.
**First *content* failure, if a resolver admitted the pack:** `BRK-CERT-001 /capabilityProfileId` (no weather areas / wrong capability set), followed by the
mobility, rating and organization failures above. Each is a separate piece of work in section 8.

## 8. Ranked list of required changes (with wh)

Sizes are ASSUMED. Basis: the Stage 1 Host wiring produced roughly seven hidden gaps over about three days (RC analysis section 1.2) and the Stage 2/3 Core
slices (#519-#540, #570, #600) were each roughly 4-12 wh; large rules work is sized by analogy to those. Ranges are wide on purpose.

| Rank | Change | Why it ranks here | Size |
| --- | --- | --- | --- |
| 1 | **Live-blocker probe on the most complete reachable source world.** Extract the world-building steps from `GrazianiSourceAuthorityTests.cs` (~1500-1860) into a reusable fixture and print `ReadinessBlockers()` and `Battlefield` coverage. | Turns section 5 "not verified" into a measured list; no production change. | 3-5 wh |
| 2 | **Owner/design ruling on the hand-off architecture and fidelity bar** (section 9 options). Includes: is a brigade a single native element or expanded to its member battalions; which terrains/hexsides affect Movement; stack limits. | Everything below depends on it; the validator (N2, N6, N9, N10) encodes the opposite choice today. | 2-4 wh to write the decision doc; HUMAN time to rule |
| 3 | **Sealed source-setup snapshot contract**: define the hash-bound artefact (owner-finalized battlefield + grouped world + pending arrivals + `RandomStreamState`) that the bridge consumes, independent of `IsGameplayReady`. | Gives the bridge a stable input; retires the two fixed-constant blockers as a *dependency* rather than a fix. | 6-10 wh |
| 4 | **Native content profile v2** (new `ContentPack` schema/profile, additive, leaves the synthetic profile untouched): weather areas for source hexes, `SourceDerived` provenance guard replaced by a rights/provenance check, organizations (brigade/regiment/division/company) with stack values, relaxed one-child formation rule, multi-cohort Trucks. | Removes validator hard blocks N1, N2, N6, N9, N10. | 14-24 wh |
| 5 | **Movement rules coverage on the historical map**: cost rows for the 9 missing terrains (2,204 hexes) and handling for the 8 missing hexside kinds (3,052 features), with source citations; map-to-pack projection of the relevant sub-map (or direct map binding). | N4, N5. Without it Movement either rejects or silently treats 31% of the map as impassable. Needs a rules ruling per terrain/feature. | 12-25 wh plus rulings |
| 6 | **Projection function** `source sealed setup -> (pack v2, World6, ContentContext, contact journal, RNG)`; replace hard-coded contact path tables with the one shared adjacency derivation. | The actual bridge; includes the N12 RNG carry-over and one derivation of contacts. | 12-20 wh |
| 7 | **Combat rating extensions**: offensive close-assault, barrage, weapon source profile, artillery/HQ classes, parenthesized defence, multi-element World7 (N7, N8). | Needed only once a real formation is *attacked*; Stage 1 Movement alone does not need it. Largest rules risk. | 20-45 wh |
| 8 | **Creation contract, durable journal and Host wiring**: new request version carrying source evidence, replay genesis, byte bounds (native evidence cap 2 MiB, source cap 24 MiB), Host openings for the hand-off position, cold-replay proof. | Follows #580's wiring pattern (sandtable-26 is editing the same Host files; sequence after it). | 8-16 wh |
| 9 | **Source supply/fuel/ammo into native**: either a native consumer or an explicit owner "pass" (as Truck Convoy / Post-Combat are today). | N11. Not required to *reach* Stage 1 Movement; required for fidelity. | 0 (pass) to 30+ wh |
| 10 | **Real-content E2E**: HTTP test in the style of `TwoRealHttpPlayersConductAnEntireOperationStageOne` from Stage 1 Movement to Combat resolution on source content, with cold replay. | The proof. | 8-14 wh |

Totals: items 1-3 are about 11-19 wh and de-risk the rest; items 4-8 and 10 are about 74-139 wh; item 9 is optional by ruling. ASSUMED.
Items 4, 5 and 7 touch `Cna1979Movement`/Combat rules and likely `CampaignExercises.cs` and `PendingDecision.cs`, which the Stage 3 chain
(sandtable-32) owns exclusively; they must sequence after it or be limited to new files.

## 9. Recommended bridge design

Three options were considered.

- **A. Flip the source gate and run the source turn engine to completion.** Rejected: the source engine implements only Stage 1 setup steps and a
  Fourth Indian move (`CampaignSourceSetupBridge.cs:46-125`), and would duplicate the Stage 1-3 native chain. Not a path to MVP-GATE-06.
- **B. Flatten source formations into synthetic battalions and reuse the native synthetic profile as is.** Rejected: contradicts the source track's
  explicit rule not to flatten grouped counters (`CampaignSourcePlayableWorld.cs:8-9`), invents stacking and ratings, and breaks the 7,073-hex fidelity the
  map transcription was built to provide.
- **C (recommended). One-way sealed hand-off at the end of source setup, into a native content profile v2.**
  1. Source track remains the *setup* engine only. When both owners have finalized, a new System step emits a **sealed setup artefact**
     (battlefield + grouped world + pending arrivals + supply evidence + `RandomStreamState`, all hash-bound; no dependency on `IsGameplayReady`).
  2. A pure projection builds a **native pack v2** and a **World6** from the artefact. The pack uses the historical map as its topology (not a
     hand-copied hex list) with terrain/hexside tables extended per item 5; grouped formations map to native elements according to the ruling in item 2.
  3. The hand-off position is **Stage 1 Initiative Declaration** (the first native position the Host already wires), not Movement: the native chain
     already auto-drains the preamble to Movement (`CampaignDurableAuthorityJournal.cs`, UX-001 comment at `:635-660`), and source GT1 supply/convoy
     steps must be complete before it. Seeding Movement directly would skip Initiative and Reserve Designation.
  4. Contacts for the first position come from the native adjacency derivation over the v2 pack, retiring the source hard-coded path tables for this
     purpose (they remain for the legacy source track).
  5. Persist as a new creation contract version with the sealed artefact as genesis evidence (cap 24 MiB like `CreatePregameSourceCampaign`), and prove
     cold replay.
  Native Combat rating extensions (item 7) are deferred behind a rule: any element whose facts the profile cannot encode causes an explicit
  `rule-not-implemented` rejection at Combat selection (the same style as the existing second-side Repeat and Reaction rejections), so Stage 1 Movement
  can be proven on real content before Combat fidelity is complete.

## 10. Rules and ratings gaps (consolidated)

Each needs a source-cited ruling; none is solved by data entry alone.

1. **Terrain effects:** 9 of 11 terrains lack movement cost rows (rough 655, gravel 463, sea 346, delta 325, salt-marsh 266, mountain 107, swamp 25, major-city 11, heavy-vegetation 6). Sea and major-city need a passability/stacking ruling rather than a cost.
2. **Hexside and route features:** 8 of 12 kinds unsupported: wadi 1,398, minor-river 410, escarpment 404, railroad 358, major-river 172, unfinished-road 162, unfinished-railroad 106, coast 42. Railroad movement is a separate phase in the rules.
3. **Mobility classes:** Cairo elements are `source-unresolved`; native knows two.
4. **Stacking:** only battalion=1 exists; company, regiment, brigade, division values and the per-hex limit are absent from `Cna1979Movement`.
5. **Combat ratings:** offensive close assault, barrage, weapon source profile, artillery and HQ-specific classes, parenthesized defence, per-component TOE limits and combined TOE limits are all rejected by the native validators (`Cna1979ContentV5CompatibilityValidator.cs`, `ContentPackV5Validator.cs:55-56`).
6. **ZOC:** two implementations (native `Cna1979Zoc`, source `CampaignSourceGroupedZoc`); source derives defensive ratings only for verified chart cells and cannot derive ZOC for independent elements.
7. **Supply, fuel, ammunition, towing, maintenance, repair, rail, patrol:** absent from the native chain (DOC: RC analysis section 1.2); source holds pregame supply state only.
8. **Weather areas:** native requires them per location; source packs and the historical map have no weather-area table in the code I read (ASSUMED absent; not searched in the data files).
9. **Reserve status and CP expenditure carry-over:** `CampaignGroupedMember` already holds `CapabilityPointsExpended` and `CohesionLevel` (`CampaignGroupedWorld.cs:11-14`), which map to `CampaignElementStateV5`; no mapping code exists.

## 11. What I did not verify

- The set of blockers still live on the most complete reachable source world (section 5, item 1 in section 8).
- The size, in bytes, of a 7,073-hex native pack and whether the 2 MiB native evidence cap or serializer limits would bind (ASSUMED not to bind if the pack is referenced by hash, as today).
- Whether the weather-area table exists anywhere in the source data (section 10 item 8).
- Host behaviour (no Host test run), and any interaction with the open #580 PR2 wiring (sandtable-26) or the CI-manifest restructure (#604).
- Correctness of the 63-hex discrepancy in my first enumeration: resolved by reading the map dictionary directly (STEP4c sums to 7,073), not by finding why my id scheme missed them.
- Wh figures in section 8 are estimates, not measurements.
