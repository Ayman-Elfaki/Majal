using Majal.Generators.Dtos.Models;
using Microsoft.CodeAnalysis;

namespace Majal.Generators.Dtos.ParameterHandlers;

internal interface IParameterHandler
{
    bool IsApplicable(ITypeSymbol unwrappedType);

    ParameterOutcome? Resolve(ParameterContext ctx, ITypeSymbol unwrappedType, bool isCollection, bool isNullable);
}