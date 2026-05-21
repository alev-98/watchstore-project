namespace WatchStore.Api.Features.Baskets;

public class BasketsModule : ICarterModule
{
    public const string GroupName = "baskets";

    public string TagName => string.Concat(GroupName[0].ToString().ToUpper(), GroupName.AsSpan(1));

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(GroupName).WithTags(TagName);

        GetBasketEndpoint.Map(group);
        UpsertBasketEndpoint.Map(group);
    }
}
