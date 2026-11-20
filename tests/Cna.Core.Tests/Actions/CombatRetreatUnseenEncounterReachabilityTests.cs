using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Actions;

/// <summary>
/// T020-Q9 reachability evidence (Task 020E1 prerequisite). The question: can a defender's
/// post-assault retreat offer a hex that holds an opposing unit the defender's side has not seen?
/// The retreat cut excludes only current contact targets and own combat occupancy, so such an offer
/// would have no defined encounter outcome.
///
/// Structural argument (scoping doc §7n): retreat is exactly one hex (CMB-POL-001), so every offered
/// destination is adjacent to the defender. The permanent adjacent-contact rule
/// (<c>sandtable.visibility.adjacent-contact.v1</c>) makes every opposing representation adjacent to,
/// or co-located with, any representation of a formation a current contact of that formation, and it
/// is re-derived on every event that places or moves a representation. So an opposing unit in a
/// destination hex is always a known target and the offer is excluded. These tests check that
/// invariant and the absence of any opposing occupant in an offered destination on every retreat
/// cut the bounded Combat fixtures reach across a seed sweep.
/// </summary>
public sealed class CombatRetreatUnseenEncounterReachabilityTests
{
    private const int SeedCount = 12;

    [Fact]
    public void NoReachableRetreatOffersAHexHoldingAnUnseenOpposingUnit()
    {
        var fixtures = Cna1979BreakdownSetupCatalog.Definitions
            .Where(value => value.SetupId.Contains(".combat-", StringComparison.Ordinal))
            .Select(value => value.SetupId).Order(StringComparer.Ordinal).ToArray();
        Assert.NotEmpty(fixtures);
        var retreatCuts = 0;
        var offeredMoves = 0;
        var adjacentEnemiesSeen = 0;
        foreach (var setupId in fixtures)
            for (ulong seed = 1; seed <= SeedCount; seed++)
                foreach (var cut in RetreatCuts(setupId, seed))
                {
                    retreatCuts++;
                    var (offers, adjacentKnown) = AssertNoUnseenOccupant(cut);
                    offeredMoves += offers;
                    adjacentEnemiesSeen += adjacentKnown;
                }
        // The sweep must actually reach retreat decisions with offers, or it proves nothing.
        Assert.True(retreatCuts > 0, "No fixture reached a post-assault retreat decision.");
        Assert.True(offeredMoves > 0, "No reached retreat decision offered a move.");
        Assert.True(adjacentEnemiesSeen > 0, "No reached retreat cut had an adjacent opposing unit.");
    }

    // Checks one reached retreat cut. Returns the number of offered moves and of adjacent opposing
    // representations (each of which must already be a current target of the defender's formation).
    private static (int Offers, int AdjacentKnown) AssertNoUnseenOccupant(ExerciseSession session)
    {
        var cut = CampaignExercises.QueryContactPostResultRetreatCut(session);
        var world = session.CurrentRepeatAssault?.PaidWorld ?? session.CurrentCombatPaidWorld!;
        var definition = session.Context.ArtifactV6!.Definition.LegacyDefinition;
        var elements = definition.Elements.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var defenderSide = CampaignSnapshotSerializer.FormatSide(cut.DefenderSide);
        var opposing = world.Representations.Where(value => value.BoundElementIds.Count > 0
            && value.BoundElementIds.All(id => elements[id].SideId != defenderSide)).ToArray();
        var formation = world.Representations.Where(value => value.BoundElementIds.Any(id =>
            elements[id].ParentFormationId == cut.DefenderFormationId)).ToArray();
        bool Adjacent(string first, string second) => first == second || definition.Edges.Any(edge =>
            (edge.FirstLocationId == first && edge.SecondLocationId == second)
            || (edge.FirstLocationId == second && edge.SecondLocationId == first));

        // Q9 itself: no offered destination holds an opposing unit, seen or unseen.
        foreach (var offer in cut.MoveOffers)
        {
            Assert.True(Adjacent(cut.Defender.CurrentLocationId, offer.DestinationLocationId));
            Assert.DoesNotContain(opposing, value => value.CurrentLocationId == offer.DestinationLocationId);
        }
        // The invariant that makes it unreachable: every opposing unit adjacent to the defender's
        // formation is a current target of that formation at its true location.
        var targets = session.CurrentContacts.CurrentTargetsFor(cut.DefenderSide, cut.DefenderFormationId)
            .Select(value => value.ApparentLocationId).ToHashSet(StringComparer.Ordinal);
        var adjacent = opposing.Where(target => formation.Any(own =>
            Adjacent(own.CurrentLocationId, target.CurrentLocationId))).ToArray();
        Assert.All(adjacent, target => Assert.Contains(target.CurrentLocationId, targets));
        return (cut.MoveOffers.Count, adjacent.Length);
    }

