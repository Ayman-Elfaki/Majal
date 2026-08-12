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
                typeName += $"<{string.Join(", ", symbol.TypeParameters.Select(t => t.Name))}>";

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

        public string[] GetParentTypeDeclarations()
        {
            var parentTypes = new List<string>();
            for (var current = symbol.ContainingType; current != null; current = current.ContainingType)
            {
                var typeKeyword = current.TypeKind switch
                {
                    TypeKind.Struct when current.IsRecord => "record struct",
                    TypeKind.Struct => "struct",
                    TypeKind.Class when current.IsRecord => "record",
                    _ => "class"
                };

                var modifier = current.IsStatic ? "static partial" : "partial";
                var accessModifier = current.DeclaredAccessibility switch
                {
                    Accessibility.Private => "private",
                    Accessibility.Internal => "internal",
                    Accessibility.Protected => "protected",
                    Accessibility.ProtectedOrInternal => "protected internal",
                    Accessibility.ProtectedAndInternal => "private protected",
                    _ => "public"
                };

                parentTypes.Add($"{accessModifier} {modifier} {typeKeyword} {current.GetTypeNameWithGenerics()}");
            }

            parentTypes.Reverse();
            return [.. parentTypes];
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

    extension(AttributeData? attribute)
    {
        public T? GetNamedArgumentValue<T>(string key)
        {
            var typedConstant = attribute?.NamedArguments.FirstOrDefault(a => a.Key == key).Value ?? default;

            if (typedConstant.Kind != TypedConstantKind.Array || !typeof(T).IsArray)
                return typedConstant.Value is null ? default : (T?)typedConstant.Value;

            var elementType = typeof(T).GetElementType();
            if (elementType is null) return default;

            var values = typedConstant.Values;
            var array = Array.CreateInstance(elementType, values.Length);
            for (var i = 0; i < values.Length; i++)
            {
                array.SetValue(values[i].Value, i);
            }

            return (T?)(object?)array;
        }
    }

    extension(Compilation compilation)
    {
        public IEnumerable<INamedTypeSymbol> GetAllTypesInCompilation()
        {
            return compilation.GlobalNamespace.GetAllSymbolsInNamespace();
        }

        public T? GetAssemblyDefaultValue<T>(string attributeName, string propertyName)
        {
            return compilation.Assembly.GetMajalAttribute(attributeName) is { } attribute
                ? attribute.GetNamedArgumentValue<T>(propertyName)
                : default;
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