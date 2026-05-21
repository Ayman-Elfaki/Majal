using System.Text;
using Microsoft.CodeAnalysis;

namespace Majal.Common.Abstractions;

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

        public bool HasAnyMajaAttribute(string attributeName)
        {
            return symbol.GetAttributes().Any(ad =>
                (ad.AttributeClass?.Name == attributeName || ad.AttributeClass?.Name == $"{attributeName}`1") &&
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

        public AttributeData? GetAnyMajalAttribute(string attributeName)
        {
            return symbol.GetAttributes().FirstOrDefault(ad =>
                (ad.AttributeClass?.Name == attributeName || ad.AttributeClass?.Name == $"{attributeName}`1") &&
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
        public bool IsSymbolDerivedFrom(ITypeSymbol baseSymbol)
        {
            for (var current = type.BaseType; current != null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseSymbol)) return true;
            }

            return false;
        }

        public (ITypeSymbol ElementType, bool IsCollection, bool IsDictionary) GetCollectionInfo()
        {
            switch (type)
            {
                case IArrayTypeSymbol arrayType:
                    return (arrayType.ElementType, true, false);
                case INamedTypeSymbol { SpecialType: SpecialType.System_String }:
                    break;
                case INamedTypeSymbol namedType:
                {
                    var enumerable = namedType.AllInterfaces.FirstOrDefault(i => i.MetadataName == "IEnumerable`1") ??
                                     (namedType.MetadataName == "IEnumerable`1" ? namedType : null);

                    var isDictionary = namedType.AllInterfaces.Any(i => i.MetadataName == "IDictionary`2") ||
                                       namedType.MetadataName == "IDictionary`2";

                    if (enumerable != null && !isDictionary)
                        return (enumerable.TypeArguments[0], true, false);

                    if (enumerable != null && isDictionary)
                        return (type, false, true);

                    break;
                }
            }

            return (type, false, false);
        }

        public (ITypeSymbol UnwrappedType, bool IsNullable) UnwrapNullable()
        {
            if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } namedType)
                return (namedType.TypeArguments[0], true);

            return type.NullableAnnotation == NullableAnnotation.Annotated ? (type, true) : (type, false);
        }
    }

    extension(AttributeData? attribute)
    {
        public T? GetNamedArgumentValue<T>(string key)
        {
            return (T?)attribute?.NamedArguments.FirstOrDefault(a => a.Key == key).Value.Value;
        }
    }

    extension(Compilation compilation)
    {
        public IEnumerable<INamedTypeSymbol> GetAllTypesInCompilation()
        {
            return compilation.GlobalNamespace.GetAllSymbolsInNamespace();
        }
    }

    extension(INamespaceSymbol ns)
    {
        private IEnumerable<INamedTypeSymbol> GetAllSymbolsInNamespace()
        {
            foreach (var member in ns.GetMembers())
            {
                if (member is INamedTypeSymbol namedType) yield return namedType;
                if (member is not INamespaceSymbol childNamespace) continue;
                foreach (var childMember in childNamespace.GetAllSymbolsInNamespace()) yield return childMember;
            }
        }
    }
}