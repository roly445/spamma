using Microsoft.CodeAnalysis;

namespace Spamma.Analyzers;

internal static class SymbolExtensions
{
    public static string GetFullyQualifiedName(this ISymbol symbol)
    {
        var symbols = new List<string>();
        var current = symbol;

        while (current is not null)
        {
            symbols.Add(current.Name);
            current = current.ContainingSymbol;
        }

        symbols.Reverse();
        return string.Join(".", symbols.Where(s => !string.IsNullOrEmpty(s)));
    }
}