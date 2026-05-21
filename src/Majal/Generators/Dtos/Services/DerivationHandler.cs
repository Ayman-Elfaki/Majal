using Microsoft.CodeAnalysis;

namespace Majal.Generators.Dtos.Services;

/// <summary>
/// Default implementation of derivation handling for abstract types.
/// </summary>
public sealed class DerivationHandler : IDerivationHandler
{
    private readonly IFactoryMethodFinder _factoryMethodFinder;
    private readonly IXmlDocumentationProcessor _docProcessor;

    public DerivationHandler(
        IFactoryMethodFinder? factoryMethodFinder = null,
        IXmlDocumentationProcessor? docProcessor = null
    )
    {
        _factoryMethodFinder = factoryMethodFinder ?? new FactoryMethodFinder();
        _docProcessor = docProcessor ?? new XmlDocumentationProcessor();
    }

    public bool TryProcessDerivedTypes(
        INamedTypeSymbol sourceSymbol,
        string factoryMethodName,
        string defaultMethodName,
        Compilation compilation,
        DtoForGenerator.DtoContext context,
        out DtoForGenerator.DtoData? resultData
    )
    {
        var derivedMethods = _factoryMethodFinder.FindFactoryMethodsInDerivedTypes(sourceSymbol, factoryMethodName, compilation);

        if (derivedMethods.Count == 0)
        {
            resultData = null;
            return false;
        }

        var derivedDtos = new List<DtoForGenerator.DtoData>();
        var derivedTypes = new List<DtoForGenerator.DerivedTypeInfo>();
        var dtoName = context.DtoName;
        var dtoNamePrefix = context.DtoNamePrefix;
        var dtoNameSuffix = context.DtoNameSuffix;
        var accessibility = context.Accessibility;
        var collected = context.Collected;

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
                    SourceSymbol = derivedSymbol,
                    IsRoot = false
                };

                // Note: This would need to call back into the DtoDataBuilder
                // For now, we'll mark it as a placeholder
                var derivedData = new DtoForGenerator.DtoData(
                    derivedContext.Namespace,
                    derivedDtoName,
                    derivedDtoName,
                    derivedContext.ParentTypeDeclarations.ToArray(),
                    accessibility,
                    null,
                    null,
                    derivedContext.IsRecord,
                    [],
                    [],
                    []
                );

                collected[derivedDtoName] = derivedData;
                derivedDtos.Add(derivedData);
            }

            derivedTypes.Add(new DtoForGenerator.DerivedTypeInfo(derivedDtoName, derivedSymbol.Name));
        }

        var commonParameters = GetCommonParameters(derivedDtos);

        if (commonParameters.Length > 0)
        {
            for (var i = 0; i < derivedDtos.Count; i++)
            {
                var derivedDto = derivedDtos[i];
                DtoForGenerator.ParameterData[] uniqueParameters =
                [
                    ..derivedDto.Parameters
                        .Where(p => commonParameters.All(cp =>
                            cp.Name != p.Name || cp.ResolvedType != p.ResolvedType))
                ];

                var updatedData = new DtoForGenerator.DtoData(
                    derivedDto.Namespace, derivedDto.DtoName, derivedDto.RawDtoName,
                    [..derivedDto.ParentTypeDeclarations], accessibility, derivedDto.XmlDocs, derivedDto.BaseDtoName,
                    derivedDto.IsRecord, [..derivedDto.DerivedTypes], uniqueParameters,
                    [..derivedDto.NestedDtos]
                );

                derivedDtos[i] = updatedData;
                collected[derivedDto.DtoName] = updatedData;
            }
        }

        var xmlDocs = _docProcessor.FormatXmlDocs(sourceSymbol.GetDocumentationCommentXml());

        DtoForGenerator.DtoData[] nestedDtos = context.IsRoot
            ? [.. collected.Values.Where(v => !string.IsNullOrEmpty(v.DtoName) && v.DtoName != dtoName)]
            : [];

        resultData = new DtoForGenerator.DtoData(
            context.Namespace, dtoName, context.RawDtoName, context.ParentTypeDeclarations.ToArray(),
            accessibility, xmlDocs, null, context.IsRecord, [.. derivedTypes], commonParameters, nestedDtos
        );

        return true;
    }

    private static DtoForGenerator.ParameterData[] GetCommonParameters(IEnumerable<DtoForGenerator.DtoData> dtos)
    {
        var dtoArray = dtos as DtoForGenerator.DtoData[] ?? [.. dtos];
        if (dtoArray.Length == 0) return [];

        return
        [
            .. dtoArray[0].Parameters.Where(p =>
                dtoArray.Skip(1).All(d => d.Parameters.Any(o => o.Name == p.Name && o.ResolvedType == p.ResolvedType))
            )
        ];
    }
}
