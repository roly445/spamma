using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Spamma.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CommandAuthorizerAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SPAMMA002";

    private const string Category = "CQRS";

    private static readonly LocalizableString Title = "Command should have authorizer";

    private static readonly LocalizableString MessageFormat =
        "Command '{0}' does not have a corresponding AbstractRequestAuthorizer<{0}> implementation. " +
        "Commands should have authorization handlers to enforce access control.";

    private static readonly LocalizableString Description =
        "CQRS commands should have authorization handlers that inherit from AbstractRequestAuthorizer. " +
        "This ensures that authorization logic is explicitly defined and enforced before command processing.";

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
            var commandsWithAuthorizers = BuildAuthorizerMap(context.Compilation);

            context.RegisterSymbolAction(ctx => CheckCommandsHaveAuthorizers(ctx, commandsWithAuthorizers, context.Compilation), SymbolKind.NamedType);
        }
    }

    private static HashSet<string> BuildAuthorizerMap(Compilation compilation)
    {
        var commandsWithAuthorizers = new HashSet<string>();

        var visitor = new AuthorizerCollector(commandsWithAuthorizers, compilation);

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var root = syntaxTree.GetRoot();
            visitor.Visit(root);
        }

        return commandsWithAuthorizers;
    }

    private static void CheckCommandsHaveAuthorizers(SymbolAnalysisContext context, HashSet<string> commandsWithAuthorizers, Compilation compilation)
    {
        var compilationName = compilation.AssemblyName ?? "Unknown";

        lock (CheckedAssemblies)
        {
            if (!CheckedAssemblies.Contains(compilationName))
            {
                CheckedAssemblies.Add(compilationName);

                var missingAuthorizers = FindMissingAuthorizers(compilation, commandsWithAuthorizers);
                foreach (var commandName in missingAuthorizers)
                {
                    var diagnostic = Diagnostic.Create(Rule, Location.None, commandName);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private static List<string> FindMissingAuthorizers(Compilation compilation, HashSet<string> commandsWithAuthorizers)
    {
        var missingAuthorizers = new List<string>();
        var currentAssemblyName = compilation.AssemblyName ?? "Unknown";
        var clientAssemblyName = currentAssemblyName.EndsWith(".Client") ? currentAssemblyName : $"{currentAssemblyName}.Client";
        var serverAssemblyName = currentAssemblyName.EndsWith(".Client") ? currentAssemblyName.Substring(0, currentAssemblyName.Length - 7) : currentAssemblyName;

        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly && (assembly.Name == currentAssemblyName || assembly.Name == clientAssemblyName || assembly.Name == serverAssemblyName))
            {
                FindMissingInNamespace(assembly.GlobalNamespace, commandsWithAuthorizers, missingAuthorizers);
            }
        }

        return missingAuthorizers;
    }

    private static void FindMissingInNamespace(INamespaceSymbol namespaceSymbol, HashSet<string> commandsWithAuthorizers, List<string> missing)
    {
        // Check all types in this namespace
        foreach (var type in namespaceSymbol.GetTypeMembers().Where(t => ImplementsICommand(t)))
        {
            var commandFullName = type.GetFullyQualifiedName();
            if (!commandsWithAuthorizers.Contains(commandFullName))
            {
                missing.Add(type.Name);
            }
        }

        foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            FindMissingInNamespace(nestedNamespace, commandsWithAuthorizers, missing);
        }
    }

    private static bool ImplementsICommand(INamedTypeSymbol typeSymbol)
    {
        // Check if the type implements ICommand interface
        return typeSymbol.AllInterfaces.Any(i => i.Name == "ICommand" && i.ContainingNamespace?.ToString() == "BluQube.Commands");
    }

    private sealed class AuthorizerCollector(HashSet<string> commandsWithAuthorizers, Compilation compilation)
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
                // Extract the command type this authorizer authorizes
                var commandType = ExtractAuthorizedCommandType(typeInfo);
                if (commandType != null)
                {
                    commandsWithAuthorizers.Add(commandType.GetFullyQualifiedName());
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

        private static ITypeSymbol? ExtractAuthorizedCommandType(INamedTypeSymbol authorizerType)
        {
            // Check base class for AbstractRequestAuthorizer<TCommand>
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