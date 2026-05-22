using System.Text;
using System.Text.RegularExpressions;
using Majal.Common.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Majal.Common.Abstractions.Constants;

namespace Majal.Generators.Dtos;

[Generator]
public sealed class DtoForGenerator : BaseGenerator<DtoForGenerator.DtoData>
{
    private static readonly SymbolDisplayFormat FullPropertyTypeFormat = new(
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
    );


    public readonly record struct ParameterData(
        (string Name, string Type) Declaration,
        bool IsNullable,
        string? XmlDocs = null
    );

    public readonly record struct DerivedTypeInfo(
        string DtoName,
        string Discriminator
    );

    public readonly record struct DtoData
    {
        public string Namespace { get; init; }
        public string DtoName { get; init; }
        public string RawDtoName { get; init; }
        public string? BaseDtoName { get; init; }
        public string? XmlDocs { get; init; }
        public bool IsRecord { get; init; }
        public Accessibility Accessibility { get; init; }
        public EquatableList<string> ParentTypeDeclarations { get; init; }
        public EquatableList<DtoData> NestedDtos { get; init; }
        public EquatableList<ParameterData> Parameters { get; init; }
        public EquatableList<DerivedTypeInfo> DerivedTypes { get; init; }

        public DtoData(string @namespace, string dtoName, string rawDtoName, string[] parentTypeDeclarations,
            Accessibility accessibility, string? xmlDocs, string? baseDtoName, bool isRecord,
            DerivedTypeInfo[] derivedTypes, ParameterData[] parameters, DtoData[] nestedDtos)
        {
            DtoName = dtoName;
            Namespace = @namespace;
            XmlDocs = xmlDocs;
            IsRecord = isRecord;
            Accessibility = accessibility;
            RawDtoName = rawDtoName;
            ParentTypeDeclarations = new EquatableList<string>(parentTypeDeclarations);
            BaseDtoName = baseDtoName;
            NestedDtos = new EquatableList<DtoData>(nestedDtos);
            Parameters = new EquatableList<ParameterData>(parameters);
            DerivedTypes = new EquatableList<DerivedTypeInfo>(derivedTypes);
        }
    }

    private readonly record struct ParameterType(
        ITypeSymbol? Type,
        bool IsNullable,
        bool IsDictionary,
        DtoContext Context
    );

    private readonly record struct DtoContext(
        string Namespace,
        string DtoName,
        string RawDtoName,
        string[] ParentTypeDeclarations,
        string DtoNamePrefix,
        string DtoNameSuffix,
        Accessibility Accessibility,
        INamedTypeSymbol SourceSymbol,
        bool IsRoot,
        bool IsRecord,
        string FactoryMethodName,
        Dictionary<string, DtoData> Collected,
        Dictionary<string, bool>? FlattenConfigs = null,
        ITypeSymbol[]? ExcludedTypes = null,
        string[]? ExcludedProperties = null!,
        Compilation? Compilation = null
    );

    private interface IParameterTypeResolver
    {
        bool CanHandle(ParameterType parameterType);

        string Resolve(ParameterType parameterType);
    }

    private sealed class ValueObjectTypeResolver : IParameterTypeResolver
    {
        public bool CanHandle(ParameterType parameterType) =>
            parameterType.Type is not null && IsValueObjectType(parameterType.Type);

        public string Resolve(ParameterType parameterType) =>
            ResolveValueObjectElementType(parameterType);
    }

    private sealed class AggregateTypeResolver : IParameterTypeResolver
    {
        public bool CanHandle(ParameterType parameterType) =>
            parameterType.Type is not null && IsAggregateType(parameterType.Type);

        public string Resolve(ParameterType parameterType) =>
            ResolveAggregateIdType(parameterType);
    }

    private sealed class EntityTypeResolver : IParameterTypeResolver
    {
        public bool CanHandle(ParameterType parameterType) =>
            parameterType.Type is not null && IsEntityType(parameterType.Type) && !IsAggregateType(parameterType.Type);

        public string Resolve(ParameterType parameterType) =>
            ResolveNestedDtoElementType(parameterType);
    }

    private sealed class DefaultTypeResolver : IParameterTypeResolver
    {
        public bool CanHandle(ParameterType parameterType)
        {
            return parameterType.Type is not null &&
                   (parameterType.Type.TypeKind is TypeKind.Enum or TypeKind.TypeParameter ||
                    parameterType.Type.Interfaces.Any(i => i.Name == "IParsable") ||
                    parameterType.IsDictionary);
        }

