using System.Reflection;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Tests.Campaigns;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Observations;

[Trait("Boundary", "UserSpace")]
public sealed class UserSpaceDisclosureManifestV2Tests
{
    [Fact]
    public void ManifestRegistersEveryReachableCurrentTypeFromDeclaredRoots()
    {
        using var manifest = ReadManifest();
        var assembly = typeof(CampaignObservationV7).Assembly;
        var roots = manifest.RootElement.GetProperty("dotnetRoots")
            .EnumerateArray()
            .Select(value => assembly.GetType(value.GetString()!, throwOnError: true)!)
            .ToArray();
        var registered = manifest.RootElement.GetProperty("dotnetTypes")
            .EnumerateArray()
            .Select(entry => entry.GetProperty("name").GetString()!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var reachable = CollectReachableCoreTypes(roots, assembly)
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var missing = reachable.Except(registered, StringComparer.Ordinal).ToArray();
        var extra = registered.Except(reachable, StringComparer.Ordinal).ToArray();
        var suggestedEntries = missing.Select(typeName =>
        {
            var type = assembly.GetType(typeName, throwOnError: true)!;
            return new
            {
                name = typeName,
                declaredMembers = type.GetProperties(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Select(property => property.Name)
                    .Order(StringComparer.Ordinal)
                    .ToArray(),
                enumValues = type.IsEnum
                    ? Enum.GetNames(type).Order(StringComparer.Ordinal).ToArray()
                    : null,
            };
        });
        Assert.True(
            missing.Length == 0 && extra.Length == 0,
            $"Manifest type closure mismatch.{Environment.NewLine}Missing:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}{Environment.NewLine}Extra:{Environment.NewLine}{string.Join(Environment.NewLine, extra)}{Environment.NewLine}Suggested entries:{Environment.NewLine}{JsonSerializer.Serialize(suggestedEntries)}");
    }

    [Fact]
    public void ManifestRegistersExactCurrentBoundaryTypeMembers()
    {
        using var manifest = ReadManifest();
        var entries = manifest.RootElement.GetProperty("dotnetTypes").EnumerateArray().ToArray();
        var assembly = typeof(CampaignObservationV7).Assembly;

        foreach (var entry in entries)
        {
            var typeName = entry.GetProperty("name").GetString()!;
            var type = assembly.GetType(typeName, throwOnError: true)!;
            var expected = entry.GetProperty("declaredMembers").EnumerateArray()
                .Select(value => value.GetString()!)
                .Order(StringComparer.Ordinal)
                .ToArray();
            var actual = type.GetProperties(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(expected, actual);
            if (type.IsEnum)
            {
                var expectedValues = entry.GetProperty("enumValues").EnumerateArray()
                    .Select(value => value.GetString()!)
                    .Order(StringComparer.Ordinal)
                    .ToArray();
                Assert.Equal(
                    expectedValues,
                    Enum.GetNames(type).Order(StringComparer.Ordinal).ToArray());
            }
        }

        var registered = entries.Select(entry => entry.GetProperty("name").GetString()!)
            .ToHashSet(StringComparer.Ordinal);
        Type[] successorCandidateTypes =
        [
            typeof(StopElementMovementAction),
            typeof(ResolveBreakdownStopAction),
            typeof(CompleteBreakdownSegmentAction),
            typeof(MoveReactingElementAction),
            typeof(CompleteReactionParticipantAction),
            typeof(ReactionWindowAction),
            typeof(DeclineReactionWindowAction),
            typeof(CloseReactionWindowUnavailableAction),
            typeof(CloseReactionWindowTimeoutAction),
            typeof(CloseReactionWindowNoEligibleAction),
        ];
        Assert.All(successorCandidateTypes, type => Assert.Contains(type.FullName!, registered));
        var decisionVariants = assembly.GetTypes()
            .Where(type => !type.IsAbstract
                && type.IsAssignableTo(typeof(CampaignObservationV7DecisionState)))
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.All(decisionVariants, typeName => Assert.Contains(typeName, registered));
    }

    [Fact]
    public void ManifestAllowlistCoversCurrentOutputAndSystemOnlyStopCapability()
    {
        using var manifest = ReadManifest();
        Assert.Equal(2, manifest.RootElement.GetProperty("manifestVersion").GetInt32());
        Assert.Equal("sandtable.user-space-declassification.v2", manifest.RootElement.GetProperty("policyId").GetString());
        var allowed = manifest.RootElement.GetProperty("allowedJsonPropertyNames").EnumerateArray()
            .Select(value => value.GetString()!).ToHashSet(StringComparer.Ordinal);
        var forbidden = manifest.RootElement.GetProperty("forbiddenPropertyNames").EnumerateArray()
            .Select(value => value.GetString()!).ToHashSet(StringComparer.Ordinal);
        var initial = BreakdownSnapshotTests.Create(true);
        var pending = BreakdownResolutionReplayTests.PendingTruck();
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = BreakdownSnapshotTests.Scenario();
        var moving = CampaignV11MoveProjector.ApplyMovement(initial,
            BreakdownMovementTests.Move(initial, "axis-truck", "west", "center"), artifact, scenario);
        var resolved = CampaignV11BreakdownProjector.Apply(pending, BreakdownResolutionReplayTests.Resolve(pending), artifact, scenario);
        var (prior, window, phasing, reactor) = CampaignObservationV7ContractTests.ReactionFixture();
        var reacting = BreakdownSnapshotTests.Copy(prior, new CampaignBreakdownFlow.Reacting(phasing, reactor),
            CampaignPositionV11.FromReaction(window.ReactingPosition), window);
        var stop = CampaignBreakdownStop.Create(prior.CampaignId, prior.RulesetHash, prior.StateVersion,
            reactor, CampaignBreakdownStopReason.ReactionTimeout, BreakdownWeatherKind.Normal, []);
        var closed = BreakdownSnapshotTests.Copy(prior, new CampaignBreakdownFlow.ReactorStopClosed(phasing, stop),
            CampaignPositionV11.FromBreakdownStop(prior.CurrentPosition.SequenceContext));
        var fingerprintNames = manifest.RootElement.GetProperty("audienceProfiles").EnumerateArray()
            .Single(value => value.GetProperty("id").GetString() == "reacting-player")
            .GetProperty("forbiddenFingerprintPropertyNames").EnumerateArray()
            .Select(value => value.GetString()!).ToHashSet(StringComparer.Ordinal);
        foreach (var snapshot in new[] { initial, moving, pending, resolved, reacting, closed })
        {
            foreach (var side in new[] { LandSide.Axis, LandSide.Commonwealth })
            {
                var observation = CampaignObservationV7ContractTests.Project(snapshot, side);
                byte[][] playerOutputs =
                [
                    CampaignObservationV7Serializer.SerializeCanonical(observation),
                    CampaignProjectedDecisionHistoryV2Serializer.SerializeCanonical(CampaignProjectedDecisionHistoryV2.Project(observation)),
                    CampaignObservationV7LegalActionSerializer.Serialize(CampaignObservationV7ActionDerivation.DerivePlayer(observation)),
                ];
                foreach (var output in playerOutputs)
                {
                    using var document = JsonDocument.Parse(output);
                    var properties = CollectPropertyNames(document.RootElement).ToArray();
                    Assert.All(properties, value => Assert.Contains(value, allowed));
                    Assert.DoesNotContain(properties, forbidden.Contains);
                    Assert.DoesNotContain("stopId", properties);
                    if (observation.DecisionState is CampaignObservationV7ReactingDecisionState
                        || (observation.DecisionState is CampaignObservationV7BreakdownWaitingDecisionState && side == LandSide.Commonwealth))
                        Assert.DoesNotContain(properties, fingerprintNames.Contains);
                }
                var system = CampaignObservationV7ActionDerivation.DeriveSystem(observation);
                using var systemDocument = JsonDocument.Parse(CampaignObservationV7LegalActionSerializer.Serialize(system));
                var systemProperties = CollectPropertyNames(systemDocument.RootElement).ToArray();
                Assert.All(systemProperties, value => Assert.Contains(value, allowed));
                Assert.DoesNotContain(systemProperties, forbidden.Contains);
                if (snapshot == pending)
                {
                    var candidate = Assert.IsType<ResolveBreakdownStopAction>(Assert.Single(system.Candidates));
                    Assert.Equal(CampaignBreakdownLifecycleFactory.CreateSystemStopCapability(snapshot), candidate.StopId);
                    Assert.NotEqual(((CampaignBreakdownFlow.PhasingStop)snapshot.BreakdownFlow).Stop.StopId, candidate.StopId);
                }
            }
        }
    }

    [Fact]
    public void NoAuthorityRecordIsReachableFromCurrentOutwardRoots()
    {
        var types = CollectReachableCoreTypes([typeof(CampaignObservationV7), typeof(CampaignProjectedDecisionHistoryV2Entry),
            typeof(CampaignLegalActionSet)], typeof(CampaignObservationV7).Assembly);
        Assert.DoesNotContain(types, type => type.Namespace == "Cna.Core.Campaigns");
        Assert.Contains(typeof(ObservedMovementRoute), types);
        Assert.Contains(typeof(ObservedBrokenVehicleLot), types);
        Assert.DoesNotContain(typeof(CampaignObservationDecisionState), types);
    }

    private static IEnumerable<string> CollectPropertyNames(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in value.EnumerateObject())
            {
                yield return property.Name;
                foreach (var nested in CollectPropertyNames(property.Value))
                {
                    yield return nested;
                }
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                foreach (var nested in CollectPropertyNames(item))
                {
                    yield return nested;
                }
            }
        }
    }

    private static HashSet<Type> CollectReachableCoreTypes(
        IEnumerable<Type> roots,
        Assembly assembly)
    {
        var reachable = new HashSet<Type>();
        var pending = new Queue<Type>(roots);
        while (pending.TryDequeue(out var candidate))
        {
            foreach (var type in ExpandContractType(candidate, assembly))
            {
                if (!reachable.Add(type))
                {
                    continue;
                }

                foreach (var property in type.GetProperties(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    pending.Enqueue(property.PropertyType);
                }

                if (type.BaseType?.Assembly == assembly)
                {
                    pending.Enqueue(type.BaseType);
                }

                foreach (var contractInterface in type.GetInterfaces().Where(value =>
                    value.Assembly == assembly))
                {
                    pending.Enqueue(contractInterface);
                }

                if (type.IsAbstract || type.IsInterface)
                {
                    foreach (var variant in assembly.GetTypes().Where(value =>
                        !value.IsAbstract && value.IsAssignableTo(type)))
                    {
                        pending.Enqueue(variant);
                    }
                }
            }
        }

        return reachable;
    }

    private static IEnumerable<Type> ExpandContractType(Type candidate, Assembly assembly)
    {
        var nullable = Nullable.GetUnderlyingType(candidate);
        if (nullable is not null)
        {
            candidate = nullable;
        }

        if (candidate.IsArray)
        {
            foreach (var elementType in ExpandContractType(candidate.GetElementType()!, assembly))
            {
                yield return elementType;
            }

            yield break;
        }

        if (candidate.IsGenericType)
        {
            foreach (var argument in candidate.GetGenericArguments())
            {
                foreach (var argumentType in ExpandContractType(argument, assembly))
                {
                    yield return argumentType;
                }
            }

            yield break;
        }

        if (candidate.Assembly == assembly)
        {
            yield return candidate;
        }
    }

    private static JsonDocument ReadManifest()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "Sandtable.slnx")))
        {
            directory = directory.Parent;
        }

        var root = directory?.FullName
            ?? throw new InvalidOperationException("Repository root not found.");
        return JsonDocument.Parse(File.ReadAllBytes(Path.Combine(
            root,
            "docs",
            "specs",
            "user-space-disclosure-manifest.v2.json")));
    }
}
