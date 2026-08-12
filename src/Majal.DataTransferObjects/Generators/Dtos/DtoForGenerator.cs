using System.Text;
using System.Text.RegularExpressions;
using Majal.Common.Abstractions;
using Majal.Generators.Dtos.Models;
using Majal.Generators.Dtos.ParameterHandlers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Majal.Generators.Dtos.ParameterHandlers.ParameterResolution;

namespace Majal.Generators.Dtos;

[Generator]
public sealed class DtoForGenerator : BaseGenerator<DtoData>
{
    private static readonly IParameterHandler[] Handlers =
    [
        new ValueObjectParameterHandler(),
        new AggregateParameterHandler(),
        new EntityParameterHandler(),
        new DefaultParameterHandler()
    ];

    private static ParameterOutcome? ProcessParameter(ParameterContext ctx)
    {
        var (elementType, isCollection) = ctx.Parameter.Type.GetCollectionInfo();
        var (unwrappedType, isNullable) = elementType.UnwrapNullable();

        var handler = Handlers.First(h => h.IsApplicable(unwrappedType));
        return handler.Resolve(ctx, unwrappedType, isCollection, isNullable);
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

    internal static DtoData? GetDtoData(DtoContext context)
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
}