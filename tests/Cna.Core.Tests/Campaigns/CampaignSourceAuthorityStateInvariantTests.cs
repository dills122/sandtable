using System.Reflection;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

// Covers the ctor-refactor of CampaignSourceAuthorityState from a plain class with
// a 49-parameter positional constructor to a `record` whose 40+ mutation methods
// build their successor with a `with` expression (see CampaignSourceAuthority.cs).
//
// A record's custom copy constructor re-running its invariant check looks like
// it should make `with` safe on its own. It does not, and the copy constructor
// here deliberately does NOT call ValidateInvariants(): verified empirically
// with a throwaway repro (a minimal record with a copy-constructor invariant
// check), a `with` expression clones the receiver via the copy constructor FIRST,
// and only AFTER that applies the `with` block's member assignments directly to
// the clone. So a copy-constructor validation call would only ever observe the
// pre-mutation, already-valid state -- it can never catch an invariant that the
// `with` mutation itself introduces -- and, worse, it would spuriously reject
// legitimate multi-step `with` composition that passes through a transiently
// inconsistent intermediate clone (see Validated() / RebindGroupedWorld in the
// production file).
//
// The actual protection in this file is that every mutation method that builds
// its successor via `with` calls the shared ValidateInvariants() method again,
// explicitly, on the *result* (through the private `Validated()` helper). This
// test proves that shared method still rejects invalid state exactly the way the
// primary constructor always has, using the same out-of-range FinalizedOwners
// check as the constructor (`if (finalizedOwners is < 0 or > 3) throw new
// ArgumentOutOfRangeException(...)`).
public sealed class CampaignSourceAuthorityStateInvariantTests
{
    [Fact]
    public void ValidateInvariantsRejectsOutOfRangeFinalizedOwnersEvenThoughTheCopyConstructorCannot()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var initialization = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "source-authority-ctor-refactor-invariant", Cna1979Ruleset.Manifest.Hash, 0,
            CampaignPregameSourceCreation.SetupId,
            CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, initialization),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        var session = CampaignExercises.Begin(request, artifact, initialization).Session!;
        var roll = Assert.Single(CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!.Candidates);
        session = CampaignExercises.Submit(session, Submission(session, roll.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        var finish = CampaignExercises.QueryPregameAxisSupplyFinishDraft(session, CampaignActionAudience.Axis).PrivateActionIds.Single();
        session = CampaignExercises.FinishPregameAxisSupply(session, Submission(session, finish, CampaignActionAudience.Axis)).SuccessorSession!;
        var state = session.SourceAuthority!;
        Assert.InRange(state.FinalizedOwners, 0, 3);

        // Simulate exactly what a `with` expression's member-initializer does to
        // an already-cloned instance: a plain property assignment, bypassing the
        // normal mutation methods and their explicit `.Validated()` call.
        var finalizedOwnersProperty = typeof(CampaignSourceAuthorityState).GetProperty(
            "FinalizedOwners", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        finalizedOwnersProperty.GetSetMethod(nonPublic: true)!.Invoke(state, [99]);
        Assert.Equal(99, state.FinalizedOwners);

        var validateInvariants = typeof(CampaignSourceAuthorityState).GetMethod(
            "ValidateInvariants", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var thrown = Assert.Throws<TargetInvocationException>(() => validateInvariants.Invoke(state, null));
        var invariantViolation = Assert.IsType<ArgumentOutOfRangeException>(thrown.InnerException);
        Assert.Equal("finalizedOwners", invariantViolation.ParamName);
    }

    private static CampaignActionSubmission Submission(ExerciseSession session, string actionId, CampaignActionAudience audience)
    {
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        return new(CampaignActionSubmission.CurrentContractVersion, checkpoint.CampaignId,
            checkpoint.StateVersion, checkpoint.PositionId, audience, actionId);
    }
}
