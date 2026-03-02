using System.Net;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages;

public class ApiScopesIndexIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApiScopesIndexIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private void SeedApiScopes(params ApiScope[] apiScopes)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        context.ApiScopes.AddRange(apiScopes);
        context.SaveChanges();
    }

    [Fact]
    public async Task Get_ApiScopesIndex_Returns200()
    {
        var response = await _client.GetAsync("/Admin/ApiScopes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_ApiScopesIndex_ContainsApiScopesHeading()
    {
        var response = await _client.GetAsync("/Admin/ApiScopes");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var heading = document.QuerySelector("h2");
        heading.Should().NotBeNull();
        heading!.TextContent.Should().Contain("API Scopes");
    }

    [Fact]
    public async Task Get_ApiScopesIndex_ContainsCreateApiScopeLink()
    {
        var response = await _client.GetAsync("/Admin/ApiScopes");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var createLink = document.QuerySelector("a[href='/Admin/ApiScopes/0/Edit']");
        createLink.Should().NotBeNull("page should contain a create API scope link");
        createLink!.TextContent.Should().Contain("Create API Scope");
    }

    [Fact]
    public async Task Get_ApiScopesExist_RendersApiScopesTable()
    {
        SeedApiScopes(
            new ApiScope { Name = "orders.read", DisplayName = "Orders Read", Description = "Read orders", Enabled = true });

        var response = await _client.GetAsync("/Admin/ApiScopes");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var table = document.QuerySelector("ck-responsive-table");
        table.Should().NotBeNull("page should contain an API scopes table");
    }

    [Fact]
    public async Task Get_ApiScopesExist_RendersOneRowPerApiScope()
    {
        SeedApiScopes(
            new ApiScope { Name = "orders.read", DisplayName = "Orders Read", Description = "Read orders", Enabled = true },
            new ApiScope { Name = "orders.write", DisplayName = "Orders Write", Description = "Write orders", Enabled = false });

        var response = await _client.GetAsync("/Admin/ApiScopes");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var rows = document.QuerySelectorAll("ck-responsive-table ck-responsive-tbody ck-responsive-row");
        rows.Length.Should().Be(2);
    }

    [Fact]
    public async Task Get_ApiScopesExist_RendersApiScopeNameAndDisplayName()
    {
        SeedApiScopes(
            new ApiScope { Name = "orders.read", DisplayName = "Orders Read", Description = "Read orders", Enabled = true });

        var response = await _client.GetAsync("/Admin/ApiScopes");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var pageText = document.Body!.TextContent;
        pageText.Should().Contain("orders.read");
        pageText.Should().Contain("Orders Read");
    }

    [Fact]
    public async Task Get_ApiScopesExist_RendersEnabledBadge()
    {
        SeedApiScopes(
            new ApiScope { Name = "orders.read", DisplayName = "Orders Read", Description = "Read orders", Enabled = true },
            new ApiScope { Name = "orders.write", DisplayName = "Orders Write", Description = "Write orders", Enabled = false });

        var response = await _client.GetAsync("/Admin/ApiScopes");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var pageText = document.Body!.TextContent;
        pageText.Should().Contain("Enabled");
        pageText.Should().Contain("Disabled");
    }

    [Fact]
    public async Task Get_ApiScopesExist_RendersEditLinkPerRow()
    {
        SeedApiScopes(
            new ApiScope { Name = "orders.read", DisplayName = "Orders Read", Description = "Read orders", Enabled = true });

        var response = await _client.GetAsync("/Admin/ApiScopes");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var editLink = document.QuerySelector("ck-responsive-tbody a[href*='/Admin/ApiScopes/'][href*='/Edit']");
        editLink.Should().NotBeNull("each API scope row should contain an edit link");
        editLink!.TextContent.Should().Contain("Edit");
    }

    [Fact]
    public async Task Get_NoApiScopesExist_ShowsEmptyStateMessage()
    {
        var response = await _client.GetAsync("/Admin/ApiScopes");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var emptyMessage = document.QuerySelector(".alert-info");
        emptyMessage.Should().NotBeNull("page should show an info message when no API scopes exist");
        emptyMessage!.TextContent.Should().Contain("No API scopes found.");
    }

    [Fact]
    public async Task Get_NoApiScopesExist_DoesNotRenderTable()
    {
        var response = await _client.GetAsync("/Admin/ApiScopes");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var table = document.QuerySelector("ck-responsive-table");
        table.Should().BeNull("table should not render when no API scopes exist");
    }

    [Fact]
    public async Task Get_ApiScopeMissingDisplayNameOrDescription_ShowsFallbackText()
    {
        SeedApiScopes(
            new ApiScope { Name = "orders.read", DisplayName = null, Description = null, Enabled = true });

        var response = await _client.GetAsync("/Admin/ApiScopes");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var pageText = document.Body!.TextContent;
        pageText.Should().Contain("No display name");
        pageText.Should().Contain("No description");
    }
}
