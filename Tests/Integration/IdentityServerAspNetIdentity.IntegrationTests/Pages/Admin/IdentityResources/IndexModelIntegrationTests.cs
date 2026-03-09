using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages.Admin.IdentityResources;

public class IndexModelIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public IndexModelIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Get_ShouldShowEmptyState_WhenNoIdentityResourcesExist()
    {
        var response = await _client.GetAsync("/Admin/IdentityResources");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.QuerySelector(".alert-info")!.TextContent.Should().Contain("No identity resources found.");
        document.QuerySelector("ck-responsive-table").Should().BeNull();
    }

    [Fact]
    public async Task Get_ShouldRenderIdentityResourceRowsAndFallbackText_WhenIdentityResourcesExist()
    {
        await SeedIdentityResourceAsync(
            name: "profile",
            displayName: "User Profile",
            description: "Basic profile data",
            enabled: true);
        await SeedIdentityResourceAsync(
            name: "address",
            displayName: null,
            description: null,
            enabled: false);

        var response = await _client.GetAsync("/Admin/IdentityResources");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var rows = document.QuerySelectorAll("ck-responsive-tbody ck-responsive-row");
        rows.Should().HaveCount(2);

        var profileRow = rows.Single(x => x.TextContent.Contains("profile"));
        profileRow.TextContent.Should().Contain("User Profile");
        profileRow.TextContent.Should().Contain("Basic profile data");
        profileRow.TextContent.Should().Contain("Enabled");

        var addressRow = rows.Single(x => x.TextContent.Contains("address"));
        addressRow.TextContent.Should().Contain("No display name");
        addressRow.TextContent.Should().Contain("No description");
        addressRow.TextContent.Should().Contain("Disabled");
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task SeedIdentityResourceAsync(
        string name,
        string? displayName = null,
        string? description = null,
        bool enabled = true)
    {
        using var scope = _factory.Services.CreateScope();
        var configurationDbContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

        configurationDbContext.IdentityResources.Add(new IdentityResource
        {
            Name = name,
            DisplayName = displayName,
            Description = description,
            Enabled = enabled
        });

        await configurationDbContext.SaveChangesAsync();
    }
}
