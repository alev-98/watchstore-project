namespace WatchStore.Api.Features.Orders;

public class OrderModule : ICarterModule
{
    public const string GroupName = "orders";

    public string TagName => string.Concat(GroupName[0].ToString().ToUpper(), GroupName.AsSpan(1));

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(GroupName).WithTags(TagName);

        GetOrderEndpoint.Map(group);
        GetOrdersEndpoint.Map(group);
    }
}
