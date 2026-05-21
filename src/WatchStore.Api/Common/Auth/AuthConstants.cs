namespace WatchStore.Api.Common.Auth;

internal static class AuthConstants
{
    internal static class Policies
    {
        public const string UserAccess = nameof(UserAccess);

        public const string AdminAccess = nameof(AdminAccess);
    }

    internal static class Roles
    {
        public const string Admin = nameof(Admin);
    }

    internal static class Schemes
    {
        public const string KeyCloak = nameof(KeyCloak);

        public const string Entra = nameof(Entra);
    }

    internal static class Claims
    {
        public const string Scope = "scope";

        public const string Role = "role";

        public const string Roles = "roles"; // Come Entra restituisce i ruoli

        public const string UserId = "userId";

        public const string Oid = "oid"; // Id univoco di entra

        public const string Scp = "scp"; // Come entra restituisce scope
    }
}