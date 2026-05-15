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

        public bool HasMajalAttribute(string attributeName)
        {
            return symbol.GetAttributes().Any(ad =>
                ad.AttributeClass?.Name == attributeName &&
                ad.AttributeClass.ContainingNamespace.ToDisplayString() == nameof(Majal));
        }

        public AttributeData? GetAttribute(string attributeName, string attributeNamespace)
        {
            return symbol.GetAttributes().FirstOrDefault(ad =>
                ad.AttributeClass?.Name == attributeName &&
                ad.AttributeClass.ContainingNamespace.ToDisplayString() == attributeNamespace);
        }

        public AttributeData? GetMajalAttribute(string attributeName)
        {
            return symbol.GetAttributes().FirstOrDefault(ad =>
                ad.AttributeClass?.Name == attributeName &&
                ad.AttributeClass.ContainingNamespace.ToDisplayString() == nameof(Majal));
        }
    }

    extension(IPropertySymbol propertySymbol)
    {
        public bool IsComputed =>
            !propertySymbol.ContainingType.GetMembers()
                .OfType<IFieldSymbol>()
                .Any(f => SymbolEqualityComparer.Default.Equals(f.AssociatedSymbol, propertySymbol));
    }

    extension(ITypeSymbol type)
    {
        public (ITypeSymbol ElementType, bool IsCollection) GetCollectionInfo()
        {
            switch (type)
            {
                case IArrayTypeSymbol arrayType:
                    return (arrayType.ElementType, true);
                case INamedTypeSymbol { SpecialType: SpecialType.System_String }:
                    break;
                case INamedTypeSymbol namedType:
                {
                    var enumerable = namedType.AllInterfaces.FirstOrDefault(i => i.MetadataName == "IEnumerable`1") ??
                                     (namedType.MetadataName == "IEnumerable`1" ? namedType : null);

                    var isDictionary = namedType.AllInterfaces.Any(i => i.MetadataName == "IDictionary`2") ||
                                       namedType.MetadataName == "IDictionary`2";

                    if (enumerable != null && !isDictionary)
                        return (enumerable.TypeArguments[0], true);
                    break;
                }
            }

            return (type, false);
        }

        public (ITypeSymbol UnwrappedType, bool IsNullable) UnwrapNullable()
        {
            if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } namedType)
                return (namedType.TypeArguments[0], true);

            return type.NullableAnnotation == NullableAnnotation.Annotated ? (type, true) : (type, false);
        }
    }
}