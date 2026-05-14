using FluentValidation;

namespace Majal.Sample.Common.Filters;

public class ValidationFilter<T> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null) return await next(context);
        var entity = context.Arguments.OfType<T>().FirstOrDefault();
        if (entity is null) return await next(context);
        var result = await validator.ValidateAsync(entity);
        if (!result.IsValid) return Results.ValidationProblem(result.ToDictionary());
        return await next(context);
    }
}