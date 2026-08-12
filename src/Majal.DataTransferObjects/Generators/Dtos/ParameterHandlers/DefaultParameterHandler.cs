using Majal.Generators.Dtos.Models;
using Microsoft.CodeAnalysis;
using static Majal.Common.Abstractions.Constants;
using static Majal.Generators.Dtos.ParameterHandlers.ParameterResolution;

namespace Majal.Generators.Dtos.ParameterHandlers;

internal sealed class DefaultParameterHandler : IParameterHandler
{
    public bool IsApplicable(ITypeSymbol unwrappedType) => true;

    public ParameterOutcome? Resolve(ParameterContext ctx, ITypeSymbol unwrappedType, bool isCollection,
        bool isNullable)
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
}