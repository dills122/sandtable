using Cna.Core.Observations;
using Cna.Core.Visibility;

namespace Cna.Core.Tests.Observations;

public sealed class CampaignContactCombatSelectionCutTests
{
    [Fact]
    public void AssaultOffersUseOnlyCurrentAdjacentContactHandlesAndApparentHexes()
    {
        CampaignObservationEdge[] edges =
        [
            new("hex-a", "hex-b", []),
            new("hex-b", "hex-c", []),
        ];
        CampaignContactTargetCandidate[] current =
        [
            new("axis-formation", "contact-adjacent", "hex-b"),
            new("axis-formation", "contact-remote", "hex-c"),
            new("other-formation", "contact-other", "hex-b"),
        ];

        var offers = CampaignContactCombatSelectionCut.DeriveAdjacentOffers(
            "axis-element", "axis-formation", "hex-a", edges, current);

        var offer = Assert.Single(offers);
        Assert.Equal("axis-element", offer.ActingElementId);
        Assert.Equal("contact-adjacent", offer.ContactHandle);
        Assert.Equal("hex-b", offer.ApparentLocationId);
        Assert.Empty(CampaignContactCombatSelectionCut.DeriveAdjacentOffers(
            "axis-element", "axis-formation", "hex-a", edges, []));
    }

    [Fact]
    public void SameVisibleContactsProduceSameStableActionReferences()
    {
        CampaignObservationEdge[] edges = [new("hex-a", "hex-b", [])];
        CampaignContactTargetCandidate[] first =
        [
            new("axis-formation", "contact-two", "hex-b"),
            new("axis-formation", "contact-one", "hex-b"),
        ];
        CampaignContactTargetCandidate[] reversed = [first[1], first[0]];

        var left = CampaignContactCombatSelectionCut.DeriveAdjacentOffers(
            "axis-element", "axis-formation", "hex-a", edges, first);
        var right = CampaignContactCombatSelectionCut.DeriveAdjacentOffers(
            "axis-element", "axis-formation", "hex-a", edges, reversed);

        Assert.Equal(left.Select(value => value.ActionId), right.Select(value => value.ActionId));
        Assert.Equal(2, left.Select(value => value.ActionId).Distinct().Count());
    }
}
