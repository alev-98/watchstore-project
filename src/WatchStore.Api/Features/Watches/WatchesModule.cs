namespace WatchStore.Api.Features.Watches;

public class WatchesModule : ICarterModule
{
    public const string GroupName = "watches";

    public string TagName => string.Concat(GroupName[0].ToString().ToUpper(), GroupName.AsSpan(1));

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(GroupName).WithTags(TagName);

        AddWatchEndpoint.Map(group);
        DeleteWatchEndpoint.Map(group);
        GetWatchEndpoint.Map(group);
        GetWatchesEndpoint.Map(group);
        UpdateWatchEndpoint.Map(group);
    }
}
