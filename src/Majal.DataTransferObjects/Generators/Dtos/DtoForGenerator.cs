using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Majal.Common.Abstractions;
using Majal.Generators.Dtos.Models;
using Majal.Generators.Dtos.ParameterHandlers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Majal.Common.Abstractions.Constants;
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

    private const string DtoAttribute = $"Majal.{nameof(DtoForAttribute<object>)}`1";

    protected override string AttributeFullName => DtoAttribute;
    protected override string GenericAttributeFullName => $"{nameof(DtoForAttribute<object>)}`1";

    protected override void Generate(SourceProductionContext context, DtoData data)
    {
        if (data.Diagnostics is not null)
        {
            foreach (var diag in data.Diagnostics)
            {
                var descriptor = new DiagnosticDescriptor(
                    diag.Id,
                    diag.Title,
                    diag.Message,
                    "Majal.DTO",
                    diag.Severity,
                    isEnabledByDefault: true);

                context.ReportDiagnostic(Diagnostic.Create(descriptor, diag.Location ?? Location.None));
            }
        }

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

        var compilation = context.SemanticModel.Compilation;

        var dtoContext = DtoConfigReader.CreateContext(dtoSymbol, sourceSymbol, attribute, compilation);

        dtoContext.Graph.Register(sourceSymbol, dtoContext.DtoName);
        var rootData = GetDtoData(dtoContext);
        if (rootData is { } data) dtoContext.Graph.Complete(sourceSymbol, data);
        return rootData;
    }

    private static string? GetTranslatableLocaleType(INamedTypeSymbol sourceSymbol, Compilation? compilation)
    {
        var translatableAttribute = sourceSymbol.GetAnyMajalAttribute(nameof(TranslatableAttribute));
        if (translatableAttribute?.AttributeClass is { TypeArguments.Length: > 0 })
            return translatableAttribute.AttributeClass.TypeArguments[0]
                .ToDisplayString(FullPropertyTypeFormat);

        if (translatableAttribute is null) return null;

        var defaultLocaleType = compilation?.GetAssemblyDefaultValue<INamedTypeSymbol>(
            nameof(TranslatableOptionsAttribute), nameof(TranslatableOptionsAttribute.DefaultLocaleType));

        return defaultLocaleType?.ToDisplayString(FullPropertyTypeFormat) ?? StringType;
    }

    internal static DtoData? GetDtoData(DtoContext context)
    {
        var createMethod = FindFactoryMethod(context.SourceSymbol, context.FactoryMethodName);

        var excludedProperties =
            new HashSet<string>(context.ExcludedProperties ?? [], StringComparer.OrdinalIgnoreCase);

        if (context.ExcludedTypeProperties != null)
        {
            if (context.ExcludedTypeProperties.TryGetValue(context.SourceSymbol.Name, out var typeSpecificProperties) ||
                context.ExcludedTypeProperties.TryGetValue(context.SourceSymbol.ToDisplayString(FullPropertyTypeFormat), out typeSpecificProperties))
            {
                excludedProperties.UnionWith(typeSpecificProperties);
            }
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

                    EquatableList<SourceMemberMap>? dtForwardMappings = null;

                    if (context.Graph.Register(derivedSymbol, derivedDtoName))
                    {
                        var derivedContext = context with
                        {
                            IsRoot = false,
                            DtoName = derivedDtoName,
                            RawDtoName = derivedDtoName,
                            SourceSymbol = derivedSymbol,
                            DtoSymbol = null
                        };

                        var derivedData = GetDtoData(derivedContext);

                        if (derivedData != null)
                        {
                            var updatedData = derivedData.Value with { BaseDtoName = context.DtoName };
                            context.Graph.Complete(derivedSymbol, updatedData);
                            derivedDtos.Add(updatedData);
                            dtForwardMappings = updatedData.ForwardMappings;
                        }
                    }
                    else if (context.Graph.TryGetNode(derivedSymbol, out var existingNode) && existingNode.Data.HasValue)
                    {
                        dtForwardMappings = existingNode.Data.Value.ForwardMappings;
                    }

                    derivedTypes.Add(new DerivedTypeInfo(
                        derivedDtoName,
                        derivedSymbol.Name,
                        derivedSymbol.ToDisplayString(FullPropertyTypeFormat),
                        dtForwardMappings));
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
                        if (context.Graph.TryGetNode(derivedDto.DtoName, out var derivedNode))
                            context.Graph.Complete(derivedNode.SourceSymbol, updatedData);
                    }
                }

                var xmlDocs = FormatXmlDocs(context.SourceSymbol.GetDocumentationCommentXml());

                var nestedDtos =
                    context.IsRoot
                        ? context.Graph.GetCompletedDtos(context.SourceSymbol).ToArray()
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
                    nestedDtos,
                    sourceTypeName: context.SourceSymbol.ToDisplayString(FullPropertyTypeFormat),
                    sourceSimpleName: context.SourceSymbol.Name,
                    translatableLocaleType: GetTranslatableLocaleType(context.SourceSymbol, context.Compilation),
                    directions: context.Directions,
                    nameMatching: context.NameMatching
                );
            }
        }

        if (createMethod is null) return null;

        var methodXml = createMethod.GetDocumentationCommentXml();
        var translatableLocaleType = GetTranslatableLocaleType(context.SourceSymbol, context.Compilation);
        var parameters = new List<ParameterData>();
        var reconstructionArguments = new List<FactoryArgument>();
        var canReconstruct = true;

        foreach (var p in createMethod.Parameters)
        {
            if (translatableLocaleType is not null &&
                string.Equals(p.Name, "locale", StringComparison.OrdinalIgnoreCase))
            {
                var localeParameterType = p.Type.ToDisplayString(FullPropertyTypeFormat);
                if (localeParameterType != translatableLocaleType && localeParameterType != StringType)
                {
                    canReconstruct = false;
                    continue;
                }

                var requiresToString = localeParameterType != translatableLocaleType;
                reconstructionArguments.Add(new FactoryArgument(p.Name, ReconstructKind.Locale, "Locale",
                    requiresToString ? "ToString" : null));
                continue;
            }

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
            {
                parameters.Add(ApplyNullable(result, nullableProperties));
            }

            if (outcome.Value.Reconstruction is not { } reconstruction)
            {
                canReconstruct = false;
                continue;
            }

            reconstructionArguments.Add(ApplyNullableToReconstruction(reconstruction, nullableProperties));
        }

        if (context.IncludedProperties is { Length: > 0 })
        {
            var sourceMembers = ForwardMappingBuilder.GetAllSourceMembers(context.SourceSymbol);
            foreach (var incProp in context.IncludedProperties)
            {
                if (parameters.Any(p => string.Equals(p.Declaration.Name, incProp, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var matched = sourceMembers.OfType<IPropertySymbol>()
                    .FirstOrDefault(m => string.Equals(m.Name, incProp, StringComparison.OrdinalIgnoreCase));

                if (matched != null)
                {
                    var (_, isTypeNullable) = matched.Type.UnwrapNullable();
                    var isNullable = matched.NullableAnnotation == NullableAnnotation.Annotated ||
                                     isTypeNullable ||
                                     nullableProperties.Contains(matched.Name);
                    var typeString = matched.Type.ToDisplayString(FullPropertyTypeFormat);
                    var xml = FormatXmlDocs(matched.GetDocumentationCommentXml());
                    parameters.Add(new ParameterData((matched.Name, typeString), isNullable, xml));
                }
                else if (ResolveSynthesizedMember(context.SourceSymbol, incProp, context.Compilation, nullableProperties) is { } synth)
                {
                    parameters.Add(synth);
                }
            }
        }

        DtoData[] nestedDtosResult =
            context.IsRoot ? [.. context.Graph.GetCompletedDtos(context.SourceSymbol)] : [];

        var xmlDocsResult = ExtractSummary(methodXml) ??
                            FormatXmlDocs(context.SourceSymbol.GetDocumentationCommentXml());

        var (forwardMappings, diagnostics) = context.Directions.HasFlag(MapDirection.ToDto)
            ? ForwardMappingBuilder.BuildForwardMappings(context.SourceSymbol, parameters, context)
            : (new List<SourceMemberMap>(), new List<DiagnosticInfo>());

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
            canReconstruct ? [.. reconstructionArguments] : null,
            translatableLocaleType,
            directions: context.Directions,
            nameMatching: context.NameMatching,
            isSourceValueType: context.SourceSymbol.IsValueType,
            isSourceValueObject: ParameterHandlers.ParameterResolution.IsValueObjectType(context.SourceSymbol),
            forwardMappings: [.. forwardMappings],
            diagnostics: [.. diagnostics]
        );
    }

    private static string ToPascalCase(string input) =>
        string.IsNullOrEmpty(input) ? input : char.ToUpperInvariant(input[0]) + input.Substring(1);

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

    private static ParameterData? ResolveSynthesizedMember(
        INamedTypeSymbol sourceSymbol,
        string propName,
        Compilation? compilation,
        HashSet<string> nullableProperties)
    {
        for (var curr = sourceSymbol; curr != null; curr = curr.BaseType)
        {
            if (string.Equals(propName, "Id", StringComparison.OrdinalIgnoreCase) && IsEntityType(curr))
            {
                var idType = GetEntityIdType(curr, compilation);
                var isNullable = nullableProperties.Contains("Id");
                return new ParameterData(("Id", idType), isNullable, null);
            }

            if (string.Equals(propName, "CreatedOn", StringComparison.OrdinalIgnoreCase) &&
                curr.HasAnyMajaAttribute("AuditableAttribute"))
            {
                var isNullable = nullableProperties.Contains("CreatedOn");
                return new ParameterData(("CreatedOn", "global::System.DateTimeOffset"), isNullable, null);
            }

            if (string.Equals(propName, "UpdatedOn", StringComparison.OrdinalIgnoreCase) &&
                curr.HasAnyMajaAttribute("AuditableAttribute"))
            {
                return new ParameterData(("UpdatedOn", "global::System.DateTimeOffset?"), true, null);
            }

            if (string.Equals(propName, "Ordinal", StringComparison.OrdinalIgnoreCase) &&
                curr.HasAnyMajaAttribute("OrdinalAttribute"))
            {
                var isNullable = nullableProperties.Contains("Ordinal");
                return new ParameterData(("Ordinal", "global::System.UInt32"), isNullable, null);
            }

            if (string.Equals(propName, "IsArchived", StringComparison.OrdinalIgnoreCase) &&
                curr.HasAnyMajaAttribute("ArchivableAttribute"))
            {
                var isNullable = nullableProperties.Contains("IsArchived");
                return new ParameterData(("IsArchived", "global::System.Boolean"), isNullable, null);
            }

            if (string.Equals(propName, "ArchivedOn", StringComparison.OrdinalIgnoreCase) &&
                curr.HasAnyMajaAttribute("ArchivableAttribute"))
            {
                return new ParameterData(("ArchivedOn", "global::System.DateTimeOffset?"), true, null);
            }
        }

        return null;
    }
}