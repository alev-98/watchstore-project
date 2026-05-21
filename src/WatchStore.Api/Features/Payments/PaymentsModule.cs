namespace WatchStore.Api.Features.Payments;

public class PaymentsModule : ICarterModule
{
    public const string GroupName = "payments";

    public string TagName => string.Concat(GroupName[0].ToString().ToUpper(), GroupName.AsSpan(1));

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(GroupName).WithTags(TagName);

        CreateCheckoutSessionEndpoint.Map(group);
        StripeWebhookEndpoint.Map(group);
    }
}
