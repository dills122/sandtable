using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

// Proves the Tobruk/Bardia static-defense garrison admission wiring without
// the giant pinned GrazianiSourceAuthorityTests fixture (see that file's own
// AdmitAxisGarrisons/ActivateAxisMinorGarrisons, which are not independently
// tested anywhere today either - driving all the way to a real CwFreeGunsWorld
// here would mean replaying most of that fixture just to admit 28 elements
// that have no player choice at all). Instead this: (1) drives the real,
// directly-callable Admit chain through CampaignSourceAuthorityState up to a
// real source Battlefield - the admission half - and (2) builds the exact
// World8 element/representation shapes the activation half produces from the
// same cited facts, then checks them against the identical "represented"
// predicate CampaignSourcePlayableWorld.RepresentedIds()/ReadinessBlockers()
// uses (Representations.SelectMany(v => v.BoundElementIds)) - the
// readiness-blocker interaction - using the real production types throughout.
public sealed class GrazianiSourceTobrukBardiaGarrisonTests
{
    // Builds the real, pinned content-pack-artifact chain (the same one
    // GrazianiSourceSetupPackArtifactTests.cs exercises for its siblings) up
    // through CW free guns, then proves the new pack both matches its own
    // pinned ContentHash and round-trips through Read() canonically - the
    // same self-check every sibling pack (MinorGarrisons, GarrisonRemainder,
    // the motor MG, CW frontline) already performs on itself.
    [Fact]
    public void ContentPackMatchesItsPinnedIdentityAndRoundTrips()
    {
        var basis = Cna1979GrazianiCairoSourceExcerpt.Build();
        var units = CampaignSourceCairoInitialSetup.Units.Select(value => new ContentSourceSetupUnit(
            value.ElementId, "commonwealth", value.CapabilityPoints, value.ComponentId,
            value.ComponentClassId, value.Toe, value.Stacking, CampaignSourceCairoActiveWorld.Origin(value.ElementId)));
        var cairo = ContentSourceSetupPackArtifact.Create(basis, units);
        var catalog = ContentSourceIndependentPackArtifact.Create(cairo);
        var map = ContentSourceLocalMapPackArtifact.Create(catalog);
        var expanded = ContentSourceExpandedIndependentPackArtifact.Create(map);
        var axisFree = ContentSourceAxisFreeArtilleryPackArtifact.Create(expanded);
        var corps = ContentSourceAxisCorpsArtilleryPackArtifact.Create(axisFree);
        var cwPriority = ContentSourceCwPriorityPackArtifact.Create(corps);
        var cwGuns = ContentSourceCwFreeGunsPackArtifact.Create(cwPriority);

        var pack = ContentSourceTobrukBardiaGarrisonPackArtifact.Create(cwGuns);
        Assert.Equal(ContentSourceTobrukBardiaGarrisonPackArtifact.ContentHash, pack.Identity.Hash);
        var reread = ContentSourceTobrukBardiaGarrisonPackArtifact.Read(pack.CanonicalBytes);
        Assert.Equal(pack.Identity, reread.Identity);
        var tampered = pack.CanonicalBytes;
        tampered[^1] ^= 0xFF;
        Assert.ThrowsAny<Exception>(() => ContentSourceTobrukBardiaGarrisonPackArtifact.Read(tampered));
    }

