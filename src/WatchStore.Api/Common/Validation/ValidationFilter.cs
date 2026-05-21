namespace WatchStore.Api.Common.Validation;

/// <summary>
/// Filtro per validazione con Fluent
/// </summary>
/// <typeparam name="T">Tipo da validare</typeparam>
/// <param name="validator">Validatore</param>
internal sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext ctx,
        EndpointFilterDelegate next)
    {
        T? dto = ctx.Arguments.OfType<T>().FirstOrDefault();

        if (dto is null)
        {
            return await next(ctx);
        }

        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(dto);

        if (!result.IsValid)
        {
            return Results.ValidationProblem(result.ToDictionary());
        }

        return await next(ctx);
    }
}