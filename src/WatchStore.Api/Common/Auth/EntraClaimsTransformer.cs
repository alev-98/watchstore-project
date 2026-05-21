namespace WatchStore.Api.Common.Auth;

internal class EntraClaimsTransformer(ILogger<EntraClaimsTransformer> logger)
{
    /// <summary>
    /// Trasforma i campi jwt di entra in un formato riconosciuto
    /// </summary>
    /// <param name="ctx">Context</param>
    public void Transform(TokenValidatedContext ctx)
    {
        if (ctx.Principal?.Identity is ClaimsIdentity identity)
        {
            identity.UnpackClaimIntoScope(AuthConstants.Claims.Scp);
            identity.AddUserIdClaim(AuthConstants.Claims.Oid);
            identity.LogAllClaims(logger);
        }
    }
}
