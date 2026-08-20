using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignCreationAdmissionTests
{
    [Fact]
    public void SharedCreationExecutionMatchesTheAuthorityFacadeAndRetainsExactEvidence()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var request = new CampaignCreationRequest(
            CampaignCreationRequest.CurrentContractVersion,
            "campaign-shared-creation",
            Cna1979Ruleset.Manifest.Hash,
            12345,
            setup.SetupId,
            setup.Hash,
            setup.Content.Pack.PackId,
            setup.Content.Pack.Hash,
            setup.Content.ScenarioId);

        var execution = CampaignCreationExecution.Execute(request);
        var facade = CampaignAuthority.Create(request);

        Assert.True(execution.IsCreated);
        Assert.Equal(CampaignCreationRejectionReason.None, execution.RejectionReason);
        Assert.NotNull(execution.CreatedEvent);
        Assert.NotNull(execution.Snapshot);
        Assert.NotNull(execution.Context);
        Assert.True(facade.IsCreated);
        Assert.Equal(
            CampaignSnapshotSerializer.Serialize(facade.Handle!.Snapshot),
            CampaignSnapshotSerializer.Serialize(execution.Snapshot!));
        Assert.Equal(
            CampaignSnapshotSerializer.Serialize(execution.Snapshot),
            CampaignSnapshotSerializer.Serialize(CampaignProjector.Apply(
                null,
                execution.CreatedEvent!,
                execution.Context!)));

        var repeated = CampaignCreationExecution.Execute(request);
        Assert.Equal(
            CampaignEventSerializer.Serialize(execution.CreatedEvent),
            CampaignEventSerializer.Serialize(repeated.CreatedEvent!));
        Assert.Equal(
            CampaignSnapshotSerializer.Serialize(execution.Snapshot),
            CampaignSnapshotSerializer.Serialize(repeated.Snapshot!));
    }

    [Fact]
    public void SharedCreationExecutionRejectsWithoutPartialAuthorityOrEvidence()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var request = new CampaignCreationRequest(
            99,
            "campaign-shared-creation",
            Cna1979Ruleset.Manifest.Hash,
            12345,
            setup.SetupId,
            setup.Hash,
            setup.Content.Pack.PackId,
            setup.Content.Pack.Hash,
            setup.Content.ScenarioId);

        var execution = CampaignCreationExecution.Execute(request);

        Assert.False(execution.IsCreated);
        Assert.Equal(CampaignCreationRejectionReason.InvalidRequest, execution.RejectionReason);
        Assert.Null(execution.CreatedEvent);
        Assert.Null(execution.Snapshot);
        Assert.Null(execution.Context);
    }

    [Fact]
    public void ExactSetupContentAndScenarioAdmissionCreatesACompleteWorld()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var resolver = new SyntheticResolver();

        var result = CampaignEngine.DecideCreation(
            null,
            Create(setup),
            resolver);

        Assert.True(result.IsAccepted);
        Assert.Equal(1, resolver.CallCount);
        var created = Assert.IsType<CampaignCreated>(Assert.Single(result.Events));
        Assert.Equal(4, created.ContractVersion);
        Assert.Equal(setup.Content, created.Setup.Content);
        Assert.Equal(4, created.InitialWorld.Elements.Count);
    }

    [Theory]
    [InlineData("unknown-setup", null, null, null, CampaignCommandRejectionReason.UnknownSetup)]
    [InlineData(null, "sha256:0000000000000000000000000000000000000000000000000000000000000000", null, null, CampaignCommandRejectionReason.SetupHashMismatch)]
    [InlineData(null, null, "unknown-pack", null, CampaignCommandRejectionReason.UnknownContent)]
    [InlineData(null, null, null, "sha256:0000000000000000000000000000000000000000000000000000000000000000", CampaignCommandRejectionReason.ContentHashMismatch)]
    public void AdmissionMapsExactIdentityFailuresWithoutEvents(
        string? setupId,
        string? setupHash,
        string? packId,
        string? contentHash,
        CampaignCommandRejectionReason expected)
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var baseline = Create(setup);
        var command = baseline with
        {
            SetupId = setupId ?? baseline.SetupId,
            SetupHash = setupHash ?? baseline.SetupHash,
            ContentPackId = packId ?? baseline.ContentPackId,
            ContentHash = contentHash ?? baseline.ContentHash,
        };

        var result = CampaignEngine.DecideCreation(null, command, new SyntheticResolver());

        Assert.False(result.IsAccepted);
        Assert.Equal(expected, result.RejectionReason);
        Assert.Empty(result.Events);
    }

    [Fact]
    public void KnownPackWithUnknownScenarioIsRejectedWithoutAnEvent()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];

        var result = CampaignEngine.DecideCreation(
            null,
            Create(setup) with { ScenarioId = "unknown-scenario" },
            new SyntheticResolver());

        Assert.Equal(CampaignCommandRejectionReason.UnknownScenario, result.RejectionReason);
        Assert.Empty(result.Events);
    }

    [Fact]
    public void KnownAlternateScenarioIsASetupContentMismatch()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];

        var result = CampaignEngine.DecideCreation(
            null,
            Create(setup) with { ScenarioId = "initiative-contested-lab" },
            new SyntheticResolver());

        Assert.Equal(CampaignCommandRejectionReason.SetupContentMismatch, result.RejectionReason);
        Assert.Empty(result.Events);
    }

    [Fact]
    public void WellFormedUnknownRulesHashIsUnsupportedButMalformedIdentityIsInvalidCommand()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];

        var unsupported = CampaignEngine.DecideCreation(
            null,
            Create(setup) with { RulesetHash = new string('0', 64) },
            new SyntheticResolver());
        var malformed = CampaignEngine.DecideCreation(
            null,
            Create(setup) with { ContentPackId = "Invalid Pack" },
            new SyntheticResolver());

        Assert.Equal(CampaignCommandRejectionReason.UnsupportedRuleset, unsupported.RejectionReason);
        Assert.Equal(CampaignCommandRejectionReason.InvalidCommand, malformed.RejectionReason);
        Assert.Empty(unsupported.Events);
        Assert.Empty(malformed.Events);
    }

    [Fact]
    public void ExistingCampaignPrecedenceValidatesPriorContentWithoutInspectingNewCommand()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var created = Assert.IsType<CampaignCreated>(Assert.Single(
            CampaignEngine.DecideCreation(null, Create(setup), new SyntheticResolver()).Events));
        var context = CampaignContentContext.Create(
            Cna1979SyntheticContentCatalog.Artifact,
            setup.Content.ScenarioId);
        var snapshot = CampaignProjector.Apply(null, created, context);
        var resolver = new SyntheticResolver();
        var malformed = Create(setup) with
        {
            CampaignId = " ",
            ContentPackId = "new-command-must-not-resolve",
            ContentHash = "bad",
        };

        var result = CampaignEngine.DecideCreation(snapshot, malformed, resolver);

        Assert.Equal(CampaignCommandRejectionReason.CampaignAlreadyCreated, result.RejectionReason);
        Assert.Equal(
            [(setup.Content.Pack.PackId, setup.Content.Pack.Hash)],
            resolver.Calls);
        Assert.Empty(result.Events);
    }

    [Fact]
    public void ExistingCampaignWithForgedWorldIsInvalidState()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var created = Assert.IsType<CampaignCreated>(Assert.Single(
            CampaignEngine.DecideCreation(null, Create(setup), new SyntheticResolver()).Events));
        var context = CampaignContentContext.Create(
            Cna1979SyntheticContentCatalog.Artifact,
            setup.Content.ScenarioId);
        var snapshot = CampaignProjector.Apply(null, created, context);
        var forged = snapshot with
        {
            World = new CampaignWorldSnapshot(
                1,
                snapshot.World.Elements
                    .Where(element => element.ElementId != "axis-element-a")
                    .Append(new CampaignElementState("axis-element-a", "east"))
                    .ToArray()),
        };

        var result = CampaignEngine.DecideCreation(
            forged,
            Create(setup),
            new SyntheticResolver());

        Assert.Equal(CampaignCommandRejectionReason.InvalidState, result.RejectionReason);
        Assert.Empty(result.Events);
    }

    [Fact]
    public void CheckpointRejectsAWellFormedUnsupportedRulesetHashWithoutAnEvent()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var created = Assert.IsType<CampaignCreated>(Assert.Single(
            CampaignEngine.DecideCreation(null, Create(setup), new SyntheticResolver()).Events));
        var context = CampaignContentContext.Create(
            Cna1979SyntheticContentCatalog.Artifact,
            setup.Content.ScenarioId);
        var snapshot = CampaignProjector.Apply(null, created, context);
        var forged = snapshot with { RulesetHash = new string('0', 64) };

        var result = CampaignEngine.Decide(
            forged,
            new ResolveInitiative(
                forged.StateVersion,
                forged.SequencePosition.PositionId),
            context);

        Assert.Equal(CampaignCommandRejectionReason.InvalidState, result.RejectionReason);
        Assert.Empty(result.Events);
    }

    [Fact]
    public void ControlledAdmissionRejectsAScenarioStartMismatchWithoutAnEvent()
    {
        var canonical = Cna1979SetupCatalog.Definitions[0];
        var mismatched = new CampaignSetupDefinition(
            Cna1979SetupCatalog.SchemaVersion,
            "rules-lab.start-mismatch",
            "Controlled start-mismatch fixture",
            true,
            canonical.InitialGameTurn + 1,
            canonical.InitialInitiative,
            canonical.OpeningPreamble,
            canonical.Weather,
            canonical.Content,
            canonical.Sources);

        var result = CampaignEngine.DecideCreation(
            null,
            Create(mismatched),
            new SyntheticResolver(),
            [mismatched]);

        Assert.Equal(CampaignCommandRejectionReason.ScenarioStartMismatch, result.RejectionReason);
        Assert.Empty(result.Events);
    }

    [Fact]
    public void ControlledAdmissionRejectsAnAlteredWeatherPolicyBeforeContentResolution()
    {
        var canonical = Cna1979SetupCatalog.Definitions[0];
        var altered = new CampaignSetupDefinition(
            canonical.SchemaVersion,
            canonical.SetupId,
            canonical.DisplayName,
            canonical.IsSynthetic,
            canonical.InitialGameTurn,
            canonical.InitialInitiative,
            canonical.OpeningPreamble,
            new CampaignWeatherPolicy(
                CampaignWeatherPolicy.CurrentContractVersion,
                CampaignWeatherPolicyKind.NoImmediateWeatherEffectSubjects,
                [new RuleReference("sandtable-rules-lab", "weather.wrong.v1")]),
            canonical.Content,
            canonical.Sources);
        var resolver = new SyntheticResolver();

        var result = CampaignEngine.DecideCreation(
            null,
            Create(altered),
            resolver,
            [altered]);

        Assert.Equal(
            CampaignCommandRejectionReason.UnsupportedWeatherPolicy,
            result.RejectionReason);
        Assert.Empty(result.Events);
        Assert.Equal(0, resolver.CallCount);
    }

    private static CreateCampaign Create(CampaignSetupDefinition setup) => new(
        "campaign-1",
        Cna1979Ruleset.Manifest.Hash,
        12345,
        setup.SetupId,
        setup.Hash,
        setup.Content.Pack.PackId,
        setup.Content.Pack.Hash,
        setup.Content.ScenarioId);

    private sealed class SyntheticResolver : IContentPackResolver
    {
        public List<(string PackId, string Hash)> Calls { get; } = [];

        public int CallCount => Calls.Count;

        public ContentCatalogResolution Resolve(string packId, string expectedHash)
        {
            Calls.Add((packId, expectedHash));
            return Cna1979SyntheticContentCatalog.Resolve(packId, expectedHash);
        }
    }
}
