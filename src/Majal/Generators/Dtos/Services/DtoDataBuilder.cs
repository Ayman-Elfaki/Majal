using Microsoft.CodeAnalysis;

namespace Majal.Generators.Dtos.Services;

/// <summary>
/// Default implementation of DTO data building with service injection.
/// </summary>
public sealed class DtoDataBuilder : IDtoDataBuilder
{
    private readonly IFactoryMethodFinder _factoryMethodFinder;
    private readonly IParameterBuilder _parameterBuilder;
    private readonly IDerivationHandler _derivationHandler;
    private readonly IXmlDocumentationProcessor _docProcessor;

    public DtoDataBuilder(
        ITypeClassifier? typeClassifier = null,
        IFactoryMethodFinder? factoryMethodFinder = null,
        IParameterBuilder? parameterBuilder = null,
        IDerivationHandler? derivationHandler = null,
        IXmlDocumentationProcessor? docProcessor = null
    )
    {
        var typeClassifier1 = typeClassifier ?? new TypeClassifier();
        _factoryMethodFinder = factoryMethodFinder ?? new FactoryMethodFinder();

        _parameterBuilder =
            parameterBuilder ?? new ParameterBuilder(typeClassifier1, _factoryMethodFinder, docProcessor);

        _docProcessor = docProcessor ?? new XmlDocumentationProcessor();
        _derivationHandler = derivationHandler ?? new DerivationHandler(_factoryMethodFinder, docProcessor);
    }

    public DtoForGenerator.DtoData? BuildDtoData(DtoForGenerator.DtoContext context)
    {
        var compilation = context.Compilation;
        var sourceSymbol = context.SourceSymbol;
        var factoryMethodName = context.FactoryMethodName;
        var defaultMethodName = context.DefaultMethodName;

        var createMethod = _factoryMethodFinder.FindFactoryMethod(sourceSymbol, factoryMethodName);

        // Handle abstract types with derived implementations
        if (createMethod is null && compilation is not null && sourceSymbol is { IsAbstract: true })
        {
            if (_derivationHandler.TryProcessDerivedTypes(sourceSymbol, factoryMethodName, defaultMethodName,
                    compilation, context, out var resultData))
            {
                return resultData;
            }
        }

        // Return null if no factory method found
        if (createMethod is null) return null;

        // Build parameters from factory method
        var parameters = _parameterBuilder.BuildParameters(
            createMethod,
            sourceSymbol,
            defaultMethodName,
            context.DtoNamePrefix,
            context.DtoNameSuffix,
            context.Accessibility,
            context.IsRecord,
            context.Namespace,
            context.Collected,
            context.ParentTypeDeclarations.ToArray(),
            context.FlattenConfigs,
            compilation
        );

        var nestedDtos = context.IsRoot
            ? context.Collected.Values.Where(v => !string.IsNullOrEmpty(v.DtoName)).ToArray()
            : [];

        var methodXml = createMethod.GetDocumentationCommentXml();
        var xmlDocsResult = _docProcessor.ExtractSummary(methodXml) ??
                            _docProcessor.FormatXmlDocs(sourceSymbol.GetDocumentationCommentXml());

        return new DtoForGenerator.DtoData(
            context.Namespace,
            context.DtoName,
            context.RawDtoName,
            context.ParentTypeDeclarations.ToArray(),
            context.Accessibility,
            xmlDocsResult,
            null,
            context.IsRecord,
            [],
            [.. parameters],
            nestedDtos
        );
    }
}