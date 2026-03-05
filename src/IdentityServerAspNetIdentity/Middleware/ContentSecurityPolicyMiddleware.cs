namespace IdentityServerAspNetIdentity.Middleware;

/// <summary>
/// Sets a restrictive Content-Security-Policy header on every response,
/// allowing styles, scripts, and fonts from cdnjs.cloudflare.com in addition
/// to same-origin sources.
/// </summary>
internal sealed class ContentSecurityPolicyMiddleware(RequestDelegate next)
{
    private const string CspValue =
        "default-src 'self'; " +
        "style-src 'self' https://cdnjs.cloudflare.com; " +
        "script-src 'self' https://cdnjs.cloudflare.com; " +
        "font-src 'self' https://cdnjs.cloudflare.com;";

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["Content-Security-Policy"] = CspValue;
        return next(context);
    }
}
