using System;
using System.Collections.Generic;
using System.Linq;
using Majal.Common.Abstractions;
using Majal.Generators.Dtos.Models;
using Microsoft.CodeAnalysis;
using static Majal.Generators.Dtos.ParameterHandlers.ParameterResolution;

namespace Majal.Generators.Dtos;

internal static class DtoConfigReader
{
    private const string ConfigAttributeName = nameof(DtoConfigAttribute);
    private const string IgnoreAttributeName = nameof(DtoIgnoreAttribute);
    private const string IgnoreTypeGenericAttributeName = $"{nameof(DtoIgnoreTypeAttribute<>)}`1";
    private const string FlattenGenericAttributeName = $"{nameof(DtoFlattenAttribute<>)}`1";
    private const string MemberAttributeName = nameof(DtoMemberAttribute);
    private const string IncludeAttributeName = nameof(DtoIncludeAttribute);

    public const string DefaultDtoSuffix = "Dto";
    public const string DefaultFactoryMethodName = "Create";

    public static DtoContext CreateContext(
        INamedTypeSymbol dtoSymbol,
        INamedTypeSymbol sourceSymbol,
        AttributeData dtoForAttribute,
        Compilation compilation)
    {
        var options = ReadDtoOptions(dtoForAttribute, dtoSymbol.Name, compilation);
        var typeConfig = ReadDtoTypeConfig(dtoSymbol, sourceSymbol);

        return new DtoContext(
            Namespace: dtoSymbol.GetNamespace(),
            DtoName: dtoSymbol.GetTypeNameWithGenerics(),
            RawDtoName: dtoSymbol.Name,
            ParentTypeDeclarations: dtoSymbol.GetParentTypeDeclarations(),
            DtoNamePrefix: options.DtoPrefix,
            DtoNameSuffix: options.DtoSuffix,
            Accessibility: dtoSymbol.DeclaredAccessibility,
            SourceSymbol: sourceSymbol,
            IsRoot: true,
            IsRecord: dtoSymbol.IsRecord,
            FactoryMethodName: options.FactoryMethodName,
            Graph: new DtoGraph(),
            NameMatching: options.NameMatching,
            Directions: options.Directions,
            MemberConfigs: typeConfig.MemberConfigs,
            FlattenConfigs: typeConfig.FlattenConfigs,
            ExcludedTypes: typeConfig.ExcludedTypes,
            ExcludedProperties: [.. typeConfig.ExcludedProperties],
            ExcludedTypeProperties: typeConfig.ExcludedTypeProperties,
            NullableProperties: [.. typeConfig.NullableProperties],
            IncludedProperties: [.. typeConfig.IncludedProperties ?? []],
            DtoSymbol: dtoSymbol,
            Compilation: compilation
        );
    }

    private static DtoOptions ReadDtoOptions(AttributeData attribute, string dtoSymbolName, Compilation compilation)
    {
        var factoryMethodName =
            attribute.GetNamedArgumentValue<string>(nameof(DtoForAttribute<object>.FactoryMethod)) ??
            compilation.GetAssemblyDefaultValue<string>(ConfigAttributeName, nameof(DtoConfigAttribute.FactoryMethod))
            ?? DefaultFactoryMethodName;

        var dtoSuffix =
            attribute.GetNamedArgumentValue<string>(nameof(DtoForAttribute<object>.Suffix)) ??
            compilation.GetAssemblyDefaultValue<string>(ConfigAttributeName, nameof(DtoConfigAttribute.Suffix))
            ?? DefaultDtoSuffix;

        var dtoPrefix =
            attribute.GetNamedArgumentValue<string>(nameof(DtoForAttribute<object>.Prefix)) ??
            compilation.GetAssemblyDefaultValue<string>(ConfigAttributeName, nameof(DtoConfigAttribute.Prefix))
            ?? dtoSymbolName;

        var nameMatchingObj = attribute.GetNamedArgumentValue<object>(nameof(DtoForAttribute<object>.NameMatching));
        var nameMatching = nameMatchingObj != null
            ? (NameMatchingStrategy)Convert.ToInt32(nameMatchingObj)
            : (compilation.GetAssemblyDefaultValue<object>(ConfigAttributeName, nameof(DtoConfigAttribute.NameMatching)) is { } asmNm
                ? (NameMatchingStrategy)Convert.ToInt32(asmNm)
                : NameMatchingStrategy.Exact);

        var directionsObj = attribute.GetNamedArgumentValue<object>(nameof(DtoForAttribute<object>.Directions));
        var directions = directionsObj != null
            ? (MapDirection)Convert.ToInt32(directionsObj)
            : (compilation.GetAssemblyDefaultValue<object>(ConfigAttributeName, nameof(DtoConfigAttribute.Directions)) is { } asmDir
                ? (MapDirection)Convert.ToInt32(asmDir)
                : MapDirection.TwoWay);

        return new DtoOptions(
            factoryMethodName,
            dtoSuffix,
            dtoPrefix,
            nameMatching,
            directions
        );
    }

