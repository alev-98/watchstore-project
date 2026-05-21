using Microsoft.Extensions.Hosting;

namespace WatchStore.IntegrationTests;

/// <summary>
/// Factory per WebApplication. Imposta servizi sostitutivi container, stub
/// e si occupa di autenticazione
/// </summary>
internal class WatchStoreWebApplicationFactory(
    IDatabaseContainer dbContainer,
    AzuriteContainer? azuriteContainer = null,
    bool authenticationSucceeds = true,
    string? userId = null,
    string? role = null,
    string? scope = null,
    bool disableBackgroundServices = false,
    IStripeEventFactory? stripeEventFactoryOverride = null,
    SaveChangesInterceptor? saveChangesInterceptor = null,
    IMessagePublisher? messagePublisher = null
) : WebApplicationFactory<Program>
{
    /// <summary>
    /// Crea il DbContext per Api: <see cref="WatchStoreContext"/>
    /// </summary>
    /// <returns>Il DbContext appena creato</returns>
    public WatchStoreContext CreateDbContext()
    {
        return Services.GetRequiredService<IDbContextFactory<WatchStoreContext>>().CreateDbContext();
    }

    /// <summary>
    /// Configura il WebHost rimpiazzandone i servizi
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        #region Configs

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            // Dummy stripe configs
            Dictionary<string, string?> stripeSettings = new()
            {
                ["Stripe:SecretKey"] = "sk_test",
                ["Stripe:CheckoutReturnUrl"] = "https://example.com/return",
                ["Stripe:EndpointSecret"] = "whsec_test"
            };

            configBuilder.AddInMemoryCollection(stripeSettings);
        });

        #endregion

        #region Services

        // Configura servizi aggiuntivi
        builder.ConfigureServices(services =>
        {
            #region Db

            services.RemoveAll<DbContextOptions<WatchStoreContext>>();

            DbContextOptionsBuilder<WatchStoreContext> dbContextOptionsBuilder = new();

            dbContextOptionsBuilder.UseNpgsql(dbContainer.GetConnectionString())
                                    .UseAsyncSeeding(async (context, _, cancellationToken) =>
                                    {
                                        SeedBrands(context);
                                        await context.SaveChangesAsync(cancellationToken);
                                    });

            if (saveChangesInterceptor is not null)
            {
                dbContextOptionsBuilder.AddInterceptors(saveChangesInterceptor);
            }

            services.AddSingleton(dbContextOptionsBuilder.Options);

            services.AddDbContextFactory<WatchStoreContext>();

            #endregion

            #region Auth

            // Auth scheme deciso a costruzine
            services.AddAuthentication("TestScheme")
                    .AddScheme<TestAuthOptions, TestAuthHandler>(
                        "TestScheme",
                        options =>
                        {
                            options.AuthenticationSucceeds = authenticationSucceeds;
                            options.UserId = userId ?? Guid.NewGuid().ToString();
                            options.Role = role;
                            options.Scope = scope;
                        });

            #endregion
        });

        // Rimpiazza i servizi con stubs di test
        builder.ConfigureTestServices(services =>
        {
            AddBlobServiceClient(services);
            AddSessionServiceStub(services);
            AddPaymentMethodServiceStub(services);
            AddPaymentIntentServiceStub(services);
            AddMessagePublisherStub(services);
            AddStripeEventFactoryStub(services);
            RemoveBackgroundServices(services);
        });

        #endregion

        builder.UseContentRoot(Directory.GetCurrentDirectory());
    }

    private void RemoveBackgroundServices(IServiceCollection services)
    {
        if (disableBackgroundServices)
        {
            services.RemoveAll<IHostedService>();
        }
    }

    #region Blob 

    /// <summary>
    /// Rimpiazza lo Storage con un Testcontainer se passato a creazione factory
    /// </summary>
    private void AddBlobServiceClient(IServiceCollection services)
    {
        if (azuriteContainer is not null)
        {
            BlobServiceClient blobServiceClient = new(azuriteContainer.GetConnectionString());
            services.RemoveAll<BlobServiceClient>();
            services.AddSingleton(blobServiceClient);
        }
    }

    #endregion

    #region Messages

    /// <summary>
    /// Assegna sempre un IMessagePublisher. Stub se non passato esplicitamente
    /// </summary>
    private void AddMessagePublisherStub(IServiceCollection services)
    {
        services.RemoveAll<IMessagePublisher>();

        messagePublisher ??= Substitute.For<IMessagePublisher>();

        services.AddSingleton(messagePublisher);
    }

    #endregion

    #region Stripe

    /// <summary>
    /// Rimpiazza Session Service e intercetta chiamate a CreateAsync
    /// </summary>
    private static void AddSessionServiceStub(IServiceCollection services)
    {
        Session session = new()
        {
            Id = "cs_test_session",
            ClientSecret = "cs_test_secret"
        };

        SessionService sessionService = Substitute.For<SessionService>();

        sessionService.CreateAsync(
            Arg.Any<SessionCreateOptions>(),
            Arg.Any<RequestOptions>(),
            default)
            .Returns(Task.FromResult(session));

        services.RemoveAll<SessionService>();
        services.AddSingleton(sessionService);
    }

    /// <summary>
    /// Rimpiazza PaymentMethod e intercetta chiamate a GetAsync
    /// </summary>
    private static void AddPaymentMethodServiceStub(IServiceCollection services)
    {
        PaymentMethod paymentMethod = new()
        {
            Id = "pm_test",
            Card = new PaymentMethodCard
            {
                Brand = "visa",
                Last4 = "4242"
            }
        };

        PaymentMethodService paymentMethodService = Substitute.For<PaymentMethodService>();

        paymentMethodService.GetAsync(
            Arg.Any<string>(),
            Arg.Any<PaymentMethodGetOptions>(),
            Arg.Any<RequestOptions>(),
            default)
            .Returns(Task.FromResult(paymentMethod));

        services.RemoveAll<PaymentMethodService>();
        services.AddSingleton(paymentMethodService);
    }

    /// <summary>
    /// Rimpiazza PaymentIntentService e intercetta GetAsync
    /// </summary>
    private static void AddPaymentIntentServiceStub(IServiceCollection services)
    {
        PaymentIntent paymentIntent = new()
        {
            Id = "pi_test",
            PaymentMethod = new PaymentMethod
            {
                Id = "pm_test",
                Card = new PaymentMethodCard
                {
                    Brand = "visa",
                    Last4 = "4242"
                }
            }
        };

        PaymentIntentService paymentIntentService = Substitute.For<PaymentIntentService>();

        paymentIntentService.GetAsync(
            Arg.Any<string>(),
            Arg.Any<PaymentIntentGetOptions>(),
            Arg.Any<RequestOptions>(),
            default)
            .Returns(Task.FromResult(paymentIntent));

        services.RemoveAll<PaymentIntentService>();
        services.AddSingleton(paymentIntentService);
    }

    /// <summary>
    /// Rimpiazza IStripeEventFactory se passato a creazione factory 
    /// </summary>
    private void AddStripeEventFactoryStub(IServiceCollection services)
    {
        if (stripeEventFactoryOverride is not null)
        {
            services.RemoveAll<IStripeEventFactory>();
            services.AddSingleton(stripeEventFactoryOverride);
        }
    }

    #endregion

    #region Seeding

    /// <summary>
    /// Seeding dei Brands
    /// </summary>
    /// <param name="context">Context sul quale effettuare seed</param>
    private static void SeedBrands(DbContext context)
    {
        Fixture fixture = new();

        fixture.Customize<Brand>(composer => composer.With(b => b.Name, fixture.Create<string>()[..20]));

        IEnumerable<Brand> brands = fixture.CreateMany<Brand>();

        context.Set<Brand>().AddRange(brands);
    }

    #endregion
}