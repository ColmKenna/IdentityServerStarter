namespace IdentityServerAspNetIdentity;

/// <summary>
/// String constants for claim-based authorization policy names used on the User admin pages.
/// </summary>
public static class UserPolicyConstants
{
    // ── Users ─────────────────────────────────────────────────────────────
    public const string UsersRead   = "UsersRead";
    public const string UsersWrite  = "UsersWrite";
    public const string UsersDelete = "UsersDelete";

    // ── User Claims ───────────────────────────────────────────────────────
    public const string UserClaimsRead   = "UserClaimsRead";
    public const string UserClaimsWrite  = "UserClaimsWrite";
    public const string UserClaimsDelete = "UserClaimsDelete";

    // ── User Roles ────────────────────────────────────────────────────────
    public const string UserRolesRead   = "UserRolesRead";
    public const string UserRolesWrite  = "UserRolesWrite";
    public const string UserRolesDelete = "UserRolesDelete";

    // ── Persisted Grants ──────────────────────────────────────────────────
    public const string UserGrantsRead   = "UserGrantsRead";
    public const string UserGrantsDelete = "UserGrantsDelete";

    // ── Server-Side Sessions ──────────────────────────────────────────────
    public const string UserSessionsRead   = "UserSessionsRead";
    public const string UserSessionsDelete = "UserSessionsDelete";

    // ── Internal claim type & values (for policy definitions) ─────────────
    public const string AdminClaimType = "admin";

    public const string ClaimUsers        = "admin:users";
    public const string ClaimUserClaims   = "admin:user_claims";
    public const string ClaimUserRoles    = "admin:user_roles";
    public const string ClaimUserGrants   = "admin:user_grants";
    public const string ClaimUserSessions = "admin:user_sessions";
}