    [Fact]
    public void AllTwentyEightElementsAreCitedAndDistinctFromAlreadyAdmittedGarrisons()
    {
        var state = CreateThroughAxisGarrisons();
        Assert.Equal(28, ContentSourceTobrukBardiaGarrisonPackArtifact.AllIds.Count);
        Assert.Equal(28, ContentSourceTobrukBardiaGarrisonPackArtifact.AllIds.Distinct(StringComparer.Ordinal).Count());
        // None of these 28 overlap the already-admitted Giarabub/minor-garrison
        // Forces, proving this really is new admission wiring, not a duplicate.
        Assert.DoesNotContain(ContentSourceTobrukBardiaGarrisonPackArtifact.AllIds,
            id => state.AxisGarrisons!.Forces.Any(force => force.ElementId == id));

        var seeds = ContentSourceTobrukBardiaGarrisonPackArtifact.CreateToeSeeds(Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        Assert.Equal(28, seeds.Count);
        // A representative multi-component coastal battery keeps its printed
        // mixed howitzer + light-AA TOE (Cna1979GrazianiSourceFacts.cs).
        var mixed = Assert.Single(seeds, value => value.ElementId == "2-tobruk-coastal-defense-artillery");
        Assert.Equal(2, mixed.InitialComponentToes.Count);
        Assert.Contains(mixed.InitialComponentToes, c => c.ComponentId.EndsWith("100-17-howitzer", StringComparison.Ordinal) && c.CurrentToe == 6);
        Assert.Contains(mixed.InitialComponentToes, c => c.ComponentId.EndsWith("i-light-aa", StringComparison.Ordinal) && c.CurrentToe == 6);
        // Every HQ is admitted with zero TOE: the retained chart subsets name
        // them but print no combat value for any of the four.
        foreach (var hqId in ContentSourceTobrukBardiaGarrisonPackArtifact.HeadquartersIds)
        {
            var hq = Assert.Single(seeds, value => value.ElementId == hqId);
            Assert.Empty(hq.InitialComponentToes);
            var facts = ContentSourceTobrukBardiaGarrisonPackArtifact.HeadquartersFacts(hqId);
            Assert.Equal(Cna1979Combat.HeadquartersClassificationId, facts.CombatClassificationId);
            Assert.Empty(facts.Components);
        }
        Assert.Equal(ContentSourceTobrukBardiaGarrisonPackArtifact.TobrukLocationId,
            ContentSourceTobrukBardiaGarrisonPackArtifact.LocationOf("31-gaf-sdic-hq"));
        Assert.Equal(ContentSourceTobrukBardiaGarrisonPackArtifact.BardiaLocationId,
            ContentSourceTobrukBardiaGarrisonPackArtifact.LocationOf("30-gaf-sdic-hq"));
    }

    [Fact]
    public void AdmitSourceTobrukBardiaGarrisonRetagsAlreadyRetainedEvidenceAtTheirCitedLocations()
    {
        var state = CreateThroughAxisGarrisons().AdmitBattlefield();
        // CampaignSourceAxisInitialForces already names all 33 Tobruk/Bardia
        // chart-group elements (28 in this pack, plus 5 out of scope - the
        // VEC MG battalion, the GaF antitank battalion, and the three GaF
        // artillery regiments), so CampaignSourceBattlefield.Create already
        // seeded source-evidence entries for them from day one. That is the
        // "sourced-but-never-admitted" gap itself: real, cited evidence with
        // no activation code. Confirm it is there before admitting it.
        Assert.True(state.CanAdmitSourceTobrukBardiaGarrison);
        foreach (var id in ContentSourceTobrukBardiaGarrisonPackArtifact.AllIds)
        {
            Assert.Contains(state.AxisInitialForces.Forces, force => force.ElementId == id
                && force.LocationId == ContentSourceTobrukBardiaGarrisonPackArtifact.LocationOf(id));
            var entry = Assert.Single(state.Battlefield!.Entries, value => value.ElementId == id);
            Assert.Equal("axis", entry.SideId);
            Assert.Equal("source-evidence", entry.AuthorityStatus);
            Assert.Equal(ContentSourceTobrukBardiaGarrisonPackArtifact.LocationOf(id), entry.LocationId);
        }

        var admitted = state.AdmitSourceTobrukBardiaGarrison();
        Assert.False(admitted.CanAdmitSourceTobrukBardiaGarrison);
        Assert.Throws<InvalidOperationException>(() => admitted.AdmitSourceTobrukBardiaGarrison());
        foreach (var id in ContentSourceTobrukBardiaGarrisonPackArtifact.AllIds)
        {
            var entry = Assert.Single(admitted.Battlefield!.Entries, value => value.ElementId == id);
            Assert.Equal("source-tobruk-bardia-garrison-admitted", entry.AuthorityStatus);
        }
        // The out-of-scope 5th-element remainder of the same chart groups is
        // untouched by this admission.
        foreach (var id in new[] { "vec-machine-gun-battalion", "30-gaf-antitank-battalion", "8-gaf-artillery", "9-gaf-artillery", "10-gaf-artillery" })
            Assert.Equal("source-evidence", Assert.Single(admitted.Battlefield!.Entries, value => value.ElementId == id).AuthorityStatus);
        // Activation is still correctly gated behind the file's established
        // CW-free-guns stage, since that is where every other late-admitted
        // independent element (minor garrisons, the remainder, the motor MG,
        // the CW frontline) is actually materialized into the active World8.
        Assert.False(admitted.CanActivateSourceTobrukBardiaGarrison);
    }

    // The minimum real chain needed to reach a genuine source Battlefield:
    // every step here is a plain, directly-callable, choice-free Admit method
    // on CampaignSourceAuthorityState (no dice, no session/action replay).
    private static CampaignSourceAuthorityState CreateThroughAxisGarrisons()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var init = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "real-graziani-tobruk-bardia-garrison", Cna1979Ruleset.Manifest.Hash, 0,
            CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, init),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        var (rolled, _) = CampaignPregameSourceState.Create(request, artifact, init).Apply(new PregameAxisSupplyRollAction());
        return CampaignSourceAuthorityState.Finish(rolled)
            .AdmitAxisSupplement()
            .AdmitCommonwealthSupplement()
            .AdmitPriorityMotorization()
            .AdmitAxisPriorityMotorization()
            .AdmitAxisGarrisons();
    }

    [Fact]
    public void ActivationBuildsIndependentElementsThatReadinessConsidersRepresented()
    {
        var seeds = ContentSourceTobrukBardiaGarrisonPackArtifact.CreateToeSeeds(Cna1979GrazianiCairoSourceExcerpt.ScenarioId)
            .ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var ids = ContentSourceTobrukBardiaGarrisonPackArtifact.AllIds;

        // This mirrors, element for element, what
        // CampaignSourceCwFreeGunsWorld.ActivateTobrukBardiaGarrison actually
        // builds (same seeds, same IndependentElement binding convention
        // already used by ActivateAxisMinorGarrisons/ActivateGarrisonRemainder).
        var elements = ids.Select(id =>
        {
            var toe = seeds[id];
            return new CampaignElementStateV5(id, ContentSourceTobrukBardiaGarrisonPackArtifact.LocationOf(id),
                CampaignElementReserveStatus.None,
                new CampaignElementOperationalStateV5(1, 1, CapabilityPointAmount.Zero, 0, null, null),
                toe.InitialComponentToes.Select(component =>
                    new CampaignComponentToeState(component.ComponentId, component.CurrentToe, component.Origin)));
        }).ToArray();
        var representations = ids.Select(id => new CampaignMapRepresentationState("tobruk-bardia-garrison." + id,
            ContentSourceTobrukBardiaGarrisonPackArtifact.LocationOf(id),
            CampaignMapRepresentationBindingKind.IndependentElement, [id])).ToArray();

        Assert.Equal(28, elements.Length);
        Assert.Equal(28, elements.Select(value => value.ElementId).Distinct(StringComparer.Ordinal).Count());
        foreach (var representation in representations)
        {
            Assert.Equal(CampaignMapRepresentationBindingKind.IndependentElement, representation.BindingKind);
            Assert.Single(representation.BoundElementIds);
        }

        // This is the exact predicate CampaignSourcePlayableWorld.RepresentedIds()
        // uses against GroupedWorld.Prior.World.Representations, and the exact
        // membership check ReadinessBlockers() runs per Battlefield entry to
        // decide "forces-incomplete". Before activation, none of these 28
        // elements are represented; after, every one of them is.
        var beforeActivation = new HashSet<string>(StringComparer.Ordinal);
        Assert.DoesNotContain(ids, id => beforeActivation.Contains(id));

        var afterActivation = representations.SelectMany(value => value.BoundElementIds).ToHashSet(StringComparer.Ordinal);
        foreach (var id in ids) Assert.Contains(id, afterActivation);
        Assert.Equal(28, afterActivation.Count);

        // And each element's own component TOE really is the cited, real
        // figure - zero for the HQs, the printed mixed battery for coastal
        // defense artillery - never an invented value.
        foreach (var hqId in ContentSourceTobrukBardiaGarrisonPackArtifact.HeadquartersIds)
            Assert.Empty(Assert.Single(elements, value => value.ElementId == hqId).Components);
        var battery = Assert.Single(elements, value => value.ElementId == "4-tobruk-coastal-defense-artillery");
        Assert.Equal(2, battery.Components.Count);
        Assert.Contains(battery.Components, c => c.ComponentId.EndsWith("105-28-gun", StringComparison.Ordinal) && c.CurrentToe == 6);
        Assert.Contains(battery.Components, c => c.ComponentId.EndsWith("i-heavy-aa", StringComparison.Ordinal) && c.CurrentToe == 4);
    }
}
