using Majal.Generators.Dtos.Models;
using Microsoft.CodeAnalysis;
using static Majal.Common.Abstractions.Constants;
using static Majal.Generators.Dtos.ParameterHandlers.ParameterResolution;

namespace Majal.Generators.Dtos.ParameterHandlers;

internal sealed class EntityParameterHandler : IParameterHandler
{
    public bool IsApplicable(ITypeSymbol unwrappedType) => IsEntityType(unwrappedType);

    public ParameterOutcome? Resolve(ParameterContext ctx, ITypeSymbol unwrappedType, bool isCollection,
        bool isNullable)
    {
        var resolveContext = new ParameterType(unwrappedType, isNullable, ctx);
        var resolvedElementType = ResolveNestedDtoElementType(resolveContext);
        var resolvedType = isCollection
            ? $"{GenericsNamespace}.IEnumerable<{resolvedElementType}>"
            : resolvedElementType;

        var propertyName = ctx.Parameter.Name;
        var paramXml = ExtractParamDoc(ctx.MethodXml, ctx.Parameter.Name);
        var dtoPropertyName = ToPascalCase(propertyName);

        TryGetNestedDto(resolvedElementType, ctx.DtoContext.Graph, out var nestedDto);
        var nestedSourceName = nestedDto.SourceSimpleName;
        if (nestedSourceName is null && ctx.DtoContext.Graph.TryGetNode(
                resolvedElementType.TrimEnd('?'), out var nestedNode))
            nestedSourceName = nestedNode.SourceSymbol.Name;

        var reconstruction = new FactoryArgument(
            ctx.Parameter.Name, ReconstructKind.NestedType, dtoPropertyName,
            nestedSourceName, isCollection,
            isCollection ? GetCollectionConversionSuffix(ctx.Parameter.Type) : "", isNullable);

        return new ParameterOutcome(
            [new ParameterData((propertyName, resolvedType), isNullable, paramXml)],
            reconstruction);
    }
}