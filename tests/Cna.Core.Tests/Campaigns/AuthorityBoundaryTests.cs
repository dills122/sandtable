using System.Reflection;
using Cna.Core.Campaigns;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

[Trait("Boundary", "UserSpace")]
public sealed class AuthorityBoundaryTests
{
    [Fact]
    public void RawAuthorityAndReplayPrimitivesAreNotPublic()
    {
        Type[] forbidden =
        [
            typeof(CampaignSnapshot),
            typeof(CampaignSetupSnapshot),
            typeof(CampaignWorldSnapshot),
            typeof(CampaignContentContext),
            typeof(CampaignCommand),
            typeof(CampaignEvent),
            typeof(CampaignEngine),
            typeof(CampaignProjector),
            typeof(CampaignEventSerializer),
            typeof(CampaignSnapshotSerializer),
            typeof(CampaignReplayPreparation),
            typeof(CampaignReplayHarness),
            typeof(CampaignCreationExecution),
            typeof(Cna.Core.Actions.CampaignActionExecution),
            typeof(InitiativeResolver),
            typeof(CampaignStageEntryPolicy),
            typeof(CampaignStageEntryPolicyCodec),
            typeof(FirstActingSideResolver),
            typeof(StageEntryEventFactory),
            typeof(StageEntryResolved),
            typeof(ResolveNoObligationOrganization),
            typeof(ResolveNoObligationNavalConvoyArrival),
            typeof(ResolveNoObligationFleetAssignment),
            typeof(ResolveNoObligationFleetRepair),
            typeof(NoObligationOrganizationResolved),
            typeof(NoObligationNavalConvoyArrivalResolved),
            typeof(NoObligationFleetAssignmentResolved),
            typeof(NoObligationFleetRepairResolved),
        ];

        Assert.All(forbidden, type =>
        {
            Assert.False(type.IsPublic, type.FullName);
            Assert.False(type.IsNestedPublic, type.FullName);
        });
    }

    // Task 020D (2026-11-20, owner ruling option c): the audience-safe Combat decision surface
    // became public C# API. Pin it exactly so the change cannot widen into raw authority: only
    // these four types, only these members, and nothing in any public signature beyond audience
    // values, the already-public ExerciseSession handle and plain BCL types.
    private static readonly Type[] CombatAudienceSurface =
    [
        typeof(Cna.Core.Actions.CampaignCombatAudienceDecision),
        typeof(Cna.Core.Actions.CampaignCombatAudienceSubmission),
        typeof(Cna.Core.Actions.CampaignCombatAudienceSubmissionResult),
        typeof(Cna.Core.Actions.CampaignCombatAudienceDecisions),
    ];

    [Fact]
    public void CombatAudienceSurfaceIsExactlyTheFourFrozenPublicTypes()
    {
        var exported = typeof(CampaignAuthorityHandle).Assembly.GetExportedTypes()
            .Where(type => type.Name.Contains("CombatAudience", StringComparison.Ordinal))
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(CombatAudienceSurface.Select(type => type.FullName!).Order(StringComparer.Ordinal), exported);
        // The draft-stage oracle stays a test-only verifier.
        Assert.False(typeof(Cna.Core.Actions.CampaignCombatAudienceDraftVerifier).IsVisible);
    }

