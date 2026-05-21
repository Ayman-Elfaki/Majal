using System.Text;
using System.Text.RegularExpressions;
using Majal.Common.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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
        string Name,
        string ResolvedType,
        bool IsNullable,
        string? XmlDocs = null
    );

    public readonly record struct DerivedTypeInfo(
        string DtoName,
        string Discriminator
    );

    public readonly record struct DtoData
    {
        public string Namespace { get; }
        public string DtoName { get; }
        public string RawDtoName { get; }
        public string? BaseDtoName { get; init; }
        public string? XmlDocs { get; }
        public bool IsRecord { get; }
        public Accessibility Accessibility { get; }
        public EquatableList<DtoData> NestedDtos { get; }
        public EquatableList<ParameterData> Parameters { get; }
        public EquatableList<DerivedTypeInfo> DerivedTypes { get; }
        public EquatableList<string> ParentTypeDeclarations { get; }

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

    private readonly record struct TypeResolveContext(
        ITypeSymbol? Type,
        bool IsNullable,
        bool IsDictionary,
        string DtoNamePrefix,
        string DtoNameSuffix,
        Accessibility Accessibility,
        bool IsRecord,
        string Namespace,
        string DefaultMethodName,
        Dictionary<string, DtoData> Collected,
        string[] ParentTypeDeclarations,
        Dictionary<string, bool>? FlattenConfigs = null,
        ITypeSymbol[]? ExcludedTypes = null,
        Compilation? Compilation = null
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
        string DefaultMethodName,
        Dictionary<string, DtoData> Collected,
        Dictionary<string, bool>? FlattenConfigs = null,
        ITypeSymbol[]? ExcludedTypes = null,
        string[]? ExcludedProperties = null!,
        Compilation? Compilation = null
    );

    private interface IParameterTypeResolver
    {
        bool CanHandle(TypeResolveContext context);

        string Resolve(TypeResolveContext context);
    }

    private sealed class ValueObjectTypeResolver : IParameterTypeResolver
    {
        public bool CanHandle(TypeResolveContext context) =>
            context.Type is not null && IsValueObjectType(context.Type);

        public string Resolve(TypeResolveContext context) =>
            ResolveValueObjectElementType(context);
    }

    private sealed class EntityTypeResolver : IParameterTypeResolver
    {
        public bool CanHandle(TypeResolveContext context) =>
            context.Type is not null && IsEntityType(context.Type) && !IsAggregateType(context.Type);

        public string Resolve(TypeResolveContext context) =>
            ResolveNestedDtoElementType(context);
    }

    private sealed class DefaultTypeResolver : IParameterTypeResolver
    {
        public bool CanHandle(TypeResolveContext context) =>
            context.Type is not null && (context.Type.TypeKind is TypeKind.Enum or TypeKind.TypeParameter ||
                                         context.Type.AllInterfaces.Any(i => i.Name == "IParsable") ||
                                         context.IsDictionary);

        public string Resolve(TypeResolveContext context)
        {
            var type = context.Type!;
            var resolvedType = type.ToDisplayString(FullPropertyTypeFormat);
            if (context.IsNullable) resolvedType += "?";
            return resolvedType;
        }
    }

    private static readonly IParameterTypeResolver[] ParameterTypeResolvers =
    [
        new ValueObjectTypeResolver(),
        new EntityTypeResolver(),
        new DefaultTypeResolver()
    ];

    private const string DtoAttribute = $"Majal.{nameof(DtoForAttribute<>)}`1";
    private const string OptionsAttributeName = $"Majal.{nameof(DtoForOptionsAttribute)}";
    private const string FlattenGenericAttributeName = $"{nameof(FlattenDtoForAttribute<>)}`1";
    private const string ExcludeGenericAttributeName = $"{nameof(ExcludeDtoForAttribute<>)}`1";
    private const string EntityAttributeName = nameof(EntityAttribute);
    private const string EntityOptionsAttributeName = nameof(EntityOptionsAttribute);

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
        if (data.ParentTypeDeclarations.Count == 0)
        {
            return $"{data.RawDtoName}.g.cs";
        }

        var parentNames =
            string.Join("_", data.ParentTypeDeclarations.Select(SanitizeParentTypeDeclarationForFileName));
        return $"{parentNames}_{data.RawDtoName}.g.cs";
    }

    private static string SanitizeParentTypeDeclarationForFileName(string declaration)
    {
        var typeName = declaration.Split([' '], StringSplitOptions.RemoveEmptyEntries).Last();
        return typeName
            .Replace('<', '_')
            .Replace('>', '_')
            .Replace(',', '_')
            .Replace(" ", "_")
            .Replace(".", "_");
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

        var assemblyAttr = context.SemanticModel.Compilation.Assembly.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == OptionsAttributeName);

        var assemblyDefaultName =
            assemblyAttr.GetNamedArgumentValue<string>(nameof(DtoForOptionsAttribute.FactoryMethodName));

        var finalDefaultName = assemblyDefaultName ?? DefaultFactoryMethodName;

        var factoryMethodName =
            attribute.GetNamedArgumentValue<string>(nameof(DtoForAttribute<>.FactoryMethodName)) ??
            finalDefaultName;

        var assemblyDefaultSuffix =
            assemblyAttr.GetNamedArgumentValue<string>(nameof(DtoForOptionsAttribute.Suffix));

        var finalDefaultSuffix = assemblyDefaultSuffix ?? DefaultDtoSuffix;

        var factoryDtoSuffix =
            attribute.GetNamedArgumentValue<string>(nameof(DtoForAttribute<>.Suffix)) ??
            finalDefaultSuffix;

        var assemblyDefaultPrefix =
            assemblyAttr.GetNamedArgumentValue<string>(nameof(DtoForOptionsAttribute.Prefix));

        var finalDefaultPrefix = assemblyDefaultPrefix ?? dtoSymbol.Name;

        var factoryDtoPrefix =
            attribute.GetNamedArgumentValue<string>(nameof(DtoForAttribute<>.Prefix)) ??
            finalDefaultPrefix;

        var assemblyExcludedPropertyNames =
            assemblyAttr.GetNamedArgumentValue<string[]>(nameof(DtoForOptionsAttribute.Exclude)) ?? [];

        var factoryExcludedPropertyNames =
            attribute.GetNamedArgumentValue<string[]>(nameof(DtoForAttribute<>.Exclude)) ?? [];

        var nestedDtos = new Dictionary<string, DtoData>();

        List<ITypeSymbol>? excludedTypes = null;
        Dictionary<string, bool>? flattenConfigs = null;

        var attributes = dtoSymbol.GetAttributes().ToArray();

        foreach (var flattenAttr in
                 attributes.Where(a => a.AttributeClass?.MetadataName == FlattenGenericAttributeName))
        {
            if (!(flattenAttr.AttributeClass?.TypeArguments.Length > 0)) continue;

            flattenConfigs ??= [];
            var targetType = flattenAttr.AttributeClass.TypeArguments[0];
            var isReversed = flattenAttr.GetNamedArgumentValue<bool?>(nameof(FlattenDtoForAttribute<>.IsReversed))
                             ?? false;

            flattenConfigs[targetType.ToDisplayString()] = isReversed;
        }

        foreach (var excludeAttr in
                 attributes.Where(a => a.AttributeClass?.MetadataName == ExcludeGenericAttributeName))
        {
            if (!(excludeAttr.AttributeClass?.TypeArguments.Length > 0)) continue;

            excludedTypes ??= [];
            excludedTypes.Add(excludeAttr.AttributeClass.TypeArguments[0]);
        }

        var dtoContext = new DtoContext(
            IsRoot: true,
            Namespace: dtoSymbol.GetNamespace(),
            DtoName: dtoSymbol.GetTypeNameWithGenerics(),
            RawDtoName: dtoSymbol.Name,
            ParentTypeDeclarations: GetParentTypeDeclarations(dtoSymbol),
            DtoNamePrefix: factoryDtoPrefix,
            DtoNameSuffix: factoryDtoSuffix,
            Accessibility: dtoSymbol.DeclaredAccessibility,
            IsRecord: dtoSymbol.IsRecord,
            SourceSymbol: sourceSymbol,
            DefaultMethodName: finalDefaultName,
            FactoryMethodName: factoryMethodName,
            Collected: nestedDtos,
            FlattenConfigs: flattenConfigs,
            ExcludedTypes: excludedTypes?.ToArray(),
            ExcludedProperties: [.. assemblyExcludedPropertyNames, .. factoryExcludedPropertyNames],
            Compilation: context.SemanticModel.Compilation
        );

        return GetDtoData(dtoContext);
    }

    private static DtoData? GetDtoData(DtoContext context)
    {
        var isRoot = context.IsRoot;
        var isRecord = context.IsRecord;
        var dtoName = context.DtoName;
        var @namespace = context.Namespace;
        var rawDtoName = context.RawDtoName;
        var dtoNamePrefix = context.DtoNamePrefix;
        var dtoNameSuffix = context.DtoNameSuffix;
        var accessibility = context.Accessibility;
        var sourceSymbol = context.SourceSymbol;
        var collected = context.Collected;
        var compilation = context.Compilation;
        var factoryMethodName = context.FactoryMethodName;
        var defaultMethodName = context.DefaultMethodName;

        var createMethod = FindFactoryMethod(sourceSymbol, factoryMethodName);
        var aggregateIdParameter = GetAggregateIdParameter(sourceSymbol, createMethod, compilation);

        var excludedPropertyNames =
            new HashSet<string>(context.ExcludedProperties ?? [], StringComparer.OrdinalIgnoreCase);

        if (createMethod is null && compilation is not null && sourceSymbol is { IsAbstract: true })
        {
            var derivedMethods = FindFactoryMethodsInDerivedTypes(sourceSymbol, factoryMethodName, compilation);

            if (derivedMethods.Count > 0)
            {
                var derivedDtos = new List<DtoData>();
                var derivedTypes = new List<DerivedTypeInfo>();

                foreach (var method in derivedMethods)
                {
                    var derivedSymbol = method.ContainingType;
                    var derivedDtoName = $"{dtoNamePrefix}{derivedSymbol.Name}{dtoNameSuffix}";

                    if (!collected.ContainsKey(derivedDtoName))
                    {
                        collected[derivedDtoName] = default;

                        var derivedContext = context with
                        {
                            DtoName = derivedDtoName,
                            RawDtoName = derivedDtoName,
                            DtoNamePrefix = dtoNamePrefix,
                            DtoNameSuffix = dtoNameSuffix,
                            SourceSymbol = derivedSymbol,
                            IsRoot = false,
                            ExcludedTypes = context.ExcludedTypes
                        };

                        var derivedData = GetDtoData(derivedContext);

                        if (derivedData != null)
                        {
                            var updatedData = derivedData.Value with { BaseDtoName = dtoName };
                            collected[derivedDtoName] = updatedData;
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
                        ParameterData[] uniqueParameters =
                        [
                            ..derivedDto.Parameters
                                .Where(p => commonParameters.All(cp =>
                                    cp.Name != p.Name || cp.ResolvedType != p.ResolvedType))
                        ];

                        var updatedData = new DtoData(derivedDto.Namespace, derivedDto.DtoName, derivedDto.RawDtoName,
                            [..derivedDto.ParentTypeDeclarations], accessibility, derivedDto.XmlDocs,
                            derivedDto.BaseDtoName, derivedDto.IsRecord, [..derivedDto.DerivedTypes], uniqueParameters,
                            [..derivedDto.NestedDtos]
                        );

                        derivedDtos[i] = updatedData;
                        collected[derivedDto.DtoName] = updatedData;
                    }
                }

                var xmlDocs = FormatXmlDocs(sourceSymbol.GetDocumentationCommentXml());

                DtoData[] nestedDtos = isRoot
                    ? [.. collected.Values.Where(v => !string.IsNullOrEmpty(v.DtoName) && v.DtoName != dtoName)]
                    : [];

                return new DtoData(@namespace, dtoName, rawDtoName, context.ParentTypeDeclarations, accessibility,
                    xmlDocs, null, isRecord,
                    [.. derivedTypes], commonParameters, nestedDtos);
            }
        }

        if (createMethod is null) return null;

        var methodXml = createMethod.GetDocumentationCommentXml();
        var parameters = new List<ParameterData>();

        if (aggregateIdParameter is not null)
        {
            parameters.Add(aggregateIdParameter.Value);
        }

        foreach (var p in createMethod.Parameters)
        {
            if (excludedPropertyNames.Contains(p.Name))
                continue;

            var (elementType, isCollection, isDictionary) = p.Type.GetCollectionInfo();
            var (unwrappedType, isNullable) = elementType.UnwrapNullable();

            if (ShouldExcludeParameter(p.Type, elementType, isCollection, isDictionary, context.ExcludedTypes))
                continue;

            if (!isCollection && unwrappedType is INamedTypeSymbol type && IsValueObjectType(type) &&
                context.FlattenConfigs is not null &&
                context.FlattenConfigs.TryGetValue(type.ToDisplayString(), out var isReversed))
            {
                var valObjFactory = FindFactoryMethod(type, defaultMethodName);
                if (valObjFactory is { Parameters.Length: > 1 })
                {
                    var valObjMethodXml = valObjFactory.GetDocumentationCommentXml();
                    foreach (var sp in valObjFactory.Parameters)
                    {
                        var (spElementType, spIsCollection, spIsDictionary) = sp.Type.GetCollectionInfo();
                        var (spUnwrappedType, spIsNullable) = spElementType.UnwrapNullable();

                        var spResolveContext = new TypeResolveContext(spUnwrappedType, spIsNullable || isNullable,
                            spIsDictionary, dtoNamePrefix, dtoNameSuffix, accessibility, isRecord, @namespace,
                            defaultMethodName, collected, context.ParentTypeDeclarations, context.FlattenConfigs,
                            context.ExcludedTypes,
                            compilation
                        );

                        var spResolver = ParameterTypeResolvers.FirstOrDefault(r => r.CanHandle(spResolveContext));
                        if (spResolver is null) continue;

                        var spResolvedElementType = spResolver.Resolve(spResolveContext);
                        var spResolvedType = spIsCollection ? $"{spResolvedElementType}[]" : spResolvedElementType;

                        var combinedName = isReversed
                            ? char.ToLowerInvariant(sp.Name[0]) + sp.Name.Substring(1) + ToPascalCase(p.Name)
                            : char.ToLowerInvariant(p.Name[0]) + p.Name.Substring(1) + ToPascalCase(sp.Name);

                        if (excludedPropertyNames.Contains(combinedName))
                            continue;

                        var spXml = ExtractParamDoc(valObjMethodXml, sp.Name) ?? ExtractParamDoc(methodXml, p.Name);

                        parameters.Add(
                            new ParameterData(combinedName, spResolvedType, spIsNullable || isNullable, spXml)
                        );
                    }

                    continue;
                }
            }

            var resolveContext = new TypeResolveContext(unwrappedType, isNullable, isDictionary, dtoNamePrefix,
                dtoNameSuffix, accessibility, isRecord, @namespace, defaultMethodName, collected,
                context.ParentTypeDeclarations, context.FlattenConfigs, context.ExcludedTypes, compilation
            );

            var resolver = ParameterTypeResolvers.FirstOrDefault(r => r.CanHandle(resolveContext));
            if (resolver is null) continue;

            var resolvedElementType = resolver.Resolve(resolveContext);
            var resolvedType = isCollection ? $"{resolvedElementType}[]" : resolvedElementType;

            var paramXml = ExtractParamDoc(methodXml, p.Name);
            parameters.Add(new ParameterData(p.Name, resolvedType, isNullable, paramXml));
        }

        DtoData[] nestedDtosResult = isRoot ? [.. collected.Values.Where(v => !string.IsNullOrEmpty(v.DtoName))] : [];
        var xmlDocsResult = ExtractSummary(methodXml) ?? FormatXmlDocs(sourceSymbol.GetDocumentationCommentXml());

        return new DtoData(@namespace, dtoName, rawDtoName, context.ParentTypeDeclarations, accessibility,
            xmlDocsResult, null, isRecord,
            [], [.. parameters], nestedDtosResult
        );
    }

    private static ParameterData[] GetCommonParameters(IEnumerable<DtoData> dtos)
    {
        var dtoArray = dtos as DtoData[] ?? [.. dtos];
        if (dtoArray.Length == 0) return [];

        return
        [
            .. dtoArray[0].Parameters.Where(p =>
                dtoArray.Skip(1).All(d => d.Parameters.Any(o => o.Name == p.Name && o.ResolvedType == p.ResolvedType))
            )
        ];
    }

    private static bool ShouldExcludeParameter(ITypeSymbol originalType, ITypeSymbol elementType, bool isCollection,
        bool isDictionary, ITypeSymbol[]? excludedTypes)
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

    private static bool IsExcludedType(ITypeSymbol type, ITypeSymbol[] excludedTypes)
    {
        return excludedTypes.Any(excludedType => SymbolEqualityComparer.Default.Equals(type, excludedType));
    }

    private static string ResolveValueObjectElementType(TypeResolveContext context)
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

        var valueObjectFactoryMethod = FindFactoryMethod(namedType, context.DefaultMethodName);
        if (valueObjectFactoryMethod is { Parameters.Length: 1 })
        {
            var resolvedType = valueObjectFactoryMethod.Parameters[0].Type.ToDisplayString(FullPropertyTypeFormat);
            if (context.IsNullable) resolvedType += "?";
            return resolvedType;
        }

        return ResolveNestedDtoElementType(context);
    }

    private static string ResolveNestedDtoElementType(TypeResolveContext context)
    {
        var eNamedType = (INamedTypeSymbol)context.Type!;
        var nestedDtoName = $"{context.DtoNamePrefix}{eNamedType.Name}{context.DtoNameSuffix}";
        var resolvedElementType = nestedDtoName;
        if (context.IsNullable) resolvedElementType += "?";

        if (context.Collected.ContainsKey(nestedDtoName)) return resolvedElementType;

        context.Collected[nestedDtoName] = default;

        var nestedContext = new DtoContext(
            IsRoot: false,
            DtoName: nestedDtoName,
            RawDtoName: nestedDtoName,
            ParentTypeDeclarations: context.ParentTypeDeclarations,
            SourceSymbol: eNamedType,
            IsRecord: context.IsRecord,
            Namespace: context.Namespace,
            DtoNamePrefix: context.DtoNamePrefix,
            DtoNameSuffix: context.DtoNameSuffix,
            Accessibility: context.Accessibility,
            FactoryMethodName: context.DefaultMethodName,
            DefaultMethodName: context.DefaultMethodName,
            Collected: context.Collected,
            FlattenConfigs: context.FlattenConfigs,
            ExcludedTypes: context.ExcludedTypes,
            Compilation: context.Compilation
        );

        var nestedData = GetDtoData(nestedContext);

        if (nestedData == null) return resolvedElementType;
        context.Collected[nestedDtoName] = nestedData.Value;

        if (nestedData.Value.DtoName == nestedDtoName) return resolvedElementType;

        var actualDtoName = nestedData.Value.DtoName;
        resolvedElementType = actualDtoName;
        if (context.IsNullable) resolvedElementType += "?";

        if (!context.Collected.ContainsKey(actualDtoName))
            context.Collected[actualDtoName] = nestedData.Value;

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

        var implementsValueObject = typeSymbol.AllInterfaces.Any(i =>
            i.MetadataName == "IValueObject" ||
            i.MetadataName.StartsWith("IValueObject`", StringComparison.Ordinal));

        return implementsValueObject || typeSymbol.HasAnyMajaAttribute(nameof(ValueObjectAttribute));
    }

    private static bool IsEntityType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return false;

        for (var current = typeSymbol; current != null; current = current.BaseType)
        {
            var implementsEntity = current.AllInterfaces.Any(i =>
                i.MetadataName.StartsWith("IEntity`", StringComparison.Ordinal));

            if (implementsEntity || current.HasAnyMajaAttribute(nameof(EntityAttribute)))
                return true;
        }

        return false;
    }

    private static bool IsAggregateType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return false;

        for (var current = typeSymbol; current != null; current = current.BaseType)
        {
            var implementsAggregate = current.AllInterfaces.Any(i =>
                i.MetadataName.StartsWith("IAggregate`", StringComparison.Ordinal));

            if (implementsAggregate || current.HasAnyMajaAttribute(nameof(AggregateAttribute)))
                return true;
        }

        return false;
    }

    private static ITypeSymbol? GetAggregateIdType(INamedTypeSymbol sourceSymbol, Compilation? compilation)
    {
        var idType = GetEntityIdType(sourceSymbol, compilation);
        if (idType is not null) return idType;

        for (var current = sourceSymbol; current != null; current = current.BaseType)
        {
            var idProperty = current.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(p => p.Name == "Id");
            if (idProperty is not null)
                return idProperty.Type;
        }

        return null;
    }

    private static ITypeSymbol? GetEntityIdType(INamedTypeSymbol symbol, Compilation? compilation)
    {
        for (var current = symbol; current != null; current = current.BaseType)
        {
            var entityAttribute = current.GetAnyMajalAttribute(EntityAttributeName);
            if (entityAttribute?.AttributeClass is { TypeArguments.Length: > 0 })
                return entityAttribute.AttributeClass.TypeArguments[0];

            if (entityAttribute is not null)
            {
                var defaultType = GetDefaultEntityIdType(compilation);
                if (defaultType is not null)
                    return defaultType;

                return compilation?.GetSpecialType(SpecialType.System_Int32);
            }

            var entityInterface = current.AllInterfaces.FirstOrDefault(i =>
                i.MetadataName.StartsWith("IEntity`", StringComparison.Ordinal));

            if (entityInterface is not null && entityInterface.TypeArguments.Length > 0)
                return entityInterface.TypeArguments[0];
        }

        return null;
    }

    private static ITypeSymbol? GetDefaultEntityIdType(Compilation? compilation)
    {
        if (compilation is null) return null;

        foreach (var attribute in compilation.Assembly.GetAttributes())
        {
            if (attribute.AttributeClass?.Name != EntityOptionsAttributeName ||
                attribute.AttributeClass.ContainingNamespace.ToDisplayString() != "Majal")
            {
                continue;
            }

            if (attribute.NamedArguments.FirstOrDefault(a => a.Key == nameof(EntityOptionsAttribute.DefaultIdType))
                    .Value.Value is ITypeSymbol type)
                return type;
        }

        return null;
    }

    private static ParameterData? GetAggregateIdParameter(INamedTypeSymbol sourceSymbol, IMethodSymbol? createMethod,
        Compilation? compilation)
    {
        if (createMethod is null) return null;
        if (!IsAggregateType(sourceSymbol)) return null;
        if (createMethod.Parameters.Any(p => string.Equals(p.Name, "id", StringComparison.OrdinalIgnoreCase)))
            return null;

        var idType = GetAggregateIdType(sourceSymbol, compilation);
        if (idType is null) return null;

        var (unwrappedType, isNullable) = idType.UnwrapNullable();
        var resolvedType = unwrappedType.ToDisplayString(FullPropertyTypeFormat);
        if (isNullable) resolvedType += "?";

        return new ParameterData("id", resolvedType, isNullable);
    }

    private static ITypeSymbol? GetValueObjectUnderlyingType(INamedTypeSymbol namedType)
    {
        var genericValueObject = namedType.AllInterfaces
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