namespace WatchStore.Api.Features.Payments.CreateCheckoutSession;

internal static class CreateCheckoutSessionEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/checkout", async (
            SessionService sessionService,
            ClaimsPrincipal user,
            OrderCreator orderCreator,
            CreateCheckoutSessionDto requestDto,
            IOptions<StripeOptions> stripeOptions,
            ILoggerFactory loggerFactory) =>
        {
            string? userIdClaimValue = user.FindFirstValue(AuthConstants.Claims.UserId);

            if (!Guid.TryParse(userIdClaimValue, out Guid userId))
            {
                return Results.Forbid();
            }

            ILogger logger = loggerFactory.CreateLogger("Payments");

            logger.LogInformation("Creating checkout for user {UserId} and operation {OperationID}",
                                    userId,
                                    requestDto.OperationId);

            CreateOrderResult result = await orderCreator.GetOrCreateOrderAsync(userId, requestDto.OperationId);

            if (result.EmptyBasket)
            {
                return Results.BadRequest();
            }

            Order order = result.Order;

            var lineItems = order.Items
                                    .OrderBy(item => item.ProductName)
                                    .Select(item => new SessionLineItemOptions
                                    {
                                        PriceData = new SessionLineItemPriceDataOptions
                                        {
                                            Currency = "eur",
                                            UnitAmount = (long)(item.Price * 100),
                                            ProductData = new SessionLineItemPriceDataProductDataOptions
                                            {
                                                Name = item.ProductName,
                                                Images = [item.ImageUri]
                                            }
                                        },
                                        Quantity = item.Quantity
                                    })
                                    .ToList();

            SessionCreateOptions options = new()
            {
                UiMode = "elements",
                Mode = "payment",
                LineItems = lineItems,
                ReturnUrl = $"{stripeOptions.Value.CheckoutReturnUrl.TrimEnd('/')}/{order.Id}",
                CustomerEmail = user.FindFirstValue(JwtRegisteredClaimNames.Email)
                                    ?? user.FindFirstValue("preferred_username"),
                Metadata = new Dictionary<string, string>
                {
                    { MetadataKeys.OrderId, order.Id.ToString()}
                }
            };

            RequestOptions requestOptions = new()
            {
                IdempotencyKey = $"cs-create-{order.Id}"
            };

            Session session = await sessionService.CreateAsync(options, requestOptions);

            logger.LogInformation("Created checkout session: {SessionId}", session.Id);

            var responseDto = new CheckoutSessionDto(
                                session.ClientSecret,
                                order.Id,
                                order.Items
                                        .Select(item => new CheckoutSessionItemDto(
                                            item.ProductId,
                                            item.ProductName,
                                            item.Price,
                                            item.Quantity,
                                            item.ImageUri
                                        )));

            return Results.Ok(responseDto);
        });
    }
}