    [Fact]
    public void CombatAudienceSurfacePublicMembersArePinned()
    {
        string[] expected =
        [
            "CampaignCombatAudienceDecision field System.Int32 CurrentContractVersion",
            "CampaignCombatAudienceDecision field System.Int32 SuccessorContractVersion",
            "CampaignCombatAudienceDecision method System.Byte[] SerializeCanonical()",
            "CampaignCombatAudienceDecision property Cna.Core.Actions.CampaignActionAudience Audience",
            "CampaignCombatAudienceDecision property System.Byte[] VisibleBytes",
            "CampaignCombatAudienceDecision property System.Collections.Generic.IReadOnlyList`1[System.String] OfferRefs",
            "CampaignCombatAudienceDecision property System.Int32 ContractVersion",
            "CampaignCombatAudienceDecision property System.Int64 AudienceRevision",
            "CampaignCombatAudienceDecision property System.String ActingElementId",
            "CampaignCombatAudienceDecision property System.String CampaignId",
            "CampaignCombatAudienceDecision property System.String Stage",
            "CampaignCombatAudienceDecision property System.String VisibleDigest",
            "CampaignCombatAudienceDecisions field System.String CustodyStage",
            "CampaignCombatAudienceDecisions field System.String CycleControlStage",
            "CampaignCombatAudienceDecisions field System.String CycleControlSubject",
            "CampaignCombatAudienceDecisions field System.String ForceAssignmentStage",
            "CampaignCombatAudienceDecisions field System.String PostAssaultRetreatStage",
            "CampaignCombatAudienceDecisions field System.String RbaDeclineStage",
            "CampaignCombatAudienceDecisions field System.String ReserveReleaseStage",
            "CampaignCombatAudienceDecisions field System.String SelectionStage",
            "CampaignCombatAudienceDecisions method Cna.Core.Actions.CampaignCombatAudienceDecision Query(Cna.Core.Exercises.ExerciseSession, Cna.Core.Actions.CampaignActionAudience, System.String, System.String)",
            "CampaignCombatAudienceDecisions method Cna.Core.Actions.CampaignCombatAudienceDecision QueryCustody(Cna.Core.Exercises.ExerciseSession, Cna.Core.Actions.CampaignActionAudience, System.String)",
            "CampaignCombatAudienceDecisions method Cna.Core.Actions.CampaignCombatAudienceDecision QueryCycleControl(Cna.Core.Exercises.ExerciseSession, Cna.Core.Actions.CampaignActionAudience)",
            "CampaignCombatAudienceDecisions method Cna.Core.Actions.CampaignCombatAudienceDecision QueryForceAssignment(Cna.Core.Exercises.ExerciseSession, Cna.Core.Actions.CampaignActionAudience, System.String)",
            "CampaignCombatAudienceDecisions method Cna.Core.Actions.CampaignCombatAudienceDecision QueryPostAssaultRetreat(Cna.Core.Exercises.ExerciseSession, Cna.Core.Actions.CampaignActionAudience, System.String)",
            "CampaignCombatAudienceDecisions method Cna.Core.Actions.CampaignCombatAudienceDecision QueryRbaDecline(Cna.Core.Exercises.ExerciseSession, Cna.Core.Actions.CampaignActionAudience, System.String)",
            "CampaignCombatAudienceDecisions method Cna.Core.Actions.CampaignCombatAudienceDecision QueryReserveRelease(Cna.Core.Exercises.ExerciseSession, Cna.Core.Actions.CampaignActionAudience, System.String)",
            "CampaignCombatAudienceDecisions method Cna.Core.Actions.CampaignCombatAudienceDecision QuerySelection(Cna.Core.Exercises.ExerciseSession, Cna.Core.Actions.CampaignActionAudience, System.String)",
            "CampaignCombatAudienceDecisions method Cna.Core.Actions.CampaignCombatAudienceSubmissionResult Submit(Cna.Core.Exercises.ExerciseSession, Cna.Core.Actions.CampaignCombatAudienceSubmission)",
            "CampaignCombatAudienceDecisions method Cna.Core.Actions.CampaignCombatAudienceSubmissionResult SubmitForceAssignment(Cna.Core.Exercises.ExerciseSession, Cna.Core.Actions.CampaignCombatAudienceSubmission)",
            "CampaignCombatAudienceDecisions method Cna.Core.Actions.CampaignCombatAudienceSubmissionResult SubmitSelection(Cna.Core.Exercises.ExerciseSession, Cna.Core.Actions.CampaignCombatAudienceSubmission)",
            "CampaignCombatAudienceDecisions method System.Int32 ContractVersionFor(System.String)",
            "CampaignCombatAudienceSubmission constructor (System.Int32, System.String, Cna.Core.Actions.CampaignActionAudience, System.String, System.String, System.Int64, System.String, System.String)",
            "CampaignCombatAudienceSubmission field System.Int32 CurrentContractVersion",
            "CampaignCombatAudienceSubmission method Cna.Core.Actions.CampaignCombatAudienceSubmission <Clone>$()",
            "CampaignCombatAudienceSubmission method System.Boolean Equals(Cna.Core.Actions.CampaignCombatAudienceSubmission)",
            "CampaignCombatAudienceSubmission method System.Boolean Equals(System.Object)",
            "CampaignCombatAudienceSubmission method System.Int32 GetHashCode()",
            "CampaignCombatAudienceSubmission method System.String ToString()",
            "CampaignCombatAudienceSubmission method System.Void Deconstruct(System.Int32&, System.String&, Cna.Core.Actions.CampaignActionAudience&, System.String&, System.String&, System.Int64&, System.String&, System.String&)",
            "CampaignCombatAudienceSubmission property Cna.Core.Actions.CampaignActionAudience Audience init",
            "CampaignCombatAudienceSubmission property System.Int32 ContractVersion init",
            "CampaignCombatAudienceSubmission property System.Int64 AudienceRevision init",
            "CampaignCombatAudienceSubmission property System.String ActingElementId init",
            "CampaignCombatAudienceSubmission property System.String CampaignId init",
            "CampaignCombatAudienceSubmission property System.String OfferRef init",
            "CampaignCombatAudienceSubmission property System.String Stage init",
            "CampaignCombatAudienceSubmission property System.String VisibleDigest init",
            "CampaignCombatAudienceSubmissionResult method System.Byte[] SerializeOutwardCanonical()",
            "CampaignCombatAudienceSubmissionResult property System.Boolean IsAccepted",
            "CampaignCombatAudienceSubmissionResult property System.String AcceptedRef",
        ];
        var actual = CombatAudienceSurface.SelectMany(PublicMemberSignatures)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CombatAudienceSurfaceSignaturesNameNoPrivateAuthorityType()
    {
        var allowed = new HashSet<Type>(CombatAudienceSurface)
        {
            typeof(Cna.Core.Actions.CampaignActionAudience),
            typeof(Cna.Core.Exercises.ExerciseSession),
        };
        var offenders = CombatAudienceSurface
            .SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.Instance
                | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .SelectMany(member => SignatureTypes(member).Select(type => (member, type)))
            .SelectMany(pair => Flatten(pair.type).Select(type => (pair.member, type)))
            .Where(pair => !allowed.Contains(pair.type) && pair.type.Namespace != "System"
                && pair.type.Namespace != "System.Collections.Generic")
            .Select(pair => $"{pair.member.DeclaringType!.Name}.{pair.member.Name}: {pair.type.FullName}")
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Assert.Empty(offenders);
        // Only Core mints decisions and results; the trusted successor step never leaves Core.
        Assert.Empty(typeof(Cna.Core.Actions.CampaignCombatAudienceDecision).GetConstructors());
        Assert.Empty(typeof(Cna.Core.Actions.CampaignCombatAudienceSubmissionResult).GetConstructors());
        Assert.Null(typeof(Cna.Core.Actions.CampaignCombatAudienceSubmissionResult).GetProperty("TrustedStep"));
    }

    private static IEnumerable<string> PublicMemberSignatures(Type type) =>
        type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(member => member is not MethodInfo { IsSpecialName: true })
            .Select(member => $"{type.Name} {member switch
            {
                FieldInfo field => $"field {field.FieldType} {field.Name}",
                PropertyInfo property => $"property {property.PropertyType} {property.Name}"
                    + (property.SetMethod is not { IsPublic: true } ? ""
                        : property.SetMethod.ReturnParameter.GetRequiredCustomModifiers()
                            .Contains(typeof(System.Runtime.CompilerServices.IsExternalInit)) ? " init" : " set"),
                ConstructorInfo constructor => $"constructor ({Parameters(constructor)})",
                MethodInfo method => $"method {method.ReturnType} {method.Name}({Parameters(method)})",
                _ => $"{member.MemberType} {member.Name}",
            }}");

    private static string Parameters(MethodBase method) =>
        string.Join(", ", method.GetParameters().Select(parameter => parameter.ParameterType.ToString()));

    private static IEnumerable<Type> SignatureTypes(MemberInfo member) => member switch
    {
        FieldInfo field => [field.FieldType],
        PropertyInfo property => [property.PropertyType],
        MethodBase method => method.GetParameters().Select(parameter => parameter.ParameterType)
            .Concat(method is MethodInfo info ? [info.ReturnType] : []),
        _ => [],
    };

    private static IEnumerable<Type> Flatten(Type type)
    {
        if (type.HasElementType) return Flatten(type.GetElementType()!);
        if (type.IsGenericType)
            return [type.GetGenericTypeDefinition(), .. type.GetGenericArguments().SelectMany(Flatten)];
        return [type];
    }

    [Fact]
    public void OnlyTestsReceiveCoreFriendAccess()
    {
        var friends = typeof(CampaignAuthorityHandle).Assembly
            .GetCustomAttributes<System.Runtime.CompilerServices.InternalsVisibleToAttribute>()
            .Select(value => value.AssemblyName)
            .ToArray();
        Assert.Equal(["Cna.Core.Tests"], friends);
    }

    [Fact]
    public void OnlyApprovedProductionProjectsReferenceCore()
    {
        var root = FindRepositoryRoot();
        var references = Directory.GetFiles(Path.Combine(root, "src"), "*.csproj",
                SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("../Cna.Core/Cna.Core.csproj",
                StringComparison.Ordinal))
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["Cna.ExerciseRunner", "Cna.OrleansHost"], references);
    }

