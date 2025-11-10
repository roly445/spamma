using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Spamma.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class QueryAuthorizerAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SPAMMA003";

    private const string Category = "CQRS";

    private static readonly LocalizableString Title = "Query should have authorizer";

    private static readonly LocalizableString MessageFormat =
        "Query '{0}' does not have a corresponding AbstractRequestAuthorizer<{0}> implementation. " +
        "Queries should have authorization handlers to enforce access control.";

    private static readonly LocalizableString Description =
        "CQRS queries should have authorization handlers that inherit from AbstractRequestAuthorizer. " +
        "This ensures that authorization logic is explicitly defined and enforced before query processing.";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    private static readonly HashSet<string> CheckedAssemblies = [];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationStartAnalysisContext context)
    {
        var compilationName = context.Compilation.AssemblyName ?? "Unknown";

        if (!compilationName.EndsWith(".Client"))
        {
            var queriesWithAuthorizers = BuildAuthorizerMap(context.Compilation);

            context.RegisterSymbolAction(ctx => CheckQueriesHaveAuthorizers(ctx, queriesWithAuthorizers, context.Compilation), SymbolKind.NamedType);
        }
    }

    private static HashSet<string> BuildAuthorizerMap(Compilation compilation)
    {
        var queriesWithAuthorizers = new HashSet<string>();

        var visitor = new AuthorizerCollector(queriesWithAuthorizers, compilation);

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var root = syntaxTree.GetRoot();
            visitor.Visit(root);
        }

        return queriesWithAuthorizers;
    }

    private static void CheckQueriesHaveAuthorizers(SymbolAnalysisContext context, HashSet<string> queriesWithAuthorizers, Compilation compilation)
    {
        var compilationName = compilation.AssemblyName ?? "Unknown";

        lock (CheckedAssemblies)
        {
            if (!CheckedAssemblies.Contains(compilationName))
            {
                CheckedAssemblies.Add(compilationName);

                var missingAuthorizers = FindMissingAuthorizers(compilation, queriesWithAuthorizers);
                foreach (var queryName in missingAuthorizers)
                {
                    var diagnostic = Diagnostic.Create(Rule, Location.None, queryName);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private static List<string> FindMissingAuthorizers(Compilation compilation, HashSet<string> queriesWithAuthorizers)
    {
        var missingAuthorizers = new List<string>();
        var currentAssemblyName = compilation.AssemblyName ?? "Unknown";
        var clientAssemblyName = currentAssemblyName.EndsWith(".Client") ? currentAssemblyName : $"{currentAssemblyName}.Client";
        var serverAssemblyName = currentAssemblyName.EndsWith(".Client") ? currentAssemblyName.Substring(0, currentAssemblyName.Length - 7) : currentAssemblyName;

        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly && (assembly.Name == currentAssemblyName || assembly.Name == clientAssemblyName || assembly.Name == serverAssemblyName))
            {
                FindMissingInNamespace(assembly.GlobalNamespace, queriesWithAuthorizers, missingAuthorizers);
            }
        }

        return missingAuthorizers;
    }

    private static void FindMissingInNamespace(INamespaceSymbol namespaceSymbol, HashSet<string> queriesWithAuthorizers, List<string> missing)
    {
        // Check all types in this namespace
        foreach (var type in namespaceSymbol.GetTypeMembers().Where(t => ImplementsIQuery(t)))
        {
            var queryFullName = type.GetFullyQualifiedName();
            if (!queriesWithAuthorizers.Contains(queryFullName))
            {
                missing.Add(type.Name);
            }
        }

        foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            FindMissingInNamespace(nestedNamespace, queriesWithAuthorizers, missing);
        }
    }

    private static bool ImplementsIQuery(INamedTypeSymbol typeSymbol)
    {
        // Check if the type implements IQuery<TResult> interface
        return typeSymbol.AllInterfaces.Any(i =>
            i.Name == "IQuery" &&
            i.IsGenericType &&
            i.ContainingNamespace?.ToString() == "BluQube.Queries");
    }

    private sealed class AuthorizerCollector(HashSet<string> queriesWithAuthorizers, Compilation compilation)
        : CSharpSyntaxWalker
    {
        private readonly Dictionary<SyntaxTree, SemanticModel> _semanticModels = new();

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var syntaxTree = node.SyntaxTree;
            if (!this._semanticModels.TryGetValue(syntaxTree, out var semanticModel))
            {
                semanticModel = compilation.GetSemanticModel(syntaxTree);
                this._semanticModels[syntaxTree] = semanticModel;
            }

            if (semanticModel.GetDeclaredSymbol(node) is { } typeInfo && IsAuthorizerType(typeInfo))
            {
                // Extract the query type this authorizer authorizes
                var queryType = ExtractAuthorizedQueryType(typeInfo);
                if (queryType != null)
                {
                    queriesWithAuthorizers.Add(queryType.GetFullyQualifiedName());
                }
            }

            base.VisitClassDeclaration(node);
        }

        private static bool IsAuthorizerType(INamedTypeSymbol typeSymbol)
        {
            // Check base classes for AbstractRequestAuthorizer
            var baseClass = typeSymbol.BaseType;
            while (baseClass is not null)
            {
                if (baseClass.Name.Contains("Authorizer"))
                {
                    return true;
                }

                baseClass = baseClass.BaseType;
            }

            return false;
        }

        private static ITypeSymbol? ExtractAuthorizedQueryType(INamedTypeSymbol authorizerType)
        {
            // Check base class for AbstractRequestAuthorizer<TQuery>
            var baseClass = authorizerType.BaseType;
            while (baseClass is not null)
            {
                if (baseClass.IsGenericType &&
                    baseClass.Name.Contains("Authorizer") &&
                    baseClass.TypeArguments.Length == 1)
                {
                    return baseClass.TypeArguments[0];
                }

                baseClass = baseClass.BaseType;
            }

            return null;
        }
    }
}
