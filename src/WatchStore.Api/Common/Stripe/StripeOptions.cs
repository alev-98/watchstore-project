namespace WatchStore.Api.Common.Stripe;

public sealed class StripeOptions
{
    [Required]
    public required string SecretKey { get; set; }

    [Required]
    public required string CheckoutReturnUrl { get; set; }

    [Required]
    public required string EndpointSecret { get; set; }
}