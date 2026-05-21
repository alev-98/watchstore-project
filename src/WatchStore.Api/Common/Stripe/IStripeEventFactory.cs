namespace WatchStore.Api.Common.Stripe;

public interface IStripeEventFactory
{
    Event Create(string jsonBody, string signatureHeader);
}
