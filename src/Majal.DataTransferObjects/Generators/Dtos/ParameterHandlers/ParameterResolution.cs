using System.Text.RegularExpressions;
using Majal.Common.Abstractions;
using Majal.Generators.Dtos.Models;
using Microsoft.CodeAnalysis;
using static Majal.Common.Abstractions.Constants;

namespace Majal.Generators.Dtos.ParameterHandlers;

internal static class ParameterResolution
{
    public static readonly SymbolDisplayFormat FullPropertyTypeFormat = new(
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
    );

    public static string? TryResolveScalarType(ParameterType parameterType)
    {
        if (parameterType.Type is null) return null;

        if (IsValueObjectType(parameterType.Type)) return ResolveValueObjectElementType(parameterType);
        if (IsAggregateType(parameterType.Type)) return ResolveAggregateIdType(parameterType);
        if (IsEntityType(parameterType.Type)) return ResolveNestedDtoElementType(parameterType);
        if (IsBindableType(parameterType.Type)) return ResolveBindableType(parameterType);

        return null;
    }

    private static bool IsBindableType(ITypeSymbol type)
    {
        var isParsable = type.Interfaces.Any(i => i.Name == "IParsable");

        var isDictionary = type.AllInterfaces.Any(i => i.MetadataName == "IDictionary`2") ||
                           type.MetadataName == "IDictionary`2";

        var isEnum = type.TypeKind is TypeKind.Enum;

        var isTypeParameter = type.TypeKind is TypeKind.TypeParameter;

        return isParsable || isDictionary || isEnum || isTypeParameter;
    }

    private static string ResolveBindableType(ParameterType parameterType)
    {
        var resolvedType = parameterType.Type!.ToDisplayString(FullPropertyTypeFormat);
        if (parameterType.IsNullable) resolvedType += "?";
        return resolvedType;
    }

    public static string ResolveValueObjectElementType(ParameterType parameterType)
    {
        var namedType = (INamedTypeSymbol)parameterType.Type!;
        var valueObjectAttr = namedType.GetAnyMajalAttribute(nameof(ValueObjectAttribute));

        if (valueObjectAttr?.AttributeClass is { TypeArguments.Length: > 0 })
        {
            var resolvedType = valueObjectAttr.AttributeClass.TypeArguments[0].ToDisplayString(FullPropertyTypeFormat);
            if (parameterType.IsNullable) resolvedType += "?";
            return resolvedType;
        }

        var underlyingValueType = GetValueObjectUnderlyingType(namedType);
        if (underlyingValueType is not null)
        {
            var resolvedType = underlyingValueType.ToDisplayString(FullPropertyTypeFormat);
            if (parameterType.IsNullable) resolvedType += "?";
            return resolvedType;
        }

        var valueObjectFactoryMethod = FindFactoryMethod(namedType, parameterType.Context.DtoContext.FactoryMethodName);
        if (valueObjectFactoryMethod is { Parameters.Length: 1 })
        {
            var resolvedType = valueObjectFactoryMethod.Parameters[0].Type.ToDisplayString(FullPropertyTypeFormat);
            if (parameterType.IsNullable) resolvedType += "?";
            return resolvedType;
        }

        return ResolveNestedDtoElementType(parameterType);
    }

    public static ITypeSymbol? GetValueObjectUnderlyingType(INamedTypeSymbol namedType)
    {
        var genericValueObject = namedType.AllInterfaces
            .FirstOrDefault(i => i.MetadataName.StartsWith("IValueObject`", StringComparison.Ordinal));

        return genericValueObject?.TypeArguments.FirstOrDefault();
    }

    public static string ResolveAggregateIdType(ParameterType parameterType)
    {
        var namedType = (INamedTypeSymbol)parameterType.Type!;
        var idType = GetEntityIdType(namedType, parameterType.Context.DtoContext.Compilation);

        if (string.IsNullOrWhiteSpace(idType))
            return ResolveNestedDtoElementType(parameterType);

        if (parameterType.IsNullable) idType += "?";
        return idType;
    }

    public static string GetEntityIdType(INamedTypeSymbol type, Compilation? compilation)
    {
        var entityAttribute = type.GetAnyMajalAttribute(nameof(EntityAttribute));
        if (entityAttribute?.AttributeClass is { TypeArguments.Length: > 0 })
            return entityAttribute.AttributeClass.TypeArguments[0].ToDisplayString(FullPropertyTypeFormat);

        var entityInterface = type.AllInterfaces.FirstOrDefault(i => i.MetadataName == "IEntity`1");
        if (entityInterface is not null)
            return entityInterface.TypeArguments[0].ToDisplayString(FullPropertyTypeFormat);

        var idProperty = compilation?.GetAssemblyDefaultValue<INamedTypeSymbol>(nameof(EntityOptionsAttribute),
            nameof(EntityOptionsAttribute.DefaultIdType));

        return idProperty?.ToDisplayString(FullPropertyTypeFormat) ?? IntType;
    }

