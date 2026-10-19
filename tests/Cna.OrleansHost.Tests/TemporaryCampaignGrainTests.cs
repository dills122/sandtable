using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.OrleansHost.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Cna.OrleansHost.Tests;

public sealed class TemporaryCampaignGrainTests
{
    private const string Generation = "11111111-1111-4111-8111-111111111111";
    private const string OtherGeneration = "22222222-2222-4222-8222-222222222222";

    [Fact]
    public void VerifiedReactionMoveWithSuppressedOwnRowsUsesPrivateFormationDirectory()
    {
        var proof = SyntheticProofFor("diary.reactionMoved");
        const string rules =
            "17f3e6047f34b5bf6f5f809055863b664a4bd82a8481db83ee83e0ae5cad3a2a";
        const string setup =
            "sha256:e6631e81ad8f97e39fd9d7eec93bad7fe2b39db4d2d3059ed94a02dd4093e7a3";
        const string content =
            "sha256:646e76e69ecceb82216b37d84e950928099acd8a3cb04b51526d0fe631e512ee";
        CampaignHostPrivateSideCut Cut(CampaignActionAudience audience, long version) =>
            Side(audience, version) with
            {
                RulesetHash = rules,
                ScenarioId = "breakdown-truck-lab",
                OwnElements = audience == proof.Actor ? [] : Side(audience, version).OwnElements,
                DecisionStateKind = audience == proof.Actor ? "reacting" : "phasing-waiting",
                Actions = audience == proof.Actor
                    ? [new CampaignHostPrivateAction(proof.ActionId, proof.ActionKind)]
                    : [],
            };
        CampaignHostPrivateSideCut Initial(CampaignActionAudience audience) =>
            Cut(audience, 1) with
            {
                OwnElements = audience == proof.Actor
                    ? [new CampaignHostPrivateOwnElement(proof.ElementId!,
                        proof.Actor == CampaignActionAudience.Axis
                            ? "axis-infantry.formation"
                            : "commonwealth-infantry.formation", "location", "None")]
                    : Side(audience, 1).OwnElements,
                DecisionStateKind = "normal",
            };
        TemporaryAudienceTracker NewTracker(bool includeOwner = true)
        {
            var initialAxis = Initial(CampaignActionAudience.Axis);
            var initialCommonwealth = Initial(CampaignActionAudience.Commonwealth);
            if (!includeOwner)
            {
                if (proof.Actor == CampaignActionAudience.Axis)
                    initialAxis = initialAxis with { OwnElements = [] };
                else initialCommonwealth = initialCommonwealth with { OwnElements = [] };
            }
            var tracker = TemporaryAudienceTracker.Create(
                "test-campaign", rules, "breakdown-truck-lab", setup, content,
                Hash("initial"), initialAxis, initialCommonwealth)!;
            Assert.True(AdvanceSynthetic(tracker, Cut(CampaignActionAudience.Axis, 2),
                Cut(CampaignActionAudience.Commonwealth, 2)));
            return tracker;
        }

        var tracker = NewTracker();
        Assert.True(AdvanceSynthetic(tracker, Cut(CampaignActionAudience.Axis, 3),
            Cut(CampaignActionAudience.Commonwealth, 3),
            copyKey: "diary.reactionMoved", suppressedReaction: true));
        var actor = tracker.Read(proof.Actor, 3, Generation)!;
        Assert.Empty(actor.OwnElements);
        Assert.Single(actor.Diary);
        Assert.Equal("Reaction move recorded", actor.Diary[0].Title);
        Assert.Equal(proof.Actor == CampaignActionAudience.Axis
            ? "axis-infantry.formation" : "commonwealth-infantry.formation",
            actor.Diary[0].FormationId);
        Assert.Equal(1UL, actor.NextDiaryCursor);
        Assert.Equal("own-element-suppressed",
            (proof.Actor == CampaignActionAudience.Axis
                ? tracker.LastAcceptedOutcome!.Axis : tracker.LastAcceptedOutcome!.Commonwealth)!
            .ReasonCode);
        var changedAxis = Cut(CampaignActionAudience.Axis, 4);
        var changedCommonwealth = Cut(CampaignActionAudience.Commonwealth, 4);
        var changedOwner = new CampaignHostPrivateOwnElement(proof.ElementId!,
            "other-formation", "location", "None");
        if (proof.Actor == CampaignActionAudience.Axis)
            changedAxis = changedAxis with { OwnElements = [changedOwner] };
        else changedCommonwealth = changedCommonwealth with
        { OwnElements = [changedOwner] };
        Assert.False(AdvanceSynthetic(tracker, changedAxis, changedCommonwealth));

        var wrongReason = NewTracker();
        Assert.False(AdvanceSynthetic(wrongReason, Cut(CampaignActionAudience.Axis, 3),
            Cut(CampaignActionAudience.Commonwealth, 3), failure: "suppression-reason",
            copyKey: "diary.reactionMoved", suppressedReaction: true));
        var disclosed = NewTracker();
        var axis = Cut(CampaignActionAudience.Axis, 3);
        var commonwealth = Cut(CampaignActionAudience.Commonwealth, 3);
        if (proof.Actor == CampaignActionAudience.Axis)
            axis = axis with { OwnElements = Side(proof.Actor, 3).OwnElements };
        else commonwealth = commonwealth with
        { OwnElements = Side(proof.Actor, 3).OwnElements };
        Assert.False(AdvanceSynthetic(disclosed, axis, commonwealth,
            copyKey: "diary.reactionMoved", suppressedReaction: true));
        var missingOwner = NewTracker(includeOwner: false);
        Assert.False(AdvanceSynthetic(missingOwner, Cut(CampaignActionAudience.Axis, 3),
            Cut(CampaignActionAudience.Commonwealth, 3),
            copyKey: "diary.reactionMoved", suppressedReaction: true));
    }

    [Fact]
    public void PrivateTrackerAdvancesBothSeatsAcrossUnreadVisibilityLoss()
    {
        const string campaign = "test-campaign";
        const string pin = "source-hash";
        var tracker = TemporaryAudienceTracker.Create(campaign, pin,
            "scenario", "setup-pin", "content-pin", Hash("initial"),
            Side(CampaignActionAudience.Axis, 1, "private-axis-contact"),
            Side(CampaignActionAudience.Commonwealth, 1, "private-commonwealth-contact"));
        Assert.NotNull(tracker);
        var firstAxis = tracker.Read(CampaignActionAudience.Axis, 1, Generation)!;
        var firstCommonwealth = tracker.Read(CampaignActionAudience.Commonwealth, 1, Generation)!;
        Assert.NotEqual(firstAxis.Contacts[0].ContactId,
            firstCommonwealth.Contacts[0].ContactId);
        Assert.DoesNotContain("private-axis-contact", firstAxis.Contacts[0].ContactId);

        Assert.True(AdvanceSynthetic(tracker,
            Side(CampaignActionAudience.Axis, 2, "private-axis-contact"),
            Side(CampaignActionAudience.Commonwealth, 2, "private-commonwealth-contact")));
        var hiddenOnly = tracker.Read(CampaignActionAudience.Axis, 2, Generation)!;
        Assert.Equal(firstAxis.AudienceRevision, hiddenOnly.AudienceRevision);
        Assert.Equal(firstAxis.ActionSetToken, hiddenOnly.ActionSetToken);
        Assert.Equal(firstAxis.Contacts[0].ContactId, hiddenOnly.Contacts[0].ContactId);

        // No read between absence and reappearance; continuity follows committed cuts.
        Assert.True(AdvanceSynthetic(tracker, Side(CampaignActionAudience.Axis, 3),
            Side(CampaignActionAudience.Commonwealth, 3, "private-commonwealth-contact")));
        Assert.True(AdvanceSynthetic(tracker,
            Side(CampaignActionAudience.Axis, 4, "private-axis-contact"),
            Side(CampaignActionAudience.Commonwealth, 4, "private-commonwealth-contact")));
        var returned = tracker.Read(CampaignActionAudience.Axis, 4, Generation)!;
        Assert.NotEqual(firstAxis.Contacts[0].ContactId, returned.Contacts[0].ContactId);
        Assert.Equal(firstAxis.AudienceRevision + 2, returned.AudienceRevision);
        Assert.Equal(firstCommonwealth.AudienceRevision,
            tracker.Read(CampaignActionAudience.Commonwealth, 4, Generation)!.AudienceRevision);
        Assert.NotEqual(returned.Contacts[0].ContactId,
            tracker.Read(CampaignActionAudience.Axis, 4, OtherGeneration)!.Contacts[0].ContactId);
        Assert.Null(tracker.Read(CampaignActionAudience.System, 4, Generation));
        Assert.Null(tracker.Read(CampaignActionAudience.Axis, 3, Generation));
        Assert.Null(tracker.Read(CampaignActionAudience.Axis, 4, "not-a-session-generation"));
        Assert.False(AdvanceSynthetic(tracker, Side(CampaignActionAudience.Axis, 6),
            Side(CampaignActionAudience.Commonwealth, 6)));
        Assert.Null(tracker.Read(CampaignActionAudience.Axis, 4, Generation));
    }

