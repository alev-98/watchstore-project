namespace WatchStore.Api.Common.Extensions;

internal static class ClaimsIdentityExtensions
{
    extension(ClaimsIdentity identity)
    {
        /// <summary>
        /// Logga tutti i Claim
        /// </summary>
        public void LogAllClaims(ILogger logger)
        {
            IEnumerable<Claim> claims = identity.Claims;

            foreach (Claim claim in claims)
            {
                logger.LogDebug("Claim: {ClaimType} - Value: {ClaimValue}",
                                    claim.Type,
                                    claim.Value);
            }
        }

        /// <summary>
        /// Spacchetta un Claim diviso da spazi in singoli Claim scope
        /// </summary>
        /// <param name="scopeClaimName">Nome del claim da spacchettare in singoli scope</param>
        public void UnpackClaimIntoScope(string scopeClaimName)
        {
            if (identity.FindFirst(scopeClaimName) is not Claim scopeClaim)
            {
                return;
            }

            string[] values = scopeClaim.Value.Split(' ');

            identity.RemoveClaim(scopeClaim);

            identity.AddClaims(values.Select(value => new Claim(AuthConstants.Claims.Scope, value)));
        }

        /// <summary>
        /// Aggiunge un Claim userId
        /// </summary>
        /// <param name="idClaimName">Da quale Claim prendere l'id</param>
        public void AddUserIdClaim(string idClaimName)
        {
            if (identity.FindFirst(idClaimName) is not Claim idClaim)
            {
                return;
            }

            Claim userIdClaim = new(AuthConstants.Claims.UserId, idClaim.Value);

            identity.AddClaim(userIdClaim);
        }
    }

}
