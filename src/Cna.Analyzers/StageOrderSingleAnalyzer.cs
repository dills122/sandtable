using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cna.Analyzers;

// CNA001: flags a no-predicate .Single()/.First()/.SingleOrDefault()/.FirstOrDefault()
// call on an IReadOnlyList<CampaignOperationStageOrder>-shaped expression, outside a
// small file allowlist. This is the exact bug shape found and fixed 7+ times this
// session (PR #518) and found-and-refuted once more (CampaignNativeReserveReleaseV27.cs,
// safe only because nothing currently appends a second CampaignOperationStageOrder --
// a landmine for the next change, not a type-level guarantee). See
// docs/work/plans/2026-11-23-codebase-overhaul-direction.md, Section 2.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StageOrderSingleAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CNA001";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Bare Single()/First() on a stage-order collection assumes cardinality 1",
        messageFormat: "'{0}' assumes this stage-order collection never holds more than one entry, which is not a type-level guarantee; filter by GameTurn and OperationStage first instead, or allowlist this file with a comment explaining why it is safe",
        category: "Cna.Determinism",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "See docs/work/plans/2026-11-23-codebase-overhaul-direction.md, Section 2.");

    // Files where a bare Single() on CampaignOperationStageOrder-shaped collections is
    // provably safe today, verified by direct inspection, not assumed:
    //  - FirstActingSideResolver.cs: does not call bare Single() at all -- it filters by
    //    (GameTurn, OperationStage) first, then checks Length == 1 explicitly. It is the
    //    canonical safe pattern this rule points callers at, not an exception to it.
    //  - CampaignSnapshotV11Validator.cs: likewise filters via SingleOrDefault(predicate),
    //    never bare.
    //  - CampaignV11Preamble.cs, CampaignSnapshotV11Admission.cs,
    //    CampaignNativeReserveReleaseV27.cs, and test file
    //    CampaignReserveReleaseRulesTests.cs (which reads the live session's snapshot
    //    the same way production code does): genuinely bare Single() calls on
    //    CampaignSnapshotV11's OperationStageOrders. Safe only because, under the
    //    standing "don't widen CampaignSnapshotV11" architecture ruling (2026-11-22
    //    snapshot-representation fork), CampaignSnapshotV11 never represents Stage 2+ and
    //    nothing appends a second CampaignOperationStageOrder to it -- confirmed by
    //    reading every call site of CampaignProjector's one Append (CampaignProjector.cs)
    //    and the Stage 2 preamble chain's StageEntryOrders helper (CampaignExercises.cs),
    //    which carries the Stage 2 order as a separate synthetic list specifically so it
    //    never touches this field. This is exactly the kind of fact this analyzer exists
    //    to stop a future change from silently invalidating -- if CampaignSnapshotV11 is
    //    ever widened, these three files must be revisited.
    public static readonly ImmutableHashSet<string> AllowedFileNames = ImmutableHashSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "FirstActingSideResolver.cs",
        "CampaignSnapshotV11Validator.cs",
        "CampaignV11Preamble.cs",
        "CampaignSnapshotV11Admission.cs",
        "CampaignNativeReserveReleaseV27.cs",
        "CampaignReserveReleaseRulesTests.cs");

    private static readonly ImmutableHashSet<string> TargetedMemberNames = ImmutableHashSet.Create(
        "Single", "First", "SingleOrDefault", "FirstOrDefault");

    private const string TargetedElementTypeName = "CampaignOperationStageOrder";
    private const string TargetedElementTypeNamespace = "Cna.Core.Campaigns";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation) return;
        if (invocation.ArgumentList.Arguments.Count != 0) return; // only the no-predicate overload
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess) return;
        var memberName = memberAccess.Name.Identifier.ValueText;
        if (!TargetedMemberNames.Contains(memberName)) return;

        var fileName = GetFileName(invocation.SyntaxTree.FilePath);
        if (AllowedFileNames.Contains(fileName)) return;

        var receiverType = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type;
        if (receiverType is null) return;
        if (!ImplementsReadOnlyListOfStageOrder(receiverType)) return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, memberAccess.Name.GetLocation(), memberName));
    }

    private static bool ImplementsReadOnlyListOfStageOrder(ITypeSymbol type)
    {
        foreach (var candidate in type.AllInterfacesAndSelf())
        {
            if (candidate is not INamedTypeSymbol { Name: "IReadOnlyList", TypeArguments.Length: 1 } named) continue;
            var element = named.TypeArguments[0];
            if (element.Name == TargetedElementTypeName &&
                element.ContainingNamespace?.ToDisplayString() == TargetedElementTypeNamespace)
                return true;
        }
        return false;
    }

    private static readonly char[] PathSeparators = { '\\', '/' };

    private static string GetFileName(string path)
    {
        var separatorIndex = path.LastIndexOfAny(PathSeparators);
        return separatorIndex < 0 ? path : path.Substring(separatorIndex + 1);
    }
}

internal static class TypeSymbolExtensions
{
    internal static System.Collections.Generic.IEnumerable<ITypeSymbol> AllInterfacesAndSelf(this ITypeSymbol type)
    {
        yield return type;
        foreach (var @interface in type.AllInterfaces) yield return @interface;
    }
}
