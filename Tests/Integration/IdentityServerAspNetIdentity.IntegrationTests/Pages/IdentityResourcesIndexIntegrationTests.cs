using System.Net;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages;

public class IdentityResourcesIndexIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public IdentityResourcesIndexIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private void SeedIdentityResources(params IdentityResource[] identityResources)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        context.IdentityResources.AddRange(identityResources);
        context.SaveChanges();
    }

    [Fact]
    public async Task Get_IdentityResourcesIndex_Returns200()
    {
        var response = await _client.GetAsync("/Admin/IdentityResources");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_IdentityResourcesIndex_ContainsIdentityResourcesHeading()
    {
        var response = await _client.GetAsync("/Admin/IdentityResources");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var heading = document.QuerySelector("h2");
        heading.Should().NotBeNull();
        heading!.TextContent.Should().Contain("Identity Resources");
    }

    [Fact]
    public async Task Get_IdentityResourcesExist_RendersIdentityResourcesTable()
    {
        SeedIdentityResources(
            new IdentityResource { Name = "profile", DisplayName = "Profile", Description = "User profile", Enabled = true });

        var response = await _client.GetAsync("/Admin/IdentityResources");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var table = document.QuerySelector("ck-responsive-table");
        table.Should().NotBeNull("page should contain an identity resources table");
    }

    [Fact]
    public async Task Get_IdentityResourcesExist_RendersOneRowPerIdentityResource()
    {
        SeedIdentityResources(
            new IdentityResource { Name = "profile", DisplayName = "Profile", Description = "User profile", Enabled = true },
            new IdentityResource { Name = "email", DisplayName = "Email", Description = "User email", Enabled = false });

        var response = await _client.GetAsync("/Admin/IdentityResources");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var rows = document.QuerySelectorAll("ck-responsive-table ck-responsive-tbody ck-responsive-row");
        rows.Length.Should().Be(2);
    }

    [Fact]
    public async Task Get_IdentityResourcesExist_RendersIdentityResourceNameAndDisplayName()
    {
        SeedIdentityResources(
            new IdentityResource { Name = "profile", DisplayName = "Profile", Description = "User profile", Enabled = true });

        var response = await _client.GetAsync("/Admin/IdentityResources");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var pageText = document.Body!.TextContent;
        pageText.Should().Contain("profile");
        pageText.Should().Contain("Profile");
    }

    [Fact]
    public async Task Get_IdentityResourcesExist_RendersEnabledBadge()
    {
        SeedIdentityResources(
            new IdentityResource { Name = "profile", DisplayName = "Profile", Description = "User profile", Enabled = true },
            new IdentityResource { Name = "email", DisplayName = "Email", Description = "User email", Enabled = false });

        var response = await _client.GetAsync("/Admin/IdentityResources");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var pageText = document.Body!.TextContent;
        pageText.Should().Contain("Enabled");
        pageText.Should().Contain("Disabled");
    }

    [Fact]
    public async Task Get_NoIdentityResourcesExist_ShowsEmptyStateMessage()
    {
        var response = await _client.GetAsync("/Admin/IdentityResources");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var emptyMessage = document.QuerySelector(".alert-info");
        emptyMessage.Should().NotBeNull("page should show an info message when no identity resources exist");
        emptyMessage!.TextContent.Should().Contain("No identity resources found.");
    }

    [Fact]
    public async Task Get_NoIdentityResourcesExist_DoesNotRenderTable()
    {
        var response = await _client.GetAsync("/Admin/IdentityResources");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var table = document.QuerySelector("ck-responsive-table");
        table.Should().BeNull("table should not render when no identity resources exist");
    }

    [Fact]
    public async Task Get_IdentityResourceMissingDisplayNameOrDescription_ShowsFallbackText()
    {
        SeedIdentityResources(
            new IdentityResource { Name = "profile", DisplayName = null, Description = null, Enabled = true });

        var response = await _client.GetAsync("/Admin/IdentityResources");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var pageText = document.Body!.TextContent;
        pageText.Should().Contain("No display name");
        pageText.Should().Contain("No description");
    }
}
