namespace WatchStore.Api.Common.Auth;

internal class KeycloakClaimsTransformer(ILogger<KeycloakClaimsTransformer> logger)
{
    /// <summary>
    /// Trasforma i campi jwt di keycloak in un formato riconosciuto
    /// </summary>
    /// <param name="ctx">Context</param>
    public void Transform(TokenValidatedContext ctx)
    {
        if (ctx.Principal?.Identity is ClaimsIdentity identity)
        {
            identity.UnpackClaimIntoScope("scope");
            identity.AddUserIdClaim(JwtRegisteredClaimNames.Sub);
            identity.LogAllClaims(logger);
        }
    }
}
