namespace WatchStore.Api.Common.Stripe;

internal class StripeEventFactory(IOptions<StripeOptions> options) : IStripeEventFactory
{
    private readonly string endpointSecret = options.Value.EndpointSecret;

    public Event Create(string jsonBody, string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(jsonBody))
        {
            throw new ArgumentException("Request body is required.", nameof(jsonBody));
        }

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            throw new ArgumentException("Stripe-Signature header is required.", nameof(signatureHeader));
        }

        return EventUtility.ConstructEvent(jsonBody, signatureHeader, endpointSecret);
    }
}
