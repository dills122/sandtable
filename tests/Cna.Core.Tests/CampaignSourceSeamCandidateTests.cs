using Cna.Core.Campaigns;
using Cna.Core.Visibility;

namespace Cna.Core.Tests;

public sealed class CampaignSourceSeamCandidateTests
{
    [Fact]
    public void PrintedDistanceTriangleRetainsAllElevenDisagreements()
    {
        var conflicts = CampaignSourceSeamCandidate.PublishedOutliers;
        Assert.Equal(CampaignSourceSeamCandidate.ContentHash, CampaignSourceSeamCandidate.Hash());
        Assert.Equal(11, conflicts.Count);
        Assert.Contains(conflicts, value => value.First == "b5925" && value.Second == "a1816" && value.PrintedDistance == 61 && value.CandidateDistance == 62);
        Assert.Contains(conflicts, value => value.First == "d2903" && value.Second == "b5504" && value.PrintedDistance == 80 && value.CandidateDistance == 78);
        var disagreement = CampaignSourceSeamCandidate.Classify("b5925", "a1816");
        Assert.True(disagreement.PublishedDistanceConflict);
        Assert.Equal(62, disagreement.CandidateDistance);
        Assert.Equal(CampaignSourceSeamCertainty.ProvisionalFar, disagreement.Certainty);
        Assert.True(CampaignSourceSeamCandidate.TryPublishedDistance("a1816", "b5925", out var printed));
        Assert.Equal(61, printed);
        Assert.False(CampaignSourceInitialContacts.TryAdjacent("a1816", "b5925", out _));
        Assert.True(CampaignSourceSeamCandidate.TryPublishedDistance("c4807", "d3714", out var tobrukMatruh));
        Assert.Equal(45, tobrukMatruh);
        Assert.False(CampaignSourceSeamCandidate.Classify("c4807", "d3714").PublishedDistanceConflict);
        Assert.True(CampaignSourceSeamCandidate.TryPublishedDistance("b5925", "d3714", out var dernaMatruh));
        Assert.Equal(66, dernaMatruh);
        Assert.False(CampaignSourceSeamCandidate.Classify("b5925", "d3714").PublishedDistanceConflict);
        Assert.False(CampaignSourceSeamCandidate.TryPublishedDistance("c4707", "d3615", out _));
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("c4707", "d3615", out var triangleAdjacent));
        Assert.False(triangleAdjacent);
        Assert.True(CampaignSourceSeamCandidate.TryPublishedDistance("c4807", "e1829", out var tobrukCairo));
        Assert.Equal(103, tobrukCairo);
        Assert.False(CampaignSourceSeamCandidate.Classify("c4807", "e1829").PublishedDistanceConflict);
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("c3617", "e1931", out var cairoAdjacent));
        Assert.False(cairoAdjacent);
        Assert.False(CampaignSourceInitialContacts.TryAdjacent("c3618", "e1931", out _));
        Assert.True(CampaignSourceSeamCandidate.TryPublishedDistance("b4921", "e1829", out var mechiliCairo));
        Assert.Equal(123, mechiliCairo);
        Assert.False(CampaignSourceSeamCandidate.Classify("b4921", "e1829").PublishedDistanceConflict);
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("b4921", "e1931", out var mechiliAdjacent));
        Assert.False(mechiliAdjacent);
        Assert.True(CampaignSourceSeamCandidate.TryPublishedDistance("c1014", "e1829", out var giarabubCairo));
        Assert.Equal(85, giarabubCairo);
        Assert.False(CampaignSourceSeamCandidate.Classify("c1014", "e1829").PublishedDistanceConflict);
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("c1715", "e1931", out var elGreinAdjacent));
        Assert.False(elGreinAdjacent);
        Assert.False(CampaignSourceInitialContacts.TryAdjacent("c1716", "e1931", out _));
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("c1715", "e1430", out var helwanAdjacent));
        Assert.False(helwanAdjacent);
        Assert.True(CampaignSourceSeamCandidate.TryPublishedDistance("c3019", "e1829", out var maddalenaCairo));
        Assert.Equal(82, maddalenaCairo);
        Assert.False(CampaignSourceSeamCandidate.Classify("c3019", "e1829").PublishedDistanceConflict);
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("c3419", "e1931", out var scheferzenAdjacent));
        Assert.False(scheferzenAdjacent);
        Assert.False(CampaignSourceInitialContacts.TryAdjacent("c3420", "e1931", out _));
        Assert.True(CampaignSourceSeamCandidate.TryPublishedDistance("c1014", "d3714", out var giarabubMatruh));
        Assert.Equal(46, giarabubMatruh);
        Assert.True(CampaignSourceSeamCandidate.TryPublishedDistance("c3019", "d3714", out var maddalenaMatruh));
        Assert.Equal(31, maddalenaMatruh);
        Assert.True(CampaignSourceSeamCandidate.TryPublishedDistance("b4921", "d3714", out var mechiliMatruh));
        Assert.Equal(65, mechiliMatruh);
        Assert.True(CampaignSourceSeamCandidate.TryPublishedDistance("a4130", "d3714", out var soluchMatruh));
        Assert.Equal(85, soluchMatruh);
        Assert.True(CampaignSourceSeamCandidate.TryPublishedDistance("a4827", "d3714", out var benghaziMatruh));
        Assert.Equal(91, benghaziMatruh);
        string[] axisSites = ["c3617", "c1715", "c3419", "c3019", "c1014", "c4321", "b4921",
            "a4130", "a4827", "b5925"];
        string[] matruhSites = ["d3612", "d3615", "d3715", "d3714"];
        foreach (var location in axisSites)
        {
            foreach (var matruh in matruhSites)
            {
                Assert.True(CampaignSourceInitialContacts.TryAdjacent(location, matruh, out var matruhAdjacent));
                Assert.False(matruhAdjacent);
            }
        }
        Assert.False(CampaignSourceInitialContacts.TryAdjacent("c1716", "d3612", out _));
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("c3419", "c3520", out var rtrAdjacent));
        Assert.True(rtrAdjacent);
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("c3520", "c3419", out rtrAdjacent));
        Assert.True(rtrAdjacent);
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("c3019", "c3020", out var hussarsAdjacent));
        Assert.True(hussarsAdjacent);
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("c3020", "c3019", out hussarsAdjacent));
        Assert.True(hussarsAdjacent);
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("c1014", "c1015", out var giarabubAdjacent));
        Assert.True(giarabubAdjacent);
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("c1015", "c1014", out giarabubAdjacent));
        Assert.True(giarabubAdjacent);
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("c3419", "c3320", out var rifleAdjacent));
        Assert.True(rifleAdjacent);
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("c3320", "c3419", out rifleAdjacent));
        Assert.True(rifleAdjacent);
        string[] artillerySites = ["c4307", "c4308", "c4407", "c4607"];
        foreach (var location in artillerySites)
        {
            Assert.True(CampaignSourceInitialContacts.TryAdjacent(location, "e1931", out var artilleryAdjacent));
            Assert.False(artilleryAdjacent);
        }
    }

    [Fact]
    public void CandidateNeverCertifiesSectionAliasOrAdjacency()
    {
        var seam = CampaignSourceSeamCandidate.Classify("b4934", "c4901");
        Assert.Equal(0, seam.CandidateDistance);
        Assert.Equal(CampaignSourceSeamCertainty.ProvisionalNear, seam.Certainty);
        Assert.Equal(seam, CampaignSourceSeamCandidate.Classify("c4901", "b4934"));
        var chartExample = CampaignSourceSeamCandidate.Classify("c4102", "d4102");
        Assert.Equal(33, chartExample.CandidateDistance);
        Assert.Equal(CampaignSourceSeamCertainty.ProvisionalFar, chartExample.Certainty);
        Assert.Equal(CampaignSourceSeamCertainty.Unknown, CampaignSourceSeamCandidate.Classify("c4102", "c4103").Certainty);
        Assert.Equal(CampaignSourceSeamCertainty.Unknown, CampaignSourceSeamCandidate.Classify("box.tripoli", "d4102").Certainty);
        Assert.False(CampaignSourceSeamCandidate.TryPublishedDistance("b3939", "c3906", out _));
        Assert.True(CampaignSourceSeamCandidate.TryPublishedDistance("a2703", "d3714", out var distance));
        Assert.Equal(115, distance);
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("a2703", "d3714", out var adjacent));
        Assert.False(adjacent);
    }
}
