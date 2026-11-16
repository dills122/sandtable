using System.Text;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Content;

public sealed class GrazianiSourceSetupPackArtifactTests
{
    [Fact]
    public void MalettiSuccessorBindsSubsetAndRejectsTampering()
    {
        var basis = Cna1979GrazianiCairoSourceExcerpt.Build();
        var units = CampaignSourceCairoInitialSetup.Units.Select(value => new ContentSourceSetupUnit(
            value.ElementId, "commonwealth", value.CapabilityPoints, value.ComponentId,
            value.ComponentClassId, value.Toe, value.Stacking, CampaignSourceCairoActiveWorld.Origin(value.ElementId)));
        var cairo = ContentSourceSetupPackArtifact.Create(basis, units);
        Assert.Equal(CampaignSourceCairoActiveWorld.SourceContentHash, cairo.Hash());
        var catalog = ContentSourceIndependentPackArtifact.Create(cairo);
        var map = ContentSourceLocalMapPackArtifact.Create(catalog);
        var expanded = ContentSourceExpandedIndependentPackArtifact.Create(map);
        var axisFree = ContentSourceAxisFreeArtilleryPackArtifact.Create(expanded);
        var corps = ContentSourceAxisCorpsArtilleryPackArtifact.Create(axisFree);
        var certified = ContentSourceCertifiedMalettiPackArtifact.Create(corps);
        Assert.Equal(CampaignSourceCwPriorityCatalog.PinnedInitialHash, CampaignSourceCwPriorityCatalog.InitialHash());
        var cwPriority = ContentSourceCwPriorityPackArtifact.Create(corps);
        var cwGuns = ContentSourceCwFreeGunsPackArtifact.Create(cwPriority);
        var fourthIndian = ContentSourceFourthIndianPackArtifact.Create(cwGuns);
        var matruhGroup = ContentSourceMatruhGroupedPackArtifact.Create(fourthIndian);
        var malettiGrouped = ContentSourceMalettiGroupedPackArtifact.Create(matruhGroup);
        var libyanGrouped = ContentSourceLibyanGroupedPackArtifact.Create(malettiGrouped);
        var italianGrouped = ContentSourceCireneMarmaricaGroupedPackArtifact.Create(libyanGrouped);
        var seventhGrouped = ContentSourceSeventhArmoredGroupedPackArtifact.Create(italianGrouped);
        var sixteenthGrouped = ContentSourceSixteenthBrigadeGroupedPackArtifact.Create(seventhGrouped);
        var catanzaroGrouped = ContentSourceCatanzaroGroupedPackArtifact.Create(sixteenthGrouped);
        Assert.Equal(ContentSourceCatanzaroGroupedPackArtifact.ContentHash, catanzaroGrouped.Hash());
        Assert.Equal(catanzaroGrouped.Identity, ContentSourceCatanzaroGroupedPackArtifact.Read(catanzaroGrouped.CanonicalBytes).Identity);
        Assert.Equal(ContentSourceSeventhArmoredGroupedPackArtifact.ContentHash, seventhGrouped.Hash());
        Assert.Equal(seventhGrouped.Identity, ContentSourceSeventhArmoredGroupedPackArtifact.Read(seventhGrouped.CanonicalBytes).Identity);
        var definition = Encoding.UTF8.GetString(CampaignSourceSeventhArmoredGroupedDefinition.SerializeCanonical());
        Assert.Contains("\"capabilityPointAllowance\":25", definition);
        Assert.Contains("\"componentId\":\"8-hussars.a10-cruiser\",\"toe\":7", definition);
        var alteredSeventh = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(seventhGrouped.CanonicalBytes)
            .Replace("sandtable.source-seventh-armored-grouped.v1", "sandtable.source-seventh-armored-grouped.v2", StringComparison.Ordinal));
        Assert.ThrowsAny<Exception>(() => ContentSourceSeventhArmoredGroupedPackArtifact.Read(alteredSeventh));
        Assert.Equal(ContentSourceLibyanGroupedPackArtifact.ContentHash, libyanGrouped.Hash());
        Assert.Equal(libyanGrouped.Identity, ContentSourceLibyanGroupedPackArtifact.Read(libyanGrouped.CanonicalBytes).Identity);
        var alteredLibyan = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(libyanGrouped.CanonicalBytes)
            .Replace("sandtable.source-libyan-grouped.v1", "sandtable.source-libyan-grouped.v2", StringComparison.Ordinal));
        Assert.ThrowsAny<Exception>(() => ContentSourceLibyanGroupedPackArtifact.Read(alteredLibyan));
        Assert.Equal(ContentSourceMalettiGroupedPackArtifact.ContentHash, malettiGrouped.Hash());
        Assert.Equal(malettiGrouped.Identity, ContentSourceMalettiGroupedPackArtifact.Read(malettiGrouped.CanonicalBytes).Identity);
        Assert.Equal(ContentSourceMatruhGroupedPackArtifact.ContentHash, matruhGroup.Hash());
        Assert.Equal(matruhGroup.Identity, ContentSourceMatruhGroupedPackArtifact.Read(matruhGroup.CanonicalBytes).Identity);
        var alteredMatruhGroup = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(matruhGroup.CanonicalBytes).Replace("\"poolPoints\":20", "\"poolPoints\":21", StringComparison.Ordinal));
        Assert.ThrowsAny<Exception>(() => ContentSourceMatruhGroupedPackArtifact.Read(alteredMatruhGroup));
        Assert.Equal(ContentSourceCwFreeGunsPackArtifact.ContentHash, cwGuns.Hash());
        Assert.Equal(cwGuns.Identity, ContentSourceCwFreeGunsPackArtifact.Read(cwGuns.CanonicalBytes).Identity);
        var alteredCwGuns = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(cwGuns.CanonicalBytes)
            .Replace("\"toe\":8", "\"toe\":7", StringComparison.Ordinal));
        Assert.ThrowsAny<Exception>(() => ContentSourceCwFreeGunsPackArtifact.Read(alteredCwGuns));
        Assert.Equal(ContentSourceCwPriorityPackArtifact.ContentHash, cwPriority.Hash());
        Assert.Equal(cwPriority.Identity, ContentSourceCwPriorityPackArtifact.Read(cwPriority.CanonicalBytes).Identity);
        var alteredCwPriority = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(cwPriority.CanonicalBytes)
            .Replace("\"toe\":1", "\"toe\":2", StringComparison.Ordinal));
        Assert.ThrowsAny<Exception>(() => ContentSourceCwPriorityPackArtifact.Read(alteredCwPriority));
        var matruhBeforeMaletti = ContentSourceMatruhDetachedPackArtifact.Create(corps, null);
        var matruhAfterMaletti = ContentSourceMatruhDetachedPackArtifact.Create(corps, certified);
        Assert.Equal(ContentSourceMatruhDetachedPackArtifact.CorpsBasisHash, matruhBeforeMaletti.Hash());
        Assert.Equal(ContentSourceMatruhDetachedPackArtifact.MalettiBasisHash, matruhAfterMaletti.Hash());
        Assert.Equal(matruhBeforeMaletti.Identity, ContentSourceMatruhDetachedPackArtifact.Read(matruhBeforeMaletti.CanonicalBytes).Identity);
        Assert.Equal(matruhAfterMaletti.Identity, ContentSourceMatruhDetachedPackArtifact.Read(matruhAfterMaletti.CanonicalBytes).Identity);
        var alteredMatruh = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(matruhBeforeMaletti.CanonicalBytes)
            .Replace("\"toe\":6", "\"toe\":5", StringComparison.Ordinal));
        Assert.ThrowsAny<Exception>(() => ContentSourceMatruhDetachedPackArtifact.Read(alteredMatruh));
        Assert.Equal(ContentSourceCertifiedMalettiPackArtifact.ContentHash, certified.Hash());
        Assert.Equal(certified.Identity, ContentSourceCertifiedMalettiPackArtifact.Read(certified.CanonicalBytes).Identity);
        var alteredCertified = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(certified.CanonicalBytes)
            .Replace("\"toe\":5", "\"toe\":6", StringComparison.Ordinal));
        Assert.ThrowsAny<Exception>(() => ContentSourceCertifiedMalettiPackArtifact.Read(alteredCertified));
        Assert.Equal(ContentSourceAxisCorpsArtilleryPackArtifact.ContentHash, corps.Hash());
        Assert.Equal(corps.Identity, ContentSourceAxisCorpsArtilleryPackArtifact.Read(corps.CanonicalBytes).Identity);
        var alteredCorps = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(corps.CanonicalBytes)
            .Replace("\"toe\":9", "\"toe\":8", StringComparison.Ordinal));
        Assert.ThrowsAny<Exception>(() => ContentSourceAxisCorpsArtilleryPackArtifact.Read(alteredCorps));
        Assert.Equal(ContentSourceAxisFreeArtilleryPackArtifact.ContentHash, axisFree.Hash());
        Assert.Equal(axisFree.Identity, ContentSourceAxisFreeArtilleryPackArtifact.Read(axisFree.CanonicalBytes).Identity);
        var alteredAxisFree = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(axisFree.CanonicalBytes)
            .Replace("\"toe\":3", "\"toe\":4", StringComparison.Ordinal));
        Assert.ThrowsAny<Exception>(() => ContentSourceAxisFreeArtilleryPackArtifact.Read(alteredAxisFree));
        Assert.Equal(ContentSourceExpandedIndependentPackArtifact.ContentHash, expanded.Hash());
        Assert.Equal(expanded.Identity, ContentSourceExpandedIndependentPackArtifact.Read(expanded.CanonicalBytes).Identity);
        var alteredExpanded = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(expanded.CanonicalBytes)
            .Replace("\"capabilityPoints\":8", "\"capabilityPoints\":10", StringComparison.Ordinal));
        Assert.ThrowsAny<Exception>(() => ContentSourceExpandedIndependentPackArtifact.Read(alteredExpanded));
        Assert.Equal(ContentSourceLocalMapPackArtifact.ContentHash, map.Hash());
        Assert.Equal(map.Identity, ContentSourceLocalMapPackArtifact.Read(map.CanonicalBytes).Identity);
        Assert.Contains("c3618", ContentSourceLocalMapPackArtifact.Neighbors("c3617"));
        Assert.Contains("c3419", ContentSourceLocalMapPackArtifact.Neighbors("c3320"));
        Assert.Throws<InvalidOperationException>(() => ContentSourceLocalMapPackArtifact.Neighbors("c3922"));
        var alteredMap = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(map.CanonicalBytes)
            .Replace("\"c3618\"", "\"c3619\"", StringComparison.Ordinal));
        Assert.ThrowsAny<Exception>(() => ContentSourceLocalMapPackArtifact.Read(alteredMap));
        Assert.Equal(ContentSourceIndependentPackArtifact.ContentHash, catalog.Hash());
        Assert.Equal(catalog.Identity, ContentSourceIndependentPackArtifact.Read(catalog.CanonicalBytes).Identity);
        var alteredCatalog = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(catalog.CanonicalBytes)
            .Replace("\"toe\":6", "\"toe\":5", StringComparison.Ordinal));
        Assert.ThrowsAny<Exception>(() => ContentSourceIndependentPackArtifact.Read(alteredCatalog));
        var ids = CampaignSourceAxisPriorityMotorization.Targets;
        foreach (var mask in Enumerable.Range(1, 7))
        {
            var subset = ids.Where((_, index) => (mask & (1 << index)) != 0);
            var artifact = ContentSourceMalettiPackArtifact.Create(cairo, subset);
            Assert.Equal(artifact.Identity, ContentSourceMalettiPackArtifact.Read(artifact.CanonicalBytes).Identity);
        }
        var first = ContentSourceMalettiPackArtifact.Create(cairo, [ids[0]]);
        var pair = ContentSourceMalettiPackArtifact.Create(cairo, [ids[1], ids[0]]);
        Assert.Equal(pair.Identity, ContentSourceMalettiPackArtifact.Read(pair.CanonicalBytes).Identity);
        Assert.Equal(pair.Identity, ContentSourceMalettiPackArtifact.Create(cairo, [ids[0], ids[1]]).Identity);
        Assert.NotEqual(first.Identity, pair.Identity);
        Assert.Equal(cairo.Identity, pair.Basis.Identity);
        Assert.Throws<InvalidOperationException>(() => ContentSourceMalettiPackArtifact.Create(cairo, [ids[0], ids[0]]));
        Assert.Throws<InvalidOperationException>(() => ContentSourceMalettiPackArtifact.Create(cairo, ["unknown"]));
        var json = Encoding.UTF8.GetString(pair.CanonicalBytes);
        Assert.ThrowsAny<Exception>(() => ContentSourceMalettiPackArtifact.Read(Encoding.UTF8.GetBytes(
            json.Replace("\"toe\":5", "\"toe\":6", StringComparison.Ordinal))));
        Assert.ThrowsAny<Exception>(() => ContentSourceMalettiPackArtifact.Read(Encoding.UTF8.GetBytes(json + " ")));
    }

    [Fact]
    public void SourceSetupPackPreservesBaseAndRejectsNoncanonicalOrFabricatedParent()
    {
        var basis = Cna1979GrazianiCairoSourceExcerpt.Build();
        var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
        [new RuleReference(Cna1979GrazianiSourceFacts.ScenariosSourceId, "60.41")]);
        var units = CampaignSourceCairoInitialSetup.Units.Select(value => new ContentSourceSetupUnit(
            value.ElementId, "commonwealth", value.CapabilityPoints, value.ComponentId,
            value.ComponentClassId, value.Toe, value.Stacking, origin)).ToArray();
        var artifact = ContentSourceSetupPackArtifact.Create(basis, units);
        var replayed = ContentSourceSetupPackArtifact.Read(artifact.CanonicalBytes);
        Assert.Equal(artifact.Identity, replayed.Identity);
        Assert.Equal(basis.Identity, replayed.Basis.Identity);
        Assert.Equal(4, replayed.Units.Count);
        Assert.Equal(artifact.Identity, ContentSourceSetupPackArtifact.Create(basis, units.Reverse()).Identity);
        Assert.Throws<ArgumentException>(() => ContentSourceSetupPackArtifact.Create(basis, units.Append(units[0])));
        var json = Encoding.UTF8.GetString(artifact.CanonicalBytes);
        var fabricated = Encoding.UTF8.GetBytes(json.Replace("\"parentFormationId\":null", "\"parentFormationId\":\"fake-parent\"", StringComparison.Ordinal));
        Assert.ThrowsAny<Exception>(() => ContentSourceSetupPackArtifact.Read(fabricated));
        Assert.ThrowsAny<Exception>(() => ContentSourceSetupPackArtifact.Read(Encoding.UTF8.GetBytes(json + " ")));

        // A structurally valid alternate TOE is not source certification: the
        // changed content gets a different identity and cannot replace a pinned profile.
        var altered = units.Select(value => value.ElementId == "1-welch" ? value with { Toe = 5 } : value);
        var changed = ContentSourceSetupPackArtifact.Create(basis, altered);
        Assert.NotEqual(artifact.Identity.Hash, changed.Identity.Hash);
        Assert.Equal(changed.Identity, ContentSourceSetupPackArtifact.Read(changed.CanonicalBytes).Identity);
    }
}