    public static string ResolveNestedDtoElementType(ParameterType parameterType)
    {
        var eNamedType = (INamedTypeSymbol)parameterType.Type!;
        var nestedDtoName =
            $"{parameterType.Context.DtoContext.DtoNamePrefix}{eNamedType.Name}{parameterType.Context.DtoContext.DtoNameSuffix}";
        var resolvedElementType = nestedDtoName;
        if (parameterType.IsNullable) resolvedElementType += "?";

        if (parameterType.Context.DtoContext.CollectedDto.ContainsKey(nestedDtoName)) return resolvedElementType;

        parameterType.Context.DtoContext.CollectedDto[nestedDtoName] = default;

        var nestedContext = parameterType.Context.DtoContext with
        {
            IsRoot = false,
            DtoName = nestedDtoName,
            RawDtoName = nestedDtoName,
            SourceSymbol = eNamedType
        };

        var nestedData = DtoForGenerator.GetDtoData(nestedContext);

        if (nestedData == null) return resolvedElementType;
        parameterType.Context.DtoContext.CollectedDto[nestedDtoName] = nestedData.Value;

        if (nestedData.Value.DtoName == nestedDtoName) return resolvedElementType;

        var actualDtoName = nestedData.Value.DtoName;
        resolvedElementType = actualDtoName;
        if (parameterType.IsNullable) resolvedElementType += "?";

        if (!parameterType.Context.DtoContext.CollectedDto.ContainsKey(actualDtoName))
            parameterType.Context.DtoContext.CollectedDto[actualDtoName] = nestedData.Value;

        return resolvedElementType;
    }

    public static FactoryArgument BuildReconstruction(ParameterContext ctx, string resolvedElementType,
        string dtoPropertyName, bool isCollection, bool isNullable, ReconstructKind scalarKind,
        string? scalarTargetTypeName)
    {
        var suffix = isCollection ? GetCollectionConversionSuffix(ctx.Parameter.Type) : "";

        if (TryGetNestedDto(resolvedElementType, ctx.DtoContext.CollectedDto, out var nestedDto))
        {
            return new FactoryArgument(ctx.Parameter.Name, ReconstructKind.NestedType, dtoPropertyName,
                nestedDto.SourceSimpleName, isCollection, suffix, isNullable);
        }

        return new FactoryArgument(ctx.Parameter.Name, scalarKind, dtoPropertyName,
            scalarTargetTypeName, isCollection, suffix, isNullable);
    }

    public static bool TryGetNestedDto(string resolvedElementType, Dictionary<string, DtoData> collectedDto,
        out DtoData nestedDto)
    {
        var key = resolvedElementType.EndsWith("?")
            ? resolvedElementType.Substring(0, resolvedElementType.Length - 1)
            : resolvedElementType;
        return collectedDto.TryGetValue(key, out nestedDto);
    }

    public static string GetCollectionConversionSuffix(ITypeSymbol parameterType)
    {
        if (parameterType is IArrayTypeSymbol) return "ToArray";
        if (parameterType is not INamedTypeSymbol namedType) return "";

        return namedType.MetadataName switch
        {
            "List`1" or "IList`1" or "ICollection`1" or "IReadOnlyList`1" or "IReadOnlyCollection`1" => "ToList",
            "HashSet`1" or "ISet`1" => "ToHashSet",
            _ => ""
        };
    }

    public static IMethodSymbol? FindFactoryMethod(INamedTypeSymbol symbol, string factoryMethodName)
    {
        for (var current = symbol; current != null; current = current.BaseType)
        {
            var createMethod = current.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => SymbolEqualityComparer.Default.Equals(m.ReturnType, symbol))
                .FirstOrDefault(m => m.IsStatic && m.Name == factoryMethodName);

            if (createMethod != null) return createMethod;
        }

        return null;
    }

    public static bool IsValueObjectType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return false;

        var implementsValueObject = typeSymbol.AllInterfaces.Any(i =>
            i.MetadataName == "IValueObject" ||
            i.MetadataName.StartsWith("IValueObject`", StringComparison.Ordinal));

        return implementsValueObject || typeSymbol.HasAnyMajaAttribute(nameof(ValueObjectAttribute));
    }

    public static bool IsEntityType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return false;

        var implementsEntity = typeSymbol.AllInterfaces.Any(i =>
            i.MetadataName.StartsWith("IEntity`", StringComparison.Ordinal));

        var hasEntityAttribute = typeSymbol.HasAnyMajaAttribute(nameof(EntityAttribute));
        return implementsEntity || hasEntityAttribute;
    }

    public static bool IsAggregateType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return false;

        var implementsAggregate = typeSymbol.AllInterfaces.Any(i =>
            i.MetadataName.StartsWith("IAggregate`", StringComparison.Ordinal));

        return implementsAggregate || typeSymbol.HasAnyMajaAttribute(nameof(AggregateAttribute));
    }

    public static string? ExtractParamDoc(string? xml, string paramName)
    {
        if (string.IsNullOrWhiteSpace(xml)) return null;

        var match = Regex.Match(xml!, $"""<param name="{paramName}">(.*?)</param>""", RegexOptions.Singleline);
        if (!match.Success) return null;

        var content = match.Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(content)) return null;

        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        return $"/// <summary>\n{string.Join("\n", lines.Select(l => "/// " + l.Trim()))}\n/// </summary>";
    }

    public static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpperInvariant(input[0]) + input.Substring(1);
    }
}