    [Fact]
    public void DecisionAndIntelligenceSourceDoNotNameStageEntryAuthority()
    {
        var root = FindRepositoryRoot();
        string[] projectDirectories =
        [
            "Cna.DecisionWorker",
            "Cna.Intelligence.Contracts",
            "Cna.Intelligence.Gateway",
        ];
        string[] forbiddenNames =
        [
            nameof(CampaignStageEntryPolicy),
            nameof(FirstActingSideResolver),
            nameof(StageEntryResolved),
            nameof(ResolveNoObligationOrganization),
            nameof(ResolveNoObligationNavalConvoyArrival),
            nameof(ResolveNoObligationFleetAssignment),
            nameof(ResolveNoObligationFleetRepair),
            nameof(NoObligationOrganizationResolved),
            nameof(NoObligationNavalConvoyArrivalResolved),
            nameof(NoObligationFleetAssignmentResolved),
            nameof(NoObligationFleetRepairResolved),
        ];

        var offenders = projectDirectories
            .SelectMany(project => Directory.GetFiles(
                Path.Combine(root, "src", project),
                "*.cs",
                SearchOption.AllDirectories))
            .Where(path => forbiddenNames.Any(name =>
                File.ReadAllText(path).Contains(name, StringComparison.Ordinal)))
            .ToArray();

        Assert.Empty(offenders);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Sandtable.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
