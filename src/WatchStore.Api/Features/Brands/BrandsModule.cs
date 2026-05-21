namespace WatchStore.Api.Features.Brands;

public class BrandsModule : ICarterModule
{
    public const string GroupName = "brands";

    public string TagName => string.Concat(GroupName[0].ToString().ToUpper(), GroupName.AsSpan(1));

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(GroupName).WithTags(TagName);

        GetBrandsEndpoint.Map(group);
        GetBrandEndpoint.Map(group);
    }
}
