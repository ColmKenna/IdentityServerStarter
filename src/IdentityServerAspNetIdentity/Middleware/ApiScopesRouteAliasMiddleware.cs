namespace IdentityServerAspNetIdentity.Middleware;

/// <summary>
/// Rewrites <c>/Admin/ApiScopes/Create</c> to <c>/Admin/ApiScopes/0/Edit</c> so that
/// the Edit Razor Page handles the create flow without a client-visible redirect.
/// </summary>
internal sealed class ApiScopesRouteAliasMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.Equals("/Admin/ApiScopes/Create", StringComparison.OrdinalIgnoreCase))
        {
            context.Request.Path = "/Admin/ApiScopes/0/Edit";
        }

        return next(context);
    }
}