    [Fact]
    public void PrivateDiaryWindowRetainsLatestFiftyAndRotatesGenerationIds()
    {
        // Synthetic host-private proofs exercise window mechanics; the in-process host
        // test below checks the genuine accepted-outcome source and seat classification.
        const string rules =
            "17f3e6047f34b5bf6f5f809055863b664a4bd82a8481db83ee83e0ae5cad3a2a";
        const string setup =
            "sha256:e6631e81ad8f97e39fd9d7eec93bad7fe2b39db4d2d3059ed94a02dd4093e7a3";
        const string content =
            "sha256:646e76e69ecceb82216b37d84e950928099acd8a3cb04b51526d0fe631e512ee";
        CampaignHostPrivateSideCut Pinned(CampaignActionAudience audience, long version) =>
            Side(audience, version) with
            {
                RulesetHash = rules,
                ScenarioId = "breakdown-truck-lab",
                OwnElements =
                [new CampaignHostPrivateOwnElement(
                    audience == SyntheticProofFor("diary.reserveDesignated").Actor
                        ? SyntheticProofFor("diary.reserveDesignated").ElementId!
                        : $"own-{audience}",
                    audience == CampaignActionAudience.Axis
                        ? "axis-infantry.formation" : "commonwealth-infantry.formation",
                    "location", "None")],
                Actions =
                [new CampaignHostPrivateAction(SyntheticProofFor("diary.initiativeOrder").ActionId,
                    SyntheticProofFor("diary.initiativeOrder").ActionKind),
                    new CampaignHostPrivateAction(SyntheticProofFor("diary.reserveDesignated").ActionId,
                        SyntheticProofFor("diary.reserveDesignated").ActionKind)],
            };
        var tracker = TemporaryAudienceTracker.Create("test-campaign", rules,
            "breakdown-truck-lab", setup, content, Hash("initial"),
            Pinned(CampaignActionAudience.Axis, 1),
            Pinned(CampaignActionAudience.Commonwealth, 1))!;
        for (var version = 2; version <= 52; version++)
            Assert.True(AdvanceSynthetic(tracker, Pinned(CampaignActionAudience.Axis, version),
                Pinned(CampaignActionAudience.Commonwealth, version),
                copyKey: "diary.initiativeOrder"));
        var acting = SyntheticProofFor("diary.initiativeOrder").Actor;
        var other = acting == CampaignActionAudience.Axis
            ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
        var axis = tracker.Read(acting, 52, Generation)!;
        var commonwealth = tracker.Read(other, 52, Generation)!;
        Assert.Equal(51UL, axis.NextDiaryCursor);
        Assert.Equal(50, axis.Diary.Length);
        Assert.Equal(Enumerable.Range(2, 50).Select(value => (ulong)value),
            axis.Diary.Select(entry => entry.AudienceCursor));
        Assert.All(axis.Diary, entry =>
        {
            Assert.Equal("Initiative order recorded", entry.Title);
            Assert.Equal("Your choice of acting order was recorded.", entry.Body);
            Assert.Null(entry.FormationId);
        });
        Assert.Empty(commonwealth.Diary);
        Assert.Equal(0UL, commonwealth.NextDiaryCursor);
        var rotated = tracker.Read(acting, 52, OtherGeneration)!;
        Assert.Equal(axis.NextDiaryCursor, rotated.NextDiaryCursor);
        Assert.Equal(axis.Diary.Select(entry => entry.Title),
            rotated.Diary.Select(entry => entry.Title));
        Assert.All(axis.Diary.Zip(rotated.Diary), pair =>
            Assert.NotEqual(pair.First.EntryId, pair.Second.EntryId));

        var unknown = TemporaryAudienceTracker.Create("test-campaign", rules,
            "breakdown-truck-lab", setup, content, Hash("initial"),
            Pinned(CampaignActionAudience.Axis, 1),
            Pinned(CampaignActionAudience.Commonwealth, 1))!;
        Assert.False(AdvanceSynthetic(unknown, Pinned(CampaignActionAudience.Axis, 2),
            Pinned(CampaignActionAudience.Commonwealth, 2), copyKey: "diary.unknown"));
        Assert.Null(unknown.Read(CampaignActionAudience.Axis, 1, Generation));
        Assert.Null(unknown.Read(CampaignActionAudience.Commonwealth, 1, Generation));

        var own = TemporaryAudienceTracker.Create("test-campaign", rules,
            "breakdown-truck-lab", setup, content, Hash("initial"),
            Pinned(CampaignActionAudience.Axis, 1),
            Pinned(CampaignActionAudience.Commonwealth, 1))!;
        Assert.True(AdvanceSynthetic(own, Pinned(CampaignActionAudience.Axis, 2),
            Pinned(CampaignActionAudience.Commonwealth, 2),
            copyKey: "diary.reserveDesignated", ownIndex: 0));
        var reserveActor = SyntheticProofFor("diary.reserveDesignated").Actor;
        Assert.Equal(reserveActor == CampaignActionAudience.Axis
                ? "axis-infantry.formation" : "commonwealth-infantry.formation",
            own.Read(reserveActor, 2, Generation)!.Diary.Single().FormationId);
        Assert.Empty(own.Read(reserveActor == CampaignActionAudience.Axis
            ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis,
            2, Generation)!.Diary);

        var missingOwn = TemporaryAudienceTracker.Create("test-campaign", rules,
            "breakdown-truck-lab", setup, content, Hash("initial"),
            Pinned(CampaignActionAudience.Axis, 1),
            Pinned(CampaignActionAudience.Commonwealth, 1))!;
        Assert.False(AdvanceSynthetic(missingOwn, Pinned(CampaignActionAudience.Axis, 2),
            Pinned(CampaignActionAudience.Commonwealth, 2),
            copyKey: "diary.reserveDesignated"));
        Assert.Null(missingOwn.Read(CampaignActionAudience.Axis, 1, Generation));

        foreach (var (key, ownIndex, failure) in new[]
        {
            ("diary.reserveCompleted", (int?)null, (string?)null),
            ("diary.initiativeOrder", (int?)null, "action-kind"),
            ("diary.reserveDesignated", (int?)1, (string?)null),
        })
        {
            var forged = TemporaryAudienceTracker.Create("test-campaign", rules,
                "breakdown-truck-lab", setup, content, Hash("initial"),
                Pinned(CampaignActionAudience.Axis, 1),
                Pinned(CampaignActionAudience.Commonwealth, 1))!;
            Assert.False(AdvanceSynthetic(forged, Pinned(CampaignActionAudience.Axis, 2),
                Pinned(CampaignActionAudience.Commonwealth, 2), failure, key, ownIndex));
            Assert.Null(forged.Read(CampaignActionAudience.Axis, 1, Generation));
            Assert.Null(forged.Read(CampaignActionAudience.Commonwealth, 1, Generation));
        }
    }

    [Fact]
    public void PrivateTrackerRejectsIncoherentSourceWithoutPublishingEitherSide()
    {
        Assert.Null(TemporaryAudienceTracker.Create("test-campaign", "source-hash",
            "wrong-scenario", "setup-pin", "content-pin", Hash("initial"),
            Side(CampaignActionAudience.Axis, 1),
            Side(CampaignActionAudience.Commonwealth, 1)));
        var axis = Side(CampaignActionAudience.Axis, 1);
        var commonwealth = Side(CampaignActionAudience.Commonwealth, 1);
        foreach (var mismatched in new[]
        {
            commonwealth with { GameTurn = 2 },
            commonwealth with { PhaseId = "other-phase" },
            commonwealth with { StageId = "other-stage" },
        })
        {
            Assert.Null(TemporaryAudienceTracker.Create("test-campaign", "source-hash",
                "scenario", "setup-pin", "content-pin", Hash("initial"), axis, mismatched));
        }
        var tracker = TemporaryAudienceTracker.Create("test-campaign", "source-hash",
            "scenario", "setup-pin", "content-pin", Hash("initial"),
            Side(CampaignActionAudience.Axis, 1),
            Side(CampaignActionAudience.Commonwealth, 1))!;
        Assert.False(AdvanceSynthetic(tracker, Side(CampaignActionAudience.Axis, 2),
            Side(CampaignActionAudience.Axis, 2)));
        Assert.Null(tracker.Read(CampaignActionAudience.Axis, 1, Generation));
        Assert.Null(tracker.Read(CampaignActionAudience.Commonwealth, 1, Generation));
    }

    [Fact]
    public void PrivateActionHandleSurvivesHiddenOnlyCoreIdRemap()
    {
        var axis = Side(CampaignActionAudience.Axis, 1);
        var commonwealth = Side(CampaignActionAudience.Commonwealth, 1);
        var tracker = TemporaryAudienceTracker.Create("test-campaign", "source-hash",
            "scenario", "setup-pin", "content-pin", Hash("initial"), axis, commonwealth)!;
        var original = tracker.Read(CampaignActionAudience.Axis, 1, Generation)!;
        Assert.True(AdvanceSynthetic(tracker, axis with
        {
            StateVersion = 2,
            Actions = [new CampaignHostPrivateAction("new-core-action", "action")],
        }, commonwealth with { StateVersion = 2 }));
        var next = tracker.Read(CampaignActionAudience.Axis, 2, Generation)!;
        Assert.Equal(original.ActionSetToken, next.ActionSetToken);
        Assert.Equal(original.Actions[0].OutwardActionId, next.Actions[0].OutwardActionId);
        Assert.Equal("new-core-action", tracker.Resolve(CampaignActionAudience.Axis,
            2, Generation, original.ActionSetToken, original.Actions[0].OutwardActionId));
        Assert.Null(tracker.Resolve(CampaignActionAudience.Commonwealth,
            2, Generation, original.ActionSetToken, original.Actions[0].OutwardActionId));
    }