        public string Resolve(ParameterType parameterType)
        {
            var type = parameterType.Type!;
            var resolvedType = type.ToDisplayString(FullPropertyTypeFormat);
            if (parameterType.IsNullable) resolvedType += "?";
            return resolvedType;
        }
    }

    private static readonly IParameterTypeResolver[] ParameterTypeResolvers =
    [
        new ValueObjectTypeResolver(),
        new AggregateTypeResolver(),
        new EntityTypeResolver(),
        new DefaultTypeResolver()
    ];

    private const string DtoAttribute = $"Majal.{nameof(DtoForAttribute<>)}`1";
    private const string OptionsAttributeName = $"Majal.{nameof(DtoForOptionsAttribute)}";
    private const string FlattenGenericAttributeName = $"{nameof(FlattenDtoForAttribute<>)}`1";
    private const string ExcludeGenericAttributeName = $"{nameof(ExcludeDtoForAttribute<>)}`1";

    private const string DefaultDtoSuffix = "Dto";
    private const string DefaultFactoryMethodName = "Create";

    protected override string AttributeFullName => DtoAttribute;
    protected override string GenericAttributeFullName => $"{nameof(DtoForAttribute<>)}`1";

    protected override void Generate(SourceProductionContext context, DtoData data)
    {
        var template = new DtoForTemplate { Data = data };
        var code = template.TransformText();
        context.AddSource(GetSourceFileName(data), SourceText.From(code, Encoding.UTF8));
    }

    private static string GetSourceFileName(DtoData data)
    {
        if (data.ParentTypeDeclarations.Count == 0) return $"{data.RawDtoName}.g.cs";

        var parentNames = string.Join("_", data.ParentTypeDeclarations.Select(GetSanitizeTypeDeclaration));
        return $"{parentNames}_{data.RawDtoName}.g.cs";

        static string GetSanitizeTypeDeclaration(string declaration) =>
            declaration.Split([' '], StringSplitOptions.RemoveEmptyEntries).Last()
                .Replace('<', '_').Replace('>', '_').Replace(',', '_')
                .Replace(" ", "_").Replace(".", "_");
    }


    private static string[] GetParentTypeDeclarations(INamedTypeSymbol dtoSymbol)
    {
        var parentTypes = new List<string>();
        for (var current = dtoSymbol.ContainingType; current != null; current = current.ContainingType)
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
        return parentTypes.ToArray();
    }

    protected override bool Filter(SyntaxNode node, CancellationToken token) =>
        node is ClassDeclarationSyntax or RecordDeclarationSyntax;

    protected override DtoData? Transform(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol dtoSymbol) return null;

        var attribute = context.Attributes
            .FirstOrDefault(a => a.AttributeClass?.MetadataName == GenericAttributeFullName);

        if (attribute?.AttributeClass?.TypeArguments.Length == 0) return null;

        if (attribute?.AttributeClass?.TypeArguments[0] is not INamedTypeSymbol sourceSymbol) return null;

        var compilation = context.SemanticModel.Compilation;

        var factoryMethodName =
            attribute.GetNamedArgumentValue<string>(nameof(DtoForAttribute<>.FactoryMethod)) ??
            compilation.GetAssemblyDefaultValue<string>(OptionsAttributeName, nameof(DtoForAttribute<>.FactoryMethod))
            ?? DefaultFactoryMethodName;

        var dtoSuffix =
            attribute.GetNamedArgumentValue<string>(nameof(DtoForAttribute<>.Suffix)) ??
            compilation.GetAssemblyDefaultValue<string>(OptionsAttributeName, nameof(DtoForOptionsAttribute.Suffix))
            ?? DefaultDtoSuffix;

        var dtoPrefix =
            attribute.GetNamedArgumentValue<string>(nameof(DtoForAttribute<>.Prefix)) ??
            compilation.GetAssemblyDefaultValue<string>(OptionsAttributeName, nameof(DtoForOptionsAttribute.Prefix))
            ?? dtoSymbol.Name;

        var assemblyExcludedPropertyNames =
            compilation.GetAssemblyDefaultValue<string[]>(OptionsAttributeName, nameof(DtoForOptionsAttribute.Exclude))
            ?? [];

        var excludedPropertyNames =
            attribute.GetNamedArgumentValue<string[]>(nameof(DtoForAttribute<>.Exclude)) ?? [];

