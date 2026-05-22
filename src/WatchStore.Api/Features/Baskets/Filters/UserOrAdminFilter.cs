namespace WatchStore.Api.Features.Baskets.Filters;

/// <summary>
/// Consente solo a un admin o al proprietario del carrello di accedervi
/// </summary>
public sealed class UserOrAdminFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ClaimsPrincipal user = context.HttpContext.User;

        if (user.IsInRole(AuthConstants.Roles.Admin))
        {
            return await next(context);
        }

        string? claimUserId = user.FindFirstValue("userId");

        Guid basketUserId = context.GetArgument<Guid>(0);

        if (!Guid.TryParse(claimUserId, out Guid userId) || userId != basketUserId)
        {
            return Results.Forbid();
        }

        return await next(context);
    }
}