    [Fact]
    public void PrivateTrackerRejectsMissingOrForgedAcceptedOutcomeBeforeEitherSideAdvances()
    {
        foreach (var failure in new[]
        {
            "missing", "prior-head", "successor-head", "source", "audience",
            "command", "event", "event-kind", "event-version", "receipt-actor",
        })
        {
            var tracker = TemporaryAudienceTracker.Create("test-campaign", "source-hash",
                "scenario", "setup-pin", "content-pin", Hash("initial"),
                Side(CampaignActionAudience.Axis, 1),
                Side(CampaignActionAudience.Commonwealth, 1))!;
            Assert.False(AdvanceSynthetic(tracker, Side(CampaignActionAudience.Axis, 2),
                Side(CampaignActionAudience.Commonwealth, 2), failure), failure);
            Assert.False(tracker.IsAvailable);
            Assert.Equal(1, tracker.StateVersion);
            Assert.Equal(Hash("initial"), tracker.CommittedHeadHash);
            Assert.Null(tracker.LastAcceptedOutcome);
            Assert.Null(tracker.Read(CampaignActionAudience.Axis, 1, Generation));
            Assert.Null(tracker.Read(CampaignActionAudience.Commonwealth, 1, Generation));
        }
    }

    [Fact]
    public async Task PrivateOutwardIngressDeduplicatesBeforeRemapAndAuthorizesRetainedGeneration()
    {
        using var host = BuildHost();
        await host.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var grain = GetTrustedGrain(host);
            Assert.Equal(TemporaryCampaignStatus.Created,
                (await grain.CreateAsync("operator-1", "create-1")).Status);
            Assert.Equal(TemporaryCampaignStatus.StepLimit,
                (await grain.AdvanceSystemAsync(3)).Status);
            var sessions = new LocalSessionStore(TimeProvider.System);
            var operatorSession = sessions.Bootstrap(null)!;
            var grants = sessions.IssueGrants(operatorSession.OperatorId,
                TemporaryCampaignGrain.FixtureCampaignId);
            var axis = (await sessions.RedeemAsync(operatorSession,
                TemporaryCampaignGrain.FixtureCampaignId, grants.Axis))!;
            var commonwealthOperator = sessions.Bootstrap(null)!;
            var otherGrants = sessions.IssueGrants(commonwealthOperator.OperatorId,
                TemporaryCampaignGrain.FixtureCampaignId);
            var commonwealth = (await sessions.RedeemAsync(commonwealthOperator,
                TemporaryCampaignGrain.FixtureCampaignId, otherGrants.Commonwealth))!;
            var ingress = new TemporaryOutwardActionIngress(sessions,
                host.Services.GetRequiredService<IGrainFactory>(),
                host.Services.GetRequiredService<TemporaryCampaignAccess>());
            var cut = (await grain.Inner.QueryPrivateSideAsync(grain.Capability,
                CampaignActionAudience.Axis, axis.Generation!)).Cut!;
            var request = new TemporaryOutwardActionRequest(1, cut.CampaignId,
                Guid.NewGuid().ToString("D"), axis.Generation!, cut.PositionId,
                cut.Actions.Single(a => a.Kind == "act-first").OutwardActionId,
                1, cut.ActionSetToken);

            var replies = await Task.WhenAll(ingress.SubmitAsync(axis.Token, request),
                ingress.SubmitAsync(axis.Token, request));
            Assert.All(replies, reply => Assert.Equal(TemporaryCampaignStatus.Accepted,
                reply.Status));
            Assert.Equal(replies[0], replies[1]);
            Assert.Equal(cut.StateVersion + 1,
                (await grain.QueryAsync(CampaignActionAudience.Axis)).View!.StateVersion);
            Assert.Equal(replies[0], await ingress.FindRequestAsync(axis.Token,
                cut.CampaignId, request.RequestId));
            Assert.Equal(replies[0], await ingress.SubmitAsync(axis.Token, request));
            Assert.Equal(TemporaryCampaignStatus.Conflict,
                (await ingress.SubmitAsync(axis.Token, request with
                {
                    ActionId = Guid.NewGuid().ToString("D"),
                })).Status);
            Assert.Equal(TemporaryCampaignStatus.Conflict,
                (await ingress.SubmitAsync(commonwealth.Token, request with
                {
                    SessionGeneration = commonwealth.Generation!,
                })).Status);
            Assert.Equal(TemporaryCampaignStatus.Conflict,
                (await ingress.FindRequestAsync(commonwealth.Token,
                    cut.CampaignId, request.RequestId)).Status);

            var beforeStale = await grain.QueryAsync(CampaignActionAudience.Axis);
            var stale = request with { RequestId = Guid.NewGuid().ToString("D") };
            var staleReply = await ingress.SubmitAsync(axis.Token, stale);
            Assert.Equal(TemporaryCampaignStatus.Rejected, staleReply.Status);
            Assert.Equal("stale-view", staleReply.Reason);
            Assert.Equal(staleReply, await ingress.SubmitAsync(axis.Token, stale));
            var current = (await grain.Inner.QueryPrivateSideAsync(grain.Capability,
                CampaignActionAudience.Axis, axis.Generation!)).Cut!;
            var forged = request with
            {
                RequestId = Guid.NewGuid().ToString("D"),
                ExpectedPositionId = current.PositionId,
                ActionSetToken = current.ActionSetToken,
                ActionId = Guid.NewGuid().ToString("D"),
            };
            var forgedReply = await ingress.SubmitAsync(axis.Token, forged);
            Assert.Equal(TemporaryCampaignStatus.Rejected, forgedReply.Status);
            Assert.Equal("invalid-action", forgedReply.Reason);
            Assert.Equal(beforeStale.View,
                (await grain.QueryAsync(CampaignActionAudience.Axis)).View);
            Assert.Equal(beforeStale.LegalActions,
                (await grain.QueryAsync(CampaignActionAudience.Axis)).LegalActions);
            Assert.True(await sessions.RelinquishAsync(axis, cut.CampaignId));
            Assert.Equal(TemporaryCampaignStatus.Rejected,
                (await ingress.FindRequestAsync(axis.Token,
                    cut.CampaignId, request.RequestId)).Status);
        }
        finally { await host.StopAsync(TestContext.Current.CancellationToken); }
    }

    [Fact]
    public async Task SeatRelinquishmentWaitsForAdmittedActionCommit()
    {
        using var host = BuildHost();
        await host.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var grain = GetTrustedGrain(host);
            await grain.CreateAsync("operator-1", "create-1");
            await grain.AdvanceSystemAsync(3);
            var sessions = new LocalSessionStore(TimeProvider.System);
            var operatorSession = sessions.Bootstrap(null)!;
            var grants = sessions.IssueGrants(operatorSession.OperatorId,
                TemporaryCampaignGrain.FixtureCampaignId);
            var axis = (await sessions.RedeemAsync(operatorSession,
                TemporaryCampaignGrain.FixtureCampaignId, grants.Axis))!;
            var cut = (await grain.Inner.QueryPrivateSideAsync(grain.Capability,
                CampaignActionAudience.Axis, axis.Generation!)).Cut!;
            var request = new TemporaryOutwardActionRequest(1, cut.CampaignId,
                Guid.NewGuid().ToString("D"), axis.Generation!, cut.PositionId,
                cut.Actions.Single(action => action.Kind == "act-first").OutwardActionId,
                1, cut.ActionSetToken);
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var inFlight = sessions.WithAuthorizedSeatAsync(axis.Token, cut.CampaignId,
                async session =>
                {
                    entered.SetResult();
                    await release.Task;
                    return await grain.Inner.SubmitOutwardAsync(grain.Capability,
                        new TemporaryAuthenticatedSeat(session.OperatorId, cut.CampaignId,
                            CampaignActionAudience.Axis, session.Generation!), request);
                }, new TemporaryOutwardActionReply(TemporaryCampaignStatus.Rejected, "unauthorized"));
            await entered.Task;
            var relinquish = sessions.RelinquishAsync(axis, cut.CampaignId);
            Assert.False(relinquish.IsCompleted);
            var lateAdmission = sessions.WithAuthorizedSeatAsync(axis.Token,
                cut.CampaignId, _ => Task.FromResult(true), false);
            Assert.Equal(cut.StateVersion,
                (await grain.QueryAsync(CampaignActionAudience.Axis)).View!.StateVersion);
            release.SetResult();
            Assert.Equal(TemporaryCampaignStatus.Accepted, (await inFlight).Status);
            Assert.True(await relinquish);
            Assert.False(await lateAdmission);
            Assert.Equal(cut.StateVersion + 1,
                (await grain.QueryAsync(CampaignActionAudience.Axis)).View!.StateVersion);
            var ingress = new TemporaryOutwardActionIngress(sessions,
                host.Services.GetRequiredService<IGrainFactory>(),
                host.Services.GetRequiredService<TemporaryCampaignAccess>());
            Assert.Equal(TemporaryCampaignStatus.Rejected,
                (await ingress.FindRequestAsync(axis.Token, cut.CampaignId,
                    request.RequestId)).Status);
            Assert.Equal(TemporaryCampaignStatus.Rejected,
                (await ingress.SubmitAsync(axis.Token, request with
                {
                    RequestId = Guid.NewGuid().ToString("D"),
                })).Status);
        }
        finally { await host.StopAsync(TestContext.Current.CancellationToken); }
    }

    [Fact]
    public async Task SeatHandoffWaitsForAdmittedAction()
    {
        var sessions = new LocalSessionStore(TimeProvider.System);
        var operatorSession = sessions.Bootstrap(null)!;
        var grants = sessions.IssueGrants(operatorSession.OperatorId,
            TemporaryCampaignGrain.FixtureCampaignId);
        var axis = (await sessions.RedeemAsync(operatorSession,
            TemporaryCampaignGrain.FixtureCampaignId, grants.Axis))!;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var inFlight = sessions.WithAuthorizedSeatAsync(axis.Token, axis.CampaignId!,
            async _ =>
            {
                entered.SetResult();
                await release.Task;
                return true;
            }, false);
        await entered.Task;
        var handoff = sessions.RedeemAsync(axis, axis.CampaignId!, grants.Commonwealth);
        Assert.False(handoff.IsCompleted);
        release.SetResult();
        Assert.True(await inFlight);
        var next = await handoff;
        Assert.Equal("commonwealth", next!.Seat);
        Assert.Null(sessions.Find(axis.Token));
        Assert.NotEqual(axis.Generation, next.Generation);
    }

    private static CampaignHostPrivateSideCut Side(CampaignActionAudience side, long version,
        string? representation = null) => new("test-campaign", version, "source-hash",
        "scenario", "position", 1, "phase", "stage", side,
        [new CampaignHostPrivateLocation("location", "terrain")],
        [new CampaignHostPrivateOwnElement($"own-{side}", $"formation-{side}", "location", "none")],
        representation is null ? [] : [new CampaignHostPrivateContact(representation, "location", false)],
        [new CampaignHostPrivateAction("core-action", "action")], "normal");

    private sealed record SyntheticProof(byte[] EventBytes, string EventKind, int EventVersion,
        string ActionKind, string ActionId, CampaignActionAudience Actor, string? ElementId);

    private static readonly Lazy<Dictionary<string, SyntheticProof>> SyntheticProofs =
        new(BuildSyntheticProofs);

    private static SyntheticProof SyntheticProofFor(string? copyKey) =>
        SyntheticProofs.Value[copyKey is null ? "silent"
            : copyKey is "diary.reserveDesignated" or "diary.reactionMoved"
                ? copyKey : "diary.initiativeOrder"];

    private static Dictionary<string, SyntheticProof> BuildSyntheticProofs()
    {
        var found = new Dictionary<string, SyntheticProof>(StringComparer.Ordinal);
        var session = CampaignHostAdapter.Create(Request()).Session!;
        for (var index = 0; index < 40 && found.Count < 3; index++)
        {
            var chosen = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet!)
                .First(set => set.Candidates.Count > 0);
            var candidate = chosen.Candidates.FirstOrDefault(value => value.Kind == "designate-reserve")
                ?? chosen.Candidates.FirstOrDefault(value => value.Kind == "act-first")
                ?? chosen.Candidates[0];
            var command = new CampaignActionSubmission(1, chosen.CampaignId,
                chosen.StateVersion, chosen.PositionId, chosen.Audience, candidate.ActionId);
            var step = CampaignHostAdapter.Submit(session, command);
            Assert.True(step.IsAccepted);
            var evidence = step.Evidence!;
            var accepted = new CampaignHostAcceptedStep(
                CampaignActionSubmissionSerializer.SerializeCanonical(command),
                evidence.EventRecords,
                CampaignActionAcceptanceReceiptSerializer.Serialize(evidence.Receipt),
                evidence.SnapshotCheckpoint);
            var decision = CampaignHostAdapter.DecideAcceptedOutcomes(session, accepted);
            Assert.True(decision.HasTypedProof);
            var actorDecision = chosen.Audience == CampaignActionAudience.Axis
                ? decision.Axis! : decision.Commonwealth!;
            var key = actorDecision.ProposedCopyKey
                ?? (chosen.Audience == CampaignActionAudience.System ? "silent" : null);
            if (key is not null && !found.ContainsKey(key))
            {
                var successor = CampaignHostAdapter.QueryPrivateSideCut(step.SuccessorSession!,
                    chosen.Audience);
                var element = actorDecision.OwnElementIndex is { } ownIndex
                    ? successor!.OwnElements[ownIndex].ElementId : null;
                using var document = JsonDocument.Parse(evidence.EventRecords[0]);
                found[key] = new SyntheticProof(evidence.EventRecords[0],
                    document.RootElement.GetProperty("eventType").GetString()!,
                    document.RootElement.GetProperty("contractVersion").GetInt32(),
                    candidate.Kind, candidate.ActionId, chosen.Audience, element);
            }
            session = step.SuccessorSession!;
        }
        Assert.Contains("silent", found.Keys);
        Assert.Contains("diary.initiativeOrder", found.Keys);
        Assert.Contains("diary.reserveDesignated", found.Keys);
        found["diary.reactionMoved"] = BuildReactionProof();
        return found;
    }

    private static SyntheticProof BuildReactionProof()
    {
        var request = new CampaignCreationRequest(1, "reaction-proof-test",
            Cna1979Ruleset.Manifest.Hash, 12345,
            "rules-lab.breakdown.reaction.v1",
            "sha256:ed4e33358ede11f3c25661798add9088f11d8853053dbf7c8992ba5f92d9a36e",
            "rules-lab.content.breakdown-reaction.v1",
            "sha256:1e10648bb65f7ec6d4ad3f33bbf612f51c11a927ebc2e8b110aa5544af3918dd",
            "breakdown-reaction-lab");
        var session = CampaignHostAdapter.Create(request).Session!;
        for (var index = 0; index < 20; index++)
        {
            var set = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet!)
                .First(value => value.Candidates.Count > 0);
            if (set.Candidates.OfType<MoveElementAction>().Any()) break;
            var next = set.Candidates.FirstOrDefault(value => value is ActFirstAction)
                ?? set.Candidates.FirstOrDefault(value => value is CompleteReserveDesignationAction)
                ?? set.Candidates[0];
            session = CampaignHostAdapter.Submit(session, new CampaignActionSubmission(1,
                set.CampaignId, set.StateVersion, set.PositionId, set.Audience, next.ActionId))
                .SuccessorSession!;
        }
        var owner = Enum.GetValues<CampaignActionAudience>()
            .Select(audience => CampaignExercises.Query(session, audience).ActionSet!)
            .First(value => value.Candidates.OfType<MoveElementAction>().Any());
        var trigger = owner.Candidates.OfType<MoveElementAction>()
            .Select(candidate => CampaignHostAdapter.Submit(session,
                new CampaignActionSubmission(1, owner.CampaignId, owner.StateVersion,
                    owner.PositionId, owner.Audience, candidate.ActionId)))
            .First(result => result.IsAccepted
                && Enum.GetValues<CampaignActionAudience>()
                    .Select(audience => CampaignExercises.Query(result.SuccessorSession!,
                        audience).ActionSet!)
                    .Any(set => set.Candidates.Any(action =>
                        action.Kind == "move-reacting-element")));
        session = trigger.SuccessorSession!;
        var reactor = owner.Audience == CampaignActionAudience.Axis
            ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
        var reacting = CampaignExercises.Query(session, reactor).ActionSet!;
        var move = reacting.Candidates.First(candidate =>
            candidate.Kind == "move-reacting-element");
        var accepted = CampaignHostAdapter.Submit(session,
            new CampaignActionSubmission(1, reacting.CampaignId,
                reacting.StateVersion, reacting.PositionId, reactor, move.ActionId));
        Assert.True(accepted.IsAccepted);
        var eventBytes = Assert.Single(accepted.Evidence!.EventRecords);
        using var document = JsonDocument.Parse(eventBytes);
        return new SyntheticProof(eventBytes,
            document.RootElement.GetProperty("eventType").GetString()!,
            document.RootElement.GetProperty("contractVersion").GetInt32(),
            move.Kind, move.ActionId, reactor,
            document.RootElement.GetProperty("elementId").GetString());
    }

    private static bool AdvanceSynthetic(TemporaryAudienceTracker tracker,
        CampaignHostPrivateSideCut axis, CampaignHostPrivateSideCut commonwealth,
        string? failure = null, string? copyKey = null, int? ownIndex = null,
        bool suppressedReaction = false)
    {
        var nextVersion = tracker.StateVersion + 1;
        var proof = SyntheticProofFor(copyKey);
        var actionId = proof.ActionId;
        var command = CampaignActionSubmissionSerializer.SerializeCanonical(
            new CampaignActionSubmission(1, "test-campaign", tracker.StateVersion,
                "position", proof.Actor, actionId));
        var receipt = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            contractVersion = 1,
            campaignId = "test-campaign",
            priorStateVersion = tracker.StateVersion,
            committedStateVersion = nextVersion,
            resultingPositionId = "position",
            audience = failure == "receipt-actor" ? "commonwealth" :
                proof.Actor.ToString().ToLowerInvariant(),
            actionId,
        }));
        var eventBytes = proof.EventBytes;
        var snapshot = Encoding.UTF8.GetBytes($"synthetic-snapshot-{nextVersion}");
        var step = new CampaignHostAcceptedStep(command,
            [failure == "event" ? Encoding.UTF8.GetBytes("forged-event") : eventBytes],
            receipt, snapshot);
        var pins = tracker.SourcePin.Split('|');
        CampaignAudienceOutcomeDecision Decision(CampaignActionAudience audience) => new(
            1, audience, Hash("test-campaign"),
            Hash(string.Join('\n', pins[0], pins[2], pins[3], pins[1])),
            tracker.CommittedHeadHash, Hash(snapshot), tracker.StateVersion,
            nextVersion, Hash(command), proof.Actor, proof.ActionKind,
            Hash(eventBytes), 0, proof.EventKind, proof.EventVersion,
            copyKey is not null && audience == proof.Actor
                ? CampaignAudienceOutcomeDisposition.Unavailable
                : CampaignAudienceOutcomeDisposition.Silent,
            copyKey is not null && audience == proof.Actor
                ? suppressedReaction && failure != "suppression-reason"
                    ? "own-element-suppressed" : "copy-catalog-unapproved"
                : "known-silent",
            audience == proof.Actor ? copyKey : null,
            audience == proof.Actor ? ownIndex : null);
        var outcomes = new CampaignAudienceOutcomePair(
            Decision(CampaignActionAudience.Axis),
            Decision(CampaignActionAudience.Commonwealth));
        if (failure == "prior-head")
            outcomes = outcomes with
            {
                Commonwealth = outcomes.Commonwealth! with { PriorHeadHash = Hash("other-head") },
            };
        if (failure == "successor-head")
            outcomes = outcomes with
            {
                Commonwealth = outcomes.Commonwealth! with { SuccessorHeadHash = Hash("other-head") },
            };
        if (failure == "source")
            outcomes = outcomes with
            {
                Commonwealth = outcomes.Commonwealth! with { SourceBindingHash = Hash("other-source") },
            };
        if (failure == "audience")
            outcomes = outcomes with
            {
                Commonwealth = outcomes.Commonwealth! with { Audience = CampaignActionAudience.Axis },
            };
        if (failure == "command")
            outcomes = outcomes with
            {
                Commonwealth = outcomes.Commonwealth! with { CommandBindingHash = Hash("other-command") },
            };
        if (failure == "event-kind")
            outcomes = outcomes with
            {
                Axis = outcomes.Axis! with { EventKind = "other-event" },
                Commonwealth = outcomes.Commonwealth! with { EventKind = "other-event" },
            };
        if (failure == "event-version")
            outcomes = outcomes with
            {
                Axis = outcomes.Axis! with { EventContractVersion = proof.EventVersion + 1 },
                Commonwealth = outcomes.Commonwealth! with { EventContractVersion = proof.EventVersion + 1 },
            };
        if (failure == "action-kind")
            outcomes = outcomes with
            {
                Axis = outcomes.Axis! with { ActionKind = "complete-reserve-designation" },
                Commonwealth = outcomes.Commonwealth! with
                { ActionKind = "complete-reserve-designation" },
            };
        return tracker.AdvanceAccepted(axis, commonwealth, step,
            failure == "missing" ? null : outcomes);
    }

    private static string Hash(string value) => Hash(Encoding.UTF8.GetBytes(value));
    private static string Hash(byte[] bytes) =>
        $"sha256:{Convert.ToHexStringLower(SHA256.HashData(bytes))}";

    [Fact]
    public async Task InProcessReactionMovePreservesApprovedDiaryAndOtherSeatPrivacy()
    {
        // The production CreateAsync fixture is the truck setup, so seed only a
        // genuine Reaction Core creation. Every action traverses the real grain.
        var request = new CampaignCreationRequest(1, TemporaryCampaignGrain.FixtureCampaignId,
            Cna1979Ruleset.Manifest.Hash, 12345,
            "rules-lab.breakdown.reaction.v1",
            "sha256:ed4e33358ede11f3c25661798add9088f11d8853053dbf7c8992ba5f92d9a36e",
            "rules-lab.content.breakdown-reaction.v1",
            "sha256:1e10648bb65f7ec6d4ad3f33bbf612f51c11a927ebc2e8b110aa5544af3918dd",
            "breakdown-reaction-lab");
        var start = CampaignHostAdapter.Create(request);
        Assert.True(start.IsStarted);
        var session = start.Session!;
        var initialAxis = CampaignHostAdapter.QueryPrivateSideCut(session,
            CampaignActionAudience.Axis)!;
        var initialCommonwealth = CampaignHostAdapter.QueryPrivateSideCut(session,
            CampaignActionAudience.Commonwealth)!;
        var tracker = TemporaryAudienceTracker.Create(request.CampaignId,
            request.RulesetHash, request.ScenarioId, request.SetupHash,
            request.ContentHash, Hash(start.InitialSnapshotBytes!),
            initialAxis, initialCommonwealth);
        Assert.NotNull(tracker);
        Assert.NotNull(TemporaryPlayerViewProjection.LookupDiaryCopy(tracker.SourcePin,
            TemporaryPlayerViewProjection.DiaryCatalogVersion, "en-CA",
            "diary.initiativeOrder"));
        var pins = tracker.SourcePin.Split('|');
        Assert.Equal(4, pins.Length);
        Assert.Null(TemporaryPlayerViewProjection.LookupDiaryCopy(
            string.Join('|', pins[0], "breakdown-truck-lab", pins[2], pins[3]),
            TemporaryPlayerViewProjection.DiaryCatalogVersion, "en-CA",
            "diary.initiativeOrder"));
        Assert.Null(TemporaryPlayerViewProjection.LookupDiaryCopy(
            string.Join('|', pins[0], pins[1], TemporaryCampaignGrain.SetupHash, pins[3]),
            TemporaryPlayerViewProjection.DiaryCatalogVersion, "en-CA",
            "diary.initiativeOrder"));
        Assert.Null(TemporaryPlayerViewProjection.LookupDiaryCopy(
            string.Join('|', pins[0], pins[1], pins[2], TemporaryCampaignGrain.ContentHash),
            TemporaryPlayerViewProjection.DiaryCatalogVersion, "en-CA",
            "diary.initiativeOrder"));

        using var host = BuildHost();
        var memory = host.Services.GetRequiredService<TemporaryCampaignMemoryStore>()
            .Get(TemporaryCampaignGrain.FixtureCampaignId);
        memory.Committed = session;
        memory.CommittedVersion = tracker.StateVersion;
        memory.AudienceTracker = tracker;
        await host.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var grain = GetTrustedGrain(host);
            for (var index = 0; index < 20; index++)
            {
                var set = Enum.GetValues<CampaignActionAudience>()
                    .Select(audience => CampaignExercises.Query(session, audience).ActionSet!)
                    .First(value => value.Candidates.Count > 0);
                if (set.Candidates.OfType<MoveElementAction>().Any()) break;
                var candidate = set.Candidates.FirstOrDefault(value => value is ActFirstAction)
                    ?? set.Candidates.FirstOrDefault(value =>
                        value is CompleteReserveDesignationAction)
                    ?? set.Candidates[0];
                var setupCommand = new CampaignActionSubmission(1, set.CampaignId,
                    set.StateVersion, set.PositionId, set.Audience, candidate.ActionId);
                var core = CampaignHostAdapter.Submit(session, setupCommand);
                Assert.True(core.IsAccepted);
                var setupReply = await grain.SubmitAsync(
                    set.Audience == CampaignActionAudience.System ? "host-system" : "seat-1",
                    $"reaction-setup-{index}", set.Audience,
                    CampaignActionSubmissionSerializer.SerializeCanonical(setupCommand));
                Assert.True(setupReply.Status == TemporaryCampaignStatus.Accepted,
                    $"{candidate.Kind} at step {index}: {setupReply.Status} {setupReply.Reason}");
                Assert.Equal(Assert.Single(core.Evidence!.EventRecords), setupReply.Event);
                session = core.SuccessorSession!;
            }
            var owner = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet!)
                .First(value => value.Candidates.OfType<MoveElementAction>().Any());
            var trigger = owner.Candidates.OfType<MoveElementAction>()
                .First(candidate =>
                {
                    var attempted = CampaignHostAdapter.Submit(session,
                        new CampaignActionSubmission(1, owner.CampaignId,
                            owner.StateVersion, owner.PositionId, owner.Audience,
                            candidate.ActionId));
                    return attempted.IsAccepted && Enum.GetValues<CampaignActionAudience>()
                        .Select(audience => CampaignExercises.Query(
                            attempted.SuccessorSession!, audience).ActionSet!)
                        .Any(value => value.Candidates.Any(action =>
                            action.Kind == "move-reacting-element"));
                });
            var opening = new CampaignActionSubmission(1, owner.CampaignId,
                owner.StateVersion, owner.PositionId, owner.Audience, trigger.ActionId);
            var coreOpening = CampaignHostAdapter.Submit(session, opening);
            Assert.True(coreOpening.IsAccepted);
            var hostedOpening = await grain.SubmitAsync("phasing-seat", "reaction-opening",
                owner.Audience, CampaignActionSubmissionSerializer.SerializeCanonical(opening));
            Assert.Equal(TemporaryCampaignStatus.Accepted, hostedOpening.Status);
            Assert.Equal(Assert.Single(coreOpening.Evidence!.EventRecords), hostedOpening.Event);
            session = coreOpening.SuccessorSession!;

            var reactor = owner.Audience == CampaignActionAudience.Axis
                ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
            var reacting = CampaignExercises.Query(session, reactor).ActionSet!;
            var move = reacting.Candidates.First(candidate =>
                candidate.Kind == "move-reacting-element");
            var command = new CampaignActionSubmission(1, reacting.CampaignId,
                reacting.StateVersion, reacting.PositionId, reactor, move.ActionId);
            var bytes = CampaignActionSubmissionSerializer.SerializeCanonical(command);
            var coreMove = CampaignHostAdapter.Submit(session, command);
            Assert.True(coreMove.IsAccepted);
            var beforeOwn = CampaignHostAdapter.QueryPrivateSideCut(session, reactor)!;
            var afterOwn = CampaignHostAdapter.QueryPrivateSideCut(coreMove.SuccessorSession!,
                reactor)!;
            Assert.Equal("reacting", beforeOwn.DecisionStateKind);
            Assert.Equal("reacting", afterOwn.DecisionStateKind);
            Assert.Empty(beforeOwn.OwnElements);
            Assert.Empty(afterOwn.OwnElements);
            var priorActor = (await grain.QueryPrivateSideAsync(reactor)).Cut!;
            var priorOther = (await grain.QueryPrivateSideAsync(owner.Audience)).Cut!;
            var reply = await grain.SubmitAsync("reacting-seat", "reaction-move", reactor,
                bytes);
            Assert.Equal(TemporaryCampaignStatus.Accepted, reply.Status);
            Assert.Equal(Assert.Single(coreMove.Evidence!.EventRecords), reply.Event);
            Assert.Equal(CampaignActionAcceptanceReceiptSerializer.Serialize(
                coreMove.Evidence.Receipt), reply.Receipt);

            var actorCut = (await grain.QueryPrivateSideAsync(reactor)).Cut!;
            var otherCut = (await grain.QueryPrivateSideAsync(owner.Audience)).Cut!;
            var decision = reactor == CampaignActionAudience.Axis
                ? tracker.LastAcceptedOutcome!.Axis! : tracker.LastAcceptedOutcome!.Commonwealth!;
            Assert.Equal("diary.reactionMoved", decision.ProposedCopyKey);
            Assert.Equal("own-element-suppressed", decision.ReasonCode);
            Assert.Null(decision.OwnElementIndex);
            Assert.Empty(actorCut.OwnElements);
            Assert.Equal(priorActor.NextDiaryCursor + 1, actorCut.NextDiaryCursor);
            var diary = actorCut.Diary[^1];
            var copy = TemporaryPlayerViewProjection.LookupDiaryCopy(actorCut.SourcePin,
                TemporaryPlayerViewProjection.DiaryCatalogVersion, "en-CA",
                "diary.reactionMoved");
            Assert.NotNull(copy);
            Assert.Equal(copy.Title, diary.Title);
            Assert.Equal(copy.Body, diary.Body);
            using var eventDocument = JsonDocument.Parse(reply.Event!);
            var elementId = eventDocument.RootElement.GetProperty("elementId").GetString();
            var initialOwn = reactor == CampaignActionAudience.Axis
                ? initialAxis : initialCommonwealth;
            Assert.Equal(Assert.Single(initialOwn.OwnElements,
                element => element.ElementId == elementId).FormationId,
                diary.FormationId);
            Assert.Equal(priorOther.NextDiaryCursor, otherCut.NextDiaryCursor);
            Assert.Equal(priorOther.Diary.Length, otherCut.Diary.Length);
            Assert.Equal(priorOther.Diary.Select(entry =>
                    (entry.AudienceCursor, entry.Title, entry.Body, entry.FormationId)),
                otherCut.Diary.Select(entry =>
                    (entry.AudienceCursor, entry.Title, entry.Body, entry.FormationId)));
            Assert.DoesNotContain(otherCut.Diary, entry =>
                entry.Title == copy.Title && entry.Body == copy.Body
                && entry.FormationId == diary.FormationId);
            Assert.DoesNotContain(otherCut.OwnElements,
                element => element.ElementId == elementId);

            var retried = await grain.SubmitAsync("reacting-seat", "reaction-move",
                reactor, bytes);
            Assert.Equal(TemporaryCampaignStatus.Accepted, retried.Status);
            Assert.Equal(reply.Receipt, retried.Receipt);
            Assert.Equal(actorCut.NextDiaryCursor,
                (await grain.QueryPrivateSideAsync(reactor)).Cut!.NextDiaryCursor);
            Assert.Equal(TemporaryCampaignStatus.Conflict,
                (await grain.SubmitAsync("other-seat", "reaction-move", reactor,
                    bytes)).Status);
            Assert.Equal(TemporaryCampaignStatus.Rejected,
                (await grain.SubmitAsync("other-seat", "wrong-seat-reaction",
                    owner.Audience, bytes)).Status);
            Assert.Equal(TemporaryCampaignStatus.Rejected,
                (await grain.SubmitAsync("reacting-seat", "stale-reaction",
                    reactor, bytes)).Status);
            Assert.Equal(actorCut.NextDiaryCursor,
                (await grain.QueryPrivateSideAsync(reactor)).Cut!.NextDiaryCursor);
            Assert.Equal(otherCut.NextDiaryCursor,
                (await grain.QueryPrivateSideAsync(owner.Audience)).Cut!.NextDiaryCursor);
        }
        finally { await host.StopAsync(TestContext.Current.CancellationToken); }
    }

    [Fact]
    public async Task InProcessHostMatchesDirectCoreAndDeduplicatesConcurrentRequests()
    {
        using var host = BuildHost();
        await host.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            Assert.Equal(0, host.Services.GetRequiredService<IOptions<EndpointOptions>>()
                .Value.GatewayPort);
            var grain = GetTrustedGrain(host);
            var directStart = CampaignHostAdapter.Create(Request());
            var direct = directStart.Session!;
            var creation = await grain.CreateAsync("operator-1", "create-1");
            Assert.Equal(TemporaryCampaignStatus.Created, creation.Status);
            Assert.Equal(directStart.CreationEventBytes, creation.Event);
            Assert.Equal(directStart.InitialSnapshotBytes, creation.Snapshot);
            var stored = host.Services.GetRequiredService<TemporaryCampaignMemoryStore>()
                .Get(TemporaryCampaignGrain.FixtureCampaignId);
            Assert.Equal(Hash(creation.Snapshot!), stored.AudienceTracker!.CommittedHeadHash);
            Assert.Null(stored.AudienceTracker.LastAcceptedOutcome);
            Assert.Equal(creation.Event, (await grain.CreateAsync("operator-1", "create-1")).Event);
            Assert.Equal(TemporaryCampaignStatus.Conflict,
                (await grain.CreateAsync("operator-2", "create-1")).Status);
            var forged = new byte[32];
            Assert.Equal(TemporaryCampaignStatus.Rejected,
                (await grain.Inner.CreateAsync(forged, "operator-1", "create-2")).Status);
            Assert.Equal(TemporaryCampaignStatus.Rejected,
                (await grain.Inner.QueryPrivateSideAsync(forged, CampaignActionAudience.Axis,
                    Generation)).Status);
            Assert.Equal(TemporaryCampaignStatus.Unavailable,
                (await grain.Inner.QueryPrivateSideAsync(grain.Capability,
                    CampaignActionAudience.System, Generation)).Status);
            var axisPrivate = await grain.Inner.QueryPrivateSideAsync(grain.Capability,
                CampaignActionAudience.Axis, Generation);
            var commonwealthPrivate = await grain.Inner.QueryPrivateSideAsync(grain.Capability,
                CampaignActionAudience.Commonwealth, Generation);
            Assert.Equal(TemporaryCampaignStatus.Current, axisPrivate.Status);
            Assert.Equal(TemporaryCampaignStatus.Current, commonwealthPrivate.Status);
            Assert.Equal(axisPrivate.Cut!.StateVersion, commonwealthPrivate.Cut!.StateVersion);
            Assert.Equal(axisPrivate.Cut.PositionId, commonwealthPrivate.Cut.PositionId);
            Assert.Equal(axisPrivate.Cut.SourcePin, commonwealthPrivate.Cut.SourcePin);
            var privateRead = await grain.Inner.QueryAsync(forged, CampaignActionAudience.Axis);
            Assert.Equal(TemporaryCampaignStatus.Rejected, privateRead.Status);
            Assert.Null(privateRead.View);
            Assert.Null(privateRead.LegalActions);
            Assert.Equal(TemporaryCampaignStatus.Rejected,
                (await grain.Inner.AdvanceSystemAsync(forged, 1)).Status);
            Assert.Equal(TemporaryCampaignStatus.Rejected,
                (await grain.Inner.SubmitAsync(forged, "host-system", "forged",
                    CampaignActionAudience.System, [])).Status);

            var sawApprovedDiaryEntry = false;
            for (var index = 0; index < 4; index++)
            {
                foreach (var side in new[] { CampaignActionAudience.Axis, CampaignActionAudience.Commonwealth })
                {
                    var hosted = await grain.QueryAsync(side);
                    var expected = CampaignHostAdapter.QueryAudience(direct, side);
                    Assert.Equal(TemporaryCampaignStatus.Current, hosted.Status);
                    Assert.Equal(expected.Observation!.CampaignId, hosted.View!.CampaignId);
                    Assert.Equal(expected.Observation.StateVersion, hosted.View.StateVersion);
                    Assert.Equal(expected.Observation.Position.PositionId, hosted.View.PositionId);
                    Assert.Equal(expected.Observation.Observer.ToString(), hosted.View.Observer);
                    Assert.Equal(expected.Observation.OwnElements.Count, hosted.View.OwnElementCount);
                    Assert.Equal(expected.Observation.ApparentOpposingPresences.Count,
                        hosted.View.ApparentOpposingPresenceCount);
                    Assert.Equal(CampaignLegalActionSerializer.Serialize(expected.LegalActions!),
                        hosted.LegalActions);
                    var privateCut = (await grain.QueryPrivateSideAsync(side)).Cut!;
                    Assert.Equal(expected.Observation.OwnElements.Select(e => e.ElementId),
                        privateCut.OwnElements.Select(e => e.ElementId));
                    Assert.Equal(expected.Observation.ApparentOpposingPresences
                            .Select(p => p.RepresentationId),
                        privateCut.Contacts.Select(c => c.RepresentationId));
                    Assert.Equal(expected.LegalActions!.Candidates.Select(a => a.ActionId),
                        privateCut.Actions.Select(a => a.CoreActionId));
                }
                var sets = Enum.GetValues<CampaignActionAudience>()
                    .Select(audience => CampaignExercises.Query(direct, audience).ActionSet!).ToArray();
                Assert.Equal(CampaignLegalActionSerializer.Serialize(sets[0]),
                    (await grain.QueryAsync(CampaignActionAudience.System)).LegalActions);
                var chosen = sets.First(value => value.Candidates.Count > 0);
                var candidate = chosen.Candidates.FirstOrDefault(value => value.Kind == "act-first")
                    ?? chosen.Candidates[0];
                var command = new CampaignActionSubmission(1, chosen.CampaignId,
                    chosen.StateVersion, chosen.PositionId, chosen.Audience, candidate.ActionId);
                var bytes = CampaignActionSubmissionSerializer.SerializeCanonical(command);
                if (chosen.Audience == CampaignActionAudience.System)
                    Assert.Equal(TemporaryCampaignStatus.Rejected,
                        (await grain.SubmitAsync("seat-1", $"impersonate-{index}",
                            CampaignActionAudience.System, bytes)).Status);
                var directStep = CampaignHostAdapter.Submit(direct, command);
                Assert.True(directStep.IsAccepted);
                var actor = chosen.Audience == CampaignActionAudience.System
                    ? "host-system" : "seat-1";
                var id = $"request-{index}";
                var priorHead = stored.AudienceTracker!.CommittedHeadHash;
                var priorAxisDiary = stored.AudienceTracker.Read(
                    CampaignActionAudience.Axis, stored.CommittedVersion, Generation)!;
                var priorCommonwealthDiary = stored.AudienceTracker.Read(
                    CampaignActionAudience.Commonwealth, stored.CommittedVersion, Generation)!;
                var replies = await Task.WhenAll(
                    grain.SubmitAsync(actor, id, chosen.Audience, bytes),
                    grain.SubmitAsync(actor, id, chosen.Audience, bytes));
                Assert.All(replies, reply => Assert.Equal(TemporaryCampaignStatus.Accepted, reply.Status));
                Assert.Equal(replies[0].Receipt, replies[1].Receipt);
                Assert.Equal(directStep.Evidence!.EventRecords.Single(), replies[0].Event);
                Assert.Equal(directStep.Evidence.SnapshotCheckpoint, replies[0].Snapshot);
                Assert.Equal(CampaignActionAcceptanceReceiptSerializer.Serialize(directStep.Evidence.Receipt),
                    replies[0].Receipt);
                var acceptedOutcome = stored.AudienceTracker.LastAcceptedOutcome;
                Assert.NotNull(acceptedOutcome);
                Assert.True(acceptedOutcome.HasTypedProof);
                var expectedCopyKey = candidate.Kind switch
                {
                    "act-first" or "act-last" => "diary.initiativeOrder",
                    "resolve-initiative" or "resolve-weather"
                        or "resolve-no-obligation-naval-convoy-schedule"
                        or "resolve-no-obligation-tactical-shipping"
                        or "resolve-no-obligation-organization" => null,
                    _ => throw new InvalidOperationException(
                        $"Unexpected fixture action in four-step diary proof: {candidate.Kind}"),
                };
                Assert.Equal(priorHead, acceptedOutcome.Axis!.PriorHeadHash);
                Assert.Equal(Hash(replies[0].Snapshot!), acceptedOutcome.Axis.SuccessorHeadHash);
                Assert.Equal(acceptedOutcome.Axis.SuccessorHeadHash,
                    acceptedOutcome.Commonwealth!.SuccessorHeadHash);
                Assert.Equal(acceptedOutcome.Axis.SuccessorHeadHash,
                    stored.AudienceTracker.CommittedHeadHash);
                Assert.Equal(stored.CommittedVersion, stored.AudienceTracker.StateVersion);
                foreach (var (audience, priorCut, decision) in new[]
                {
                    (CampaignActionAudience.Axis, priorAxisDiary, acceptedOutcome.Axis),
                    (CampaignActionAudience.Commonwealth, priorCommonwealthDiary,
                        acceptedOutcome.Commonwealth!),
                })
                {
                    var currentCut = stored.AudienceTracker.Read(
                        audience, stored.CommittedVersion, Generation)!;
                    var hasEntry = audience == chosen.Audience && expectedCopyKey is not null;
                    Assert.Equal(hasEntry ? expectedCopyKey : null, decision.ProposedCopyKey);
                    Assert.Equal(hasEntry ? "copy-catalog-unapproved" : "known-silent",
                        decision.ReasonCode);
                    Assert.Equal(priorCut.NextDiaryCursor + (hasEntry ? 1UL : 0UL),
                        currentCut.NextDiaryCursor);
                    Assert.Equal(priorCut.Diary.Length + (hasEntry ? 1 : 0),
                        currentCut.Diary.Length);
                    if (!hasEntry) continue;
                    sawApprovedDiaryEntry = true;
                    var copy = TemporaryPlayerViewProjection.LookupDiaryCopy(
                        currentCut.SourcePin,
                        TemporaryPlayerViewProjection.DiaryCatalogVersion, "en-CA",
                        decision.ProposedCopyKey!);
                    Assert.NotNull(copy);
                    var entry = currentCut.Diary[^1];
                    Assert.Equal(copy.Title, entry.Title);
                    Assert.Equal(copy.Body, entry.Body);
                    Assert.Equal(currentCut.NextDiaryCursor, entry.AudienceCursor);
                    Assert.True(Guid.TryParse(entry.EntryId, out _));
                    Assert.NotEqual(entry.EntryId,
                        stored.AudienceTracker.Read(audience, stored.CommittedVersion,
                            OtherGeneration)!.Diary[^1].EntryId);
                }
                Assert.Equal(replies[0].Receipt,
                    (await grain.FindRequestAsync(actor, id, chosen.Audience)).Receipt);
                Assert.Equal(TemporaryCampaignStatus.Conflict,
                    (await grain.SubmitAsync("different-seat", id, chosen.Audience, bytes)).Status);
                Assert.Equal(TemporaryCampaignStatus.Conflict,
                    (await grain.SubmitAsync(actor, id,
                        chosen.Audience == CampaignActionAudience.System
                            ? CampaignActionAudience.Axis : CampaignActionAudience.System,
                        bytes)).Status);
                Assert.Equal(TemporaryCampaignStatus.Conflict,
                    (await grain.SubmitAsync(actor, id, chosen.Audience,
                        CampaignActionSubmissionSerializer.SerializeCanonical(command with
                        { ActionId = "sha256:" + new string('0', 64) }))).Status);
                var beforeStale = await grain.QueryAsync(CampaignActionAudience.Axis);
                Assert.Equal(TemporaryCampaignStatus.Rejected,
                    (await grain.SubmitAsync(actor, $"fresh-stale-{index}", chosen.Audience,
                        bytes)).Status);
                var afterStale = await grain.QueryAsync(CampaignActionAudience.Axis);
                Assert.Equal(beforeStale.LegalActions, afterStale.LegalActions);
                Assert.Equal(beforeStale.View, afterStale.View);
                Assert.Same(acceptedOutcome, stored.AudienceTracker.LastAcceptedOutcome);
                Assert.Equal(Hash(replies[0].Snapshot!), stored.AudienceTracker.CommittedHeadHash);
                if (index is 0 or 3)
                {
                    var beforePrivate = (await grain.QueryPrivateSideAsync(
                        CampaignActionAudience.Axis)).Cut!;
                    var priorActivation = await grain.ActivationIdAsync();
                    await grain.DeactivateAsync();
                    Assert.NotEqual(priorActivation, await grain.ActivationIdAsync());
                    var afterPrivate = (await grain.QueryPrivateSideAsync(
                        CampaignActionAudience.Axis)).Cut!;
                    Assert.Equal(beforePrivate.StateVersion, afterPrivate.StateVersion);
                    Assert.Equal(beforePrivate.ActionSetToken, afterPrivate.ActionSetToken);
                    Assert.Equal(beforePrivate.Contacts.Select(c => c.ContactId),
                        afterPrivate.Contacts.Select(c => c.ContactId));
                    Assert.Equal(beforePrivate.NextDiaryCursor, afterPrivate.NextDiaryCursor);
                    Assert.Equal(beforePrivate.Diary, afterPrivate.Diary);
                    Assert.Equal(replies[0].Receipt,
                        (await grain.FindRequestAsync(actor, id, chosen.Audience)).Receipt);
                    Assert.Equal(beforeStale.LegalActions,
                        (await grain.QueryAsync(CampaignActionAudience.Axis)).LegalActions);
                }
                direct = directStep.SuccessorSession!;
            }
            Assert.True(sawApprovedDiaryEntry);
        }
        finally { await host.StopAsync(TestContext.Current.CancellationToken); }
    }

    [Fact]
    public async Task SystemProgressionStopsForHumanAndRestartLosesTemporaryCampaign()
    {
        var port = FreePort();
        using (var firstHost = BuildHost(port))
        {
            await firstHost.StartAsync(TestContext.Current.CancellationToken);
            try
            {
                var grain = GetTrustedGrain(firstHost);
                await grain.CreateAsync("operator-1", "create-1");
                Assert.Equal(TemporaryCampaignStatus.Rejected,
                    (await grain.AdvanceSystemAsync(0)).Status);
                Assert.Equal(TemporaryCampaignStatus.StepLimit,
                    (await grain.AdvanceSystemAsync(1)).Status);
                var afterSystem = await grain.QueryPrivateSideAsync(CampaignActionAudience.Axis);
                Assert.Equal(TemporaryCampaignStatus.Current, afterSystem.Status);
                Assert.Equal(2, afterSystem.Cut!.StateVersion);
                var progress = await grain.AdvanceSystemAsync(8);
                Assert.Equal(TemporaryCampaignStatus.NeedsHuman, progress.Status);
                Assert.Equal(2, progress.Steps);
                Assert.Equal(4, (await grain.QueryPrivateSideAsync(
                    CampaignActionAudience.Commonwealth)).Cut!.StateVersion);
                Assert.Equal(TemporaryCampaignStatus.Missing,
                    (await grain.FindRequestAsync("seat-1", "absent", CampaignActionAudience.Axis)).Status);
            }
            finally { await firstHost.StopAsync(TestContext.Current.CancellationToken); }
        }
        using var secondHost = BuildHost(port);
        await secondHost.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var grain = GetTrustedGrain(secondHost);
            Assert.Equal(TemporaryCampaignStatus.Missing,
                (await grain.QueryAsync(CampaignActionAudience.Axis)).Status);
            Assert.Equal(TemporaryCampaignStatus.Missing,
                (await grain.QueryPrivateSideAsync(CampaignActionAudience.Axis)).Status);
            Assert.Equal(TemporaryCampaignStatus.Missing,
                (await grain.FindRequestAsync("host-system", "system-1",
                    CampaignActionAudience.System)).Status);
        }
        finally { await secondHost.StopAsync(TestContext.Current.CancellationToken); }
    }

    [Fact]
    public async Task TwelveStepTraceStopsAtUnsupportedCombatWithoutGuessingTerminal()
    {
        using var host = BuildHost();
        await host.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var grain = GetTrustedGrain(host);
            await grain.CreateAsync("operator-1", "create-1");
            var direct = CampaignHostAdapter.Create(Request()).Session!;
            var expectedKinds = new[]
            {
                "resolve-initiative", "resolve-no-obligation-naval-convoy-schedule",
                "resolve-no-obligation-tactical-shipping", "act-first", "resolve-weather",
                "resolve-no-obligation-organization", "resolve-no-obligation-naval-convoy-arrival",
                "resolve-no-obligation-fleet-assignment", "resolve-no-obligation-fleet-repair",
                "complete-reserve-designation", "complete-movement-segment",
                "complete-breakdown-segment",
            };
            var sawTypedSubject = false;
            for (var index = 0; index < expectedKinds.Length; index++)
            {
                var set = Enum.GetValues<CampaignActionAudience>()
                    .Select(audience => CampaignExercises.Query(direct, audience).ActionSet!)
                    .First(value => value.Candidates.Count > 0);
                var candidate = set.Candidates.FirstOrDefault(value => value.Kind == "act-first")
                    ?? set.Candidates.FirstOrDefault(value => value.Kind == "complete-reserve-designation")
                    ?? set.Candidates.FirstOrDefault(value => value.Kind == "complete-movement-segment")
                    ?? set.Candidates[0];
                Assert.Equal(expectedKinds[index], candidate.Kind);
                var command = new CampaignActionSubmission(1, set.CampaignId,
                    set.StateVersion, set.PositionId, set.Audience, candidate.ActionId);
                var directStep = CampaignHostAdapter.Submit(direct, command);
                Assert.True(directStep.IsAccepted);
                var actor = set.Audience == CampaignActionAudience.System
                    ? "host-system" : "seat-1";
                var hosted = await grain.SubmitAsync(actor, $"trace-{index}", set.Audience,
                    CampaignActionSubmissionSerializer.SerializeCanonical(command));
                Assert.Equal(TemporaryCampaignStatus.Accepted, hosted.Status);
                Assert.Equal(directStep.Evidence!.EventRecords.Single(), hosted.Event);
                Assert.Equal(CampaignActionAcceptanceReceiptSerializer.Serialize(directStep.Evidence.Receipt),
                    hosted.Receipt);
                Assert.Equal(directStep.Evidence.SnapshotCheckpoint, hosted.Snapshot);
                direct = directStep.SuccessorSession!;
                foreach (var side in new[] { CampaignActionAudience.Axis, CampaignActionAudience.Commonwealth })
                {
                    var expected = CampaignHostAdapter.QueryAudience(direct, side);
                    var actual = await grain.QueryAsync(side);
                    Assert.Equal(CampaignLegalActionSerializer.Serialize(expected.LegalActions!),
                        actual.LegalActions);
                    Assert.Equal(expected.Observation!.StateVersion, actual.View!.StateVersion);
                    var expectedPrivate = CampaignHostAdapter.QueryPrivateSideCut(direct, side)!;
                    var actualPrivate = (await grain.QueryPrivateSideAsync(side)).Cut!;
                    Assert.Equal(expectedPrivate.Actions.Select(a =>
                            (a.CoreActionId, a.ElementId, a.TargetLocationId, a.OpportunityId)),
                        actualPrivate.Actions.Select(a =>
                            (a.CoreActionId, a.ElementId, a.TargetLocationId, a.OpportunityId)));
                    sawTypedSubject |= expectedPrivate.Actions.Any(a =>
                        a.Kind is "move-element" or "designate-reserve" && a.ElementId is not null);
                }
                Assert.Equal(CampaignLegalActionSerializer.Serialize(
                        CampaignHostAdapter.QuerySystem(direct).ActionSet!),
                    (await grain.QueryAsync(CampaignActionAudience.System)).LegalActions);
            }
            var checkpoint = CampaignExercises.QueryCheckpoint(direct);
            Assert.Equal("land.position.operation-1.first-player.movement-and-combat.combat.position-determination",
                checkpoint.PositionId);
            var stored = host.Services.GetRequiredService<TemporaryCampaignMemoryStore>()
                .Get("host-rsh-001");
            var snapshotBefore = CampaignExercises.Reconstruct(stored.Committed!).ReconstructedSnapshotBytes!;
            Assert.Equal("222b36760b250ab6ea8eb14a7913ffb56bab1b1786cb7a9d9b1f720360ca3c84",
                Convert.ToHexStringLower(SHA256.HashData(snapshotBefore)));
            var randomBefore = RandomBytes(snapshotBefore);
            foreach (var audience in Enum.GetValues<CampaignActionAudience>())
            {
                var legal = CampaignLegalActionSerializer.DeserializeCanonical(
                    (await grain.QueryAsync(audience)).LegalActions!);
                Assert.Empty(legal.Candidates);
            }
            var forgedCombat = new CampaignActionSubmission(1, checkpoint.CampaignId,
                checkpoint.StateVersion, checkpoint.PositionId, CampaignActionAudience.Axis,
                "sha256:" + new string('0', 64));
            Assert.False(CampaignHostAdapter.Submit(direct, forgedCombat).IsAccepted);
            Assert.Equal(TemporaryCampaignStatus.Rejected,
                (await grain.SubmitAsync("seat-1", "forged-combat", CampaignActionAudience.Axis,
                    CampaignActionSubmissionSerializer.SerializeCanonical(forgedCombat))).Status);
            Assert.Equal(TemporaryCampaignStatus.UnsupportedPhase,
                (await grain.AdvanceSystemAsync(4)).Status);
            var snapshotAfter = CampaignExercises.Reconstruct(stored.Committed!).ReconstructedSnapshotBytes!;
            Assert.Equal(randomBefore, RandomBytes(snapshotAfter));
            Assert.Equal(snapshotBefore,
                snapshotAfter);
            foreach (var audience in Enum.GetValues<CampaignActionAudience>())
                Assert.Empty(CampaignLegalActionSerializer.DeserializeCanonical(
                    (await grain.QueryAsync(audience)).LegalActions!).Candidates);
            Assert.Equal(13, CampaignExercises.QueryCheckpoint(direct).StateVersion);
            Assert.True(sawTypedSubject);
        }
        finally { await host.StopAsync(TestContext.Current.CancellationToken); }
    }

    private static IHost BuildHost(int? port = null)
    {
        var siloPort = port ?? FreePort();
        return new HostBuilder().UseOrleans(silo => silo
            .UseTemporaryCampaignDevelopmentSilo(siloPort)).Build();
    }

    private static TrustedGrain GetTrustedGrain(IHost host) => new(
        host.Services.GetRequiredService<IGrainFactory>()
            .GetGrain<ITemporaryCampaignGrain>("host-rsh-001"),
        host.Services.GetRequiredService<TemporaryCampaignAccess>().Capability);

    private sealed class TrustedGrain(ITemporaryCampaignGrain inner, byte[] capability)
    {
        internal ITemporaryCampaignGrain Inner => inner;
        internal byte[] Capability => capability.ToArray();
        internal Task<TemporaryCampaignReply> CreateAsync(string actor, string id) =>
            inner.CreateAsync(capability, actor, id);
        internal Task<TemporaryCampaignReply> QueryAsync(CampaignActionAudience audience) =>
            inner.QueryAsync(capability, audience);
        internal Task<TemporaryPrivateCutReply> QueryPrivateSideAsync(
            CampaignActionAudience audience) =>
            inner.QueryPrivateSideAsync(capability, audience, Generation);
        internal Task<TemporaryCampaignReply> SubmitAsync(string actor, string id,
            CampaignActionAudience audience, byte[] command) =>
            inner.SubmitAsync(capability, actor, id, audience, command);
        internal Task<TemporaryCampaignReply> FindRequestAsync(string actor, string id,
            CampaignActionAudience audience) =>
            inner.FindRequestAsync(capability, actor, id, audience);
        internal Task<TemporaryCampaignReply> AdvanceSystemAsync(int budget) =>
            inner.AdvanceSystemAsync(capability, budget);
        internal Task<string> ActivationIdAsync() => inner.ActivationIdForTestAsync(capability);
        internal Task DeactivateAsync() => inner.DeactivateForTestAsync(capability);
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static CampaignCreationRequest Request() => new(1, "host-rsh-001",
        Cna1979Ruleset.Manifest.Hash, 12345, "rules-lab.breakdown.truck.v1",
        "sha256:e6631e81ad8f97e39fd9d7eec93bad7fe2b39db4d2d3059ed94a02dd4093e7a3",
        "rules-lab.content.breakdown-truck.v1",
        "sha256:646e76e69ecceb82216b37d84e950928099acd8a3cb04b51526d0fe631e512ee",
        "breakdown-truck-lab");

    private static byte[] RandomBytes(byte[] snapshot)
    {
        using var document = JsonDocument.Parse(snapshot);
        return Encoding.UTF8.GetBytes(document.RootElement.GetProperty("randomState").GetRawText());
    }
}
