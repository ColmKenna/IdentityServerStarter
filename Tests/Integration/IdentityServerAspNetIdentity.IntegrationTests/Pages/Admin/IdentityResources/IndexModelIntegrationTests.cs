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
    public async Task Get_ShouldRenderIdentityResourceRows_WhenIdentityResourcesExist()
    {
        await TestDataHelper.SeedIdentityResourceAsync(_factory, "profile", displayName: "User Profile", description: "Your user profile information", enabled: true);
        await TestDataHelper.SeedIdentityResourceAsync(_factory, "email", displayName: "Your email address", description: "Your email address", enabled: false);

        var response = await _client.GetAsync("/Admin/IdentityResources");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var rows = document.QuerySelectorAll("ck-responsive-tbody ck-responsive-row");
        rows.Should().HaveCount(2);

        rows.Should().Contain(r => r.TextContent.Contains("profile"));
        rows.Should().Contain(r => r.TextContent.Contains("email"));
    }

    [Fact]
    public async Task Get_ShouldReturnForbidden_WhenUserIsNotAdmin()
    {
        using var nonAdminFactory = new CustomWebApplicationFactory<NonAdminTestAuthHandler>();
        using var nonAdminClient = nonAdminFactory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await nonAdminClient.GetAsync("/Admin/IdentityResources");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        using var unauthFactory = new CustomWebApplicationFactory<UnauthenticatedTestAuthHandler>();
        using var unauthClient = unauthFactory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await unauthClient.GetAsync("/Admin/IdentityResources");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
