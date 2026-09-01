using System.Reflection;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Tests.Campaigns;

namespace Cna.Core.Tests.Observations;

[Trait("Boundary", "UserSpace")]
public sealed class UserSpaceDisclosureManifestTests
{
    [Fact]
    public void ManifestRegistersEveryReachableCoreTypeFromDeclaredRoots()
    {
        using var manifest = ReadManifest();
        var assembly = typeof(CampaignObservationV6).Assembly;
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
    public void ManifestRegistersExactDormantBoundaryTypeMembers()
    {
        using var manifest = ReadManifest();
        var entries = manifest.RootElement.GetProperty("dotnetTypes").EnumerateArray().ToArray();
        var assembly = typeof(CampaignObservationV6).Assembly;

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
                && type.IsAssignableTo(typeof(CampaignObservationDecisionState)))
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.All(decisionVariants, typeName => Assert.Contains(typeName, registered));
    }

    [Fact]
    public void ManifestIsAClosedAllowlistForEveryCurrentDecisionOutputVariant()
    {
        using var manifest = ReadManifest();
        Assert.Equal(1, manifest.RootElement.GetProperty("manifestVersion").GetInt32());
        Assert.Equal(
            "sandtable.user-space-declassification.v1",
            manifest.RootElement.GetProperty("policyId").GetString());
        Assert.Equal(
            [
                "authority-revalidates-current-membership",
                "indistinguishable-reacting-capabilities-reject-before-projection",
                "reacting-move-cost-matches-published-edge-traversal",
                "reacting-opportunity-id-binds-window-state-and-capability",
                "reacting-root-movement-ended-element-ids-empty",
                "reacting-root-own-elements-empty",
                "reaction-decision-audience-matches-active-side",
                "submission-carries-action-id-not-element-id",
            ],
            manifest.RootElement.GetProperty("semanticRules").EnumerateArray()
                .Select(value => value.GetString()!).Order(StringComparer.Ordinal));
        var allowed = manifest.RootElement.GetProperty("allowedJsonPropertyNames")
            .EnumerateArray().Select(value => value.GetString()!)
            .ToHashSet(StringComparer.Ordinal);
        var forbidden = manifest.RootElement.GetProperty("forbiddenPropertyNames")
            .EnumerateArray().Select(value => value.GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        var fixture = CampaignV10TestData.Create();
        var normal = CampaignObservationV6Projector.Project(
            fixture.MovementSnapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Axis,
            new CampaignObservationV6AuthorityFacts([], []));
        var triggered = CampaignV10Projector.ApplyMovement(
            fixture.MovementSnapshot,
            fixture.TriggeringMove,
            fixture.Artifact,
            fixture.Scenario,
            (_, _) => fixture.TriggeringMove);
        var phasing = CampaignObservationV6Projector.Project(
            triggered,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Axis,
            new CampaignObservationV6AuthorityFacts([], []));
        var reacting = CreateReactingObservationWithMoveOption();
        CampaignObservationV6[] observations = [normal, phasing, reacting];
        var outputs = observations
            .SelectMany(observation => new byte[][]
            {
                CampaignObservationV6Serializer.SerializeCanonical(observation),
                CampaignProjectedDecisionHistorySerializer.SerializeCanonical(
                    CampaignProjectedDecisionHistory.Project(observation)),
                CampaignObservationV6LegalActionSerializer.Serialize(
                    CampaignObservationV6ActionDerivation.DerivePlayer(observation)),
                CampaignObservationV6LegalActionSerializer.Serialize(
                    CampaignObservationV6ActionDerivation.DeriveSystem(observation)),
            })
            .ToArray();

        foreach (var output in outputs)
        {
            using var document = JsonDocument.Parse(output);
            var properties = CollectPropertyNames(document.RootElement).ToArray();
            Assert.All(properties, property => Assert.Contains(property, allowed));
            Assert.DoesNotContain(properties, forbidden.Contains);
        }

        var profiles = manifest.RootElement.GetProperty("audienceProfiles")
            .EnumerateArray().ToDictionary(
                profile => profile.GetProperty("id").GetString()!,
                StringComparer.Ordinal);
        Assert.Equal(
            ["normal-player", "phasing-waiting-player", "reacting-player"],
            profiles.Keys.Order(StringComparer.Ordinal));
        var reactingProfile = profiles["reacting-player"];
        var forbiddenFingerprintProperties = reactingProfile
            .GetProperty("forbiddenFingerprintPropertyNames")
            .EnumerateArray().Select(value => value.GetString()!)
            .ToHashSet(StringComparer.Ordinal);
        byte[][] reactingOutputs =
        [
            CampaignObservationV6Serializer.SerializeCanonical(reacting),
            CampaignProjectedDecisionHistorySerializer.SerializeCanonical(
                CampaignProjectedDecisionHistory.Project(reacting)),
            CampaignObservationV6LegalActionSerializer.Serialize(
                CampaignObservationV6ActionDerivation.DerivePlayer(reacting)),
            CampaignObservationV6LegalActionSerializer.Serialize(
                CampaignObservationV6ActionDerivation.DeriveSystem(reacting)),
        ];
        foreach (var output in reactingOutputs)
        {
            using var document = JsonDocument.Parse(output);
            var reactingProperties = CollectPropertyNames(document.RootElement).ToArray();
            Assert.DoesNotContain(reactingProperties, forbiddenFingerprintProperties.Contains);
            foreach (var path in reactingProfile.GetProperty("forbiddenPaths")
                .EnumerateArray().Select(value => value.GetString()!))
            {
                Assert.Empty(ResolvePath(document.RootElement, path));
            }
        }

        foreach (var output in reactingOutputs.Take(1))
        {
            using var document = JsonDocument.Parse(output);
            foreach (var path in reactingProfile.GetProperty("observationMustBeEmpty")
                .EnumerateArray().Select(value => value.GetString()!))
            {
                var matches = ResolvePath(document.RootElement, path).ToArray();
                Assert.Single(matches);
                Assert.Equal(JsonValueKind.Array, matches[0].ValueKind);
                Assert.Empty(matches[0].EnumerateArray());
            }
        }
    }

    private static IEnumerable<JsonElement> ResolvePath(JsonElement root, string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return Resolve(root, segments, 0);

        static IEnumerable<JsonElement> Resolve(
            JsonElement current,
            IReadOnlyList<string> segments,
            int index)
        {
            if (index == segments.Count)
            {
                yield return current;
                yield break;
            }

            var segment = segments[index];
            if (segment == "*")
            {
                if (current.ValueKind != JsonValueKind.Array)
                {
                    yield break;
                }

                foreach (var item in current.EnumerateArray())
                {
                    foreach (var match in Resolve(item, segments, index + 1))
                    {
                        yield return match;
                    }
                }

                yield break;
            }

            if (current.ValueKind == JsonValueKind.Object
                && current.TryGetProperty(segment, out var property))
            {
                foreach (var match in Resolve(property, segments, index + 1))
                {
                    yield return match;
                }
            }
        }
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

    private static CampaignObservationV6 CreateReactingObservationWithMoveOption()
    {
        var fixture = CampaignV10TestData.Create();
        var before = CampaignObservationV6Projector.Project(
            fixture.MovementSnapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Commonwealth,
            new CampaignObservationV6AuthorityFacts([], []));
        var snapshot = CampaignV10Projector.ApplyMovement(
            fixture.MovementSnapshot,
            fixture.TriggeringMove,
            fixture.Artifact,
            fixture.Scenario,
            (_, _) => fixture.TriggeringMove);
        var projected = CampaignObservationV6Projector.Project(
            snapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Commonwealth,
            new CampaignObservationV6AuthorityFacts([], []));
        var state = Assert.IsType<CampaignObservationReactingDecisionState>(
            projected.DecisionState);
        Assert.Single(state.OwnOpportunities);
        var element = Assert.Single(before.OwnElements);
        var stacking = Cna1979Movement.LookupStackingValue(element.OrganizationId);
        Assert.True(stacking.IsSupported);
        var options = CampaignObservationV6ActionDerivation.DeriveReactionMoveOptions(
            projected.Position,
            projected.Locations,
            projected.Edges,
            [],
            [],
            element,
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [element.CurrentLocationId] = stacking.Value.StackingValue,
            });
        var decision = new CampaignObservationReactingDecisionState(
            state.WindowId,
            state.ApparentTrigger,
            [new ObservedReactionOpportunity(
                CampaignObservationV6DisclosureIdentity.CreateOpportunity(
                    state.WindowId,
                    projected.StateVersion,
                    CampaignObservationV6DisclosureIdentity.CreateCapabilityKey(options)),
                options)],
            null);
        return new CampaignObservationV6(
            projected.ContractVersion,
            projected.PolicyId,
            projected.CampaignId,
            projected.StateVersion,
            projected.RulesetHash,
            projected.ScenarioId,
            projected.Observer,
            projected.Position,
            projected.Weather,
            projected.Locations,
            projected.Edges,
            [],
            [],
            [],
            [],
            decision);
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
            "user-space-disclosure-manifest.v1.json")));
    }
}
