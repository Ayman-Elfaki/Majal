using Microsoft.CodeAnalysis;

namespace Majal.Abstractions;

public static class SymbolExtensions
{
    extension(INamedTypeSymbol symbol)
    {
        public string GetNamespace()
        {
            return symbol.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : $"namespace {symbol.ContainingNamespace.ToDisplayString()};";
        }
        

        public string GetTypeNameWithGenerics()
        {
            var typeName = symbol.Name;
            if (symbol.TypeParameters.Length > 0)
            {
                typeName += $"<{string.Join(", ", symbol.TypeParameters.Select(t => t.Name))}>";
            }
            return typeName;
        }

        public string[] GetPropertyNames()
        {
            return symbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.Kind == SymbolKind.Property)
                .Select(p => p.Name)
                .ToArray();
        }
    }

    extension(ISymbol symbol)
    {
        public bool HasAttribute(string attributeName, string attributeNamespace)
        {
            return symbol.GetAttributes().Any(ad =>
                ad.AttributeClass?.Name == attributeName &&
                ad.AttributeClass.ContainingNamespace.ToDisplayString() == attributeNamespace);
        }

        public AttributeData? GetAttribute(string attributeName, string attributeNamespace)
        {
            return symbol.GetAttributes().FirstOrDefault(ad =>
                ad.AttributeClass?.Name == attributeName &&
                ad.AttributeClass.ContainingNamespace.ToDisplayString() == attributeNamespace);
        }
    }
    
    extension(IPropertySymbol propertySymbol)
    {
        public bool IsComputed =>
            !propertySymbol.ContainingType.GetMembers()
                .OfType<IFieldSymbol>()
                .Any(f => SymbolEqualityComparer.Default.Equals(f.AssociatedSymbol, propertySymbol));
    }
}
