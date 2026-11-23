using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cna.Analyzers;

// CNA002: flags direct use of DateTime.Now/DateTime.UtcNow, Random.Shared, an unseeded
// `new Random()`, or Guid.NewGuid() -- the exact non-deterministic API surface DET-001/
// DET-002 forbid under src/Cna.Core. Today's audit (2026-11-22/23) found zero existing
// violations; this rule's entire value is keeping that true as the codebase grows, not
// fixing anything that exists now. See
// docs/work/plans/2026-11-23-codebase-overhaul-direction.md, Section 5.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NonDeterministicApiAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CNA002";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Non-deterministic API used where replay determinism (DET-001/DET-002) is required",
        messageFormat: "'{0}' is non-deterministic and must not be used directly; thread randomness through an explicit, seeded RandomStreamState instead, or allowlist this file with a comment explaining why it is provably outside the replay path",
        category: "Cna.Determinism",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "See docs/work/plans/2026-11-23-codebase-overhaul-direction.md, Section 5.");

    // Empty today: the 2026-11-22/23 audit found zero violations of this rule anywhere
    // under src/Cna.Core. Add an entry here only for a specific, reviewed, provably-safe
    // call site -- never to silence a real finding without first triaging it.
    public static readonly ImmutableHashSet<string> AllowedFileNames = ImmutableHashSet<string>.Empty;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
    }

    private static bool IsAllowed(SyntaxNode node) => AllowedFileNames.Contains(GetFileName(node.SyntaxTree.FilePath));

    // DateTime.Now / DateTime.UtcNow / Random.Shared
    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not MemberAccessExpressionSyntax memberAccess) return;
        if (IsAllowed(memberAccess)) return;

        var symbol = context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken).Symbol;
        if (symbol is not IPropertySymbol property) return;
        var containingType = property.ContainingType?.ToDisplayString();

        var isFlagged = (containingType, property.Name) switch
        {
            ("System.DateTime", "Now") => true,
            ("System.DateTime", "UtcNow") => true,
            ("System.Random", "Shared") => true,
            _ => false,
        };
        if (!isFlagged) return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, memberAccess.GetLocation(),
            $"{containingType}.{property.Name}"));
    }

    // Guid.NewGuid()
    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation) return;
        if (IsAllowed(invocation)) return;

        var symbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol;
        if (symbol is not IMethodSymbol { Name: "NewGuid" } method) return;
        if (method.ContainingType?.ToDisplayString() != "System.Guid") return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), "System.Guid.NewGuid()"));
    }

    // new Random() with no arguments (unseeded)
    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ObjectCreationExpressionSyntax creation) return;
        if (IsAllowed(creation)) return;
        if (creation.ArgumentList is { Arguments.Count: > 0 }) return; // a seeded new Random(seed) is fine

        var symbol = context.SemanticModel.GetSymbolInfo(creation, context.CancellationToken).Symbol;
        if (symbol is not IMethodSymbol { MethodKind: MethodKind.Constructor } constructor) return;
        if (constructor.ContainingType?.ToDisplayString() != "System.Random") return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, creation.GetLocation(), "new System.Random()"));
    }

    private static readonly char[] PathSeparators = { '\\', '/' };

    private static string GetFileName(string path)
    {
        var separatorIndex = path.LastIndexOfAny(PathSeparators);
        return separatorIndex < 0 ? path : path.Substring(separatorIndex + 1);
    }
}
