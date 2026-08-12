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

    public enum ReconstructKind
    {
        Direct,
        ValueObject,
        FlattenedValueObject,
        NestedType
    }

    public readonly record struct FlattenedArgument(
        string SubFactoryParameterName,
        string DtoPropertyName
    );

    public readonly record struct FactoryArgument(
        string FactoryParameterName,
        ReconstructKind Kind,
        string? DtoPropertyName = null,
        string? TargetTypeName = null,
        bool IsCollection = false,
        string CollectionConversionKind = "",
        bool IsNullable = false,
        EquatableList<FlattenedArgument>? FlattenedArguments = null
    );

    public readonly record struct DtoData
    {
        public string Namespace { get; }
        public string DtoName { get; }
        public string RawDtoName { get; }
        public string? BaseDtoName { get; init; }
        public string? XmlDocs { get; }
        public bool IsRecord { get; }
        public Accessibility Accessibility { get; init; }
        public EquatableList<DtoData> NestedDtos { get; }
        public EquatableList<ParameterData> Parameters { get; init; }
        public EquatableList<DerivedTypeInfo> DerivedTypes { get; }
        public EquatableList<string> ParentTypeDeclarations { get; }
        public string? SourceTypeName { get; }
        public string? SourceSimpleName { get; }
        public string? FactoryMethodName { get; }
        public EquatableList<FactoryArgument>? ReconstructionArguments { get; }

        public DtoData(string @namespace, string dtoName, string rawDtoName, string[] parentTypeDeclarations,
            Accessibility accessibility, string? xmlDocs, string? baseDtoName, bool isRecord,
            DerivedTypeInfo[] derivedTypes, ParameterData[] parameters, DtoData[] nestedDtos,
            string? sourceTypeName = null, string? sourceSimpleName = null, string? factoryMethodName = null,
            FactoryArgument[]? reconstructionArguments = null)
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
            SourceTypeName = sourceTypeName;
            SourceSimpleName = sourceSimpleName;
            FactoryMethodName = factoryMethodName;
            ReconstructionArguments = reconstructionArguments is null
                ? null
                : new EquatableList<FactoryArgument>(reconstructionArguments);
        }
    }

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
        Dictionary<string, DtoData> CollectedDto,
        Dictionary<string, bool>? FlattenConfigs = null,
        ITypeSymbol[]? ExcludedTypes = null,
        string[]? ExcludedProperties = null!,
        Dictionary<string, string[]>? ExcludedTypeProperties = null,
        string[]? NullableProperties = null!,
        Compilation? Compilation = null
    );

    private readonly record struct ParameterType(
        ITypeSymbol? Type,
        bool IsNullable,
        ParameterContext Context
    );

    private readonly record struct ParameterContext(
        IParameterSymbol Parameter,
        DtoContext DtoContext,
        HashSet<string> ExcludedProperties,
        string? MethodXml
    );

    private readonly record struct ParameterOutcome(
        ParameterData[] Properties,
        FactoryArgument? Reconstruction
    );

    private static ParameterOutcome? ProcessParameter(ParameterContext ctx)
    {
        var (elementType, isCollection) = ctx.Parameter.Type.GetCollectionInfo();
        var (unwrappedType, isNullable) = elementType.UnwrapNullable();

        if (unwrappedType is INamedTypeSymbol namedType && IsValueObjectType(namedType))
        {
            if (!isCollection)
            {
                var flattened = TryFlattenValueObject(ctx, namedType, isNullable);
                if (flattened is not null) return flattened;
            }

            return ProcessValueObjectParameter(ctx, namedType, isCollection, isNullable);
        }

        if (IsAggregateType(unwrappedType))
            return ProcessAggregateParameter(ctx, unwrappedType, isCollection, isNullable);

        if (IsEntityType(unwrappedType))
            return ProcessEntityParameter(ctx, unwrappedType, isCollection, isNullable);

        return ProcessDefaultParameter(ctx, unwrappedType, isCollection, isNullable);
    }

    private static ParameterOutcome? TryFlattenValueObject(ParameterContext ctx, INamedTypeSymbol type,
        bool isNullable)
    {
        if (ctx.DtoContext.FlattenConfigs is null ||
            !ctx.DtoContext.FlattenConfigs.TryGetValue(type.ToDisplayString(), out var isReversed))
            return null;

        var valObjFactory = FindFactoryMethod(type, ctx.DtoContext.FactoryMethodName);
        if (valObjFactory is not { Parameters.Length: > 1 }) return null;

        var valObjMethodXml = valObjFactory.GetDocumentationCommentXml();
        var flattened = new List<ParameterData>();
        var flattenedArgs = new List<FlattenedArgument>();
        var complete = true;

        foreach (var sp in valObjFactory.Parameters)
        {
            var (spElementType, spIsCollection) = sp.Type.GetCollectionInfo();
            var (spUnwrappedType, spIsNullable) = spElementType.UnwrapNullable();

            var spResolveContext = new ParameterType(
                spUnwrappedType,
                spIsNullable || isNullable,
                ctx
            );

            var spResolvedElementType = TryResolveScalarType(spResolveContext);
            if (spResolvedElementType is null)
            {
                complete = false;
                continue;
            }

            var combinedName = isReversed
                ? char.ToLowerInvariant(sp.Name[0]) + sp.Name.Substring(1) +
                  ToPascalCase(ctx.Parameter.Name)
                : char.ToLowerInvariant(ctx.Parameter.Name[0]) + ctx.Parameter.Name.Substring(1) +
                  ToPascalCase(sp.Name);

            if (ctx.ExcludedProperties.Contains(combinedName))
            {
                complete = false;
                continue;
            }

            var spResolvedType = spIsCollection
                ? $"{GenericsNamespace}.IEnumerable<{spResolvedElementType}>"
                : spResolvedElementType;

            var spXml = ExtractParamDoc(valObjMethodXml, sp.Name) ??
                        ExtractParamDoc(ctx.MethodXml, ctx.Parameter.Name);

            flattened.Add(new ParameterData((combinedName, spResolvedType), spIsNullable || isNullable, spXml));
            flattenedArgs.Add(new FlattenedArgument(sp.Name, ToPascalCase(combinedName)));
        }

        if (flattened.Count == 0) return null;

        var reconstruction = complete
            ? new FactoryArgument(
                ctx.Parameter.Name,
                ReconstructKind.FlattenedValueObject,
                TargetTypeName: type.ToDisplayString(FullPropertyTypeFormat),
                IsNullable: isNullable,
                FlattenedArguments: new EquatableList<FlattenedArgument>([.. flattenedArgs]))
            : (FactoryArgument?)null;

        return new ParameterOutcome([.. flattened], reconstruction);
    }

    private static ParameterOutcome? ProcessValueObjectParameter(ParameterContext ctx, INamedTypeSymbol type,
        bool isCollection, bool isNullable)
    {
        var resolveContext = new ParameterType(type, isNullable, ctx);
        var resolvedElementType = TryResolveScalarType(resolveContext);
        if (resolvedElementType is null) return null;

        var resolvedType = isCollection
            ? $"{GenericsNamespace}.IEnumerable<{resolvedElementType}>"
            : resolvedElementType;

        var propertyName = ctx.Parameter.Name;
        var paramXml = ExtractParamDoc(ctx.MethodXml, ctx.Parameter.Name);
        var dtoPropertyName = ToPascalCase(propertyName);

        var reconstruction = BuildReconstruction(ctx, resolvedElementType, dtoPropertyName, isCollection,
            isNullable, ReconstructKind.ValueObject, type.ToDisplayString(FullPropertyTypeFormat));

        return new ParameterOutcome(
            [new ParameterData((propertyName, resolvedType), isNullable, paramXml)],
            reconstruction);
    }

    private static ParameterOutcome ProcessAggregateParameter(ParameterContext ctx, ITypeSymbol unwrappedType,
        bool isCollection, bool isNullable)
    {
        var resolveContext = new ParameterType(unwrappedType, isNullable, ctx);
        var resolvedElementType = ResolveAggregateIdType(resolveContext);
        var resolvedType = isCollection
            ? $"{GenericsNamespace}.IEnumerable<{resolvedElementType}>"
            : resolvedElementType;

        var propertyName = isCollection ? $"{unwrappedType.Name}Ids" : $"{unwrappedType.Name}Id";
        var paramXml = ExtractParamDoc(ctx.MethodXml, ctx.Parameter.Name);

        return new ParameterOutcome(
            [new ParameterData((propertyName, resolvedType), isNullable, paramXml)],
            null);
    }

    private static ParameterOutcome ProcessEntityParameter(ParameterContext ctx, ITypeSymbol unwrappedType,
        bool isCollection, bool isNullable)
    {
        var resolveContext = new ParameterType(unwrappedType, isNullable, ctx);
        var resolvedElementType = ResolveNestedDtoElementType(resolveContext);
        var resolvedType = isCollection
            ? $"{GenericsNamespace}.IEnumerable<{resolvedElementType}>"
            : resolvedElementType;

        var propertyName = ctx.Parameter.Name;
        var paramXml = ExtractParamDoc(ctx.MethodXml, ctx.Parameter.Name);
        var dtoPropertyName = ToPascalCase(propertyName);

        TryGetNestedDto(resolvedElementType, ctx.DtoContext.CollectedDto, out var nestedDto);
        var reconstruction = new FactoryArgument(
            ctx.Parameter.Name, ReconstructKind.NestedType, dtoPropertyName,
            nestedDto.SourceSimpleName, isCollection,
            isCollection ? GetCollectionConversionSuffix(ctx.Parameter.Type) : "", isNullable);

        return new ParameterOutcome(
            [new ParameterData((propertyName, resolvedType), isNullable, paramXml)],
            reconstruction);
    }

    private static ParameterOutcome? ProcessDefaultParameter(ParameterContext ctx, ITypeSymbol unwrappedType,
        bool isCollection, bool isNullable)
    {
        var resolveContext = new ParameterType(unwrappedType, isNullable, ctx);
        var resolvedElementType = TryResolveScalarType(resolveContext);
        if (resolvedElementType is null) return null;

        var resolvedType = isCollection
            ? $"{GenericsNamespace}.IEnumerable<{resolvedElementType}>"
            : resolvedElementType;

        var propertyName = ctx.Parameter.Name;
        var paramXml = ExtractParamDoc(ctx.MethodXml, ctx.Parameter.Name);

        var reconstruction = BuildReconstruction(ctx, resolvedElementType, ToPascalCase(propertyName), isCollection,
            isNullable, ReconstructKind.Direct, null);

        return new ParameterOutcome(
            [new ParameterData((propertyName, resolvedType), isNullable, paramXml)],
            reconstruction);
    }

    private static string? TryResolveScalarType(ParameterType parameterType)
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

    private const string DtoAttribute = $"Majal.{nameof(DtoForAttribute<>)}`1";
    private const string OptionsAttributeName = nameof(DtoForOptionsAttribute);
    private const string FlattenGenericAttributeName = $"{nameof(FlattenDtoForAttribute<>)}`1";
    private const string ExcludeGenericAttributeName = $"{nameof(ExcludeDtoForAttribute<>)}`1";

    private const string DefaultDtoSuffix = "Dto";
    private const string DefaultFactoryMethodName = "Create";
    private const string NullablePropertyName = nameof(DtoForAttribute<>.Nullable);

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

    protected override bool Filter(SyntaxNode node, CancellationToken token) =>
        node is ClassDeclarationSyntax or RecordDeclarationSyntax;

    private readonly record struct DtoOptions(
        string FactoryMethodName,
        string DtoSuffix,
        string DtoPrefix,
        string[] ExcludedProperties,
        string[] NullableProperties);

    private readonly record struct DtoTypeConfig(
        Dictionary<string, bool>? FlattenConfigs,
        ITypeSymbol[]? ExcludedTypes,
        Dictionary<string, string[]>? ExcludedTypeProperties);

    protected override DtoData? Transform(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol dtoSymbol) return null;

        var attribute = context.Attributes
            .FirstOrDefault(a => a.AttributeClass?.MetadataName == GenericAttributeFullName);

        if (attribute?.AttributeClass?.TypeArguments.Length == 0) return null;

        if (attribute?.AttributeClass?.TypeArguments[0] is not INamedTypeSymbol sourceSymbol) return null;

        if (sourceSymbol.IsAbstract) return null;

        var compilation = context.SemanticModel.Compilation;

        var options = ReadDtoOptions(attribute, dtoSymbol.Name, compilation);
        var typeConfig = ReadDtoTypeConfig(dtoSymbol);

        var dtoContext = new DtoContext(
            IsRoot: true,
            Namespace: dtoSymbol.GetNamespace(),
            DtoName: dtoSymbol.GetTypeNameWithGenerics(),
            RawDtoName: dtoSymbol.Name,
            ParentTypeDeclarations: dtoSymbol.GetParentTypeDeclarations(),
            DtoNamePrefix: options.DtoPrefix,
            DtoNameSuffix: options.DtoSuffix,
            Accessibility: dtoSymbol.DeclaredAccessibility,
            IsRecord: dtoSymbol.IsRecord,
            SourceSymbol: sourceSymbol,
            FactoryMethodName: options.FactoryMethodName,
            CollectedDto: new Dictionary<string, DtoData>(),
            FlattenConfigs: typeConfig.FlattenConfigs,
            ExcludedTypes: typeConfig.ExcludedTypes,
            ExcludedProperties: options.ExcludedProperties,
            ExcludedTypeProperties: typeConfig.ExcludedTypeProperties,
            NullableProperties: options.NullableProperties,
            Compilation: compilation
        );

        return GetDtoData(dtoContext);
    }

    private static DtoOptions ReadDtoOptions(AttributeData attribute, string dtoSymbolName, Compilation compilation)
    {
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
            ?? dtoSymbolName;

        var assemblyExcludedPropertyNames =
            compilation.GetAssemblyDefaultValue<string[]>(OptionsAttributeName, nameof(DtoForOptionsAttribute.Exclude))
            ?? [];

        var excludedPropertyNames =
            attribute.GetNamedArgumentValue<string[]>(nameof(DtoForAttribute<>.Exclude)) ?? [];

        var assemblyNullablePropertyNames =
            compilation.GetAssemblyDefaultValue<string[]>(OptionsAttributeName, nameof(DtoForOptionsAttribute.Nullable))
            ?? [];

        var nullablePropertyNames =
            attribute.GetNamedArgumentValue<string[]>(NullablePropertyName) ?? [];

        return new DtoOptions(
            factoryMethodName,
            dtoSuffix,
            dtoPrefix,
            [.. assemblyExcludedPropertyNames, .. excludedPropertyNames],
            [.. assemblyNullablePropertyNames, .. nullablePropertyNames]);
    }

    private static DtoTypeConfig ReadDtoTypeConfig(INamedTypeSymbol dtoSymbol)
    {
        var excludedTypeProperties = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        List<ITypeSymbol>? excludedTypes = null;
        Dictionary<string, bool>? flattenConfigs = null;

        foreach (var attr in dtoSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.MetadataName == FlattenGenericAttributeName)
            {
                if (!(attr.AttributeClass?.TypeArguments.Length > 0)) continue;

                flattenConfigs ??= [];
                var targetType = attr.AttributeClass.TypeArguments[0];
                var isReversed = attr.GetNamedArgumentValue<bool?>(nameof(FlattenDtoForAttribute<>.IsReversed))
                                 ?? false;

                flattenConfigs[targetType.ToDisplayString()] = isReversed;
            }

            if (attr.AttributeClass?.MetadataName == ExcludeGenericAttributeName)
            {
                if (!(attr.AttributeClass?.TypeArguments.Length > 0)) continue;

                var excludedType = attr.AttributeClass.TypeArguments[0];
                var excludedPropertiesForType =
                    attr.GetNamedArgumentValue<string[]>(nameof(ExcludeDtoForAttribute<>.Properties)) ?? [];
                if (excludedPropertiesForType.Length == 0)
                {
                    excludedTypes ??= [];
                    excludedTypes.Add(excludedType);
                }
                else
                {
                    excludedTypeProperties[excludedType.ToDisplayString(FullPropertyTypeFormat)] =
                        excludedPropertiesForType;
                }
            }
        }

        return new DtoTypeConfig(
            flattenConfigs,
            excludedTypes?.ToArray(),
            excludedTypeProperties.Count > 0 ? excludedTypeProperties : null);
    }

    private static DtoData? GetDtoData(DtoContext context)
    {
        var createMethod = FindFactoryMethod(context.SourceSymbol, context.FactoryMethodName);

        var excludedProperties =
            new HashSet<string>(context.ExcludedProperties ?? [], StringComparer.OrdinalIgnoreCase);

        if (context.ExcludedTypeProperties?.TryGetValue(context.SourceSymbol.ToDisplayString(FullPropertyTypeFormat),
                out var typeSpecificProperties) == true)
        {
            excludedProperties.UnionWith(typeSpecificProperties);
        }

        var nullableProperties =
            new HashSet<string>(context.NullableProperties ?? [], StringComparer.OrdinalIgnoreCase);

        if (createMethod is null && context.Compilation is not null && context.SourceSymbol is { IsAbstract: true })
        {
            var derivedMethods = FindFactoryMethodsInDerivedTypes(context.SourceSymbol, context.FactoryMethodName,
                context.Compilation);

            if (derivedMethods.Count > 0)
            {
                var derivedDtos = new List<DtoData>();
                var derivedTypes = new List<DerivedTypeInfo>();

                foreach (var method in derivedMethods)
                {
                    var derivedSymbol = method.ContainingType;
                    var derivedDtoName = $"{context.DtoNamePrefix}{derivedSymbol.Name}{context.DtoNameSuffix}";

                    if (!context.CollectedDto.ContainsKey(derivedDtoName))
                    {
                        context.CollectedDto[derivedDtoName] = default;

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
                            context.CollectedDto[derivedDtoName] = updatedData;
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
                        context.CollectedDto[derivedDto.DtoName] = updatedData;
                    }
                }

                var xmlDocs = FormatXmlDocs(context.SourceSymbol.GetDocumentationCommentXml());

                var nestedDtos =
                    context.IsRoot
                        ? context.CollectedDto.Values
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
        var reconstructionArguments = new List<FactoryArgument>();
        var canReconstruct = true;

        foreach (var p in createMethod.Parameters)
        {
            if (excludedProperties.Contains(p.Name))
            {
                canReconstruct = false;
                continue;
            }

            var (elementTypeCheck, _) = p.Type.GetCollectionInfo();
            if (IsParameterExcluded(elementTypeCheck, context.ExcludedTypes))
            {
                canReconstruct = false;
                continue;
            }

            var ctx = new ParameterContext(p, context, excludedProperties, methodXml);
            var outcome = ProcessParameter(ctx);
            if (outcome is null)
            {
                canReconstruct = false;
                continue;
            }

            foreach (var result in outcome.Value.Properties)
                parameters.Add(ApplyNullable(result, nullableProperties));

            if (outcome.Value.Reconstruction is not { } reconstruction)
            {
                canReconstruct = false;
                continue;
            }

            reconstructionArguments.Add(ApplyNullableToReconstruction(reconstruction, nullableProperties));
        }

        DtoData[] nestedDtosResult =
            context.IsRoot ? [.. context.CollectedDto.Values.Where(v => !string.IsNullOrEmpty(v.DtoName))] : [];

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
            nestedDtosResult,
            context.SourceSymbol.ToDisplayString(FullPropertyTypeFormat),
            context.SourceSymbol.Name,
            context.FactoryMethodName,
            canReconstruct ? [.. reconstructionArguments] : null
        );
    }

    private static ParameterData ApplyNullable(ParameterData data, HashSet<string> nullableProperties)
    {
        var propertyName = ToPascalCase(data.Declaration.Name);
        if (!nullableProperties.Contains(propertyName)) return data;

        var type = data.Declaration.Type;
        if (!type.EndsWith("?")) type += "?";
        return data with { Declaration = (data.Declaration.Name, type), IsNullable = true };
    }

    private static FactoryArgument ApplyNullableToReconstruction(FactoryArgument reconstruction,
        HashSet<string> nullableProperties)
    {
        if (reconstruction.Kind == ReconstructKind.FlattenedValueObject) return reconstruction;
        if (reconstruction.DtoPropertyName is null) return reconstruction;
        if (reconstruction.IsNullable || !nullableProperties.Contains(reconstruction.DtoPropertyName))
            return reconstruction;

        return reconstruction with { IsNullable = true };
    }

    private static bool TryGetNestedDto(string resolvedElementType, Dictionary<string, DtoData> collectedDto,
        out DtoData nestedDto)
    {
        var key = resolvedElementType.EndsWith("?")
            ? resolvedElementType.Substring(0, resolvedElementType.Length - 1)
            : resolvedElementType;
        return collectedDto.TryGetValue(key, out nestedDto);
    }

    private static FactoryArgument BuildReconstruction(ParameterContext ctx, string resolvedElementType,
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

    private static string GetCollectionConversionSuffix(ITypeSymbol parameterType)
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

    private static bool IsParameterExcluded(ITypeSymbol elementType, ITypeSymbol[]? excludedTypes)
    {
        if (excludedTypes is null || excludedTypes.Length == 0) return false;
        var (typeToCheck, _) = elementType.UnwrapNullable();
        return excludedTypes.Any(excludedType => SymbolEqualityComparer.Default.Equals(typeToCheck, excludedType));
    }

    private static string ResolveValueObjectElementType(ParameterType parameterType)
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

    private static string ResolveAggregateIdType(ParameterType parameterType)
    {
        var namedType = (INamedTypeSymbol)parameterType.Type!;
        var idType = GetEntityIdType(namedType, parameterType.Context.DtoContext.Compilation);

        if (string.IsNullOrWhiteSpace(idType))
            return ResolveNestedDtoElementType(parameterType);

        if (parameterType.IsNullable) idType += "?";
        return idType;
    }

    private static string GetEntityIdType(INamedTypeSymbol type, Compilation? compilation)
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

    private static string ResolveNestedDtoElementType(ParameterType parameterType)
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

        var nestedData = GetDtoData(nestedContext);

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

        var implementsEntity = typeSymbol.AllInterfaces.Any(i =>
            i.MetadataName.StartsWith("IEntity`", StringComparison.Ordinal));

        var hasEntityAttribute = typeSymbol.HasAnyMajaAttribute(nameof(EntityAttribute));
        return implementsEntity || hasEntityAttribute;
    }

    private static bool IsAggregateType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return false;

        var implementsAggregate = typeSymbol.AllInterfaces.Any(i =>
            i.MetadataName.StartsWith("IAggregate`", StringComparison.Ordinal));

        return implementsAggregate || typeSymbol.HasAnyMajaAttribute(nameof(AggregateAttribute));
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