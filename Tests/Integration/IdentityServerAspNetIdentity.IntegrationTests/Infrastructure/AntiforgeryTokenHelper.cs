using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;

public static class AntiforgeryTokenHelper
{
    public static async Task<(string Token, string CookieHeader, string FieldName)> GetTokensAsync(CustomWebApplicationFactory factory, ClaimsPrincipal? user = null)
    {
        using var scope = factory.Services.CreateScope();
        var antiforgery = scope.ServiceProvider.GetRequiredService<IAntiforgery>();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            User = user ?? CreateTestPrincipal()
        };

        var tokens = antiforgery.GetAndStoreTokens(httpContext);

        var setCookie = httpContext.Response.Headers["Set-Cookie"].ToString();
        if (string.IsNullOrWhiteSpace(setCookie))
        {
            throw new InvalidOperationException("Failed to generate antiforgery cookie.");
        }

        // Use only the cookie name/value portion.
        var cookieHeader = setCookie.Split(';')[0];

        // Token might be null depending on configuration.
        var requestToken = tokens.RequestToken ?? string.Empty;
        if (string.IsNullOrWhiteSpace(requestToken))
        {
            throw new InvalidOperationException("Failed to generate antiforgery token.");
        }

        await Task.CompletedTask;
        return (requestToken, cookieHeader, tokens.FormFieldName);
    }

    private static ClaimsPrincipal CreateTestPrincipal()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "testadmin"),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Email, "admin@test.com"),
            new Claim(ClaimTypes.Role, "ADMIN"),
            new Claim("admin", "admin:users"),
            new Claim("admin", "admin:user_claims"),
            new Claim("admin", "admin:user_roles"),
            new Claim("admin", "admin:user_grants"),
            new Claim("admin", "admin:user_sessions"),
        };

        var identity = new ClaimsIdentity(claims, "TestScheme");
        return new ClaimsPrincipal(identity);
    }
}
