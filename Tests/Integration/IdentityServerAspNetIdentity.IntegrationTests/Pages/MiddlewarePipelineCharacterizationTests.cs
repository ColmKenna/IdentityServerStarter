using System.Net;
using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages;

/// <summary>
/// Characterization tests locking in the HTTP pipeline behaviour
/// before middleware is extracted to named classes (Phase 3e).
/// </summary>
public class MiddlewarePipelineCharacterizationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MiddlewarePipelineCharacterizationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // ── Content-Security-Policy middleware ────────────────────────────────

    [Fact]
    public async Task Get_AnyPage_ResponseContainsContentSecurityPolicyHeader()
    {
        var response = await _client.GetAsync("/Admin");

        response.Headers.TryGetValues("Content-Security-Policy", out var values)
            .Should().BeTrue("every response should include a Content-Security-Policy header");
        values.Should().ContainSingle();
    }

    [Fact]
    public async Task Get_AnyPage_ContentSecurityPolicyAllowsSelfAndCdn()
    {
        var response = await _client.GetAsync("/Admin");

        var csp = response.Headers.GetValues("Content-Security-Policy").Single();

        csp.Should().Contain("default-src 'self'",
            "the CSP should restrict default sources to same-origin");
        csp.Should().Contain("https://cdnjs.cloudflare.com",
            "the CSP should allow styles/scripts from the project CDN");
    }

    [Fact]
    public async Task Get_StaticFile_ResponseContainsContentSecurityPolicyHeader()
    {
        // Static files also pass through the CSP middleware
        var response = await _client.GetAsync("/css/site.css");

        // A 200 or 404 is fine — what matters is the header is present either way
        response.Headers.TryGetValues("Content-Security-Policy", out _)
            .Should().BeTrue("CSP middleware runs before static file serving");
    }

    // ── Route alias middleware ─────────────────────────────────────────────
    // Already covered comprehensively in ApiScopesCreateIntegrationTests.
    // One smoke test here keeps the characterization complete in a single file.

    [Fact]
    public async Task Get_ApiScopesCreate_RouteAliasReturns200WithoutRedirect()
    {
        using var noRedirect = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await noRedirect.GetAsync("/Admin/ApiScopes/Create");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "/Admin/ApiScopes/Create should be served inline via the route alias middleware without a redirect");
    }
}
