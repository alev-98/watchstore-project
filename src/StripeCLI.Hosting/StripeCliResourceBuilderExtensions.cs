using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace StripeCLI.Hosting;

public static class StripeCliResourceBuilderExtensions
{
    private const string Image = "stripe/stripe-cli";

    private const string Tag = "v1.40.9";

    private const string ApiKeyForCliEnvVarName = "STRIPE_API_KEY";

    private const string ApiKeyForReferenceEnvVarName = "Stripe__SecretKey";

    private const string SecretsContainerPath = "/secrets";

    private const string EndpointSecretEnvVarName = "Stripe__EndpointSecret";

    extension(IDistributedApplicationBuilder builder)
    {
        public IResourceBuilder<StripeCliResource> AddStripeCli(
            string name,
            IResourceBuilder<ParameterResource> stripeApiKey
        )
        {
            var resource = new StripeCliResource(name, stripeApiKey.Resource);

            return builder.AddResource(resource)
                            .WithImage(Image)
                            .WithImageTag(Tag)
                            .WithEnvironment(
                                ApiKeyForCliEnvVarName,
                                resource.ApiKey
                            );
        }
    }

    extension(IResourceBuilder<StripeCliResource> builder)
    {
        public IResourceBuilder<StripeCliResource> WithWebookEventListener(
            ReferenceExpression forwardToEndpoint,
            string webhookSecretFilePath
        )
        {
            builder.WithArgs("listen", "--forward-to", forwardToEndpoint);
            builder.Resource.EndpointSecretFilePath = webhookSecretFilePath;
            return builder;
        }

        public IResourceBuilder<StripeCliResource> WithPrintSecret(
            string webhookSecretFilePath
        )
        {
            var secretsDirectory = Path.GetDirectoryName(webhookSecretFilePath)
                            ?? throw new ArgumentException(
                                "Invalid secrets file path",
                                nameof(webhookSecretFilePath));

            var webhookSecretFileName = Path.GetFileName(webhookSecretFilePath);

            return builder.WithBindMount(secretsDirectory, SecretsContainerPath)
                            .WithEntrypoint("/bin/sh")
                            .WithArgs("-c", $"stripe listen --print-secret > {SecretsContainerPath}/{webhookSecretFileName}");
        }
    }

    extension<TDestination>(IResourceBuilder<TDestination> builder)
        where TDestination : IResourceWithEnvironment
    {
        public IResourceBuilder<TDestination> WithReference(IResourceBuilder<StripeCliResource> source)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(source);

            return builder.WithEnvironment(context =>
            {
                context.EnvironmentVariables[ApiKeyForReferenceEnvVarName] = source.Resource.ApiKey;

                if (!string.IsNullOrEmpty(source.Resource.EndpointSecretFilePath))
                {
                    var endpointSecret = File.ReadAllText(source.Resource.EndpointSecretFilePath)
                                                .Trim();

                    context.EnvironmentVariables[EndpointSecretEnvVarName] = endpointSecret;
                }
            });
        }
    }
}
