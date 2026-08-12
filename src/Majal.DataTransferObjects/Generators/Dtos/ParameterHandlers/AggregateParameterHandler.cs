using Majal.Generators.Dtos.Models;
using Microsoft.CodeAnalysis;
using static Majal.Common.Abstractions.Constants;
using static Majal.Generators.Dtos.ParameterHandlers.ParameterResolution;

namespace Majal.Generators.Dtos.ParameterHandlers;

internal sealed class AggregateParameterHandler : IParameterHandler
{
    public bool IsApplicable(ITypeSymbol unwrappedType) => IsAggregateType(unwrappedType);

    public ParameterOutcome? Resolve(ParameterContext ctx, ITypeSymbol unwrappedType, bool isCollection,
        bool isNullable)
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
}