    private static DtoTypeConfig ReadDtoTypeConfig(INamedTypeSymbol dtoSymbol, INamedTypeSymbol sourceSymbol)
    {
        var excludedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var nullableProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var excludedTypeProperties = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var includedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        List<ITypeSymbol>? excludedTypes = null;
        Dictionary<string, bool>? flattenConfigs = null;
        Dictionary<string, DtoMemberConfig>? memberConfigs = null;

        // Check attributes on dtoSymbol
        foreach (var attr in dtoSymbol.GetAttributes())
        {
            var attrClassName = attr.AttributeClass?.Name;
            var attrMetadataName = attr.AttributeClass?.MetadataName;

            if (attrMetadataName == FlattenGenericAttributeName && attr.AttributeClass?.TypeArguments.Length > 0)
            {
                flattenConfigs ??= [];
                var targetType = attr.AttributeClass.TypeArguments[0];
                var isReversed = attr.GetNamedArgumentValue<bool?>(nameof(DtoFlattenAttribute<object>.IsReversed)) ?? false;
                flattenConfigs[targetType.ToDisplayString()] = isReversed;
            }
            else if (attrMetadataName == IgnoreTypeGenericAttributeName && attr.AttributeClass?.TypeArguments.Length > 0)
            {
                excludedTypes ??= [];
                excludedTypes.Add(attr.AttributeClass.TypeArguments[0]);
            }
            else if (attrClassName == IgnoreAttributeName)
            {
                var ignoredNames = new List<string>();
                if (attr.ConstructorArguments.Length > 0)
                {
                    var first = attr.ConstructorArguments[0];
                    if (first.Kind == TypedConstantKind.Array)
                    {
                        foreach (var v in first.Values)
                        {
                            if (v.Value is string s) ignoredNames.Add(s);
                        }
                    }
                    else if (first.Value is string single)
                    {
                        ignoredNames.Add(single);
                    }
                }

                foreach (var prop in ignoredNames)
                {
                    if (prop.Contains("."))
                    {
                        var parts = prop.Split('.');
                        if (!excludedTypeProperties.TryGetValue(parts[0], out var list))
                        {
                            list = [];
                            excludedTypeProperties[parts[0]] = list;
                        }
                        list.Add(parts[1]);
                    }
                    else
                    {
                        excludedProperties.Add(prop);
                    }
                }
            }
            else if (attrClassName == MemberAttributeName)
            {
                var propName = attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string name
                    ? name
                    : null;

                if (!string.IsNullOrEmpty(propName))
                {
                    memberConfigs ??= new Dictionary<string, DtoMemberConfig>(StringComparer.OrdinalIgnoreCase);
                    var mapFrom = attr.GetNamedArgumentValue<string>(nameof(DtoMemberAttribute.MapFrom));
                    var isNullable = attr.GetNamedArgumentValue<bool?>(nameof(DtoMemberAttribute.Nullable)) ?? false;
                    var usingType = attr.GetNamedArgumentValue<ITypeSymbol>(nameof(DtoMemberAttribute.Using));
                    var usingTypeName = usingType?.ToDisplayString(FullPropertyTypeFormat);

                    memberConfigs[propName!] = new DtoMemberConfig(propName!, mapFrom, isNullable, usingTypeName);

                    if (isNullable)
                    {
                        nullableProperties.Add(propName!);
                    }
                }
            }
            else if (attrClassName == IncludeAttributeName)
            {
                if (attr.ConstructorArguments.Length > 0)
                {
                    var arg = attr.ConstructorArguments[0];
                    if (arg.Values.Length > 0)
                    {
                        foreach (var val in arg.Values)
                        {
                            if (val.Value is string s && !string.IsNullOrWhiteSpace(s))
                            {
                                includedProperties.Add(s);
                            }
                        }
                    }
                    else if (arg.Value is string s && !string.IsNullOrWhiteSpace(s))
                    {
                        includedProperties.Add(s);
                    }
                }
            }
        }

        // Check attributes on sourceSymbol members: e.g. [DtoIgnore] on property/field
        for (var curr = sourceSymbol; curr != null; curr = curr.BaseType)
        {
            foreach (var member in curr.GetMembers())
            {
                if (member.GetAttributes().Any(a => a.AttributeClass?.Name == IgnoreAttributeName))
                {
                    excludedProperties.Add(member.Name);
                }
            }
        }

        var dictResult = excludedTypeProperties.Count > 0
            ? excludedTypeProperties.ToDictionary(k => k.Key, v => v.Value.ToArray(), StringComparer.OrdinalIgnoreCase)
            : null;

        return new DtoTypeConfig(
            flattenConfigs,
            excludedTypes?.ToArray(),
            dictResult,
            excludedProperties.ToArray(),
            nullableProperties.ToArray(),
            memberConfigs,
            includedProperties.ToArray()
        );
    }
}