    // Every first-cycle assault the fixture reaches at its first Combat position, one branch per
    // (acting element, assault offer), stopped at each result that requires a defender retreat.
    private static IEnumerable<ExerciseSession> RetreatCuts(string setupId, ulong seed)
    {
        var atCombat = AtCombat(setupId, seed);
        if (atCombat is null) yield break;
        var definition = atCombat.Context.ArtifactV6?.Definition.LegacyDefinition;
        if (definition is null || atCombat.Context.CombatRetreatSource is null) yield break;
        foreach (var side in new[] { LandSide.Axis, LandSide.Commonwealth })
            foreach (var element in definition.Elements.Where(value =>
                         value.SideId == CampaignSnapshotSerializer.FormatSide(side)))
            {
                IReadOnlyList<string> offers;
                try
                {
                    offers = CampaignExercises.QueryContactOwnerCombatCut(atCombat, side, element.ElementId)
                        .AssaultOffers.Select(value => value.ActionId).ToArray();
                }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                {
                    continue;
                }
                foreach (var offer in offers)
                    if (ToResult(atCombat, side, element.ElementId, offer) is { } result
                        && result.CurrentCombatAssaultResult!.Facts.RequiredRetreat == 1)
                        yield return result;
            }
    }

    private static ExerciseSession? ToResult(ExerciseSession session, LandSide side, string element, string offer)
    {
        var attacker = side == LandSide.Axis ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        var defender = side == LandSide.Axis ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
        var step = CampaignHostAdapter.SubmitContactOwnerCombatSelection(session, Command(session, attacker, offer), element);
        if (!step.IsAccepted) return null;
        session = step.SuccessorSession!;
        if (!Try(CampaignHostAdapter.CompleteNativeCombatBarrage(session), ref session)) return null;
        var rba = CampaignExercises.QueryContactRbaCut(session);
        if (!Try(CampaignExercises.SubmitContactDefenderRbaDecline(session,
                Command(session, defender, rba.DeclineOffer.ActionId), rba.DefenderElementId), ref session)) return null;
        foreach (var (audience, owner) in new[] { (attacker, element), (defender, rba.DefenderElementId) })
        {
            try
            {
                var force = CampaignHostAdapter.QueryContactForceOwnerDraft(session, audience, owner);
                if (!Try(CampaignHostAdapter.SubmitContactOwnerForceSeal(session,
                        Command(session, audience, force.PrivateActionIds[0]), owner), ref session)) return null;
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                return null; // V19 force-unavailable or similar cancellation: no assault result.
            }
        }
        if (!Try(CampaignHostAdapter.CompleteNativeCombatForceAssignment(session), ref session)) return null;
        if (!Try(CampaignHostAdapter.CompleteNativeCombatAntiArmor(session), ref session)) return null;
        return Try(CampaignHostAdapter.ResolveNativeCombatAssault(session), ref session) ? session : null;
    }

    private static ExerciseSession? AtCombat(string setupId, ulong seed)
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions.Single(value =>
            value.SetupId == setupId));
        var request = new CampaignCreationRequest(1, $"campaign-q9-{seed}", Cna1979Ruleset.Manifest.Hash,
            seed, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId, setup.Content.Pack.Hash,
            setup.Content.ScenarioId);
        var started = CampaignExercises.Begin(request);
        if (!started.IsStarted) return null;
        var session = started.Session!;
        for (var index = 0; index < 80; index++)
        {
            var position = session.CurrentSnapshot.CurrentPosition.SequenceContext;
            if (position.SegmentId == LandSegmentIds.Combat && position.StepId == LandStepIds.PositionDetermination)
                return session;
            var set = Enum.GetValues<CampaignActionAudience>().Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .FirstOrDefault(value => value?.Candidates.Count > 0);
            if (set is null) return null;
            var action = set.Candidates.FirstOrDefault(value => value is not MoveElementAction and not DesignateReserveAction)
                ?? set.Candidates[0];
            var step = CampaignExercises.Submit(session, new CampaignActionSubmission(1, set.CampaignId,
                set.StateVersion, set.PositionId, set.Audience, action.ActionId));
            if (!step.IsAccepted) return null;
            session = step.SuccessorSession!;
        }
        return null;
    }

    private static bool Try(ExerciseStepResult result, ref ExerciseSession session)
    {
        if (!result.IsAccepted) return false;
        session = result.SuccessorSession!;
        return true;
    }

    private static CampaignActionSubmission Command(ExerciseSession session, CampaignActionAudience audience, string action)
    {
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        return new(1, checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId, audience, action);
    }
}
