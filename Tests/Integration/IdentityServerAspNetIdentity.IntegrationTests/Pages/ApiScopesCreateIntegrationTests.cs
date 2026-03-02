using System.Net;
using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages;

public class ApiScopesCreateIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApiScopesCreateIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Get_CreateApiScope_Returns302RedirectToEditWithZeroId()
    {
        using var noRedirectClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await noRedirectClient.GetAsync("/Admin/ApiScopes/Create");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Be("/Admin/ApiScopes/0/Edit");
    }

    [Fact]
    public async Task Get_CreateApiScope_WithAutoRedirect_RendersEditCreateMode()
    {
        var response = await _client.GetAsync("/Admin/ApiScopes/Create");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.QuerySelector("h2")!.TextContent.Should().Contain("Create API Scope");
        document.QuerySelector("form[method='post']")!.GetAttribute("id").Should().Be("edit-api-scope-form");
    }

    [Fact]
    public async Task PostCreate_AnyPayload_Returns302RedirectToEditWithZeroId()
    {
        using var noRedirectClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await noRedirectClient.PostAsync(
            "/Admin/ApiScopes/Create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.Name"] = "orders.read",
                ["Input.DisplayName"] = "Orders Read",
                ["Input.Description"] = "Read orders",
                ["Input.Enabled"] = "true"
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Be("/Admin/ApiScopes/0/Edit");
    }
}