        var nestedDtos = new Dictionary<string, DtoData>();

        List<ITypeSymbol>? excludedTypes = null;
        Dictionary<string, bool>? flattenConfigs = null;

        foreach (var attr in dtoSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.MetadataName == FlattenGenericAttributeName)
            {
                if (!(attr.AttributeClass?.TypeArguments.Length > 0)) continue;

                flattenConfigs ??= new Dictionary<string, bool>();
                var targetType = attr.AttributeClass.TypeArguments[0];
                var isReversed = attr.GetNamedArgumentValue<bool?>(nameof(FlattenDtoForAttribute<>.IsReversed))
                                 ?? false;

                flattenConfigs[targetType.ToDisplayString()] = isReversed;
            }

            if (attr.AttributeClass?.MetadataName == ExcludeGenericAttributeName)
            {
                if (!(attr.AttributeClass?.TypeArguments.Length > 0)) continue;
                excludedTypes ??= [];
                excludedTypes.Add(attr.AttributeClass.TypeArguments[0]);
            }
        }

        var dtoContext = new DtoContext(
            IsRoot: true,
            Namespace: dtoSymbol.GetNamespace(),
            DtoName: dtoSymbol.GetTypeNameWithGenerics(),
            RawDtoName: dtoSymbol.Name,
            ParentTypeDeclarations: GetParentTypeDeclarations(dtoSymbol),
            DtoNamePrefix: dtoPrefix,
            DtoNameSuffix: dtoSuffix,
            Accessibility: dtoSymbol.DeclaredAccessibility,
            IsRecord: dtoSymbol.IsRecord,
            SourceSymbol: sourceSymbol,
            FactoryMethodName: factoryMethodName,
            Collected: nestedDtos,
            FlattenConfigs: flattenConfigs,
            ExcludedTypes: excludedTypes?.ToArray(),
            ExcludedProperties: [.. assemblyExcludedPropertyNames, .. excludedPropertyNames],
            Compilation: context.SemanticModel.Compilation
        );

        return GetDtoData(dtoContext);
    }

    private static DtoData? GetDtoData(DtoContext context)
    {
        var createMethod = FindFactoryMethod(context.SourceSymbol, context.FactoryMethodName);

        var excludedProperties =
            new HashSet<string>(context.ExcludedProperties ?? [], StringComparer.OrdinalIgnoreCase);

        if (createMethod is null && context.Compilation is not null &&
            context.SourceSymbol is { IsAbstract: true })
        {
            var derivedMethods =
                FindFactoryMethodsInDerivedTypes(context.SourceSymbol, context.FactoryMethodName,
                    context.Compilation);

            if (derivedMethods.Count > 0)
            {
                var derivedDtos = new List<DtoData>();
                var derivedTypes = new List<DerivedTypeInfo>();

                foreach (var method in derivedMethods)
                {
                    var derivedSymbol = method.ContainingType;
                    var derivedDtoName = $"{context.DtoNamePrefix}{derivedSymbol.Name}{context.DtoNameSuffix}";

                    if (!context.Collected.ContainsKey(derivedDtoName))
                    {
                        context.Collected[derivedDtoName] = default;

                        var derivedContext = context with
                        {
                            IsRoot = false,
                            DtoName = derivedDtoName,
                            RawDtoName = derivedDtoName,
                            SourceSymbol = derivedSymbol
                        };

                        var derivedData = GetDtoData(derivedContext);

                        if (derivedData != null)
                        {
                            var updatedData = derivedData.Value with { BaseDtoName = context.DtoName };
                            context.Collected[derivedDtoName] = updatedData;
                            derivedDtos.Add(updatedData);
                        }
                    }

                    derivedTypes.Add(new DerivedTypeInfo(derivedDtoName, derivedSymbol.Name));
                }

                var commonParameters = GetCommonParameters(derivedDtos);

                if (commonParameters.Length > 0)
                {
                    for (var i = 0; i < derivedDtos.Count; i++)
                    {
                        var derivedDto = derivedDtos[i];

                        var uniqueParameters = derivedDto.Parameters
                            .Where(p => commonParameters.All(cp => cp.Declaration != p.Declaration))
                            .ToArray();

                        var updatedData = derivedDto with
                        {
                            Accessibility = context.Accessibility,
                            Parameters = new EquatableList<ParameterData>([.. uniqueParameters])
                        };

                        derivedDtos[i] = updatedData;
                        context.Collected[derivedDto.DtoName] = updatedData;
                    }
                }

                var xmlDocs = FormatXmlDocs(context.SourceSymbol.GetDocumentationCommentXml());

                var nestedDtos =
                    context.IsRoot
                        ? context.Collected.Values
                            .Where(v => !string.IsNullOrEmpty(v.DtoName) && v.DtoName != context.DtoName).ToArray()
                        : [];

                return new DtoData(
                    context.Namespace,
                    context.DtoName,
                    context.RawDtoName,
                    context.ParentTypeDeclarations,
                    context.Accessibility,
                    xmlDocs,
                    null,
                    context.IsRecord,
                    [.. derivedTypes],
                    commonParameters,
                    nestedDtos
                );
            }
        }

        if (createMethod is null) return null;

        var methodXml = createMethod.GetDocumentationCommentXml();
        var parameters = new List<ParameterData>();

        foreach (var p in createMethod.Parameters.Where(p => !excludedProperties.Contains(p.Name)))
        {
            var (elementType, isCollection, isDictionary) = p.Type.GetCollectionInfo();
            var (unwrappedType, isNullable) = elementType.UnwrapNullable();

            if (ShouldExcludeParameter(p.Type, elementType, isDictionary, context.ExcludedTypes)) continue;

            if (!isCollection && unwrappedType is INamedTypeSymbol type && IsValueObjectType(type) &&
                context.FlattenConfigs is not null &&
                context.FlattenConfigs.TryGetValue(type.ToDisplayString(), out var isReversed))
            {
                var valObjFactory = FindFactoryMethod(type, context.FactoryMethodName);
                if (valObjFactory is { Parameters.Length: > 1 })
                {
                    var valObjMethodXml = valObjFactory.GetDocumentationCommentXml();

                    foreach (var sp in valObjFactory.Parameters)
                    {
                        var (spElementType, spIsCollection, spIsDictionary) = sp.Type.GetCollectionInfo();
                        var (spUnwrappedType, spIsNullable) = spElementType.UnwrapNullable();

                        var spResolveContext = new ParameterType(spUnwrappedType, spIsNullable || isNullable,
                            spIsDictionary, context);

                        var spResolver = ParameterTypeResolvers.FirstOrDefault(r => r.CanHandle(spResolveContext));
                        if (spResolver is null) continue;

                        var spResolvedElementType = spResolver.Resolve(spResolveContext);
                        var spResolvedType = spIsCollection
                            ? $"{GenericsNamespace}.IEnumerable{spResolvedElementType}>"
                            : spResolvedElementType;

                        var combinedName = isReversed
                            ? char.ToLowerInvariant(sp.Name[0]) + sp.Name.Substring(1) + ToPascalCase(p.Name)
                            : char.ToLowerInvariant(p.Name[0]) + p.Name.Substring(1) + ToPascalCase(sp.Name);

                        if (excludedProperties.Contains(combinedName)) continue;

                        var spXml = ExtractParamDoc(valObjMethodXml, sp.Name) ?? ExtractParamDoc(methodXml, p.Name);
                        var parameter =
                            new ParameterData((combinedName, spResolvedType), spIsNullable || isNullable, spXml);

                        parameters.Add(parameter);
                    }

                    continue;
                }
            }

            var isAggregate = IsAggregateType(unwrappedType);
            var resolveContext = new ParameterType(unwrappedType, isNullable, isDictionary, context);

            var resolver = ParameterTypeResolvers.FirstOrDefault(r => r.CanHandle(resolveContext));
            if (resolver is null) continue;

            var resolvedElementType = resolver.Resolve(resolveContext);

            var resolvedType = isCollection
                ? $"{GenericsNamespace}.IEnumerable<{resolvedElementType}>"
                : resolvedElementType;

            var propertyName = isAggregate
                ? $"{unwrappedType.Name}{(isCollection ? "Ids" : "Id")}"
                : p.Name;

            var paramXml = ExtractParamDoc(methodXml, p.Name);
            parameters.Add(new ParameterData((propertyName, resolvedType), isNullable, paramXml));
        }

        DtoData[] nestedDtosResult =
            context.IsRoot ? [.. context.Collected.Values.Where(v => !string.IsNullOrEmpty(v.DtoName))] : [];

        var xmlDocsResult = ExtractSummary(methodXml) ??
                            FormatXmlDocs(context.SourceSymbol.GetDocumentationCommentXml());

        return new DtoData(
            context.Namespace,
            context.DtoName,
            context.RawDtoName,
            context.ParentTypeDeclarations,
            context.Accessibility,
            xmlDocsResult,
            null,
            context.IsRecord,
            [],
            [.. parameters],
            nestedDtosResult
        );
    }

    private static ParameterData[] GetCommonParameters(IEnumerable<DtoData> dtos)
    {
        var dtoArray = dtos as DtoData[] ?? [.. dtos];
        if (dtoArray.Length == 0) return [];

        return
        [
            .. dtoArray[0].Parameters.Where(p =>
                dtoArray.Skip(1).All(d => d.Parameters.Any(o => o.Declaration == p.Declaration))
            )
        ];
    }

    private static bool ShouldExcludeParameter(ITypeSymbol originalType, ITypeSymbol elementType, bool isDictionary,
        ITypeSymbol[]? excludedTypes)
    {
        if (excludedTypes is null || excludedTypes.Length == 0) return false;

        if (isDictionary && originalType is INamedTypeSymbol dictionaryType)
        {
            foreach (var typeArg in dictionaryType.TypeArguments)
            {
                var (unwrappedTypeArg, _) = typeArg.UnwrapNullable();
                if (IsExcludedType(unwrappedTypeArg, excludedTypes)) return true;
            }

            return false;
        }

        var (typeToCheck, _) = elementType.UnwrapNullable();
        return IsExcludedType(typeToCheck, excludedTypes);
    }

    private static bool IsExcludedType(ITypeSymbol type, ITypeSymbol[] excludedTypes) =>
        excludedTypes.Any(excludedType => SymbolEqualityComparer.Default.Equals(type, excludedType));

    private static string ResolveValueObjectElementType(ParameterType context)
    {
        var namedType = (INamedTypeSymbol)context.Type!;
        var valueObjectAttr = namedType.GetAnyMajalAttribute(nameof(ValueObjectAttribute));

        if (valueObjectAttr?.AttributeClass is { TypeArguments.Length: > 0 })
        {
            var resolvedType = valueObjectAttr.AttributeClass.TypeArguments[0].ToDisplayString(FullPropertyTypeFormat);
            if (context.IsNullable) resolvedType += "?";
            return resolvedType;
        }

        var underlyingValueType = GetValueObjectUnderlyingType(namedType);
        if (underlyingValueType is not null)
        {
            var resolvedType = underlyingValueType.ToDisplayString(FullPropertyTypeFormat);
            if (context.IsNullable) resolvedType += "?";
            return resolvedType;
        }

        var valueObjectFactoryMethod = FindFactoryMethod(namedType, context.Context.FactoryMethodName);
        if (valueObjectFactoryMethod is { Parameters.Length: 1 })
        {
            var resolvedType = valueObjectFactoryMethod.Parameters[0].Type.ToDisplayString(FullPropertyTypeFormat);
            if (context.IsNullable) resolvedType += "?";
            return resolvedType;
        }

        return ResolveNestedDtoElementType(context);
    }

    private static string ResolveAggregateIdType(ParameterType context)
    {
        var namedType = (INamedTypeSymbol)context.Type!;
        var idType = GetEntityIdType(namedType);

        if (string.IsNullOrWhiteSpace(idType))
            return ResolveNestedDtoElementType(context);

        if (context.IsNullable) idType += "?";
        return idType;
    }

    private static string GetEntityIdType(INamedTypeSymbol type)
    {
        var entityAttribute = type.GetAnyMajalAttribute(nameof(EntityAttribute));
        if (entityAttribute?.AttributeClass is { TypeArguments.Length: > 0 })
            return entityAttribute.AttributeClass.TypeArguments[0].ToDisplayString(FullPropertyTypeFormat);

        var entityInterface = type.AllInterfaces.FirstOrDefault(i => i.MetadataName == "IEntity`1");
        if (entityInterface is not null)
            return entityInterface.TypeArguments[0].ToDisplayString(FullPropertyTypeFormat);

        var idProperty = type.GetMembers("Id").OfType<IPropertySymbol>().FirstOrDefault();
        return idProperty?.Type is not null ? idProperty.Type.ToDisplayString(FullPropertyTypeFormat) : string.Empty;
    }

    private static string ResolveNestedDtoElementType(ParameterType context)
    {
        var eNamedType = (INamedTypeSymbol)context.Type!;
        var nestedDtoName = $"{context.Context.DtoNamePrefix}{eNamedType.Name}{context.Context.DtoNameSuffix}";
        var resolvedElementType = nestedDtoName;
        if (context.IsNullable) resolvedElementType += "?";

        if (context.Context.Collected.ContainsKey(nestedDtoName)) return resolvedElementType;

        context.Context.Collected[nestedDtoName] = default;

        var nestedContext = context.Context with
        {
            IsRoot = false,
            DtoName = nestedDtoName,
            RawDtoName = nestedDtoName,
            SourceSymbol = eNamedType
        };

        var nestedData = GetDtoData(nestedContext);

        if (nestedData == null) return resolvedElementType;
        context.Context.Collected[nestedDtoName] = nestedData.Value;

        if (nestedData.Value.DtoName == nestedDtoName) return resolvedElementType;

        var actualDtoName = nestedData.Value.DtoName;
        resolvedElementType = actualDtoName;
        if (context.IsNullable) resolvedElementType += "?";

        if (!context.Context.Collected.ContainsKey(actualDtoName))
            context.Context.Collected[actualDtoName] = nestedData.Value;

        return resolvedElementType;
    }

    private static List<IMethodSymbol> FindFactoryMethodsInDerivedTypes(INamedTypeSymbol symbol, string methodName,
        Compilation compilation)
    {
        var methods = new List<IMethodSymbol>();
        var allTypes = compilation.GetAllTypesInCompilation().ToArray();

        foreach (var derivedType in allTypes)
        {
            if (!derivedType.IsSymbolDerivedFrom(symbol)) continue;
            if (FindFactoryMethod(derivedType, methodName) is { } method) methods.Add(method);
        }

        return methods;
    }

    private static IMethodSymbol? FindFactoryMethod(INamedTypeSymbol symbol, string factoryMethodName)
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

    private static bool IsValueObjectType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return false;

        var implementsValueObject = typeSymbol.Interfaces.Any(i =>
            i.MetadataName == "IValueObject" ||
            i.MetadataName.StartsWith("IValueObject`", StringComparison.Ordinal));

        return implementsValueObject || typeSymbol.HasAnyMajaAttribute(nameof(ValueObjectAttribute));
    }

    private static bool IsEntityType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return false;

        var implementsEntity = typeSymbol.Interfaces.Any(i =>
            i.MetadataName.StartsWith("IEntity`", StringComparison.Ordinal));

        var hasEntityAttribute = typeSymbol.HasAnyMajaAttribute(nameof(EntityAttribute));
        return implementsEntity || hasEntityAttribute;
    }

    private static bool IsAggregateType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return false;

        var implementsAggregate = typeSymbol.Interfaces.Any(i =>
            i.MetadataName.StartsWith("IAggregate`", StringComparison.Ordinal));

        return implementsAggregate || typeSymbol.HasAnyMajaAttribute(nameof(AggregateAttribute));
    }

    private static ITypeSymbol? GetValueObjectUnderlyingType(INamedTypeSymbol namedType)
    {
        var genericValueObject = namedType.Interfaces
            .FirstOrDefault(i => i.MetadataName.StartsWith("IValueObject`", StringComparison.Ordinal));

        return genericValueObject?.TypeArguments.FirstOrDefault();
    }

    private static string? FormatXmlDocs(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return null;

        var lines = xml!.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var docLines = lines.Where(l => !l.TrimStart().StartsWith("<member") && !l.TrimStart().StartsWith("</member"));
        var formatted = string.Join("\n", docLines.Select(l => "/// " + l.TrimStart()));

        return string.IsNullOrWhiteSpace(formatted) ? null : formatted;
    }

    private static string? ExtractSummary(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return null;

        var match = Regex.Match(xml!, "<summary>(.*?)</summary>", RegexOptions.Singleline);
        if (!match.Success) return null;

        var content = match.Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(content)) return null;

        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        return $"/// <summary>\n{string.Join("\n", lines.Select(l => "/// " + l.Trim()))}\n/// </summary>";
    }

    private static string? ExtractParamDoc(string? xml, string paramName)
    {
        if (string.IsNullOrWhiteSpace(xml)) return null;

        var match = Regex.Match(xml!, $"""<param name="{paramName}">(.*?)</param>""", RegexOptions.Singleline);
        if (!match.Success) return null;

        var content = match.Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(content)) return null;

        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        return $"/// <summary>\n{string.Join("\n", lines.Select(l => "/// " + l.Trim()))}\n/// </summary>";
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpperInvariant(input[0]) + input.Substring(1);
    }
}