using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Spamma.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CommandValidatorAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SPAMMA001";

    private const string Category = "CQRS";

    private static readonly LocalizableString Title = "Command must have validator";

    private static readonly LocalizableString MessageFormat =
        "Command '{0}' does not have a corresponding IValidator<{0}> implementation. " +
        "All commands must have at least one validator to ensure validation occurs before command processing.";

    private static readonly LocalizableString Description =
        "All CQRS commands must have at least one FluentValidation validator. " +
        "This ensures that validation logic is explicitly defined and enforced before command handlers execute.";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description);

    private static readonly HashSet<string> CheckedAssemblies = new();

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
            var commandsWithValidators = BuildValidatorMap(context.Compilation);

            context.RegisterSymbolAction(ctx => CheckCommandsHaveValidators(ctx, commandsWithValidators, context.Compilation), SymbolKind.NamedType);
        }
    }

    private static HashSet<string> BuildValidatorMap(Compilation compilation)
    {
        var commandsWithValidators = new HashSet<string>();

        var visitor = new ValidatorCollector(commandsWithValidators, compilation);

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var root = syntaxTree.GetRoot();
            visitor.Visit(root);
        }

        return commandsWithValidators;
    }

    private static void CheckCommandsHaveValidators(SymbolAnalysisContext context, HashSet<string> commandsWithValidators, Compilation compilation)
    {
        var compilationName = compilation.AssemblyName ?? "Unknown";

        lock (CheckedAssemblies)
        {
            if (!CheckedAssemblies.Contains(compilationName))
            {
                CheckedAssemblies.Add(compilationName);

                var missingValidators = FindMissingValidators(compilation, commandsWithValidators);
                foreach (var commandName in missingValidators)
                {
                    var diagnostic = Diagnostic.Create(Rule, Location.None, commandName);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private static List<string> FindMissingValidators(Compilation compilation, HashSet<string> commandsWithValidators)
    {
        var missingValidators = new List<string>();
        var currentAssemblyName = compilation.AssemblyName ?? "Unknown";
        var clientAssemblyName = currentAssemblyName.EndsWith(".Client") ? currentAssemblyName : $"{currentAssemblyName}.Client";
        var serverAssemblyName = currentAssemblyName.EndsWith(".Client") ? currentAssemblyName.Substring(0, currentAssemblyName.Length - 7) : currentAssemblyName;

        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly && (assembly.Name == currentAssemblyName || assembly.Name == clientAssemblyName || assembly.Name == serverAssemblyName))
            {
                FindMissingInNamespace(assembly.GlobalNamespace, commandsWithValidators, missingValidators);
            }
        }

        return missingValidators;
    }

    private static void FindMissingInNamespace(INamespaceSymbol namespaceSymbol, HashSet<string> commandsWithValidators, List<string> missing)
    {
        // Check all types in this namespace
        foreach (var type in namespaceSymbol.GetTypeMembers().Where(t => ImplementsICommand(t)))
        {
            var commandFullName = type.GetFullyQualifiedName();
            if (!commandsWithValidators.Contains(commandFullName))
            {
                missing.Add(type.Name);
            }
        }

        foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            FindMissingInNamespace(nestedNamespace, commandsWithValidators, missing);
        }
    }

    private static bool ImplementsICommand(INamedTypeSymbol typeSymbol)
    {
        // Check if the type implements ICommand interface
        return typeSymbol.AllInterfaces.Any(i => i.Name == "ICommand" && i.ContainingNamespace?.ToString() == "BluQube.Commands");
    }

    private sealed class ValidatorCollector(HashSet<string> commandsWithValidators, Compilation compilation)
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

            if (semanticModel.GetDeclaredSymbol(node) is { } typeInfo && ValidatorCollector.IsValidatorTypeStatic(typeInfo))
            {
                // Extract the command type this validator validates
                var commandType = ValidatorCollector.ExtractValidatedCommandTypeStatic(typeInfo);
                if (commandType != null)
                {
                    commandsWithValidators.Add(commandType.GetFullyQualifiedName());
                }
            }

            base.VisitClassDeclaration(node);
        }

        private static bool IsValidatorTypeStatic(INamedTypeSymbol typeSymbol)
        {
            // Check base classes for AbstractValidator or implementations of IValidator
            var baseClass = typeSymbol.BaseType;
            while (baseClass is not null)
            {
                if (baseClass.Name.Contains("Validator"))
                {
                    return true;
                }

                baseClass = baseClass.BaseType;
            }

            foreach (var iface in typeSymbol.AllInterfaces)
            {
                if (iface.IsGenericType && iface.Name == "IValidator")
                {
                    return true;
                }
            }

            return false;
        }

        private static ITypeSymbol? ExtractValidatedCommandTypeStatic(INamedTypeSymbol validatorType)
        {
            // Check if implements IValidator<TCommand>
            foreach (var iface in validatorType.AllInterfaces)
            {
                if (iface.IsGenericType && iface.Name == "IValidator" && iface.TypeArguments.Length == 1)
                {
                    return iface.TypeArguments[0];
                }
            }

            // Check base class for AbstractValidator<TCommand>
            var baseClass = validatorType.BaseType;
            while (baseClass is not null)
            {
                if (baseClass.IsGenericType && baseClass.Name.Contains("Validator") && baseClass.TypeArguments.Length == 1)
                {
                    return baseClass.TypeArguments[0];
                }

                baseClass = baseClass.BaseType;
            }

            return null;
        }
    }
}