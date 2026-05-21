namespace WatchStore.Api.Features.Payments.StripeWebhook;

internal static class StripeWebhookEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/stripe-webhook", async (
            HttpContext context,
            PaymentIntentService paymentIntentService,
            ConfirmOrderPaymentOperation confirmOrderPayment,
            IStripeEventFactory stripeEventFactory,
            IOptions<StripeOptions> options,
            ILoggerFactory loggerFactory
        ) =>
        {
            string jsonBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
            string signature = context.Request.Headers["Stripe-Signature"].ToString();

            ILogger logger = loggerFactory.CreateLogger("Payments");

            try
            {
                Event stripeEvent = stripeEventFactory.Create(jsonBody, signature);

                logger.LogInformation("Received Stripe event: {EventType}", stripeEvent.Type);

                if (stripeEvent.Type is EventTypes.CheckoutSessionCompleted)
                {
                    if (stripeEvent.Data.Object is not Session session)
                    {
                        logger.LogError("Unexpected object type {ObjType} for event {Id}",
                                            stripeEvent.Data.Object.Object,
                                            stripeEvent.Id);

                        return Results.BadRequest();
                    }

                    if (!session.Metadata.TryGetValue(MetadataKeys.OrderId, out string? orderIdString)
                        || !Guid.TryParse(orderIdString, out Guid orderId))
                    {
                        logger.LogError("Missing or invalid OrderId metadata on session {Id}", session.Id);

                        return Results.BadRequest();
                    }

                    logger.LogInformation("Payment succeded for checkout session {SessionId} and {OrderId}",
                                            session.Id,
                                            orderId);

                    PaymentIntentGetOptions paymentIntentOptions = new()
                    {
                        Expand = ["payment_method"]
                    };

                    var paymentIntent = await paymentIntentService.GetAsync(
                        session.PaymentIntentId,
                        paymentIntentOptions);

                    string cardBrand = paymentIntent.PaymentMethod.Card.Brand;
                    string cardLast4 = paymentIntent.PaymentMethod.Card.Last4;

                    if (session.AmountTotal is null)
                    {
                        logger.LogError("Session {SessionId} completed without AmountTotal for order {OrderId}",
                                            session.Id,
                                            orderId);

                        return Results.BadRequest();
                    }

                    decimal amountCharged = session.AmountTotal.Value / 100m;

                    bool paymentConfirmed = await confirmOrderPayment.ExecuteAsync(
                        orderId,
                        paymentIntent.Id,
                        cardBrand,
                        cardLast4,
                        amountCharged
                    );

                    if (!paymentConfirmed)
                    {
                        logger.LogWarning("Failed to confirm payment information on order {OrderId}", orderId);

                        return Results.BadRequest();
                    }

                    logger.LogInformation("Succesfully processed payment for order {OrderId}", orderId);
                }

                return Results.Ok();
            }
            catch (StripeException ex)
            {
                logger.LogWarning(ex, "Stribe webhook signature verification failed");
                return Results.BadRequest("Invalid Stripe signature");
            }
        })
        .AllowAnonymous();
    }
}
