namespace WatchStore.Api.Common.Extensions.Services;

internal static class AuthExtensions
{
    private const string ApiAccessScope = "watchstore_api.all";

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Aggiunge le Policies di autorizzazione
        /// </summary>
        public IServiceCollection AddAuthorizationPolicies()
        {
            services.AddAuthorizationBuilder()
                    .AddFallbackPolicy(AuthConstants.Policies.UserAccess, authBuilder =>
                    {
                        authBuilder.RequireClaim(AuthConstants.Claims.Scope, ApiAccessScope);
                    })
                    .AddPolicy(AuthConstants.Policies.AdminAccess, authBuilder =>
                    {
                        authBuilder.RequireClaim(AuthConstants.Claims.Scope, ApiAccessScope);
                        authBuilder.RequireRole(AuthConstants.Roles.Admin);
                    });

            return services;
        }

        /// <summary>
        /// Aggiunge Keycloak come IdentityProvider per autenticazione
        /// </summary>
        public IServiceCollection AddKeycloakAuthentication()
        {
            services.AddSingleton<KeycloakClaimsTransformer>();

            services.AddAuthentication(AuthConstants.Schemes.KeyCloak)
                    .AddJwtBearer(AuthConstants.Schemes.KeyCloak, options =>
                    {
                        options.MapInboundClaims = false;
                        options.TokenValidationParameters.RoleClaimType = AuthConstants.Claims.Role;
                        options.RequireHttpsMetadata = false;
                        options.Events = new JwtBearerEvents
                        {
                            OnTokenValidated = ctx =>
                            {
                                var transformer = ctx.HttpContext
                                                        .RequestServices
                                                        .GetRequiredService<KeycloakClaimsTransformer>();
                                transformer.Transform(ctx);

                                return Task.CompletedTask;
                            }
                        };
                    });

            return services;
        }

        /// <summary>
        /// Aggiunge Entra come IdentityProvider per autenticazione
        /// </summary>
        public IServiceCollection AddEntraAuthentication()
        {
            services.AddSingleton<EntraClaimsTransformer>();

            services.AddAuthentication(AuthConstants.Schemes.Entra)
                    .AddJwtBearer(AuthConstants.Schemes.Entra, options =>
                    {
                        options.MapInboundClaims = false;
                        options.TokenValidationParameters.RoleClaimType = AuthConstants.Claims.Roles;
                        options.RequireHttpsMetadata = false;
                        options.Events = new JwtBearerEvents
                        {
                            OnTokenValidated = ctx =>
                            {
                                var transformer = ctx.HttpContext
                                                        .RequestServices
                                                        .GetRequiredService<EntraClaimsTransformer>();
                                transformer.Transform(ctx);

                                return Task.CompletedTask;
                            }
                        };
                    });

            return services;
        }
    }
}