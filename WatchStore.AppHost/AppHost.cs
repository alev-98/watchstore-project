
var builder = DistributedApplication.CreateBuilder(args);

#region Params

var stripeApiKey = builder.AddParameter("StripeApiKey", secret: true);
var checkoutReturnUrl = builder.AddParameter("CheckoutReturnUrl");

#endregion

#region Services

var db = builder.AddDb();

var blobs = builder.AddStorage();

var serviceBus = builder.AddServiceBus();

var api = builder.AddApi()
                    .WithReference(db)
                    .WaitFor(db)
                    .WithReference(blobs)
                    .WaitFor(blobs)
                    .WithReference(serviceBus)
                    .WaitFor(serviceBus);

var worker = builder.AddWorker()
                    .WithReference(db)
                    .WaitFor(db)
                    .WithReference(serviceBus)
                    .WaitFor(serviceBus);

api.WithEnvironment("Stripe__CheckoutReturnUrl", checkoutReturnUrl);

#endregion

if (builder.ExecutionContext.IsRunMode)
{
    #region Keycloak

    var keycloak = builder.AddKeycloak();

    var keycloakAuthority = ReferenceExpression.Create(
            $"{keycloak.GetEndpoint("http").Property(EndpointProperty.Url)}/realms/watchstore"
    );

    api.WaitFor(keycloak)
        .WithEnvironment(
            "Authentication__Schemes__Keycloak__Authority",
            keycloakAuthority)
        .WithEnvironment(
            "Authentication__Schemes__Keycloak__ValidAudience",
            "watchstore-api");

    #endregion

    #region StripeCli

    var forwardExpression = ReferenceExpression.Create(
        $"{api.GetEndpoint("http")}/payments/stripe-webhook"
    );

    var webhookSecretFilePath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "..\\.stripe",
        "webhook_secret.txt"
    );

    var stripeSecretGenerator = builder.AddStripeCli("stripeSecretGen", stripeApiKey)
                                        .WithPrintSecret(webhookSecretFilePath);

    var stripeListener = builder.AddStripeCli("stripeListener", stripeApiKey)
                                .WithWebookEventListener(forwardExpression, webhookSecretFilePath);

    api.WithReference(stripeListener)
        .WaitFor(stripeListener)
        .WaitForCompletion(stripeSecretGenerator);

    #endregion

}

if (builder.ExecutionContext.IsPublishMode)
{
    #region Entra

    var entraAuthority = builder.AddParameter("EntraAuthority");
    var entraValidAudience = builder.AddParameter("EntraValidAudience");

    api.WithEnvironment(
            "Authentication__Schemes__Entra__ValidAudience",
            entraValidAudience)
        .WithEnvironment(
            "Authentication__Schemes__Entra__Authority",
            entraAuthority);

    #endregion

    #region Keyvault

    var keyvault = builder.AddAzureKeyVault("keyvault");

    keyvault.AddSecret("stripeApiKeySecret", "Stripe--SecretKey", stripeApiKey);

    api.WithReference(keyvault);

    #endregion

    #region Insights

    var insights = builder.AddAzureApplicationInsights("app-insights");

    api.WithReference(insights);
    worker.WithReference(insights);

    #endregion
}

builder.Build().Run();
