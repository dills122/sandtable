using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.OrleansHost.Api;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Tests;

public sealed class CampaignDurableAuthorityJournalTests
{
    private const string OperatorSecret = "bb2ba979667134402906109cd92a6cf68427d84b58c30db5bcd0fae8bef692b1";

    [Fact]
    public void ContactCombatOpeningCommitsOnceAndColdReplaysSelectedAuthority()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = Request();
        var session = AdvanceToContactCombat(journal, request);
        Assert.NotEmpty(CampaignHostAdapter.QueryContactCombatOwnerDraft(session,
            CampaignActionAudience.Axis, "axis-infantry").PrivateCombatActionIds);
        var credential = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Axis, OperatorSecret))!;
        var opening = journal.QueryCombatOwnerOpening(credential, "axis-infantry");
        Assert.Equal(2, opening.PublicActionRefs.Count);
        var visible = System.Text.Encoding.UTF8.GetString(opening.VisibleBytes);
        Assert.DoesNotContain("boundRepresentationId", visible, StringComparison.Ordinal);
        var privateAction = CampaignHostAdapter.QueryContactCombatOwnerDraft(
            session, CampaignActionAudience.Axis, "axis-infantry").PrivateCombatActionIds[0];
        Assert.DoesNotContain(privateAction, visible, StringComparison.Ordinal);
        var actionRef = opening.PublicActionRefs[0];
        Assert.False(journal.SubmitOwnerCombatSelection(credential, "axis-infantry",
            opening.Revision, "act.forged").IsAccepted);
        var acceptedCombat = journal.SubmitOwnerCombatSelection(credential, "axis-infantry",
            opening.Revision, actionRef);
        Assert.True(acceptedCombat.IsAccepted);
        var reopened = fixture.Open();
        var retry = reopened.SubmitOwnerCombatSelection(credential, "axis-infantry",
            opening.Revision, actionRef);
        Assert.True(retry.IsAccepted);
        Assert.Equal(acceptedCombat.CanonicalReceipt, retry.CanonicalReceipt);
        var verified = reopened.ReadVerified(request.CampaignId);
        Assert.Equal(CampaignExercises.QueryCheckpoint(session).StateVersion + 2,
            CampaignExercises.QueryCheckpoint(verified).StateVersion);
        Assert.True(CampaignExercises.Reconstruct(verified).IsVerified);
        Assert.Equal(14, CampaignExercises.QueryCheckpoint(verified).ContractVersion);
        var defenderId = CampaignHostAdapter.QueryContactRbaDefenderId(verified,
            CampaignActionAudience.Commonwealth);
        var defenderCredential = reopened.RedeemSeatGrant(reopened.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Commonwealth, OperatorSecret))!;
        var defenderOpening = reopened.QueryRbaOwnerOpening(defenderCredential,
            defenderId);
        Assert.Contains("decline-retreat-before-assault",
            System.Text.Encoding.UTF8.GetString(defenderOpening.VisibleBytes),
            StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = reopened.QueryRbaOwnerOpening(credential, defenderId);
        });
        var declineRef = defenderOpening.PublicActionRefs[^1];
        var acceptedDecline = reopened.SubmitOwnerRbaDecline(defenderCredential,
            defenderId, defenderOpening.Revision, declineRef);
        Assert.True(acceptedDecline.IsAccepted);
        var coldDecline = fixture.Open();
        var declineRetry = coldDecline.SubmitOwnerRbaDecline(defenderCredential,
            defenderId, defenderOpening.Revision, declineRef);
        Assert.True(declineRetry.IsAccepted);
        Assert.Equal(acceptedDecline.CanonicalReceipt, declineRetry.CanonicalReceipt);
        var afterDecline = coldDecline.ReadVerified(request.CampaignId);
        Assert.Equal(15, CampaignExercises.QueryCheckpoint(afterDecline).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(afterDecline).IsVerified);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT COUNT(*) FROM accepted_steps "
            + "WHERE campaign_id=$id AND action_kind='native-combat-barrage'";
        query.Parameters.AddWithValue("$id", request.CampaignId);
        Assert.Equal(1L, query.ExecuteScalar());
    }

    [Fact]
    public void DefenderRetreatMoveAndExplicitStopColdReplayThroughOwnerSeat()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = Request();
        AdvanceToContactCombat(journal, request);
        var attacker = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Axis, OperatorSecret))!;
        var combatOpening = journal.QueryCombatOwnerOpening(attacker, "axis-infantry");
        Assert.True(journal.SubmitOwnerCombatSelection(attacker, "axis-infantry",
            combatOpening.Revision, combatOpening.PublicActionRefs[0]).IsAccepted);
        var defenderId = CampaignHostAdapter.QueryContactRbaDefenderId(
            journal.ReadVerified(request.CampaignId), CampaignActionAudience.Commonwealth);
        var defender = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Commonwealth, OperatorSecret))!;
        var opening = journal.QueryRbaOwnerOpening(defender, defenderId);
        Assert.True(opening.PublicActionRefs.Count > 1);
        var startingSession = journal.ReadVerified(request.CampaignId);
        var draft = CampaignHostAdapter.QueryContactRbaOwnerDraft(startingSession,
            CampaignActionAudience.Commonwealth, defenderId);
        var moveOrdinal = Enumerable.Range(0, draft.PrivateActionIds.Count - 1).First(index =>
        {
            var candidate = CampaignHostAdapter.SubmitContactDefenderRbaMove(startingSession,
                new CampaignActionSubmission(1, request.CampaignId,
                    CampaignExercises.QueryCheckpoint(startingSession).StateVersion,
                    CampaignExercises.QueryCheckpoint(startingSession).PositionId,
                    CampaignActionAudience.Commonwealth, draft.PrivateActionIds[index]),
                defenderId);
            return candidate.IsAccepted
                && CampaignHostAdapter.QueryContactRbaOwnerDraft(candidate.SuccessorSession!,
                    CampaignActionAudience.Commonwealth, defenderId).PrivateActionIds.Count > 1;
        });
        var moveRef = opening.PublicActionRefs[moveOrdinal];
        Assert.False(journal.SubmitOwnerRbaDecline(defender, defenderId,
            opening.Revision, moveRef).IsAccepted);
        var moved = journal.SubmitOwnerRbaMove(defender, defenderId,
            opening.Revision, moveRef);
        Assert.True(moved.IsAccepted);
        var cold = fixture.Open();
        Assert.Equal(moved.CanonicalReceipt,
            cold.SubmitOwnerRbaMove(defender, defenderId,
                opening.Revision, moveRef).CanonicalReceipt);
        var afterMove = cold.ReadVerified(request.CampaignId);
        Assert.Equal(16, CampaignExercises.QueryCheckpoint(afterMove).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(afterMove).IsVerified);
        var next = cold.QueryRbaOwnerOpening(defender, defenderId);
        Assert.True(next.Revision > opening.Revision);
        Assert.True(next.PublicActionRefs.Count > 1);
        var repeated = cold.SubmitOwnerRbaMove(defender, defenderId,
            next.Revision, next.PublicActionRefs[0]);
        Assert.True(repeated.IsAccepted);
        var afterRepeat = fixture.Open().ReadVerified(request.CampaignId);
        Assert.Equal(16, CampaignExercises.QueryCheckpoint(afterRepeat).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(afterRepeat).IsVerified);
        var finalOpening = cold.QueryRbaOwnerOpening(defender, defenderId);
        Assert.True(finalOpening.Revision > next.Revision);
        var stopped = cold.SubmitOwnerRbaDecline(defender, defenderId,
            finalOpening.Revision, finalOpening.PublicActionRefs[^1]);
        Assert.True(stopped.IsAccepted);
        var final = fixture.Open().ReadVerified(request.CampaignId);
        Assert.Equal(15, CampaignExercises.QueryCheckpoint(final).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(final).IsVerified);
    }

    [Fact]
    public void VoluntaryCombatFinishHasNoPrivateTargetAndRetainsReceiptAcrossRestart()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = Request();
        AdvanceToContactCombat(journal, request);
        var credential = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Axis, OperatorSecret))!;
        var opening = journal.QueryCombatOwnerOpening(credential, "axis-infantry");
        Assert.Equal(2, opening.PublicActionRefs.Count);
        Assert.Contains("finish-combat-without-attack",
            System.Text.Encoding.UTF8.GetString(opening.VisibleBytes), StringComparison.Ordinal);

        var finishRef = opening.PublicActionRefs[1];
        var accepted = journal.SubmitOwnerCombatSelection(credential, "axis-infantry",
            opening.Revision, finishRef);
        Assert.True(accepted.IsAccepted);
        var reopened = fixture.Open();
        var retry = reopened.SubmitOwnerCombatSelection(credential, "axis-infantry",
            opening.Revision, finishRef);
        Assert.True(retry.IsAccepted);
        Assert.Equal(accepted.CanonicalReceipt, retry.CanonicalReceipt);
        var verified = reopened.ReadVerified(request.CampaignId);
        var reconstruction = CampaignExercises.Reconstruct(verified);
        Assert.True(reconstruction.IsVerified);
        using var snapshot = System.Text.Json.JsonDocument.Parse(
            reconstruction.ReconstructedSnapshotBytes!);
        Assert.Equal(13, snapshot.RootElement.GetProperty("contractVersion").GetInt32());
        Assert.Equal(CampaignExercises.QueryCheckpoint(verified).PositionId,
            snapshot.RootElement.GetProperty("positionId").GetString());
        var decision = snapshot.RootElement.GetProperty("priorDecisionState")
            .GetProperty("decision");
        Assert.Equal("finish-combat-without-attack",
            decision.GetProperty("choice").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null,
            decision.GetProperty("boundRepresentationId").ValueKind);
        Assert.Equal(5, snapshot.RootElement.GetProperty("completion")
            .GetProperty("steps").GetArrayLength());
    }

    [Fact]
    public void SeatCredentialSurvivesRestartAndRotationRevokesIt()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        journal.CreateCampaign(Request());
        Assert.Throws<UnauthorizedAccessException>(() => journal.IssueSeatGrant(
            "campaign-durable-journal", CampaignActionAudience.Axis,
            new string('0', 64)));
        var grant = journal.IssueSeatGrant("campaign-durable-journal",
            CampaignActionAudience.Axis, OperatorSecret);
        Assert.DoesNotContain(grant.Secret, grant.ToString());
        var credential = fixture.Open().RedeemSeatGrant(grant)!;
        Assert.NotNull(credential);
        Assert.DoesNotContain(credential.Secret, credential.ToString());
        Assert.Null(fixture.Open().RedeemSeatGrant(grant));
        Assert.Equal(grant.Epoch, fixture.Open().AuthenticateSeat(credential)!.Epoch);
        Assert.Null(fixture.Open().AuthenticateSeat(credential with { Secret = new string('0', 64) }));

        var replacement = fixture.Open().IssueSeatGrant("campaign-durable-journal",
            CampaignActionAudience.Axis, OperatorSecret);
        Assert.Equal(grant.Epoch + 1, replacement.Epoch);
        Assert.Null(fixture.Open().AuthenticateSeat(credential));
        Assert.NotNull(fixture.Open().AuthenticateSeat(fixture.Open().RedeemSeatGrant(replacement)!));
    }

    [Fact]
    public void GrantAndCredentialExpireAtTheirStoredDeadlines()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        journal.CreateCampaign(Request());
        var expiredGrant = journal.IssueSeatGrant("campaign-durable-journal",
            CampaignActionAudience.Axis, OperatorSecret);
        clock.Advance(TimeSpan.FromMinutes(10));
        Assert.Null(fixture.Open(clock).RedeemSeatGrant(expiredGrant));

        var grant = journal.IssueSeatGrant("campaign-durable-journal",
            CampaignActionAudience.Axis, OperatorSecret);
        var credential = fixture.Open(clock).RedeemSeatGrant(grant)!;
        Assert.NotNull(fixture.Open(clock).AuthenticateSeat(credential));
        clock.Advance(TimeSpan.FromHours(2));
        Assert.Null(fixture.Open(clock).AuthenticateSeat(credential));
    }

    [Fact]
    public void OwnerOpeningRemainsByteIdenticalAcrossUnseenAuthorityAdvanceAndRestart()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var session = journal.CreateCampaign(Request());
        var grant = journal.IssueSeatGrant("campaign-durable-journal",
            CampaignActionAudience.Axis, OperatorSecret);
        var credential = journal.RedeemSeatGrant(grant)!;
        var elementId = CampaignHostAdapter.QueryPrivateSideCut(session,
            CampaignActionAudience.Axis)!.OwnElements[0].ElementId;
        var first = journal.QueryOwnerOpening(credential, elementId);
        var reopened = fixture.Open().QueryOwnerOpening(credential, elementId);
        Assert.Equal(first.VisibleBytes, reopened.VisibleBytes);
        Assert.Equal(first.PublicActionRefs, reopened.PublicActionRefs);
        Assert.Equal(first.Revision, reopened.Revision);

        var system = CampaignExercises.Query(session, CampaignActionAudience.System).ActionSet!;
        var advance = new CampaignActionSubmission(1, system.CampaignId,
            system.StateVersion, system.PositionId, system.Audience,
            system.Candidates.Single().ActionId);
        Assert.True(journal.Append(advance.CampaignId, advance).IsAccepted);
        var after = fixture.Open().QueryOwnerOpening(credential, elementId);
        Assert.Equal(first.VisibleBytes, after.VisibleBytes);
        Assert.Equal(first.PublicActionRefs, after.PublicActionRefs);
        Assert.Equal(first.Revision, after.Revision);

        var replacementGrant = journal.IssueSeatGrant("campaign-durable-journal",
            CampaignActionAudience.Axis, OperatorSecret);
        var replacement = journal.RedeemSeatGrant(replacementGrant)!;
        Assert.Throws<UnauthorizedAccessException>(() => journal.QueryOwnerOpening(
            credential, elementId));
        Assert.Equal(first.Revision + 1,
            journal.QueryOwnerOpening(replacement, elementId).Revision);
    }

    [Fact]
    public void ContactMovementReferenceCommitsOnceAndReplaysAfterRestart()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = new CampaignCreationRequest(1, "campaign-contact-owner-host",
            Cna1979Ruleset.Manifest.Hash, 12345,
            "rules-lab.breakdown.reaction.v1",
            "sha256:ed4e33358ede11f3c25661798add9088f11d8853053dbf7c8992ba5f92d9a36e",
            "rules-lab.content.breakdown-reaction.v1",
            "sha256:1e10648bb65f7ec6d4ad3f33bbf612f51c11a927ebc2e8b110aa5544af3918dd",
            "breakdown-reaction-lab");
        var session = journal.CreateCampaign(request);
        for (var index = 0; index < 20; index++)
        {
            var set = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet!)
                .First(value => value.Candidates.Count > 0);
            if (set.Candidates.OfType<MoveElementAction>().Any()) break;
            var next = set.Candidates.FirstOrDefault(value => value is ActFirstAction)
                ?? set.Candidates.FirstOrDefault(value => value is CompleteReserveDesignationAction)
                ?? set.Candidates[0];
            var command = new CampaignActionSubmission(1, set.CampaignId, set.StateVersion,
                set.PositionId, set.Audience, next.ActionId);
            var accepted = journal.Append(request.CampaignId, command);
            Assert.True(accepted.IsAccepted);
            session = accepted.SuccessorSession!;
        }
        var grant = journal.IssueSeatGrant(request.CampaignId,
            CampaignActionAudience.Axis, OperatorSecret);
        var credential = journal.RedeemSeatGrant(grant)!;
        var opening = journal.QueryOwnerOpening(credential, "axis-infantry");
        Assert.NotEmpty(opening.PublicActionRefs);
        var reference = opening.PublicActionRefs[0];
        Assert.False(journal.SubmitOwnerMovement(credential, "axis-infantry",
            opening.Revision, "act.forged").IsAccepted);
        Assert.False(journal.SubmitOwnerMovement(credential, "axis-infantry",
            opening.Revision, "act." + new string('a', 1_000_000)).IsAccepted);

        var acceptedOwner = journal.SubmitOwnerMovement(credential, "axis-infantry",
            opening.Revision, reference);
        Assert.True(acceptedOwner.IsAccepted);
        var reopened = fixture.Open();
        var retry = reopened.SubmitOwnerMovement(credential, "axis-infantry",
            opening.Revision, reference);
        Assert.True(retry.IsAccepted);
        Assert.Equal(acceptedOwner.CanonicalReceipt, retry.CanonicalReceipt);
        Assert.False(reopened.SubmitOwnerMovement(credential, "axis-infantry",
            opening.Revision + 1, reference).IsAccepted);
        Assert.Equal(CampaignExercises.QueryCheckpoint(session).StateVersion + 1,
            CampaignExercises.QueryCheckpoint(reopened.ReadVerified(request.CampaignId)).StateVersion);

        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            using var update = connection.CreateCommand();
            update.CommandText = """
                UPDATE accepted_steps SET owner_revision=owner_revision+1
                WHERE owner_public_ref IS NOT NULL
                """;
            Assert.Equal(1, update.ExecuteNonQuery());
        }
        Assert.Throws<InvalidOperationException>(() => fixture.Open().ReadVerified(request.CampaignId));
    }

    [Fact]
    public void RetainedMovementReferenceUsesCurrentVisibleCutWhenPrivateFenceIsOlder()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = ContactRequest();
        AdvanceToContactMovement(journal, request);
        var grant = journal.IssueSeatGrant(request.CampaignId,
            CampaignActionAudience.Axis, OperatorSecret);
        var credential = journal.RedeemSeatGrant(grant)!;
        var opening = journal.QueryOwnerOpening(credential, "axis-infantry");
        Assert.NotEmpty(opening.PublicActionRefs);

        // Retain the same opening/action map with an earlier private authority fence. This
        // isolates the host's equal-visible submission rule from the small fixture's sight
        // changes when another formation moves.
        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            using var update = connection.CreateCommand();
            update.CommandText = "UPDATE owner_openings SET authority_version=authority_version-1 "
                + "WHERE campaign_id=$id AND acting_element_id='axis-infantry'";
            update.Parameters.AddWithValue("$id", request.CampaignId);
            Assert.Equal(1, update.ExecuteNonQuery());
        }
        Assert.True(fixture.Open().SubmitOwnerMovement(credential, "axis-infantry",
            opening.Revision, opening.PublicActionRefs[0]).IsAccepted);
    }

    [Fact]
    public void ContactEvidenceIsRetainedAndTamperingBlocksColdRead()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = ContactRequest();
        var session = AdvanceToContactMovement(journal, request);
        var grant = journal.IssueSeatGrant(request.CampaignId,
            CampaignActionAudience.Axis, OperatorSecret);
        var credential = journal.RedeemSeatGrant(grant)!;
        var opening = journal.QueryOwnerOpening(credential, "axis-infantry");
        Assert.NotEmpty(opening.PublicActionRefs);
        Assert.True(journal.SubmitOwnerMovement(credential, "axis-infantry",
            opening.Revision, opening.PublicActionRefs[0]).IsAccepted);
        Assert.NotNull(fixture.Open().ReadVerified(request.CampaignId));

        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT initial_contacts FROM campaigns WHERE campaign_id=$id";
        query.Parameters.AddWithValue("$id", request.CampaignId);
        Assert.True(((byte[])query.ExecuteScalar()!).Length >= sizeof(int));
        query.CommandText = "SELECT canonical_contacts FROM accepted_steps "
            + "WHERE campaign_id=$id AND action_kind='contact-movement'";
        Assert.True(((byte[])query.ExecuteScalar()!).Length >= sizeof(int));
        using var tamper = connection.CreateCommand();
        tamper.CommandText = "UPDATE accepted_steps SET canonical_contacts=x'01000000' "
            + "WHERE campaign_id=$id AND action_kind='contact-movement'";
        tamper.Parameters.AddWithValue("$id", request.CampaignId);
        Assert.Equal(1, tamper.ExecuteNonQuery());
        Assert.Throws<InvalidOperationException>(() => fixture.Open().ReadVerified(request.CampaignId));
    }

    [Fact]
    public void ExistingSharedDirectoryIsRejectedWithoutChangingItsPermissions()
    {
        if (OperatingSystem.IsWindows()) return;
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "sandtable-authority-shared-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var sharedMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
            | UnixFileMode.GroupRead | UnixFileMode.GroupExecute;
        File.SetUnixFileMode(directory, sharedMode);
        try
        {
            Assert.Throws<InvalidOperationException>(() =>
                CampaignDurableAuthorityJournal.CreateNew(System.IO.Path.Combine(directory,
                    "campaign.sqlite"), OperatorSecret));
            Assert.Equal(sharedMode, File.GetUnixFileMode(directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void AcceptedCommandSurvivesColdReadAndStaleRetryDoesNotAdvanceHead()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var session = journal.CreateCampaign(Request());
        var actionSet = CampaignExercises.Query(session, CampaignActionAudience.System).ActionSet!;
        var command = new CampaignActionSubmission(1, CampaignExercises.QueryCheckpoint(session).CampaignId,
            actionSet.StateVersion, actionSet.PositionId, CampaignActionAudience.System,
            actionSet.Candidates.Single().ActionId);

        var accepted = journal.Append(command.CampaignId, command);
        Assert.True(accepted.IsAccepted);
        var reopened = fixture.Open();
        Assert.Equal(CampaignExercises.QueryCheckpoint(accepted.SuccessorSession!).StateVersion,
            CampaignExercises.QueryCheckpoint(reopened.ReadVerified(command.CampaignId)).StateVersion);

        var stale = reopened.Append(command.CampaignId, command);
        Assert.False(stale.IsAccepted);
        Assert.Equal(CampaignExercises.QueryCheckpoint(accepted.SuccessorSession!).StateVersion,
            CampaignExercises.QueryCheckpoint(fixture.Open().ReadVerified(command.CampaignId)).StateVersion);
    }

    [Fact]
    public void AlteredAcceptedEventFailsColdRead()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var session = journal.CreateCampaign(Request());
        var actionSet = CampaignExercises.Query(session, CampaignActionAudience.System).ActionSet!;
        var command = new CampaignActionSubmission(1, CampaignExercises.QueryCheckpoint(session).CampaignId,
            actionSet.StateVersion, actionSet.PositionId, CampaignActionAudience.System,
            actionSet.Candidates.Single().ActionId);
        Assert.True(journal.Append(command.CampaignId, command).IsAccepted);
        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            using var update = connection.CreateCommand();
            update.CommandText = "UPDATE accepted_steps SET canonical_event=x'00'";
            Assert.Equal(1, update.ExecuteNonQuery());
        }
        Assert.Throws<InvalidOperationException>(() => fixture.Open().ReadVerified(command.CampaignId));
    }

    private static CampaignCreationRequest Request()
    {
        return new CampaignCreationRequest(1, "campaign-durable-journal",
            Cna1979Ruleset.Manifest.Hash, 12345,
            "rules-lab.breakdown.truck.v1", TemporaryCampaignGrain.SetupHash,
            "rules-lab.content.breakdown-truck.v1", TemporaryCampaignGrain.ContentHash,
            TemporaryCampaignGrain.ScenarioId);
    }

    private static CampaignCreationRequest ContactRequest() => new(1,
        "campaign-contact-owner-host", Cna1979Ruleset.Manifest.Hash, 12345,
        "rules-lab.breakdown.reaction.v1",
        "sha256:ed4e33358ede11f3c25661798add9088f11d8853053dbf7c8992ba5f92d9a36e",
        "rules-lab.content.breakdown-reaction.v1",
        "sha256:1e10648bb65f7ec6d4ad3f33bbf612f51c11a927ebc2e8b110aa5544af3918dd",
        "breakdown-reaction-lab");

    private static ExerciseSession AdvanceToContactCombat(
        CampaignDurableAuthorityJournal journal, CampaignCreationRequest request)
    {
        var session = journal.CreateCampaign(request);
        for (var index = 0; index < 80; index++)
        {
            try
            {
                _ = CampaignHostAdapter.QueryContactCombatOwnerDraft(session,
                    CampaignActionAudience.Axis, "axis-infantry");
                return session;
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                // Advance through admitted pre-Combat positions.
            }
            var set = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .FirstOrDefault(value => value?.Candidates.Count > 0);
            Assert.NotNull(set);
            var next = set.Candidates.OfType<MoveElementAction>().FirstOrDefault(value =>
                value.ElementId == "axis-infantry" && value.DestinationLocationId == "north")
                ?? set.Candidates[0];
            var accepted = journal.Append(request.CampaignId, new CampaignActionSubmission(1,
                set.CampaignId, set.StateVersion, set.PositionId, set.Audience, next.ActionId));
            Assert.True(accepted.IsAccepted);
            session = accepted.SuccessorSession!;
        }
        throw new InvalidOperationException("Combat position was not reached.");
    }

    private static ExerciseSession AdvanceToContactMovement(
        CampaignDurableAuthorityJournal journal, CampaignCreationRequest request)
    {
        var session = journal.CreateCampaign(request);
        for (var index = 0; index < 20; index++)
        {
            var set = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet!)
                .First(value => value.Candidates.Count > 0);
            if (set.Candidates.OfType<MoveElementAction>().Any()) return session;
            var next = set.Candidates.FirstOrDefault(value => value is ActFirstAction)
                ?? set.Candidates.FirstOrDefault(value => value is CompleteReserveDesignationAction)
                ?? set.Candidates[0];
            var accepted = journal.Append(request.CampaignId, new CampaignActionSubmission(1,
                set.CampaignId, set.StateVersion, set.PositionId, set.Audience, next.ActionId));
            Assert.True(accepted.IsAccepted);
            session = accepted.SuccessorSession!;
        }
        throw new InvalidOperationException("No owner movement cut was reached.");
    }

    private sealed class JournalFixture : IDisposable
    {
        private readonly string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "sandtable-authority-journal-" + Guid.NewGuid().ToString("N"));
        internal string Path => System.IO.Path.Combine(directory, "campaign.sqlite");
        internal CampaignDurableAuthorityJournal Create(TimeProvider? clock = null) =>
            CampaignDurableAuthorityJournal.CreateNew(Path, OperatorSecret, clock);
        internal CampaignDurableAuthorityJournal Open(TimeProvider? clock = null) =>
            CampaignDurableAuthorityJournal.OpenExisting(Path, clock);
        public void Dispose() => Directory.Delete(directory, recursive: true);
    }

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.Parse("2026-01-01T00:00:00Z",
            global::System.Globalization.CultureInfo.InvariantCulture);
        public override DateTimeOffset GetUtcNow() => now;
        internal void Advance(TimeSpan duration) => now = now.Add(duration);
    }
}
